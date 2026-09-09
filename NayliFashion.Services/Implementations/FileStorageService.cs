using System;
using System.IO;
using System.Threading.Tasks;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Services.Implementations;

/// <summary>
/// تطبيق خدمة تخزين الصور محلياً مع التحقق من الامتدادات وتوليد أسماء فريدة
/// </summary>
public class FileStorageService : IFileStorageService
{
    private readonly string _baseStoragePath;

    public FileStorageService(string? baseStoragePath = null)
    {
        _baseStoragePath = baseStoragePath ?? Path.Combine(@"D:\repos\NayliFashion", "Storage", "Images");
        Directory.CreateDirectory(_baseStoragePath);
    }

    public async Task<string> SaveProductImageAsync(string sourceFilePath, string subFolder = "Products")
    {
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException("ملف الصورة المحدد غير موجود!", sourceFilePath);

        string extension = Path.GetExtension(sourceFilePath).ToLowerInvariant();
        if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".webp")
            throw new InvalidOperationException("صيغة الملف غير مدعومة كصورة صالحة!");

        string targetDir = Path.Combine(_baseStoragePath, subFolder);
        Directory.CreateDirectory(targetDir);

        string uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        string targetFilePath = Path.Combine(targetDir, uniqueFileName);

        using (var sourceStream = File.OpenRead(sourceFilePath))
        using (var destinationStream = File.Create(targetFilePath))
        {
            await sourceStream.CopyToAsync(destinationStream);
        }

        return Path.Combine("Storage", "Images", subFolder, uniqueFileName);
    }

    public async Task<string> SaveImageFromBytesAsync(byte[] imageBytes, string fileName, string subFolder = "Products")
    {
        if (imageBytes == null || imageBytes.Length == 0)
            throw new ArgumentException("بيانات الصورة فارغة!");

        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension)) extension = ".jpg";

        string targetDir = Path.Combine(_baseStoragePath, subFolder);
        Directory.CreateDirectory(targetDir);

        string uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        string targetFilePath = Path.Combine(targetDir, uniqueFileName);

        await File.WriteAllBytesAsync(targetFilePath, imageBytes);
        return Path.Combine("Storage", "Images", subFolder, uniqueFileName);
    }

    public string GetImageFullPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return string.Empty;
        if (Path.IsPathRooted(relativePath)) return relativePath;
        return Path.Combine(@"D:\repos\NayliFashion", relativePath);
    }

    public bool DeleteImage(string relativePath)
    {
        try
        {
            string fullPath = GetImageFullPath(relativePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }
        }
        catch
        {
            // تجاهل الخطأ عند فشل الحذف
        }
        return false;
    }
}