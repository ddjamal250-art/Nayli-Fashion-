using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Catalog;
using NayliFashion.Core.Models.System;
using NayliFashion.Core.Models.Users;
using NayliFashion.Data.Context;

namespace NayliFashion.Data.Seed;

/// <summary>
/// فئة تهيئة قاعدة البيانات وزرع البيانات الأولية الضرورية للتشغيل الفوري
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(AppDbContext context)
    {
        // إنشاء المخطط والجداول تلقائياً في حال عدم وجودها
        await context.Database.EnsureCreatedAsync();

        // 1. زرع إعدادات المحل الافتراضية
        if (!await context.AppSettings.AnyAsync())
        {
            var defaultSetting = new AppSetting
            {
                StoreName = "Nayli Fashion - نايل فاشن",
                StoreSlogan = "للألبسة والأقمشة والعطور والكراء الفاخر",
                StoreAddress = "حي برنوس، ولاية الجلفة",
                StorePhoneNumber = "027 00 00 00 / 06 00 00 00 00",
                ThermalPaperWidthMm = 80,
                AutoOpenCashDrawerOnPrint = true,
                PrintBaridiMobQrOnReceipt = true,
                ReceiptFooterMessage = "شكراً لزيارتكم - البضاعة المباعة لا ترد ولا تستبدل إلا خلال 48 ساعة",
                BackupFolderPath = @"D:\repos\NayliFashion\Backups"
            };

            await context.AppSettings.AddAsync(defaultSetting);
            await context.SaveChangesAsync();
        }

        // 2. زرع حسابات المستخدمين الافتراضية للتشغيل الفوري واختبار تبديل الموظفين
        if (!await context.Users.AnyAsync(u => u.Username == "admin"))
        {
            CreatePasswordHash("admin123", out string hash, out string salt);

            var adminUser = new User
            {
                Username = "admin",
                FullName = "مدير النظام (Super Admin)",
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = UserRole.SuperAdmin,
                PhoneNumber = "0600000000"
            };

            await context.Users.AddAsync(adminUser);
            await context.SaveChangesAsync();
        }

        if (!await context.Users.AnyAsync(u => u.Username == "cashier"))
        {
            CreatePasswordHash("1234", out string hash, out string salt);

            var cashierUser = new User
            {
                Username = "cashier",
                FullName = "كاشير الصندوق (Cashier)",
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = UserRole.Cashier,
                PhoneNumber = "0611111111"
            };

            await context.Users.AddAsync(cashierUser);
            await context.SaveChangesAsync();
        }

        if (!await context.Users.AnyAsync(u => u.Username == "manager"))
        {
            CreatePasswordHash("manager123", out string hash, out string salt);

            var managerUser = new User
            {
                Username = "manager",
                FullName = "مسؤول المحل (Store Manager)",
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = UserRole.StoreManager,
                PhoneNumber = "0622222222"
            };

            await context.Users.AddAsync(managerUser);
            await context.SaveChangesAsync();
        }

        // 3. زرع الفئات الرئيسية للنشاط
        if (!await context.Categories.AnyAsync())
        {
            var defaultCategories = new List<Category>
            {
                new Category { Name = "أقمصة وأثواب صلاة", Description = "أقمصة الدفة، الأصيل، الإماراتي، والسعودي بمقاسات الطول والعرض", DisplayOrder = 1 },
                new Category { Name = "ألبسة رجالية وأطقم", Description = "بدلات رسمية، قمصان، سترات، وبنطلونات", DisplayOrder = 2 },
                new Category { Name = "ألبسة نسائية وفساتين", Description = "جباب، فساتين سهرة، أطقم عصرية وإسدالات", DisplayOrder = 3 },
                new Category { Name = "أحذية ومصنوعات جلدية", Description = "أحذية كلاسيك، رياضية، وصنادل بمقاسات أوروبية", DisplayOrder = 4 },
                new Category { Name = "أقمشة ومنسوجات بالمتر", Description = "أقمشة نسائية ورجالية بالبكرات والأمتار العشرية", DisplayOrder = 5 },
                new Category { Name = "ألبسة تراثية جلفاوية", Description = "برنوس وڨشابية وبر الإبل الخالص وصوف الجلفة الأصيل", DisplayOrder = 6 },
                new Category { Name = "عطور وزيوت ومسك", Description = "عطور أصلية، زيوت تعبئة، مسك الطهارة، وسواك", DisplayOrder = 7 },
                new Category { Name = "أفرشة وجهاز العروسة", Description = "أطقم أفرشة فاخرة، كوفريلي، ولحف الأعراس", DisplayOrder = 8 },
                new Category { Name = "كراء الفساتين والبدلات", Description = "قسم التأجير وعقود كراء فساتين الأعراس وبدلات المناسبات", DisplayOrder = 9 }
            };

            await context.Categories.AddRangeAsync(defaultCategories);
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// توليد ملح وتشفير كلمة المرور بتقنية PBKDF2 الآمنة
    /// </summary>
    public static void CreatePasswordHash(string password, out string hash, out string salt)
    {
        byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
        salt = Convert.ToBase64String(saltBytes);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256);
        byte[] hashBytes = pbkdf2.GetBytes(32);
        hash = Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// التحقق من مطابقة كلمة المرور المدخلة للتجزئة المخزنة
    /// </summary>
    public static bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        byte[] saltBytes = Convert.FromBase64String(storedSalt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100000, HashAlgorithmName.SHA256);
        byte[] hashBytes = pbkdf2.GetBytes(32);
        string computedHash = Convert.ToBase64String(hashBytes);
        return computedHash == storedHash;
    }
}
