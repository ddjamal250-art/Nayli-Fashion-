using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Catalog;
using NayliFashion.Core.Models.Customers;
using NayliFashion.Core.Models.Rentals;
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
                StoreName = "Nayli Market & Fashion - نايلي ماركت وفاشن",
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

        // 4. زرع أصناف ومنتجات نموذجية أولية للتشغيل الفوري لجميع الأقسام
        if (!await context.Products.AnyAsync())
        {
            var catQamis = await context.Categories.FirstOrDefaultAsync(c => c.Name.Contains("أقمصة"));
            var catTraditional = await context.Categories.FirstOrDefaultAsync(c => c.Name.Contains("تراثية"));
            var catPerfume = await context.Categories.FirstOrDefaultAsync(c => c.Name.Contains("عطور"));
            var catFabric = await context.Categories.FirstOrDefaultAsync(c => c.Name.Contains("أقمشة"));
            var catRentals = await context.Categories.FirstOrDefaultAsync(c => c.Name.Contains("كراء"));
            var catShoes = await context.Categories.FirstOrDefaultAsync(c => c.Name.Contains("أحذية"));

            if (catQamis != null)
            {
                var prodQamis = new Product
                {
                    Name = "قميص صلاة الأصيل فاخر",
                    CategoryId = catQamis.Id,
                    ProductType = ProductType.IslamicQamisWear,
                    CodeSku = "QAM-ASEEL-01",
                    Description = "قميص صلاة شرعي نخب أول قماش كوري بارد",
                    IsSaleAllowed = true,
                    HasVariants = true
                };
                prodQamis.Variants.Add(new ProductVariant
                {
                    VariantName = "مقاس 56/24 - أبيض ناصع",
                    Barcode = "6130001000018",
                    Sku = "QAM-56-24-WHT",
                    ColorName = "أبيض ناصع",
                    RetailPrice = 4200m,
                    PurchaseCostPrice = 2800m,
                    StockQuantity = 15m,
                    MinStockAlertQuantity = 3m
                });
                await context.Products.AddAsync(prodQamis);
            }

            if (catTraditional != null)
            {
                var prodBarnous = new Product
                {
                    Name = "برنوس وبر الإبل نايلي أصيل",
                    CategoryId = catTraditional.Id,
                    ProductType = ProductType.TraditionalGarment,
                    CodeSku = "BRN-NAYLI-01",
                    Description = "برنوس مسعدي جلفاوي صوف وبر الإبل حياكة يدوية",
                    IsSaleAllowed = true,
                    IsRentalAllowed = true,
                    HasVariants = true
                };
                prodBarnous.Variants.Add(new ProductVariant
                {
                    VariantName = "وبري طبيعي نخب أول",
                    Barcode = "6130001000025",
                    Sku = "BRN-WBR-NAT",
                    ColorName = "وبري صحراوي",
                    RetailPrice = 68000m,
                    PurchaseCostPrice = 45000m,
                    RentalDailyRate = 8000m,
                    RentalSecurityDeposit = 15000m,
                    StockQuantity = 4m,
                    MinStockAlertQuantity = 1m
                });
                await context.Products.AddAsync(prodBarnous);
            }

            if (catPerfume != null)
            {
                var prodMusk = new Product
                {
                    Name = "مسك الطهارة والعرائس الأصلي",
                    CategoryId = catPerfume.Id,
                    ProductType = ProductType.MuskAndIncense,
                    CodeSku = "MSK-THR-01",
                    Description = "مسك أبيض فاخر مركز ثبات عالي",
                    IsSaleAllowed = true,
                    HasVariants = true
                };
                prodMusk.Variants.Add(new ProductVariant
                {
                    VariantName = "تولة كريستال 12 مل",
                    Barcode = "6130001000032",
                    Sku = "MSK-TOL-12",
                    ColorName = "أبيض لؤلؤي",
                    RetailPrice = 1200m,
                    PurchaseCostPrice = 650m,
                    StockQuantity = 30m,
                    MinStockAlertQuantity = 5m
                });
                await context.Products.AddAsync(prodMusk);
            }

            if (catFabric != null)
            {
                var prodFabric = new Product
                {
                    Name = "طاقة قماش كشمير إنجليزي بالمتر",
                    CategoryId = catFabric.Id,
                    ProductType = ProductType.FabricByMeter,
                    CodeSku = "FAB-CSH-01",
                    Description = "قماش بدلات وأقمشة رجالية شتوية وصيفية بالمتر العشري",
                    IsSaleAllowed = true,
                    HasVariants = true
                };
                prodFabric.Variants.Add(new ProductVariant
                {
                    VariantName = "عرض 150 سم - كحلي ملكي",
                    Barcode = "6130001000049",
                    Sku = "FAB-CSH-NAVY",
                    ColorName = "كحلي ملكي",
                    RetailPrice = 1800m,
                    PurchaseCostPrice = 1100m,
                    StockQuantity = 60m,
                    MinStockAlertQuantity = 10m
                });
                await context.Products.AddAsync(prodFabric);
            }

            if (catRentals != null)
            {
                var prodDress = new Product
                {
                    Name = "فستان زفاف ملكي كريستال مطرز",
                    CategoryId = catRentals.Id,
                    ProductType = ProductType.RentalAsset,
                    CodeSku = "RNT-DRS-01",
                    Description = "فستان سهرة وأعراس فاخر مخصص للكراء مع الطرحة والإكسسوارات",
                    IsSaleAllowed = false,
                    IsRentalAllowed = true,
                    HasVariants = true
                };
                var dressVariant = new ProductVariant
                {
                    VariantName = "أبيض عاجي - مقاس 38-42",
                    Barcode = "6130001000056",
                    Sku = "RNT-DRS-38-42",
                    ColorName = "أبيض عاجي",
                    RentalDailyRate = 25000m,
                    RentalSecurityDeposit = 15000m,
                    RetailPrice = 120000m,
                    PurchaseCostPrice = 80000m,
                    StockQuantity = 2m
                };
                prodDress.Variants.Add(dressVariant);
                await context.Products.AddAsync(prodDress);
                await context.SaveChangesAsync();

                // زرع بطاقة الأصل التأجيري المباشر
                var rentalAsset = new RentalAssetItem
                {
                    ProductVariantId = dressVariant.Id,
                    AssetSerialTag = "TAG-RN-DEMO-001",
                    AssetNameDescription = "فستان زفاف ملكي مطرز كريستال نايلي (عاجي)",
                    Status = RentalAssetStatus.AvailableForRent,
                    CurrentCondition = ItemConditionGrade.BrandNew,
                    PurchaseCost = 80000m,
                    TotalRentalCount = 0
                };
                await context.RentalAssetItems.AddAsync(rentalAsset);
            }

            if (catShoes != null)
            {
                var prodShoes = new Product
                {
                    Name = "حذاء كلاسيك جلد طبيعي",
                    CategoryId = catShoes.Id,
                    ProductType = ProductType.FootwearShoes,
                    CodeSku = "SH-CLS-01",
                    Description = "حذاء رجالي كلاسيك نعل طبي جلد عجل فاخر",
                    IsSaleAllowed = true,
                    HasVariants = true
                };
                prodShoes.Variants.Add(new ProductVariant
                {
                    VariantName = "مقاس 42 - أسود ملكي",
                    Barcode = "6130001000063",
                    Sku = "SH-42-BLK",
                    ShoeSizeEu = FootwearSizeEu.Size42,
                    ColorName = "أسود ملكي",
                    RetailPrice = 6500m,
                    PurchaseCostPrice = 4200m,
                    StockQuantity = 8m,
                    MinStockAlertQuantity = 2m
                });
                await context.Products.AddAsync(prodShoes);
            }

            await context.SaveChangesAsync();
        }

        // 5. زرع زبائن افتراضيين لتشغيل الكارني وسندات القبض
        if (!await context.Customers.AnyAsync())
        {
            var defaultCustomers = new List<Customer>
            {
                new Customer
                {
                    FullName = "عمر نايلي (زبون دائم)",
                    PhoneNumber = "0555998877",
                    AddressNeighborhood = "حي برنوس، ولاية الجلفة",
                    MaxCreditLimitDzd = 50000m,
                    CurrentDebtDzd = 12000m,
                    SalaryVirementDay = DateTime.Now.Day
                },
                new Customer
                {
                    FullName = "زبون نقدي عام (كاشير)",
                    PhoneNumber = "0600000000",
                    AddressNeighborhood = "الجلفة",
                    MaxCreditLimitDzd = 0m,
                    CurrentDebtDzd = 0m
                }
            };
            await context.Customers.AddRangeAsync(defaultCustomers);
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
