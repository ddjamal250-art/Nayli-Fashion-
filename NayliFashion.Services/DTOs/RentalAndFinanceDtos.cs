using NayliFashion.Core.Enums;

namespace NayliFashion.Services.DTOs;

/// <summary>
/// بيانات إنشاء عقد كراء جديد
/// </summary>
public class RentalCheckoutRequestDto
{
    public int CustomerId { get; set; }
    public int CashierUserId { get; set; }
    public int? ActiveCashShiftId { get; set; }

    public List<int> SelectedRentalAssetIds { get; set; } = new List<int>();

    public DateTime EventStartDate { get; set; }
    public DateTime ExpectedReturnDate { get; set; }

    public decimal TotalRentFeeDzd { get; set; }
    public decimal AdvancePaidDzd { get; set; }

    // بيانات وثيقة الضمان
    public GuaranteeDocumentType GuaranteeType { get; set; } = GuaranteeDocumentType.BiometricNationalIdCardCNI;
    public string? GuaranteeDocumentNumber { get; set; }
    public string? GuaranteeSafeLocation { get; set; }
    public decimal SecurityDepositCashDzd { get; set; } = 0m;

    public string? Notes { get; set; }
}

/// <summary>
/// بيانات معاينة إرجاع القطع المؤجرة والتسوية المالية
/// </summary>
public class RentalReturnInspectionDto
{
    public int RentalOrderId { get; set; }
    public int CashierUserId { get; set; }
    public int? ActiveCashShiftId { get; set; }

    public DateTime ActualReturnDate { get; set; } = DateTime.UtcNow;

    // حالة كل قطعة تم إرجاعها
    public List<RentalItemReturnStateDto> ReturnedItems { get; set; } = new List<RentalItemReturnStateDto>();

    public decimal AdditionalDamageCostDzd { get; set; } = 0m;
    public decimal AdditionalDryCleaningFeeDzd { get; set; } = 0m;
    public bool ReturnGuaranteeDocumentToCustomer { get; set; } = true;
    public decimal AmountPaidByCustomerOnReturn { get; set; } = 0m;

    public string? InspectionNotes { get; set; }
}

public class RentalItemReturnStateDto
{
    public int RentalItemId { get; set; }
    public ItemConditionGrade ConditionAtReturn { get; set; } = ItemConditionGrade.GoodMinorWear;
    public bool SendDirectlyToDryCleaning { get; set; } = true; // إرسال تلقائي للمصبغة
    public string? ReturnNotes { get; set; }
}

/// <summary>
/// تقرير Z اليومي لإغلاق الوردية والمطابقة المحاسبية
/// </summary>
public class ZReportDto
{
    public string ShiftNumber { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;
    public DateTime OpenedAt { get; set; }
    public DateTime ClosedAt { get; set; }

    public decimal OpeningFloatDzd { get; set; }
    public decimal TotalCashSalesDzd { get; set; }
    public decimal TotalBaridiMobSalesDzd { get; set; }
    public decimal TotalCardSalesDzd { get; set; }
    public decimal TotalDebtCollectionsDzd { get; set; }
    public decimal TotalExpensesDzd { get; set; }
    public decimal TotalSupplierPayoutsDzd { get; set; }

    public decimal ExpectedCashInDrawerDzd { get; set; }
    public decimal ActualCountedCashDzd { get; set; }
    public decimal VarianceDifferenceDzd { get; set; } // + زيادة، - عجز

    public int InvoicesCount { get; set; }
    public int RentalOrdersCount { get; set; }
}
