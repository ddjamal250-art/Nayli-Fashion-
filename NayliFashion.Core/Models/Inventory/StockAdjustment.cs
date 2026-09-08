using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Catalog;
using NayliFashion.Core.Models.Common;
using NayliFashion.Core.Models.Users;

namespace NayliFashion.Core.Models.Inventory;

/// <summary>
/// سند تسوية مخزنية (للجرد الدوري، معالجة العجز والزيادة، هالك الأقمشة، وتبخر العطور)
/// </summary>
public class StockAdjustment : BaseEntity
{
    public string AdjustmentNumber { get; set; } = string.Empty; // رقم السند (ADJ-2026-0001)
    public DateTime AdjustmentDate { get; set; } = DateTime.UtcNow;
    public StockAdjustmentReason Reason { get; set; } = StockAdjustmentReason.PhysicalStocktakeVariance;
    public string? Notes { get; set; }

    // المستخدم المسؤول عن الجرد
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public virtual ICollection<StockAdjustmentItem> Items { get; set; } = new List<StockAdjustmentItem>();
}

/// <summary>
/// تفاصيل سند التسوية المخزنية لكل متغير
/// </summary>
public class StockAdjustmentItem : BaseEntity
{
    public int StockAdjustmentId { get; set; }
    public virtual StockAdjustment StockAdjustment { get; set; } = null!;

    public int ProductVariantId { get; set; }
    public virtual ProductVariant ProductVariant { get; set; } = null!;

    public decimal SystemQuantityBefore { get; set; }     // الرصيد الدفتري المسجل في النظام
    public decimal ActualCountedQuantity { get; set; }    // الرصيد الفعلي بعد العد والجرد
    public decimal DifferenceQuantity { get; set; }       // الفارق (+ زيادة، - عجز)
    public decimal UnitCost { get; set; }                 // تكلفة الوحدة لحساب الأثر المالي
    public decimal TotalFinancialImpact { get; set; }     // إجمالي الأثر المالي للفارق
}
