using Microsoft.EntityFrameworkCore;
using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Catalog;
using NayliFashion.Core.Models.Users;
using NayliFashion.Data.Context;
using NayliFashion.Data.Seed;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Services.Implementations;

/// <summary>
/// تطبيق خدمة توليد وفحص الباركود القياسي EAN-13 وتنسيق الملصقات
/// </summary>
public class BarcodeService : IBarcodeService
{
    private static int _counter = Random.Shared.Next(1000, 9999);

    public string GenerateUniqueBarcode(string prefix = "200")
    {
        // توليد باركود داخلي قياسي فريد EAN-13 (12 رقم بيانات + رقم تدقيق Check Digit)
        prefix = string.Concat((prefix ?? "200").Where(char.IsDigit));
        if (prefix.Length == 0) prefix = "200";
        if (prefix.Length > 8) prefix = prefix.Substring(0, 8);

        int needed = 12 - prefix.Length;
        int seq = Interlocked.Increment(ref _counter);
        int timePart = (int)(DateTime.UtcNow.TimeOfDay.TotalMilliseconds % 100_000);

        long combined = ((long)timePart * 10_000L + (seq % 10_000)) % (long)Math.Pow(10, needed);
        string dynamicPart = combined.ToString().PadLeft(needed, '0');

        string base12 = prefix + dynamicPart;
        int checkDigit = CalculateEan13CheckDigit(base12);
        return $"{base12}{checkDigit}";
    }

    public bool ValidateEan13(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode) || barcode.Length != 13 || !barcode.All(char.IsDigit))
            return false;

        string base12 = barcode.Substring(0, 12);
        int expectedCheckDigit = CalculateEan13CheckDigit(base12);
        int actualCheckDigit = barcode[12] - '0';

        return expectedCheckDigit == actualCheckDigit;
    }

    public string FormatBarcodeLabelText(ProductVariant variant, string storeName)
    {
        return $"[ {storeName} ]\n" +
               $"{variant.VariantName}\n" +
               $"السعر: {variant.RetailPrice:N0} دج\n" +
               $"باركود: {variant.Barcode}";
    }

    private static int CalculateEan13CheckDigit(string base12)
    {
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = base12[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }

        int remainder = sum % 10;
        return (remainder == 0) ? 0 : 10 - remainder;
    }
}

/// <summary>
/// تطبيق خدمة المصادقة وإدارة جلسة المستخدم الحالية والصلاحيات
/// </summary>
public class AuthService : IAuthService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    public User? CurrentUser { get; private set; }

    public AuthService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == username.Trim().ToLower() && !u.IsDeleted);

        if (user == null || !user.IsActive)
            return null;

        bool isValid = DatabaseInitializer.VerifyPassword(password, user.PasswordHash, user.PasswordSalt);
        if (!isValid)
            return null;

        user.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        CurrentUser = user;
        return user;
    }

    public void Logout()
    {
        CurrentUser = null;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync(userId);
        if (user == null) return false;

        bool isOldValid = DatabaseInitializer.VerifyPassword(oldPassword, user.PasswordHash, user.PasswordSalt);
        if (!isOldValid) return false;

        DatabaseInitializer.CreatePasswordHash(newPassword, out string hash, out string salt);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Users.OrderBy(u => u.Username).ToListAsync();
    }

    public async Task<User> CreateUserAsync(string username, string fullName, string password, UserRole role, string? phone)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        DatabaseInitializer.CreatePasswordHash(password, out string hash, out string salt);

        var user = new User
        {
            Username = username.Trim().ToLower(),
            FullName = fullName.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = role,
            PhoneNumber = phone
        };

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> UpdateUserStatusAsync(int userId, bool isActive)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync(userId);
        if (user == null) return false;

        user.IsActive = isActive;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResetUserPasswordAsync(int userId, string newPassword)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var user = await context.Users.FindAsync(userId);
        if (user == null) return false;

        DatabaseInitializer.CreatePasswordHash(newPassword, out string hash, out string salt);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;

        await context.SaveChangesAsync();
        return true;
    }
}

/// <summary>
/// تطبيق خدمة النسخ الاحتياطي التفاعلي والتلقائي لقاعدة بيانات SQLite مع دعم المسارات المخصصة والفلاش ديسك
/// </summary>
public class BackupService : IBackupService
{
    private readonly string _databaseFilePath;
    private readonly string _defaultBackupDirectory;

    public BackupService(string? databaseFilePath = null, string? defaultBackupDirectory = null)
    {
        _databaseFilePath = databaseFilePath ?? Path.Combine(@"D:\repos\NayliFashion", "nayli_fashion.db");
        _defaultBackupDirectory = defaultBackupDirectory ?? Path.Combine(@"D:\repos\NayliFashion", "Backups");
    }

    public string GetDatabaseFilePath() => _databaseFilePath;
    public string GetDefaultBackupDirectory() => _defaultBackupDirectory;

    public async Task<string> CreateBackupAsync(string? customDestinationPath = null)
    {
        string targetFilePath;

        if (!string.IsNullOrWhiteSpace(customDestinationPath))
        {
            if (customDestinationPath.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
            {
                // مسار ملف محدد بالكامل (مثلاً من SaveFileDialog)
                string? dir = Path.GetDirectoryName(customDestinationPath);
                if (!string.IsNullOrWhiteSpace(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                targetFilePath = customDestinationPath;
            }
            else
            {
                // مسار مجلد مخصص (مثلاً فلاش ديسك E:\)
                Directory.CreateDirectory(customDestinationPath);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                targetFilePath = Path.Combine(customDestinationPath, $"NayliFashion_Backup_{timestamp}.db");
            }
        }
        else
        {
            Directory.CreateDirectory(_defaultBackupDirectory);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            targetFilePath = Path.Combine(_defaultBackupDirectory, $"NayliFashion_Backup_{timestamp}.db");
        }

        if (File.Exists(_databaseFilePath))
        {
            // إغلاق أي اتصالات معلقة لضمان سلامة النسخ
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            await Task.Run(() => File.Copy(_databaseFilePath, targetFilePath, true));
            return targetFilePath;
        }

        throw new FileNotFoundException("ملف قاعدة البيانات الأصلي غير موجود لنسخه!");
    }

    public async Task<bool> RestoreBackupAsync(string backupFilePath)
    {
        if (!File.Exists(backupFilePath))
            return false;

        // تحرير قفل الملفات لضمان عدم حدوث تصادم مع الاتصالات المفتوحة
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        string? parentDir = Path.GetDirectoryName(_databaseFilePath);
        if (!string.IsNullOrWhiteSpace(parentDir))
        {
            Directory.CreateDirectory(parentDir);
        }

        await Task.Run(() => File.Copy(backupFilePath, _databaseFilePath, true));
        return true;
    }

    public List<string> GetAvailableBackups()
    {
        if (!Directory.Exists(_defaultBackupDirectory))
            return new List<string>();

        return Directory.GetFiles(_defaultBackupDirectory, "*.db")
                        .OrderByDescending(f => File.GetCreationTime(f))
                        .ToList();
    }
}
