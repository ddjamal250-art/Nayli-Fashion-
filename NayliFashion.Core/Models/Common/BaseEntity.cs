namespace NayliFashion.Core.Models.Common;

/// <summary>
/// الكيان الأساسي الموروث لجميع جداول النظام (مع دعم التدقيق والحذف الآمن)
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;
}
