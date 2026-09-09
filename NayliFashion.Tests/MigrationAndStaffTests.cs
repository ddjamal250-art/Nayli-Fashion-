using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Catalog;
using NayliFashion.Core.Models.Customers;
using NayliFashion.Data.Context;
using NayliFashion.Data.Seed;
using NayliFashion.Services.DTOs;
using NayliFashion.Services.Implementations;
using Xunit;

namespace NayliFashion.Tests;

public class MigrationAndStaffTests
{
    private class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly string _connectionString;

        public TestDbContextFactory(string dbName)
        {
            _connectionString = $"Data Source={dbName}.db";
        }

        public AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connectionString)
                .Options;
            return new AppDbContext(options);
        }
    }

    [Fact]
    public async Task TestUniversalDataMigration_JsonFile()
    {
        string testDbName = $"MigDb_{Guid.NewGuid():N}";
        var factory = new TestDbContextFactory(testDbName);

        using (var initDb = factory.CreateDbContext())
        {
            await DatabaseInitializer.InitializeDatabaseAsync(initDb);
        }

        var barcodeService = new BarcodeService();
        var migrationService = new DataMigrationService(factory, barcodeService);

        string tempJsonPath = Path.Combine(Path.GetTempPath(), $"import_test_{Guid.NewGuid():N}.json");
        var sampleData = new
        {
            products = new[]
            {
                new { name = "قشابية وبرية فاخرة", category = "ألبسة تقليدية", price = 45000.0, cost = 28000.0, stock = 5.0, color = "وبري صحراوي", sku = "QSH-001", barcode = "6130001" },
                new { name = "قميص صلاة إماراتي", category = "أقمشة وصلاة", price = 3800.0, cost = 2200.0, stock = 20.0, color = "أبيض", sku = "QAM-001", barcode = "" }
            },
            customers = new[]
            {
                new { name = "الحاج بلقاسم الجلفاوي", phone = "0661234567", address = "حي الضاية، الجلفة", debt = 15000.0, maxDebt = 100000.0 }
            }
        };

        await File.WriteAllTextAsync(tempJsonPath, JsonSerializer.Serialize(sampleData));

        try
        {
            var preview = await migrationService.PreviewFileAsync(tempJsonPath);
            Assert.Contains("JSON", preview.FileType);
            Assert.Equal(2, preview.EstimatedProductCount);
            Assert.Equal(1, preview.EstimatedCustomerCount);

            var options = new MigrationOptionsDto
            {
                GenerateBarcodeIfMissing = true,
                SkipDuplicates = true,
                DefaultCategoryName = "واردات عامة"
            };

            var result = await migrationService.ImportDataAsync(tempJsonPath, options);
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.TotalProductsImported);
            Assert.Equal(1, result.TotalCustomersImported);

            using (var verifyDb = factory.CreateDbContext())
            {
                var qashabiya = await verifyDb.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Name.Contains("قشابية"));
                Assert.NotNull(qashabiya);
                var variant = qashabiya.Variants.First();
                Assert.Equal(45000m, variant.RetailPrice);
                Assert.Equal(5m, variant.StockQuantity);

                var qamis = await verifyDb.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Name.Contains("قميص"));
                Assert.NotNull(qamis);
                var qamisVar = qamis.Variants.First();
                Assert.False(string.IsNullOrWhiteSpace(qamisVar.Barcode));
                Assert.Equal(13, qamisVar.Barcode.Length);

                var customer = await verifyDb.Customers.FirstOrDefaultAsync(c => c.FullName.Contains("بلقاسم"));
                Assert.NotNull(customer);
                Assert.Equal(15000m, customer.CurrentDebtDzd);
                Assert.Equal("0661234567", customer.PhoneNumber);
            }
        }
        finally
        {
            if (File.Exists(tempJsonPath))
                File.Delete(tempJsonPath);

            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists($"{testDbName}.db"))
                File.Delete($"{testDbName}.db");
        }
    }

    [Fact]
    public async Task TestUniversalDataMigration_CsvFile()
    {
        string testDbName = $"MigCsvDb_{Guid.NewGuid():N}";
        var factory = new TestDbContextFactory(testDbName);

        using (var initDb = factory.CreateDbContext())
        {
            await DatabaseInitializer.InitializeDatabaseAsync(initDb);
        }

        var barcodeService = new BarcodeService();
        var migrationService = new DataMigrationService(factory, barcodeService);

        string tempCsvPath = Path.Combine(Path.GetTempPath(), $"import_test_{Guid.NewGuid():N}.csv");
        string csvContent = "Name,Category,Barcode,RetailPrice,CostPrice,StockQuantity,Color\n" +
                            "حذاء كلاسيك إيطالي جلد,أحذية,6131234567890,6500,4000,12,بني داكن\n" +
                            "برنوس وبري أصلي,ألبسة تقليدية,,55000,35000,3,عسلي\n";

        await File.WriteAllTextAsync(tempCsvPath, csvContent, System.Text.Encoding.UTF8);

        try
        {
            var preview = await migrationService.PreviewFileAsync(tempCsvPath);
            Assert.Contains("CSV", preview.FileType);
            Assert.Equal(2, preview.EstimatedProductCount);

            var result = await migrationService.ImportDataAsync(tempCsvPath, new MigrationOptionsDto
            {
                GenerateBarcodeIfMissing = true,
                SkipDuplicates = false
            });

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.TotalProductsImported);

            using (var verifyDb = factory.CreateDbContext())
            {
                var shoe = await verifyDb.ProductVariants.FirstOrDefaultAsync(v => v.Barcode == "6131234567890");
                Assert.NotNull(shoe);
                Assert.Equal(6500m, shoe.RetailPrice);
                Assert.Equal("بني داكن", shoe.ColorName);
            }
        }
        finally
        {
            if (File.Exists(tempCsvPath))
                File.Delete(tempCsvPath);

            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists($"{testDbName}.db"))
                File.Delete($"{testDbName}.db");
        }
    }

    [Fact]
    public async Task TestAuthService_ResetUserPassword()
    {
        string testDbName = $"AuthDb_{Guid.NewGuid():N}";
        var factory = new TestDbContextFactory(testDbName);

        using (var initDb = factory.CreateDbContext())
        {
            await DatabaseInitializer.InitializeDatabaseAsync(initDb);
        }

        var authService = new AuthService(factory);

        var user = await authService.CreateUserAsync("amine_stock", "أمين المخزن علي", "InitialPassword123!", UserRole.StockKeeper, "0555123456");
        Assert.NotNull(user);

        var authSuccess = await authService.LoginAsync("amine_stock", "InitialPassword123!");
        Assert.NotNull(authSuccess);
        Assert.Equal(UserRole.StockKeeper, authSuccess.Role);

        var resetResult = await authService.ResetUserPasswordAsync(authSuccess.Id, "SuperNayli2026!");
        Assert.True(resetResult);

        var oldAuth = await authService.LoginAsync("amine_stock", "InitialPassword123!");
        Assert.Null(oldAuth);

        var newAuth = await authService.LoginAsync("amine_stock", "SuperNayli2026!");
        Assert.NotNull(newAuth);
        Assert.Equal("amine_stock", newAuth.Username);

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists($"{testDbName}.db"))
            File.Delete($"{testDbName}.db");
    }

    [Fact]
    public async Task TestSmartOmnisearch_SearchByNameColorBrandPrice()
    {
        string testDbName = $"OmniDb_{Guid.NewGuid():N}";
        var factory = new TestDbContextFactory(testDbName);

        using (var initDb = factory.CreateDbContext())
        {
            await DatabaseInitializer.InitializeDatabaseAsync(initDb);
        }

        var barcodeService = new BarcodeService();
        var inventoryService = new InventoryService(factory, barcodeService);

        using (var db = factory.CreateDbContext())
        {
            var brand = new Brand { Name = "الرافدين للأصالة" };
            await db.Brands.AddAsync(brand);

            var cat = await db.Categories.FirstAsync();

            var prod = new Product
            {
                Name = "جبة نائلية تقليدية مطرزة بالحرير",
                CategoryId = cat.Id,
                Brand = brand,
                CodeSku = "NAYLI-JEBBA-01"
            };
            await db.Products.AddAsync(prod);
            await db.SaveChangesAsync();

            var variant = new ProductVariant
            {
                ProductId = prod.Id,
                VariantName = "مقاس 42 / خمري ملكي",
                Barcode = "2009876543210",
                RetailPrice = 18500m,
                PurchaseCostPrice = 12000m,
                ColorName = "خمري ملكي",
                StockQuantity = 4m
            };
            await db.ProductVariants.AddAsync(variant);
            await db.SaveChangesAsync();
        }

        // 1. البحث باسم اللون
        var byColor = await inventoryService.GetProductsAsync(null, "خمري");
        Assert.NotEmpty(byColor);
        Assert.Contains(byColor, p => p.Name.Contains("جبة نائلية"));

        // 2. البحث برقم السعر
        var byPrice = await inventoryService.GetProductsAsync(null, "18500");
        Assert.NotEmpty(byPrice);
        Assert.Contains(byPrice, p => p.Name.Contains("جبة نائلية"));

        // 3. البحث بالباركود
        var byBarcode = await inventoryService.GetProductsAsync(null, "2009876543210");
        Assert.NotEmpty(byBarcode);

        // 4. البحث بالعلامة التجارية
        var byBrand = await inventoryService.GetProductsAsync(null, "الرافدين");
        Assert.NotEmpty(byBrand);

        // 5. البحث بـ SKU
        var bySku = await inventoryService.GetProductsAsync(null, "NAYLI-JEBBA");
        Assert.NotEmpty(bySku);

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists($"{testDbName}.db"))
            File.Delete($"{testDbName}.db");
    }

    [Fact]
    public async Task TestCustomPathBackupAndRestore()
    {
        string testDbName = $"BackupDb_{Guid.NewGuid():N}";
        string dbFile = $"{testDbName}.db";
        var factory = new TestDbContextFactory(testDbName);

        using (var initDb = factory.CreateDbContext())
        {
            await DatabaseInitializer.InitializeDatabaseAsync(initDb);
            var setting = await initDb.AppSettings.FirstAsync();
            setting.StoreName = "نايلي فاشن - فرع أول نوفمبر";
            await initDb.SaveChangesAsync();
        }

        var backupService = new BackupService(dbFile);
        string customBackupPath = Path.Combine(Path.GetTempPath(), $"nayli_custom_backup_{Guid.NewGuid():N}.db");

        try
        {
            string createdPath = await backupService.CreateBackupAsync(customBackupPath);
            Assert.True(File.Exists(createdPath));

            using (var modifyDb = factory.CreateDbContext())
            {
                var setting = await modifyDb.AppSettings.FirstAsync();
                setting.StoreName = "اسم تم تدميره";
                await modifyDb.SaveChangesAsync();
            }

            bool restoreSuccess = await backupService.RestoreBackupAsync(customBackupPath);
            Assert.True(restoreSuccess);

            using (var verifyDb = factory.CreateDbContext())
            {
                var setting = await verifyDb.AppSettings.FirstAsync();
                Assert.Equal("نايلي فاشن - فرع أول نوفمبر", setting.StoreName);
            }
        }
        finally
        {
            if (File.Exists(customBackupPath))
                File.Delete(customBackupPath);

            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbFile))
                File.Delete(dbFile);
        }
    }

    [Fact]
    public async Task TestMultiUserSeedAndPinAuthentication()
    {
        string testDbName = $"AuthDb_{Guid.NewGuid():N}";
        string dbFile = $"{testDbName}.db";
        var factory = new TestDbContextFactory(testDbName);

        using (var initDb = factory.CreateDbContext())
        {
            await DatabaseInitializer.InitializeDatabaseAsync(initDb);
        }

        var authService = new AuthService(factory);

        try
        {
            // 1. التحقق من وجود المستخدمين الثلاثة المعتمدين
            var allUsers = await authService.GetAllUsersAsync();
            Assert.Contains(allUsers, u => u.Username == "admin" && u.Role == UserRole.SuperAdmin);
            Assert.Contains(allUsers, u => u.Username == "cashier" && u.Role == UserRole.Cashier);
            Assert.Contains(allUsers, u => u.Username == "manager" && u.Role == UserRole.StoreManager);

            // 2. تسجيل دخول مدير النظام
            var adminUser = await authService.LoginAsync("admin", "admin123");
            Assert.NotNull(adminUser);
            Assert.Equal("admin", adminUser.Username);

            // 3. تسجيل دخول الكاشير عبر رمز PIN الرقمي 1234
            var cashierUser = await authService.LoginAsync("cashier", "1234");
            Assert.NotNull(cashierUser);
            Assert.Equal("cashier", cashierUser.Username);
            Assert.Equal(UserRole.Cashier, cashierUser.Role);

            // 4. التحقق من فشل كلمة مرور خاطئة
            var wrongUser = await authService.LoginAsync("cashier", "9999");
            Assert.Null(wrongUser);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(dbFile))
                File.Delete(dbFile);
        }
    }
}
