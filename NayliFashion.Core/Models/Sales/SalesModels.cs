using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Catalog;
using NayliFashion.Core.Models.Common;
using NayliFashion.Core.Models.Customers;
using NayliFashion.Core.Models.Finance;
using NayliFashion.Core.Models.Users;

namespace NayliFashion.Core.Models.Sales;

/// <summary>
/// فاتورة مبيعات في نقطة البيع (POS Sale Invoice)
/// </summary>
public class SaleInvoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty; // رقم الفاتورة التسلسلي (INV-2026-00001)
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public InvoiceType InvoiceType { get; set; } = InvoiceType.RetailSale;

    // العميل (اختياري، في حال زبون عابر أو عميل مسجل)
    public int? CustomerId { get; set; }
    public virtual Customer? Customer { get; set; }

    // الكاشير البائع
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    // وردية الصندوق المرتبطة بهذه الفاتورة
    public int? CashShiftId { get; set; }
    public virtual CashShift? CashShift { get; set; }

    // المبالغ المالية بالدينار الجزائري DZD
    public decimal GrossTotalAmount { get; set; } = 0m;
    public decimal DiscountAmount { get; set; } = 0m;
    public decimal TaxVatAmount { get; set; } = 0m;
    public decimal NetTotalAmount { get; set; } = 0m;

    // تفاصيل الدفع والصرف
    public decimal PaidAmount { get; set; } = 0m;
    public decimal ChangeDueAmount { get; set; } = 0m;       // الصرف / الفكة المرجعة للزبون
    public decimal RemainingDebtAmount { get; set; } = 0m;   // المبلغ المتبقي كدين (كريدي)

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashDZD;
    public string? BaridiMobTransactionNumber { get; set; }  // رقم عملية التحويل في بريدي موب
    public string? Notes { get; set; }

    // بنود الفاتورة
    public virtual ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}

/// <summary>
/// بند من بنود فاتورة المبيعات (مع دعم الكميات العشرية وتحليل الأرباح)
/// </summary>
public class SaleItem : BaseEntity
{
    public int SaleInvoiceId { get; set; }
    public virtual SaleInvoice SaleInvoice { get; set; } = null!;

    public int ProductVariantId { get; set; }
    public virtual ProductVariant ProductVariant { get; set; } = null!;

    // طاقة القماش إذا تم القص من بكرة محددة
    public int? FabricRollId { get; set; }
    public virtual FabricRoll? FabricRoll { get; set; }

    // الكمية المباعة (حرجة جداً: decimal لدعم الأقمشة بالمتر والعطور بالمليلتر/الغرام)
    public decimal Quantity { get; set; } = 1.0m;
    public decimal UnitPrice { get; set; } = 0m;
    public decimal UnitCostPrice { get; set; } = 0m;        // تكلفة الشراء لحظة البيع لحساب الربح الصافي
    public decimal DiscountLineAmount { get; set; } = 0m;
    public decimal TotalLineAmount { get; set; } = 0m;

    // الربح الصافي المحقق من هذا البند بدقة
    public decimal NetProfitLineAmount { get; set; } = 0m;
}

/// <summary>
/// جدول مسودات السلال اللحظية (لحفظ السلال المعلقة والتعافي التلقائي عند انقطاع الكهرباء المفاجئ)
/// </summary>
public class ActiveDraftCart : BaseEntity
{
    public string CartIdentifier { get; set; } = "Cart-1";  // معرف السلة (Cart-1, Cart-2, Suspended-1)
    public int UserId { get; set; }                         // الكاشير الحالي
    public int? CustomerId { get; set; }
    public string JsonItemsData { get; set; } = string.Empty;// تمثيل JSON للبنود والكميات الحالية بالسلة
    public decimal TotalEstimateAmount { get; set; } = 0m;
    public DateTime LastAutoSavedAt { get; set; } = DateTime.UtcNow;
}
