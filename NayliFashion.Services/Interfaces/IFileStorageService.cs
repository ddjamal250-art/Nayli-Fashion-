using System.Threading.Tasks;

namespace NayliFashion.Services.Interfaces;

/// <summary>
/// خدمة إدارة وتخزين صور المنتجات والأصناف محلياً
/// </summary>
public interface IFileStorageService
{
    Task<string> SaveProductImageAsync(string sourceFilePath, string subFolder = "Products");
    Task<string> SaveImageFromBytesAsync(byte[] imageBytes, string fileName, string subFolder = "Products");
    string GetImageFullPath(string relativePath);
    bool DeleteImage(string relativePath);
}