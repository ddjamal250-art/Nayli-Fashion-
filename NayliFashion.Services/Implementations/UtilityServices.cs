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
}

/// <summary>
/// تطبيق خدمة النسخ الاحتياطي التلقائي لقاعدة بيانات SQLite لحماية بيانات المحل من الضياع
/// </summary>
public class BackupService : IBackupService
{
    private readonly string _databaseFilePath = Path.Combine(@"D:\repos\NayliFashion", "nayli_fashion.db");
    private readonly string _defaultBackupDirectory = Path.Combine(@"D:\repos\NayliFashion", "Backups");

    public async Task<string> CreateBackupAsync(string? customDestinationFolder = null)
    {
        string targetDir = customDestinationFolder ?? _defaultBackupDirectory;
        Directory.CreateDirectory(targetDir);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string backupFileName = $"NayliFashion_Backup_{timestamp}.db";
        string backupFilePath = Path.Combine(targetDir, backupFileName);

        if (File.Exists(_databaseFilePath))
        {
            // نسخ آمن للملف
            await Task.Run(() => File.Copy(_databaseFilePath, backupFilePath, true));
            return backupFilePath;
        }

        throw new FileNotFoundException("ملف قاعدة البيانات الأصلي غير موجود لنسخه!");
    }

    public async Task<bool> RestoreBackupAsync(string backupFilePath)
    {
        if (!File.Exists(backupFilePath))
            return false;

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
