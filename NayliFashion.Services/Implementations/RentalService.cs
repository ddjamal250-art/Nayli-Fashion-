using Microsoft.EntityFrameworkCore;
using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Finance;
using NayliFashion.Core.Models.Rentals;
using NayliFashion.Data.Context;
using NayliFashion.Services.DTOs;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Services.Implementations;

/// <summary>
/// تطبيق خدمة نظام الكراء وتتبع الفساتين والبدلات والضمانات والمصبغة وغرامات التأخير
/// </summary>
public class RentalService : IRentalService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public RentalService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<RentalAssetItem>> GetRentalAssetsAsync(int? productVariantId = null, RentalAssetStatus? status = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.RentalAssetItems
                           .Include(a => a.ProductVariant)
                               .ThenInclude(v => v.Product)
                           .AsQueryable();

        if (productVariantId.HasValue)
            query = query.Where(a => a.ProductVariantId == productVariantId.Value);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        return await query.OrderBy(a => a.AssetSerialTag).ToListAsync();
    }

    public async Task<RentalAssetItem?> GetRentalAssetByTagAsync(string serialTag)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.RentalAssetItems
                            .Include(a => a.ProductVariant)
                                .ThenInclude(v => v.Product)
                            .FirstOrDefaultAsync(a => a.AssetSerialTag == serialTag.Trim());
    }

    public async Task<RentalAssetItem> RegisterNewRentalAssetAsync(RentalAssetItem asset)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        if (string.IsNullOrWhiteSpace(asset.AssetSerialTag))
        {
            int count = await context.RentalAssetItems.CountAsync();
            asset.AssetSerialTag = $"TAG-RN-{(count + 1):D4}";
        }

        await context.RentalAssetItems.AddAsync(asset);
        await context.SaveChangesAsync();
        return asset;
    }

    public async Task<bool> UpdateAssetStatusAsync(int assetId, RentalAssetStatus newStatus, ItemConditionGrade condition, string? notes)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var asset = await context.RentalAssetItems.FindAsync(assetId);
        if (asset == null) return false;

        asset.Status = newStatus;
        asset.CurrentCondition = condition;
        if (!string.IsNullOrWhiteSpace(notes))
            asset.LastInspectionNotes = notes;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsAssetAvailableForDatesAsync(int assetId, DateTime startDate, DateTime returnDate, int? excludeOrderId = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // فحص ما إذا كانت القطعة محجوزة في نفس الفترة ضمن عقد غير مكتمل
        var conflictingOrder = await context.RentalItems
            .Include(ri => ri.RentalOrder)
            .Where(ri => ri.RentalAssetItemId == assetId &&
                         !ri.RentalOrder.IsOrderCompleted &&
                         (!excludeOrderId.HasValue || ri.RentalOrderId != excludeOrderId.Value))
            .Where(ri => (startDate >= ri.RentalOrder.EventStartDate && startDate <= ri.RentalOrder.ExpectedReturnDate) ||
                         (returnDate >= ri.RentalOrder.EventStartDate && returnDate <= ri.RentalOrder.ExpectedReturnDate) ||
                         (startDate <= ri.RentalOrder.EventStartDate && returnDate >= ri.RentalOrder.ExpectedReturnDate))
            .AnyAsync();

        return !conflictingOrder;
    }

    public async Task<RentalOrder> CreateRentalOrderAsync(RentalCheckoutRequestDto request)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        int todayCount = await context.RentalOrders.CountAsync(o => o.CreatedAt.Date == DateTime.UtcNow.Date);
        string contractNumber = $"RNT-{DateTime.UtcNow:yyyyMMdd}-{(todayCount + 1):D4}";

        decimal remaining = Math.Max(0, request.TotalRentFeeDzd - request.AdvancePaidDzd);

        var order = new RentalOrder
        {
            ContractNumber = contractNumber,
            OrderDate = DateTime.UtcNow,
            CustomerId = request.CustomerId,
            UserId = request.CashierUserId,
            CashShiftId = request.ActiveCashShiftId,
            EventStartDate = request.EventStartDate,
            ExpectedReturnDate = request.ExpectedReturnDate,
            TotalRentFeeDzd = request.TotalRentFeeDzd,
            AdvancePaidDzd = request.AdvancePaidDzd,
            RemainingRentFeeDzd = remaining,
            GuaranteeType = request.GuaranteeType,
            GuaranteeDocumentNumber = request.GuaranteeDocumentNumber,
            GuaranteeSafeLocation = request.GuaranteeSafeLocation,
            SecurityDepositCashDzd = request.SecurityDepositCashDzd,
            IsGuaranteeReturnedToCustomer = false,
            IsOrderCompleted = false,
            Notes = request.Notes
        };

        await context.RentalOrders.AddAsync(order);
        await context.SaveChangesAsync();

        foreach (var assetId in request.SelectedRentalAssetIds)
        {
            var asset = await context.RentalAssetItems.FindAsync(assetId);
            if (asset != null)
            {
                var item = new RentalItem
                {
                    RentalOrderId = order.Id,
                    RentalAssetItemId = asset.Id,
                    AgreedRentalRateDzd = request.TotalRentFeeDzd / request.SelectedRentalAssetIds.Count,
                    ConditionAtCheckout = asset.CurrentCondition
                };

                await context.RentalItems.AddAsync(item);

                // تغيير حالة القطعة إلى محجوزة أو مؤجرة
                bool isStartingNow = request.EventStartDate.Date <= DateTime.UtcNow.Date;
                asset.Status = isStartingNow ? RentalAssetStatus.RentedOutWithCustomer : RentalAssetStatus.BookedReserved;
                asset.TotalRentalCount++;
            }
        }

        // تسجيل العربون النقدي في وردية الكاشير إذا وجد
        if (request.ActiveCashShiftId.HasValue && request.AdvancePaidDzd > 0)
        {
            var shift = await context.CashShifts.FindAsync(request.ActiveCashShiftId.Value);
            if (shift != null)
            {
                shift.TotalCashSalesDzd += request.AdvancePaidDzd;
            }
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return order;
    }

    public async Task<RentalOrder> ProcessReturnInspectionAsync(RentalReturnInspectionDto request)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        var order = await context.RentalOrders
                                 .Include(o => o.RentalItems)
                                 .FirstOrDefaultAsync(o => o.Id == request.RentalOrderId);

        if (order == null) throw new InvalidOperationException("عقد الكراء غير موجود");

        order.ActualReturnDate = request.ActualReturnDate;

        // 1. حساب غرامات التأخير اليومية إن وجدت
        if (request.ActualReturnDate.Date > order.ExpectedReturnDate.Date)
        {
            int overdueDays = (int)(request.ActualReturnDate.Date - order.ExpectedReturnDate.Date).TotalDays;
            order.CalculatedLateFeesDzd = overdueDays * order.LateFeePerDayDzd;
        }

        order.DamageRepairCostDzd = request.AdditionalDamageCostDzd;
        order.DryCleaningFeeDzd = request.AdditionalDryCleaningFeeDzd;
        order.IsGuaranteeReturnedToCustomer = request.ReturnGuaranteeDocumentToCustomer;
        order.IsOrderCompleted = true;

        // 2. تحديث حالة كل قطعة تم إرجاعها
        foreach (var returnItemDto in request.ReturnedItems)
        {
            var item = order.RentalItems.FirstOrDefault(i => i.Id == returnItemDto.RentalItemId);
            if (item != null)
            {
                item.ConditionAtReturn = returnItemDto.ConditionAtReturn;
                item.ReturnInspectionNotes = returnItemDto.ReturnNotes;

                var asset = await context.RentalAssetItems.FindAsync(item.RentalAssetItemId);
                if (asset != null)
                {
                    asset.CurrentCondition = returnItemDto.ConditionAtReturn;

                    if (returnItemDto.SendDirectlyToDryCleaning)
                    {
                        asset.Status = RentalAssetStatus.InDryCleaningPressing;
                    }
                    else if (returnItemDto.ConditionAtReturn == ItemConditionGrade.DamagedRequiresRepair)
                    {
                        asset.Status = RentalAssetStatus.InMaintenanceRepair;
                    }
                    else
                    {
                        asset.Status = RentalAssetStatus.AvailableForRent;
                    }
                }
            }
        }

        // 3. إضافة المبالغ المستلمة عند الإرجاع للصندوق
        if (request.ActiveCashShiftId.HasValue && request.AmountPaidByCustomerOnReturn > 0)
        {
            var shift = await context.CashShifts.FindAsync(request.ActiveCashShiftId.Value);
            if (shift != null)
            {
                shift.TotalCashSalesDzd += request.AmountPaidByCustomerOnReturn;
            }
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return order;
    }

    public async Task SendToDryCleaningAsync(int assetId, decimal cleaningCostDzd, string? pressingPartnerName = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var asset = await context.RentalAssetItems.FindAsync(assetId);
        if (asset == null) return;

        asset.Status = RentalAssetStatus.InDryCleaningPressing;
        asset.LastInspectionNotes = $"تم الإرسال للمصبغة ({pressingPartnerName ?? "عام"}) بتاريخ {DateTime.UtcNow:yyyy-MM-dd} بتكلفة {cleaningCostDzd:N2} دج";

        await context.SaveChangesAsync();
    }

    public async Task ReceiveFromDryCleaningAsync(int assetId, ItemConditionGrade newCondition = ItemConditionGrade.ExcellentAsNew)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var asset = await context.RentalAssetItems.FindAsync(assetId);
        if (asset == null) return;

        asset.Status = RentalAssetStatus.AvailableForRent;
        asset.CurrentCondition = newCondition;
        asset.LastInspectionNotes = $"تم الاستلام من المصبغة جاهزاً للعرض بتاريخ {DateTime.UtcNow:yyyy-MM-dd}";

        await context.SaveChangesAsync();
    }

    public async Task<List<RentalOrder>> GetActiveRentalsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.RentalOrders
                            .Include(o => o.Customer)
                            .Include(o => o.RentalItems)
                                .ThenInclude(ri => ri.RentalAssetItem)
                            .Where(o => !o.IsOrderCompleted)
                            .OrderBy(o => o.ExpectedReturnDate)
                            .ToListAsync();
    }

    public async Task<List<RentalOrder>> GetOverdueRentalsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        DateTime today = DateTime.UtcNow.Date;
        return await context.RentalOrders
                            .Include(o => o.Customer)
                            .Include(o => o.RentalItems)
                                .ThenInclude(ri => ri.RentalAssetItem)
                            .Where(o => !o.IsOrderCompleted && o.ExpectedReturnDate.Date < today)
                            .OrderBy(o => o.ExpectedReturnDate)
                            .ToListAsync();
    }

    public async Task<List<RentalOrder>> GetBookingsForPeriodAsync(DateTime fromDate, DateTime toDate)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.RentalOrders
                            .Include(o => o.Customer)
                            .Include(o => o.RentalItems)
                                .ThenInclude(ri => ri.RentalAssetItem)
                            .Where(o => (o.EventStartDate >= fromDate && o.EventStartDate <= toDate) ||
                                        (o.ExpectedReturnDate >= fromDate && o.ExpectedReturnDate <= toDate))
                            .OrderBy(o => o.EventStartDate)
                            .ToListAsync();
    }
}
