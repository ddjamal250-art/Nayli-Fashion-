using NayliFashion.Core.Models.Common;
using NayliFashion.Core.Models.Rentals;
using NayliFashion.Core.Models.Sales;

namespace NayliFashion.Core.Models.Customers;

/// <summary>
/// ملف العميل ودفتر الديون (مكيف لواقع السوق الجزائري والجلفاوي)
/// </summary>
public class Customer : BaseEntity
{
    public string FullName { get; set; } = string.Empty;   // الاسم الشخصي
    public string FamilyName { get; set; } = string.Empty; // اللقب أو العرش
    public string? Nickname { get; set; }                  // اسم الشهرة / المعروف به بالمنطقة
    public string PhoneNumber { get; set; } = string.Empty;
    public string? AddressNeighborhood { get; set; }      // الحي أو البلدية (مثال: حي برنوس، الجلفة)
    public string? NationalIdCardNumber { get; set; }      // رقم بطاقة التعريف الوطنية البيومترية

    // دفتر الديون وسقف الائتمان
    public decimal MaxCreditLimitDzd { get; set; } = 50000m;// سقف الدين الأقصى المسموح به
    public decimal CurrentDebtDzd { get; set; } = 0m;      // الرصيد الحالي المدين به
    public int? SalaryVirementDay { get; set; }            // يوم الفيرمون/نزول الراتب (بين 1 و 31) لتوليد التنبيهات

    public string? Notes { get; set; }

    public virtual ICollection<SaleInvoice> SaleInvoices { get; set; } = new List<SaleInvoice>();
    public virtual ICollection<RentalOrder> RentalOrders { get; set; } = new List<RentalOrder>();
    public virtual ICollection<CustomerTransaction> Transactions { get; set; } = new List<CustomerTransaction>();
}

/// <summary>
/// دفتر حركة ديون العميل وسندات القبض (Carnet de Crédit)
/// </summary>
public class CustomerTransaction : BaseEntity
{
    public int CustomerId { get; set; }
    public virtual Customer Customer { get; set; } = null!;

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string TransactionType { get; set; } = string.Empty; // "فاتورة مبيعات آجلة", "سند قبض نقدي", "عقد كراء"
    public string? ReferenceNumber { get; set; }
    public decimal DebitAmount { get; set; } = 0m;         // مدين (أخذ بضاعة دين)
    public decimal CreditAmount { get; set; } = 0m;        // دائن (سدد دفعة نقدية)
    public decimal BalanceAfter { get; set; } = 0m;        // الرصيد المتبقي بذمته بعد العملية
    public string? Notes { get; set; }
}
