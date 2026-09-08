using NayliFashion.Core.Models.Common;

namespace NayliFashion.Core.Models.Catalog;

/// <summary>
/// عنصر في شجرة المواد والتركيب (BOM) لمنتجات عطور التعبئة والخلطات المركبة
/// يربط المنتج المركب النهائي بالمواد الأولية (الزيت الخام + الكحول + الزجاجة + البخاخ)
/// </summary>
public class CompositeRecipeItem : BaseEntity
{
    // المنتج النهائي المركب (مثال: عطر 50 مل تعبئة)
    public int ParentVariantId { get; set; }
    public virtual ProductVariant ParentVariant { get; set; } = null!;

    // المكون الخام من المخزون (مثال: زيت عطر فرنسي خام، كحول نقي، أو قارورة فارغة 50 مل)
    public int ComponentVariantId { get; set; }
    public virtual ProductVariant ComponentVariant { get; set; } = null!;

    // الكمية المطلوبة من المكون لكل وحدة واحدة من المنتج النهائي
    public decimal QuantityRequired { get; set; }          // مثال: 15.00 مل زيت، 35.00 مل كحول، 1 قارورة
    public string UnitOfMeasure { get; set; } = "ml";      // وحدة القياس: ml, g, piece, tola
    public decimal EstimatedCostShare { get; set; } = 0m;  // حصة هذا المكون من التكلفة
}
