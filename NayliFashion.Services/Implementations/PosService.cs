using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Customers;
using NayliFashion.Core.Models.Sales;
using NayliFashion.Data.Context;
using NayliFashion.Services.DTOs;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Services.Implementations;

/// <summary>
/// تطبيق خدمة نقطة البيع POS، الكاشير السريع، الفواتير، وحفظ السلال المعلقة
/// </summary>
public class PosService : IPosService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IInventoryService _inventoryService;

    public PosService(IDbContextFactory<AppDbContext> contextFactory, IInventoryService inventoryService)
    {
        _contextFactory = contextFactory;
        _inventoryService = inventoryService;
    }

    public CartSummaryDto CalculateSummary(List<CartItemDto> items, decimal overallDiscount = 0m, decimal vatRatePercentage = 0m)
    {
        decimal subTotal = items.Sum(i => i.LineTotal);
        decimal totalDiscount = overallDiscount + items.Sum(i => i.DiscountAmount);
        decimal afterDiscount = Math.Max(0m, subTotal - overallDiscount);

        decimal vatAmount = (afterDiscount * vatRatePercentage) / 100m;
        decimal netTotal = afterDiscount + vatAmount;

        return new CartSummaryDto
        {
            SubTotalGross = subTotal,
            TotalDiscount = totalDiscount,
            TaxVatAmount = vatAmount,
            NetTotalDzd = netTotal
        };
    }

    public async Task<CheckoutResultDto> ProcessCheckoutAsync(CheckoutRequestDto request)
    {
        if (request.Items == null || !request.Items.Any())
        {
            return new CheckoutResultDto { IsSuccess = false, Message = "سلة المشتريات فارغة!" };
        }

        using var context = await _contextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var summary = CalculateSummary(request.Items, request.OverallInvoiceDiscount);
            decimal netTotal = summary.NetTotalDzd;
            decimal paidAmount = request.PaidAmount;
            decimal changeDue = 0m;
            decimal remainingDebt = 0m;

            if (paidAmount >= netTotal)
            {
                changeDue = paidAmount - netTotal;
                remainingDebt = 0m;
            }
            else
            {
                remainingDebt = netTotal - paidAmount;
                changeDue = 0m;
            }

            // في حال وجود دين (كريدي)
            if (remainingDebt > 0)
            {
                if (!request.CustomerId.HasValue)
                {
                    return new CheckoutResultDto { IsSuccess = false, Message = "لا يمكن البيع بالدين دون تحديد العميل!" };
                }

                var customer = await context.Customers.FindAsync(request.CustomerId.Value);
                if (customer == null)
                {
                    return new CheckoutResultDto { IsSuccess = false, Message = "العميل المحدد غير موجود!" };
                }

                // فحص سقف الائتمان للعميل
                if (customer.CurrentDebtDzd + remainingDebt > customer.MaxCreditLimitDzd)
                {
                    return new CheckoutResultDto
                    {
                        IsSuccess = false,
                        Message = $"تم تجاوز سقف الائتمان المسموح به للعميل ({customer.MaxCreditLimitDzd:N2} دج). الدين الحالي: {customer.CurrentDebtDzd:N2} دج."
                    };
                }

                // زيادة رصيد دين العميل وتسجيل الحركة بالدفتر
                customer.CurrentDebtDzd += remainingDebt;

                var customerTx = new CustomerTransaction
                {
                    CustomerId = customer.Id,
                    TransactionDate = DateTime.UtcNow,
                    TransactionType = "فاتورة بيع آجل (كريدي)",
                    DebitAmount = remainingDebt,
                    CreditAmount = 0m,
                    BalanceAfter = customer.CurrentDebtDzd,
                    Notes = $"دين متبقي من الفاتورة"
                };

                await context.CustomerTransactions.AddAsync(customerTx);
            }

            // إنشاء رقم الفاتورة التسلسلي
            int todayCount = await context.SaleInvoices.CountAsync(i => i.CreatedAt.Date == DateTime.UtcNow.Date);
            string invoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{(todayCount + 1):D4}";

            var invoice = new SaleInvoice
            {
                InvoiceNumber = invoiceNumber,
                InvoiceDate = DateTime.UtcNow,
                InvoiceType = request.InvoiceType,
                CustomerId = request.CustomerId,
                UserId = request.CashierUserId,
                CashShiftId = request.ActiveCashShiftId,
                GrossTotalAmount = summary.SubTotalGross,
                DiscountAmount = summary.TotalDiscount,
                TaxVatAmount = summary.TaxVatAmount,
                NetTotalAmount = netTotal,
                PaidAmount = paidAmount,
                ChangeDueAmount = changeDue,
                RemainingDebtAmount = remainingDebt,
                PaymentMethod = request.PaymentMethod,
                BaridiMobTransactionNumber = request.BaridiMobTransactionNumber,
                Notes = request.Notes
            };

            await context.SaleInvoices.AddAsync(invoice);
            await context.SaveChangesAsync();

            // حفظ بنود الفاتورة وخصم المخزون
            foreach (var item in request.Items)
            {
                var saleItem = new SaleItem
                {
                    SaleInvoiceId = invoice.Id,
                    ProductVariantId = item.ProductVariantId,
                    FabricRollId = item.SelectedFabricRollId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    UnitCostPrice = item.UnitCostPrice,
                    DiscountLineAmount = item.DiscountAmount,
                    TotalLineAmount = item.LineTotal,
                    NetProfitLineAmount = item.LineNetProfit
                };

                await context.SaleItems.AddAsync(saleItem);

                // استدعاء خصم المخزون (بما يشمل أمتار البكرات وعطور التعبئة BOM)
                await _inventoryService.DeductStockForSaleItemAsync(item.ProductVariantId, item.Quantity, item.SelectedFabricRollId, context);
            }

            // تحديث حركة وردية الصندوق إذا كان الدفع نقداً أو ببريدي موب
            if (request.ActiveCashShiftId.HasValue && paidAmount > 0)
            {
                var shift = await context.CashShifts.FindAsync(request.ActiveCashShiftId.Value);
                if (shift != null && shift.Status == CashShiftStatus.OpenActive)
                {
                    decimal effectiveReceived = Math.Min(paidAmount, netTotal); // المبلغ الفعلي المتبقي بالدرج بعد إرجاع الصرف

                    if (request.PaymentMethod == PaymentMethod.CashDZD || request.PaymentMethod == PaymentMethod.CustomerDebtCarnet)
                    {
                        shift.TotalCashSalesDzd += effectiveReceived;
                    }
                    else if (request.PaymentMethod == PaymentMethod.BaridiMobRIP)
                    {
                        shift.TotalBaridiMobSalesDzd += effectiveReceived;
                    }
                    else if (request.PaymentMethod == PaymentMethod.BankCardTPE)
                    {
                        shift.TotalCardSalesDzd += effectiveReceived;
                    }
                }
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return new CheckoutResultDto
            {
                IsSuccess = true,
                Message = "تمت عملية البيع وحفظ الفاتورة بنجاح",
                InvoiceId = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                TotalAmountDzd = netTotal,
                PaidAmountDzd = paidAmount,
                ChangeDueDzd = changeDue,
                RemainingDebtDzd = remainingDebt
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return new CheckoutResultDto
            {
                IsSuccess = false,
                Message = $"حدث خطأ أثناء تنفيذ الفاتورة: {ex.Message}"
            };
        }
    }

    public async Task SaveActiveDraftCartAsync(int userId, string cartIdentifier, List<CartItemDto> items, int? customerId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var draft = await context.ActiveDraftCarts.FirstOrDefaultAsync(d => d.UserId == userId && d.CartIdentifier == cartIdentifier);

        string json = JsonSerializer.Serialize(items);
        decimal estimateTotal = items.Sum(i => i.LineTotal);

        if (draft == null)
        {
            draft = new ActiveDraftCart
            {
                UserId = userId,
                CartIdentifier = cartIdentifier,
                CustomerId = customerId,
                JsonItemsData = json,
                TotalEstimateAmount = estimateTotal,
                LastAutoSavedAt = DateTime.UtcNow
            };
            await context.ActiveDraftCarts.AddAsync(draft);
        }
        else
        {
            draft.CustomerId = customerId;
            draft.JsonItemsData = json;
            draft.TotalEstimateAmount = estimateTotal;
            draft.LastAutoSavedAt = DateTime.UtcNow;
            draft.IsDeleted = false;
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<CartItemDto>?> LoadActiveDraftCartAsync(int userId, string cartIdentifier)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var draft = await context.ActiveDraftCarts.FirstOrDefaultAsync(d => d.UserId == userId && d.CartIdentifier == cartIdentifier && !d.IsDeleted);
        if (draft == null || string.IsNullOrWhiteSpace(draft.JsonItemsData))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<CartItemDto>>(draft.JsonItemsData);
        }
        catch
        {
            return null;
        }
    }

    public async Task ClearActiveDraftCartAsync(int userId, string cartIdentifier)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var draft = await context.ActiveDraftCarts.FirstOrDefaultAsync(d => d.UserId == userId && d.CartIdentifier == cartIdentifier);
        if (draft != null)
        {
            draft.IsDeleted = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task<SaleInvoice> ProcessReturnAsync(int originalInvoiceId, List<CartItemDto> returnedItems, int cashierUserId, int? activeShiftId, string? reason)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        var originalInvoice = await context.SaleInvoices.FindAsync(originalInvoiceId);
        if (originalInvoice == null) throw new InvalidOperationException("الفاتورة الأصلية غير موجودة");

        decimal totalRefund = returnedItems.Sum(i => i.LineTotal);

        int todayCount = await context.SaleInvoices.CountAsync(i => i.CreatedAt.Date == DateTime.UtcNow.Date);
        string returnNumber = $"RET-{DateTime.UtcNow:yyyyMMdd}-{(todayCount + 1):D4}";

        var returnInvoice = new SaleInvoice
        {
            InvoiceNumber = returnNumber,
            InvoiceDate = DateTime.UtcNow,
            InvoiceType = InvoiceType.CustomerReturn,
            CustomerId = originalInvoice.CustomerId,
            UserId = cashierUserId,
            CashShiftId = activeShiftId,
            GrossTotalAmount = totalRefund,
            DiscountAmount = 0m,
            TaxVatAmount = 0m,
            NetTotalAmount = totalRefund,
            PaidAmount = totalRefund, // المبلغ المسترجع نقداً للزبون
            PaymentMethod = PaymentMethod.CashDZD,
            Notes = $"مرتجع للفاتورة {originalInvoice.InvoiceNumber}: {reason}"
        };

        await context.SaleInvoices.AddAsync(returnInvoice);
        await context.SaveChangesAsync();

        foreach (var item in returnedItems)
        {
            var saleItem = new SaleItem
            {
                SaleInvoiceId = returnInvoice.Id,
                ProductVariantId = item.ProductVariantId,
                FabricRollId = item.SelectedFabricRollId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                UnitCostPrice = item.UnitCostPrice,
                TotalLineAmount = item.LineTotal,
                NetProfitLineAmount = -item.LineNetProfit
            };

            await context.SaleItems.AddAsync(saleItem);

            // استرجاع البضاعة للمخزن
            await _inventoryService.RestoreStockForReturnedItemAsync(item.ProductVariantId, item.Quantity, item.SelectedFabricRollId, context);
        }

        // خصم المبلغ المسترجع من نقدية وردية الكاشير
        if (activeShiftId.HasValue)
        {
            var shift = await context.CashShifts.FindAsync(activeShiftId.Value);
            if (shift != null)
            {
                shift.TotalCashSalesDzd -= totalRefund;
            }
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return returnInvoice;
    }

    public async Task<SaleInvoice?> GetInvoiceByIdAsync(int invoiceId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.SaleInvoices
                            .Include(i => i.Customer)
                            .Include(i => i.User)
                            .Include(i => i.Items)
                                .ThenInclude(item => item.ProductVariant)
                                    .ThenInclude(v => v.Product)
                            .FirstOrDefaultAsync(i => i.Id == invoiceId);
    }

    public async Task<List<SaleInvoice>> GetInvoicesListAsync(DateTime? fromDate = null, DateTime? toDate = null, int? customerId = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.SaleInvoices
                           .Include(i => i.Customer)
                           .Include(i => i.User)
                           .Include(i => i.Items)
                           .AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(i => i.InvoiceDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(i => i.InvoiceDate <= toDate.Value);

        if (customerId.HasValue)
            query = query.Where(i => i.CustomerId == customerId.Value);

        return await query.OrderByDescending(i => i.InvoiceDate).ToListAsync();
    }
}
