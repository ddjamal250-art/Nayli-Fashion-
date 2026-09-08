using Microsoft.EntityFrameworkCore;
using NayliFashion.Core.Models.Purchasing;
using NayliFashion.Data.Context;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Services.Implementations;

/// <summary>
/// تطبيق خدمة المشتريات والموردين وحساب متوسط التكلفة المرجح (WAC) آلياً
/// </summary>
public class PurchasingService : IPurchasingService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public PurchasingService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Supplier>> GetAllSuppliersAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Suppliers.OrderBy(s => s.CompanyName).ToListAsync();
    }

    public async Task<Supplier?> GetSupplierByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Suppliers
                            .Include(s => s.PurchaseInvoices)
                            .Include(s => s.Transactions)
                            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Supplier> CreateSupplierAsync(Supplier supplier)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        await context.Suppliers.AddAsync(supplier);
        await context.SaveChangesAsync();
        return supplier;
    }

    public async Task<Supplier> UpdateSupplierAsync(Supplier supplier)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Suppliers.Update(supplier);
        await context.SaveChangesAsync();
        return supplier;
    }

    public async Task<PurchaseInvoice> CreatePurchaseInvoiceAsync(PurchaseInvoice invoice)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
        {
            int todayCount = await context.PurchaseInvoices.CountAsync(p => p.CreatedAt.Date == DateTime.UtcNow.Date);
            invoice.InvoiceNumber = $"PI-{DateTime.UtcNow:yyyyMMdd}-{(todayCount + 1):D4}";
        }

        invoice.InvoiceDate = DateTime.UtcNow;
        invoice.RemainingDebtAmount = Math.Max(0m, invoice.NetTotalAmount - invoice.PaidAmount);

        // معالجة كل بند: إضافة المخزون وحساب متوسط التكلفة المرجح (WAC)
        foreach (var item in invoice.Items)
        {
            var variant = await context.ProductVariants.FindAsync(item.ProductVariantId);
            if (variant != null)
            {
                item.PreviousCostPrice = variant.PurchaseCostPrice;

                decimal existingQty = Math.Max(0m, variant.StockQuantity);
                decimal newQty = item.QuantityPurchased;
                decimal currentCost = variant.PurchaseCostPrice;
                decimal unitPrice = item.UnitPurchasePrice;

                // معادلة متوسط التكلفة المرجح WAC:
                decimal totalQty = existingQty + newQty;
                decimal calculatedWac = totalQty > 0
                    ? ((existingQty * currentCost) + (newQty * unitPrice)) / totalQty
                    : unitPrice;

                item.NewCalculatedCostPrice = Math.Round(calculatedWac, 2);

                // تحديث المتغير بالمخزن
                variant.PurchaseCostPrice = item.NewCalculatedCostPrice;
                variant.StockQuantity += newQty;
            }
        }

        await context.PurchaseInvoices.AddAsync(invoice);

        // تحديث رصيد المورد بالدين المتبقي
        var supplier = await context.Suppliers.FindAsync(invoice.SupplierId);
        if (supplier != null)
        {
            supplier.CurrentBalanceDzd += invoice.RemainingDebtAmount;

            var supplierTx = new SupplierTransaction
            {
                SupplierId = supplier.Id,
                TransactionDate = DateTime.UtcNow,
                TransactionType = "فاتورة شراء بضاعة",
                ReferenceNumber = invoice.InvoiceNumber,
                DebitAmount = invoice.PaidAmount,
                CreditAmount = invoice.NetTotalAmount,
                BalanceAfter = supplier.CurrentBalanceDzd,
                Notes = $"فاتورة توريد بضاعة {invoice.InvoiceNumber}"
            };

            await context.SupplierTransactions.AddAsync(supplierTx);
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return invoice;
    }

    public async Task<SupplierTransaction> RecordSupplierPaymentAsync(
        int supplierId,
        decimal amountDzd,
        int userId,
        int? activeShiftId,
        string? notes = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        var supplier = await context.Suppliers.FindAsync(supplierId);
        if (supplier == null) throw new InvalidOperationException("المورد غير موجود");

        supplier.CurrentBalanceDzd = Math.Max(0m, supplier.CurrentBalanceDzd - amountDzd);

        var supplierTx = new SupplierTransaction
        {
            SupplierId = supplier.Id,
            TransactionDate = DateTime.UtcNow,
            TransactionType = "سند صرف وسداد للمورد",
            ReferenceNumber = $"PAY-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100, 999)}",
            DebitAmount = amountDzd,
            CreditAmount = 0m,
            BalanceAfter = supplier.CurrentBalanceDzd,
            Notes = notes ?? "سداد دفعة نقدية لحساب المورد"
        };

        await context.SupplierTransactions.AddAsync(supplierTx);

        // تسجيل خروج النقد من وردية الصندوق إذا تم الدفع نقداً من الدرج
        if (activeShiftId.HasValue)
        {
            var shift = await context.CashShifts.FindAsync(activeShiftId.Value);
            if (shift != null)
            {
                shift.TotalSupplierPayoutsDzd += amountDzd;
            }
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return supplierTx;
    }

    public async Task<List<SupplierTransaction>> GetSupplierStatementAsync(int supplierId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.SupplierTransactions
                            .Where(t => t.SupplierId == supplierId)
                            .OrderByDescending(t => t.TransactionDate)
                            .ToListAsync();
    }
}
