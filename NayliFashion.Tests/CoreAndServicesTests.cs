using Microsoft.EntityFrameworkCore;
using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Catalog;
using NayliFashion.Core.Models.Rentals;
using NayliFashion.Data.Context;
using NayliFashion.Data.Seed;
using NayliFashion.Services.Helpers;
using NayliFashion.Services.Implementations;
using Xunit;

namespace NayliFashion.Tests;

public class CoreAndServicesTests
{
    private class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly string _dbName;

        public TestDbContextFactory(string dbName)
        {
            _dbName = dbName;
        }

        public AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={_dbName}.db")
                .Options;
            return new AppDbContext(options);
        }
    }

    [Fact]
    public async Task TestDatabaseInitializationAndSeeding()
    {
        string dbName = $"TestDb_{Guid.NewGuid():N}";
        var factory = new TestDbContextFactory(dbName);

        using (var db = factory.CreateDbContext())
        {
            await DatabaseInitializer.InitializeDatabaseAsync(db);

            var admin = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
            Assert.NotNull(admin);
            Assert.Equal(UserRole.SuperAdmin, admin.Role);

            var setting = await db.AppSettings.FirstOrDefaultAsync();
            Assert.NotNull(setting);
            Assert.Contains("Nayli Fashion", setting.StoreName);

            var categoriesCount = await db.Categories.CountAsync();
            Assert.True(categoriesCount >= 9);
        }

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists($"{dbName}.db"))
            File.Delete($"{dbName}.db");
    }

    [Fact]
    public void TestArabicTafqeetAndPopularCentimes()
    {
        decimal amount = 3500m;
        string inWords = ArabicTafqeetHelper.ToWords(amount);
        Assert.Contains("ثلاثة آلاف", inWords);
        Assert.Contains("خمسمائة", inWords);
        Assert.Contains("دينار جزائري", inWords);

        string centimes = ArabicTafqeetHelper.ToPopularCentimes(amount);
        Assert.Equal("350 آلاف سنتيم", centimes);

        decimal twentyFiveThousand = 25000m;
        string millions = ArabicTafqeetHelper.ToPopularCentimes(twentyFiveThousand);
        Assert.Contains("2.5 مليون سنتيم", millions);
    }

    [Fact]
    public async Task TestFabricDecimalRollCuttingDeduction()
    {
        string dbName = $"TestDb_{Guid.NewGuid():N}";
        var factory = new TestDbContextFactory(dbName);
        var barcodeService = new BarcodeService();
        var inventoryService = new InventoryService(factory, barcodeService);

        using (var db = factory.CreateDbContext())
        {
            await db.Database.EnsureCreatedAsync();

            var category = new Category { Name = "أقمشة" };
            await db.Categories.AddAsync(category);
            await db.SaveChangesAsync();

            var product = new Product
            {
                Name = "قماش كريب صيفي فاخر",
                CategoryId = category.Id,
                ProductType = ProductType.FabricByMeter
            };
            await db.Products.AddAsync(product);
            await db.SaveChangesAsync();

            var variant = new ProductVariant
            {
                ProductId = product.Id,
                VariantName = "أزرق ملكي",
                Barcode = "2001234567890",
                RetailPrice = 1200m,
                PurchaseCostPrice = 700m,
                StockQuantity = 0m
            };
            await db.ProductVariants.AddAsync(variant);
            await db.SaveChangesAsync();

            var roll = new FabricRoll
            {
                ProductVariantId = variant.Id,
                RollCode = "ROL-TEST-01",
                InitialMeters = 50.0m,
                RemainingMeters = 50.0m,
                WidthCm = 150m,
                CostPerMeter = 700m
            };
            await inventoryService.AddFabricRollAsync(roll);

            // التحقق من أن المخزون الإجمالي للمتغير أصبح 50 متر
            var loadedVariant = await inventoryService.GetVariantByIdAsync(variant.Id);
            Assert.Equal(50.0m, loadedVariant!.StockQuantity);

            // قص 3.5 متر (قيسة فستان)
            await inventoryService.DeductStockForSaleItemAsync(variant.Id, 3.5m, roll.Id);

            var rolls = await inventoryService.GetAvailableFabricRollsAsync(variant.Id);
            Assert.Single(rolls);
            Assert.Equal(46.5m, rolls[0].RemainingMeters);
        }

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists($"{dbName}.db"))
            File.Delete($"{dbName}.db");
    }

    [Fact]
    public async Task TestPerfumeCompositeBomRecipeDeduction()
    {
        string dbName = $"TestDb_{Guid.NewGuid():N}";
        var factory = new TestDbContextFactory(dbName);
        var barcodeService = new BarcodeService();
        var inventoryService = new InventoryService(factory, barcodeService);

        using (var db = factory.CreateDbContext())
        {
            await db.Database.EnsureCreatedAsync();

            var category = new Category { Name = "عطور تعبئة" };
            await db.Categories.AddAsync(category);
            await db.SaveChangesAsync();

            // 1. مادة خام: زيت عطر فرنسي خام (المخزون: 1000 مل)
            var rawOilProd = new Product { Name = "زيت الرمال الذهبية خام", CategoryId = category.Id };
            await db.Products.AddAsync(rawOilProd);
            await db.SaveChangesAsync();

            var rawOilVariant = new ProductVariant
            {
                ProductId = rawOilProd.Id,
                VariantName = "زيت مركز 100%",
                Barcode = "2000000000017",
                StockQuantity = 1000m
            };
            await db.ProductVariants.AddAsync(rawOilVariant);

            // 2. مادة خام: كحول فرنسي نقي (المخزون: 5000 مل)
            var alcoholProd = new Product { Name = "كحول عطور نقي", CategoryId = category.Id };
            await db.Products.AddAsync(alcoholProd);
            await db.SaveChangesAsync();

            var alcoholVariant = new ProductVariant
            {
                ProductId = alcoholProd.Id,
                VariantName = "كحول نقي",
                Barcode = "2000000000024",
                StockQuantity = 5000m
            };
            await db.ProductVariants.AddAsync(alcoholVariant);

            // 3. مادة خام: قارورة 50 مل زجاجية فارغة (المخزون: 100 قارورة)
            var bottleProd = new Product { Name = "زجاجة 50 مل بخاخ فارغة", CategoryId = category.Id };
            await db.Products.AddAsync(bottleProd);
            await db.SaveChangesAsync();

            var bottleVariant = new ProductVariant
            {
                ProductId = bottleProd.Id,
                VariantName = "قارورة 50 مل فابو",
                Barcode = "2000000000031",
                StockQuantity = 100m
            };
            await db.ProductVariants.AddAsync(bottleVariant);
            await db.SaveChangesAsync();

            // 4. المنتج النهائي المركب: عطر 50 مل تعبئة
            var finishedPerfume = new Product { Name = "عطر الرمال الذهبية 50 مل", CategoryId = category.Id, IsCompositeRecipe = true };
            await db.Products.AddAsync(finishedPerfume);
            await db.SaveChangesAsync();

            var finishedVariant = new ProductVariant
            {
                ProductId = finishedPerfume.Id,
                VariantName = "50 مل تركيز Eau De Parfum",
                Barcode = "2000000000048",
                RetailPrice = 2500m
            };
            await db.ProductVariants.AddAsync(finishedVariant);
            await db.SaveChangesAsync();

            // إعداد وصفة الـ BOM: قارورة واحدة تتطلب 15 مل زيت + 35 مل كحول + 1 زجاجة فارغة
            var recipeItems = new List<CompositeRecipeItem>
            {
                new CompositeRecipeItem { ComponentVariantId = rawOilVariant.Id, QuantityRequired = 15m, UnitOfMeasure = "ml" },
                new CompositeRecipeItem { ComponentVariantId = alcoholVariant.Id, QuantityRequired = 35m, UnitOfMeasure = "ml" },
                new CompositeRecipeItem { ComponentVariantId = bottleVariant.Id, QuantityRequired = 1m, UnitOfMeasure = "piece" }
            };
            await inventoryService.SetCompositeRecipeAsync(finishedVariant.Id, recipeItems);

            // بيع 2 قارورة من العطر المركب
            await inventoryService.DeductStockForSaleItemAsync(finishedVariant.Id, 2m);

            // فحص خصم المواد الأولية:
            // الزيت: 1000 - (15 * 2) = 970 مل
            // الكحول: 5000 - (35 * 2) = 4930 مل
            // القوارير: 100 - (1 * 2) = 98 قارورة
            await db.Entry(rawOilVariant).ReloadAsync();
            await db.Entry(alcoholVariant).ReloadAsync();
            await db.Entry(bottleVariant).ReloadAsync();

            Assert.Equal(970m, rawOilVariant.StockQuantity);
            Assert.Equal(4930m, alcoholVariant.StockQuantity);
            Assert.Equal(98m, bottleVariant.StockQuantity);
        }

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists($"{dbName}.db"))
            File.Delete($"{dbName}.db");
    }

    [Fact]
    public void TestBarcodeEan13Validation()
    {
        var barcodeService = new BarcodeService();

        string generated = barcodeService.GenerateUniqueBarcode("200");
        Assert.Equal(13, generated.Length);

        bool isValid = barcodeService.ValidateEan13(generated);
        Assert.True(isValid);
    }
}
