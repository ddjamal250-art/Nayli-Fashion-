using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Catalog;
using NayliFashion.Core.Models.Common;
using NayliFashion.Core.Models.Customers;
using NayliFashion.Core.Models.Finance;
using NayliFashion.Core.Models.Users;

namespace NayliFashion.Core.Models.Rentals;

/// <summary>
/// الأصل المادي المؤجر المرقم تسلسلياً (مثل: فستان نايلي محدد، بدلة عريس محددة، أو طقم أفرشة محدد)
/// </summary>
public class RentalAssetItem : BaseEntity
{
    public int ProductVariantId { get; set; }
    public virtual ProductVariant ProductVariant { get; set; } = null!;

    public string AssetSerialTag { get; set; } = string.Empty; // باركود مميز خاص بهذه القطعة المادية (TAG-RN-001)
    public string AssetNameDescription { get; set; } = string.Empty; // مثال: "فستان نايلي أبيض حرير - القطعة 2"

    public RentalAssetStatus Status { get; set; } = RentalAssetStatus.AvailableForRent;
    public ItemConditionGrade CurrentCondition { get; set; } = ItemConditionGrade.ExcellentAsNew;

    public int TotalRentalCount { get; set; } = 0;         // عدد مرات كراء هذه القطعة لتتبع استهلاكها
    public decimal PurchaseCost { get; set; } = 0m;        // تكلفة اقتناء هذا الأصل
    public string? StorageLocker { get; set; }             // رقم الخزانة أو المعلاق بالصالة
    public string? LastInspectionNotes { get; set; }

    public virtual ICollection<RentalItem> RentalHistory { get; set; } = new List<RentalItem>();
}

/// <summary>
/// عقد / طلب كراء (Rental Contract Order)
/// </summary>
public class RentalOrder : BaseEntity
{
    public string ContractNumber { get; set; } = string.Empty; // رقم العقد (RNT-2026-00001)
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public int CustomerId { get; set; }
    public virtual Customer Customer { get; set; } = null!;

    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public int? CashShiftId { get; set; }
    public virtual CashShift? CashShift { get; set; }

    // مواعيد الكراء والمناسبة
    public DateTime EventStartDate { get; set; }          // موعد استلام الفستان أو بدء المناسبة
    public DateTime ExpectedReturnDate { get; set; }       // تاريخ الإرجاع المتفق عليه بالعقد
    public DateTime? ActualReturnDate { get; set; }        // تاريخ الإرجاع الفعلي للقطعة

    // رسوم الكراء والمدفوعات
    public decimal TotalRentFeeDzd { get; set; } = 0m;     // إجمالي تكلفة الكراء
    public decimal AdvancePaidDzd { get; set; } = 0m;      // العربون المدفوع مسبقاً
    public decimal RemainingRentFeeDzd { get; set; } = 0m; // المتبقي من قيمة الكراء

    // تفاصيل وثيقة الضمان (العرف الجزائري)
    public GuaranteeDocumentType GuaranteeType { get; set; } = GuaranteeDocumentType.BiometricNationalIdCardCNI;
    public string? GuaranteeDocumentNumber { get; set; }   // رقم بطاقة التعريف أو رخصة السياقة
    public string? GuaranteeSafeLocation { get; set; }     // مكان حفظ الوثيقة (مثال: الخزنة رقم 1 / ملف الأعراس)
    public decimal SecurityDepositCashDzd { get; set; } = 0m; // مبلغ الضمان المالي النقدي (في حال وجوده)
    public bool IsGuaranteeReturnedToCustomer { get; set; } = false; // هل تم استرداد الوثيقة للزبون؟

    // غرامات التأخير وتكاليف التلفيات أو التنظيف
    public decimal LateFeePerDayDzd { get; set; } = 1000m; // غرامة التأخير اليومية
    public decimal CalculatedLateFeesDzd { get; set; } = 0m;// إجمالي غرامات التأخير المستحقة
    public decimal DamageRepairCostDzd { get; set; } = 0m; // تكلفة إصلاح أي تلف أو تمزق
    public decimal DryCleaningFeeDzd { get; set; } = 0m;   // مصاريف المصبغة إن طُبقت على العميل

    public bool IsOrderCompleted { get; set; } = false;    // هل تمت التسوية النهائية واستلام القطعة؟
    public string? Notes { get; set; }

    public virtual ICollection<RentalItem> RentalItems { get; set; } = new List<RentalItem>();
}

/// <summary>
/// تفاصيل القطع المؤجرة في العقد
/// </summary>
public class RentalItem : BaseEntity
{
    public int RentalOrderId { get; set; }
    public virtual RentalOrder RentalOrder { get; set; } = null!;

    public int RentalAssetItemId { get; set; }
    public virtual RentalAssetItem RentalAssetItem { get; set; } = null!;

    public decimal AgreedRentalRateDzd { get; set; }       // سعر كراء هذه القطعة بالعقد
    public ItemConditionGrade ConditionAtCheckout { get; set; } = ItemConditionGrade.ExcellentAsNew;
    public ItemConditionGrade? ConditionAtReturn { get; set; }
    public string? CheckoutNotes { get; set; }
    public string? ReturnInspectionNotes { get; set; }
}
