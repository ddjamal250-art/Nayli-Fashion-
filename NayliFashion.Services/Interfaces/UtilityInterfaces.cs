using NayliFashion.Core.Models.Catalog;
using NayliFashion.Core.Models.Users;

namespace NayliFashion.Services.Interfaces;

/// <summary>
/// واجهة خدمة توليد والتحقق من الباركود والملصقات
/// </summary>
public interface IBarcodeService
{
    // توليد باركود قياسي فريد (EAN-13 أو Code-128 داخلي)
    string GenerateUniqueBarcode(string prefix = "200");

    // التحقق من صحة الباركود ومفتاح الفحص Check Digit
    bool ValidateEan13(string barcode);

    // تجهيز بيانات ملصق الباركود القابل للطباعة
    string FormatBarcodeLabelText(ProductVariant variant, string storeName);
}

/// <summary>
/// واجهة خدمة المصادقة وإدارة الجلسات وصلاحيات المستخدمين
/// </summary>
public interface IAuthService
{
    User? CurrentUser { get; }
    Task<User?> LoginAsync(string username, string password);
    void Logout();
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    Task<List<User>> GetAllUsersAsync();
    Task<User> CreateUserAsync(string username, string fullName, string password, Core.Enums.UserRole role, string? phone);
    Task<bool> UpdateUserStatusAsync(int userId, bool isActive);
    Task<bool> ResetUserPasswordAsync(int userId, string newPassword);
}

/// <summary>
/// واجهة خدمة النسخ الاحتياطي التلقائي والتفاعلي لقاعدة بيانات SQLite مع دعم المسارات المخصصة والفلاش ديسك
/// </summary>
public interface IBackupService
{
    Task<string> CreateBackupAsync(string? customDestinationPath = null);
    Task<bool> RestoreBackupAsync(string backupFilePath);
    List<string> GetAvailableBackups();
    string GetDatabaseFilePath();
    string GetDefaultBackupDirectory();
}
