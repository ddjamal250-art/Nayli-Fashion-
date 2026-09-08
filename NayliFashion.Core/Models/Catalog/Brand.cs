using NayliFashion.Core.Models.Common;

namespace NayliFashion.Core.Models.Catalog;

/// <summary>
/// العلامة التجارية أو المصنع (الماركة / الصانع التقليدي)
/// </summary>
public class Brand : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? CountryOfOrigin { get; set; } // بلد المنشأ (الجزائر، السعودية، إيطاليا، فرنسا...)
    public string? LogoPath { get; set; }
    public string? Notes { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
