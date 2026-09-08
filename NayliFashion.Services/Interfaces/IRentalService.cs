using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Rentals;
using NayliFashion.Services.DTOs;

namespace NayliFashion.Services.Interfaces;

/// <summary>
/// واجهة خدمة نظام الكراء، تتبع الأصول، الضمانات، المصبغة، وغرامات التأخير
/// </summary>
public interface IRentalService
{
    // إدارة الأصول المؤجرة المادية
    Task<List<RentalAssetItem>> GetRentalAssetsAsync(int? productVariantId = null, RentalAssetStatus? status = null);
    Task<RentalAssetItem?> GetRentalAssetByTagAsync(string serialTag);
    Task<RentalAssetItem> RegisterNewRentalAssetAsync(RentalAssetItem asset);
    Task<bool> UpdateAssetStatusAsync(int assetId, RentalAssetStatus newStatus, ItemConditionGrade condition, string? notes);

    // فحص توفر القطعة وعدم تضارب المواعيد مع مناسبات أخرى
    Task<bool> IsAssetAvailableForDatesAsync(int assetId, DateTime startDate, DateTime returnDate, int? excludeOrderId = null);

    // إنشاء عقد كراء جديد
    Task<RentalOrder> CreateRentalOrderAsync(RentalCheckoutRequestDto request);

    // معاينة استرجاع القطع المؤجرة والتسوية المالية والضمان
    Task<RentalOrder> ProcessReturnInspectionAsync(RentalReturnInspectionDto request);

    // إدارة المصبغة والتنظيف الجاف (Pressing)
    Task SendToDryCleaningAsync(int assetId, decimal cleaningCostDzd, string? pressingPartnerName = null);
    Task ReceiveFromDryCleaningAsync(int assetId, ItemConditionGrade newCondition = ItemConditionGrade.ExcellentAsNew);

    // تقارير وتنبيهات الكراء
    Task<List<RentalOrder>> GetActiveRentalsAsync();
    Task<List<RentalOrder>> GetOverdueRentalsAsync();
    Task<List<RentalOrder>> GetBookingsForPeriodAsync(DateTime fromDate, DateTime toDate);
}
