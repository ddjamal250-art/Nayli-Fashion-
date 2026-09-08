using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Common;

namespace NayliFashion.Core.Models.Catalog;

/// <summary>
/// المنتج الرئيسي (Master Product) الذي يحمل البيانات العامة والتعريفية
/// </summary>
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? CodeSku { get; set; }
    public string? Description { get; set; }
    public string? MainImagePath { get; set; } // مسار الصورة المحفوظة محلياً

    public ProductType ProductType { get; set; } = ProductType.ReadyToWearClothing;

    // التصنيف والعلامة التجارية
    public int CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;

    public int? BrandId { get; set; }
    public virtual Brand? Brand { get; set; }

    // معايير خاصة بالأقمشة والمنسوجات (في حال كان قماشاً أو أفرشة)
    public FabricWidthStandard FabricWidth { get; set; } = FabricWidthStandard.CustomWidth;
    public FabricMaterialType FabricMaterial { get; set; } = FabricMaterialType.Cotton;
    public TraditionalGarmentGrade TraditionalGrade { get; set; } = TraditionalGarmentGrade.NotTraditional;

    // محددات النشاط والخصائص العامة
    public bool IsRentalAllowed { get; set; } = false; // هل يمكن كراء هذا المنتج؟
    public bool IsSaleAllowed { get; set; } = true;    // هل يمكن بيعه؟
    public bool IsCompositeRecipe { get; set; } = false; // هل هو مركب من مواد أولية (BOM)؟
    public bool HasVariants { get; set; } = true;      // هل يملك متغيرات مقاسات/ألوان؟

    // المتغيرات والنسخ الفرعية للمنتج
    public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
}
