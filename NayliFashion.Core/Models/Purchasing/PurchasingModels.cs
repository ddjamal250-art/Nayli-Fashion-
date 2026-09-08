using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Catalog;
using NayliFashion.Core.Models.Common;
using NayliFashion.Core.Models.Users;

namespace NayliFashion.Core.Models.Purchasing;

/// <summary>
/// بيانات المورد (تاجر الجملة أو المستورد أو مصنع الصوف)
/// </summary>
public class Supplier : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? AddressCity { get; set; }
    public string? TaxNumberNif { get; set; }
    public string? CommercialRegisterRc { get; set; }

    // الرصيد المالي الحالي للمورد (+ دائن يطالبنا بمستحقات، - مدين)
    public decimal CurrentBalanceDzd { get; set; } = 0m;

    public virtual ICollection<PurchaseInvoice> PurchaseInvoices { get; set; } = new List<PurchaseInvoice>();
    public virtual ICollection<SupplierTransaction> Transactions { get; set; } = new List<SupplierTransaction>();
}

/// <summary>
/// فاتورة شراء وتوريد بضاعة للمخزن
/// </summary>
public class PurchaseInvoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty; // رقم فاتورة المورد (PI-2026-0001)
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

    public int SupplierId { get; set; }
    public virtual Supplier Supplier { get; set; } = null!;

    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    // المبالغ بالدينار الجزائري DZD
    public decimal TotalGrossAmount { get; set; } = 0m;
    public decimal DiscountAmount { get; set; } = 0m;
    public decimal NetTotalAmount { get; set; } = 0m;
    public decimal PaidAmount { get; set; } = 0m;
    public decimal RemainingDebtAmount { get; set; } = 0m;

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashDZD;
    public string? Notes { get; set; }

    public virtual ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
}

/// <summary>
/// بنود وتفاصيل فاتورة الشراء
/// </summary>
public class PurchaseItem : BaseEntity
{
    public int PurchaseInvoiceId { get; set; }
    public virtual PurchaseInvoice PurchaseInvoice { get; set; } = null!;

    public int ProductVariantId { get; set; }
    public virtual ProductVariant ProductVariant { get; set; } = null!;

    public decimal QuantityPurchased { get; set; }        // الكمية المشتراة (تدعم الكسور العشرية)
    public decimal UnitPurchasePrice { get; set; }        // سعر شراء الوحدة في هذه الفاتورة
    public decimal TotalLineAmount { get; set; }          // الإجمالي
    public decimal PreviousCostPrice { get; set; }        // سعر التكلفة قبل هذا الشراء
    public decimal NewCalculatedCostPrice { get; set; }    // متوسط التكلفة المرجح الجديد (WAC)
}

/// <summary>
/// كشف حساب وسجل حركات المورد (فواتير، سندات دفع وسداد)
/// </summary>
public class SupplierTransaction : BaseEntity
{
    public int SupplierId { get; set; }
    public virtual Supplier Supplier { get; set; } = null!;

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string TransactionType { get; set; } = string.Empty; // "فاتورة شراء", "سند دفع للمورد"
    public string? ReferenceNumber { get; set; }
    public decimal DebitAmount { get; set; } = 0m;         // مدين (ما دفعناه له)
    public decimal CreditAmount { get; set; } = 0m;        // دائن (قيمة الفاتورة عليه)
    public decimal BalanceAfter { get; set; } = 0m;        // الرصيد بعد الحركة
    public string? Notes { get; set; }
}
