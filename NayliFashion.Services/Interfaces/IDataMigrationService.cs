using NayliFashion.Services.DTOs;

namespace NayliFashion.Services.Interfaces;

/// <summary>
/// واجهة محرك استيراد وترحيل البيانات من مختلف قواعد البيانات والملفات
/// (Universal Database & Multi-Format Importer)
/// </summary>
public interface IDataMigrationService
{
    /// <summary>
    /// معاينة وفحص محتوى الملف أو قاعدة البيانات قبل بدء الترحيل الفعلي
    /// </summary>
    Task<MigrationPreviewDto> PreviewFileAsync(string filePath);

    /// <summary>
    /// تنفيذ الترحيل والاستيراد الكامل وحفظ السلع والعملاء في قاعدة البيانات
    /// </summary>
    Task<MigrationResultDto> ImportDataAsync(string filePath, MigrationOptionsDto? options = null);
}
