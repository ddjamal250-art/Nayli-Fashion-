using NayliFashion.Core.Models.Common;

namespace NayliFashion.Core.Models.Catalog;

/// <summary>
/// طاقة / بكرة قماش مفردة (لتتبع الأقمشة بالمتر ومنع بيع أمتار غير متصلة من بكرات مختلفة)
/// </summary>
public class FabricRoll : BaseEntity
{
    public int ProductVariantId { get; set; }
    public virtual ProductVariant ProductVariant { get; set; } = null!;

    public string RollCode { get; set; } = string.Empty;   // رقم تسلسلي فريد للبكرة (مثال: "ROL-2026-004")
    public decimal InitialMeters { get; set; } = 0m;       // الطول الكلي للبكرة عند الشراء (مثال: 50.00 متر)
    public decimal RemainingMeters { get; set; } = 0m;     // الطول المتبقي الفعلي في الطاقة
    public decimal WidthCm { get; set; } = 150m;           // عرض القماش بالسانتيمتر
    public decimal CostPerMeter { get; set; } = 0m;        // تكلفة المتر الواحد
    public string? RackLocation { get; set; }              // مكان وضع البكرة بالرف
    public bool IsExhausted { get; set; } = false;         // هل نفدت البكرة بالكامل؟
}
