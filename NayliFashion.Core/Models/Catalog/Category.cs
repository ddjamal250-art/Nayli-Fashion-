using NayliFashion.Core.Models.Common;

namespace NayliFashion.Core.Models.Catalog;

/// <summary>
/// فئات وأقسام المنتجات (شجرة فئات متعددة المستويات)
/// </summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconKey { get; set; } // اسم الأيقونة في الواجهة
    public string? ImagePath { get; set; }
    public int DisplayOrder { get; set; } = 0;

    // فئة رئيسية أعلى (شجرة تصنيفات)
    public int? ParentCategoryId { get; set; }
    public virtual Category? ParentCategory { get; set; }

    // الفئات الفرعية
    public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();

    // المنتجات التابعة لهذه الفئة
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
