using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Catalog;
using NayliFashion.Core.Models.Customers;
using NayliFashion.Core.Models.Finance;
using NayliFashion.Core.Models.Purchasing;
using NayliFashion.Core.Models.Rentals;
using NayliFashion.Core.Models.Sales;
using NayliFashion.Data.Context;
using NayliFashion.Data.Seed;
using NayliFashion.Services.DTOs;
using NayliFashion.Services.Helpers;
using NayliFashion.Services.Implementations;
using Xunit;

namespace NayliFashion.Tests;

/// <summary>
/// حزمة اختبارات المحاكاة الشاملة لجميع سيناريوهات الاستخدام الفعلي والضغط وحالات الحافة
/// (Exhaustive QA Simulation & Edge Case Testing)
/// </summary>
public class ExhaustiveSimulationTests
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
    public async Task RunFullSystemSimulationScenario()
    {
        string testDbName = $"SimDb_{Guid.NewGuid():N}";
        var factory = new TestDbContextFactory(testDbName);

        // 1. تهيئة قاعدة البيانات وزرع البيانات الأولية
        using (var initDb = factory.CreateDbContext())
        {
            await DatabaseInitializer.InitializeDatabaseAsync(initDb);
        }

        // تهيئة الخدمات
        var barcodeService = new BarcodeService();
        var backupService = new BackupService();
        var authService = new AuthService(factory);
        var inventoryService = new InventoryService(factory, barcodeService);
        var posService = new PosService(factory, inventoryService);
        var rentalService = new RentalService(factory);
        var cashShiftService = new CashShiftService(factory);
        var customerService = new CustomerService(factory);
        var purchasingService = new PurchasingService(factory);
        var printerService = new ReceiptPrinterService(factory);

        // =========================================================================
        // الاختبار 1: المصادقة وتسجيل الدخول وتغيير كلمة المرور
        // =========================================================================
        var loggedInUser = await authService.LoginAsync("admin", "admin123");
        Assert.NotNull(loggedInUser);
        Assert.Equal(UserRole.SuperAdmin, loggedInUser.Role);

        // محاولة دخول فاشلة
        var badLogin = await authService.LoginAsync("admin", "wrongpassword");
        Assert.Null(badLogin);

        // =========================================================================
        // الاختبار 2: فتح وردية الصندوق اليومية (Shift Management)
        // =========================================================================
        decimal openingFloat = 5000m; // 5000 دج عهدة
        var shift = await cashShiftService.OpenShiftAsync(loggedInUser.Id, openingFloat);
        Assert.NotNull(shift);
        Assert.Equal(CashShiftStatus.OpenActive, shift.Status);
        Assert.Equal(openingFloat, shift.OpeningFloatBalanceDzd);

        // تسجيل مصروف نثري من الدرج (مصبغة 1500 دج)
        await cashShiftService.RecordExpenseAsync(shift.Id, loggedInUser.Id, "مصبغة فساتين", 1500m, "مصبغة النور", "تنظيف فستان عرس");

        // =========================================================================
        // الاختبار 3: المشتريات والموردين ومتوسط التكلفة المرجح (WAC)
        // =========================================================================
        var supplier = await purchasingService.CreateSupplierAsync(new Supplier
        {
            CompanyName = "شركة الأنسجة الراقية - العلمة",
            ContactPerson = "الحاج بلقاسم",
            PhoneNumber = "0661000000",
            AddressCity = "سطيف / العلمة"
        });
        Assert.True(supplier.Id > 0);

        // إنشاء منتج قماش بالمتر
        var fabricCategory = (await inventoryService.GetAllCategoriesAsync()).First(c => c.Name.Contains("أقمشة"));
        var fabricProduct = await inventoryService.CreateProductAsync(new Product
        {
            Name = "قماش حرير نايلي مطرز",
            CategoryId = fabricCategory.Id,
            ProductType = ProductType.FabricByMeter
        });

        var fabricVariant = await inventoryService.CreateVariantAsync(new ProductVariant
        {
            ProductId = fabricProduct.Id,
            VariantName = "أبيض سكري / عرض 150 سم",
            RetailPrice = 2500m,
            WholesalePrice = 2100m,
            PurchaseCostPrice = 1500m,
            StockQuantity = 0m
        });

        // توريد بضاعة: شراء طاقة قماش 50 متر بسعر 1400 دج
        var purchaseInvoice = new PurchaseInvoice
        {
            SupplierId = supplier.Id,
            UserId = loggedInUser.Id,
            TotalGrossAmount = 70000m,
            NetTotalAmount = 70000m,
            PaidAmount = 30000m, // دفعنا له 30000 دج كاش وبقي 40000 دج دين
            Items = new List<PurchaseItem>
            {
                new PurchaseItem
                {
                    ProductVariantId = fabricVariant.Id,
                    QuantityPurchased = 50m,
                    UnitPurchasePrice = 1400m,
                    TotalLineAmount = 70000m
                }
            }
        };

        var createdPI = await purchasingService.CreatePurchaseInvoiceAsync(purchaseInvoice);
        Assert.Equal(40000m, createdPI.RemainingDebtAmount);

        // التحقق من حساب متوسط التكلفة WAC
        var loadedFabricVariant = await inventoryService.GetVariantByIdAsync(fabricVariant.Id);
        Assert.Equal(50m, loadedFabricVariant!.StockQuantity);
        Assert.Equal(1400m, loadedFabricVariant.PurchaseCostPrice);

        // إضافة طاقة قماش مسجلة بالبكرة
        var roll = await inventoryService.AddFabricRollAsync(new FabricRoll
        {
            ProductVariantId = fabricVariant.Id,
            RollCode = "ROL-SIM-001",
            InitialMeters = 50m,
            RemainingMeters = 50m,
            WidthCm = 150m,
            CostPerMeter = 1400m
        });

        // =========================================================================
        // الاختبار 4: تركيب عطور التعبئة والـ BOM
        // =========================================================================
        var perfumeCategory = (await inventoryService.GetAllCategoriesAsync()).First(c => c.Name.Contains("عطور"));

        var rawOil = await inventoryService.CreateProductAsync(new Product { Name = "زيت مسك الغزال خام", CategoryId = perfumeCategory.Id });
        var rawOilVar = await inventoryService.CreateVariantAsync(new ProductVariant { ProductId = rawOil.Id, VariantName = "زيت 100%", StockQuantity = 500m, PurchaseCostPrice = 30m, RetailPrice = 60m });

        var alcohol = await inventoryService.CreateProductAsync(new Product { Name = "كحول عطور خام", CategoryId = perfumeCategory.Id });
        var alcoholVar = await inventoryService.CreateVariantAsync(new ProductVariant { ProductId = alcohol.Id, VariantName = "كحول فرنسي", StockQuantity = 2000m, PurchaseCostPrice = 2m, RetailPrice = 5m });

        var emptyBottle = await inventoryService.CreateProductAsync(new Product { Name = "زجاجة بخاخ 50 مل فارغة", CategoryId = perfumeCategory.Id });
        var emptyBottleVar = await inventoryService.CreateVariantAsync(new ProductVariant { ProductId = emptyBottle.Id, VariantName = "زجاجة كرستال", StockQuantity = 50m, PurchaseCostPrice = 150m, RetailPrice = 250m });

        var compositePerfume = await inventoryService.CreateProductAsync(new Product { Name = "عطر مسك الغزال 50 مل تعبئة", CategoryId = perfumeCategory.Id, IsCompositeRecipe = true });
        var compositePerfumeVar = await inventoryService.CreateVariantAsync(new ProductVariant { ProductId = compositePerfume.Id, VariantName = "50 مل بخاخ", RetailPrice = 1800m, PurchaseCostPrice = 670m });

        await inventoryService.SetCompositeRecipeAsync(compositePerfumeVar.Id, new List<CompositeRecipeItem>
        {
            new CompositeRecipeItem { ComponentVariantId = rawOilVar.Id, QuantityRequired = 15m, UnitOfMeasure = "ml" },
            new CompositeRecipeItem { ComponentVariantId = alcoholVar.Id, QuantityRequired = 35m, UnitOfMeasure = "ml" },
            new CompositeRecipeItem { ComponentVariantId = emptyBottleVar.Id, QuantityRequired = 1m, UnitOfMeasure = "piece" }
        });

        // =========================================================================
        // الاختبار 5: العملاء وسقف الائتمان ومواعيد الفيرمون
        // =========================================================================
        var customer = await customerService.CreateCustomerAsync(new Customer
        {
            FullName = "عمر",
            FamilyName = "النايلي",
            Nickname = "أبو فاروق",
            PhoneNumber = "0662000000",
            AddressNeighborhood = "حي برنوس، الجلفة",
            MaxCreditLimitDzd = 20000m, // سقف الدين 20 ألف دج
            SalaryVirementDay = DateTime.UtcNow.Day // موعد راتبه اليوم
        });

        // فحص ظهور العميل في تنبيهات الفيرمون
        var dueCustomers = await customerService.GetCustomersWithApproachingSalaryDueAsync(3);
        // لا دين عليه حالياً فلن يظهر حتى يصبح مديناً
        Assert.DoesNotContain(dueCustomers, c => c.Id == customer.Id);

        // =========================================================================
        // الاختبار 6: عمليات نقطة البيع (الكاشير، الخصم، الأقمشة، العطور، والكريدي)
        // =========================================================================
        var cartItems = new List<CartItemDto>
        {
            // 1. بيع 3.5 متر قماش من البكرة
            new CartItemDto
            {
                ProductVariantId = fabricVariant.Id,
                ProductName = fabricProduct.Name,
                VariantName = fabricVariant.VariantName,
                Quantity = 3.5m,
                UnitPrice = fabricVariant.RetailPrice,
                UnitCostPrice = fabricVariant.PurchaseCostPrice,
                SelectedFabricRollId = roll.Id
            },
            // 2. بيع قارورة عطر تعبئة 50 مل
            new CartItemDto
            {
                ProductVariantId = compositePerfumeVar.Id,
                ProductName = compositePerfume.Name,
                VariantName = compositePerfumeVar.VariantName,
                Quantity = 1.0m,
                UnitPrice = compositePerfumeVar.RetailPrice,
                UnitCostPrice = compositePerfumeVar.PurchaseCostPrice
            }
        };

        // حساب الملخص: 3.5 * 2500 = 8750 + 1800 = 10550 دج
        var summary = posService.CalculateSummary(cartItems, overallDiscount: 550m);
        Assert.Equal(10000m, summary.NetTotalDzd);
        Assert.Equal("1 مليون سنتيم", ArabicTafqeetHelper.ToPopularCentimes(summary.NetTotalDzd));

        // تجربة البيع بالدين: دفع 3000 دج كاش وباقي 7000 دج دين على العميل
        var checkoutRequest = new CheckoutRequestDto
        {
            Items = cartItems,
            CustomerId = customer.Id,
            CashierUserId = loggedInUser.Id,
            ActiveCashShiftId = shift.Id,
            PaymentMethod = PaymentMethod.CustomerDebtCarnet,
            OverallInvoiceDiscount = 550m,
            PaidAmount = 3000m
        };

        var checkoutResult = await posService.ProcessCheckoutAsync(checkoutRequest);
        Assert.True(checkoutResult.IsSuccess);
        Assert.Equal(7000m, checkoutResult.RemainingDebtDzd);
        Assert.Equal(3000m, checkoutResult.PaidAmountDzd);

        // التحقق من تزايد دين العميل وتحديث كشف الحساب
        var updatedCustomer = await customerService.GetCustomerByIdAsync(customer.Id);
        Assert.Equal(7000m, updatedCustomer!.CurrentDebtDzd);

        // الآن بما أن لديه دين وموعد الفيرمون حان، يجب أن يظهر في قائمة التنبيهات
        var virementAlerts = await customerService.GetCustomersWithApproachingSalaryDueAsync(3);
        Assert.Contains(virementAlerts, c => c.Id == customer.Id);

        // التحقق من خصم طاقة القماش بدقة (50 - 3.5 = 46.5)
        var updatedRolls = await inventoryService.GetAvailableFabricRollsAsync(fabricVariant.Id);
        Assert.Equal(46.5m, updatedRolls.First().RemainingMeters);

        // التحقق من خصم المواد الخام للعطر:
        // الزيت: 500 - 15 = 485 مل
        // الكحول: 2000 - 35 = 1965 مل
        // القارورة: 50 - 1 = 49 قارورة
        var checkOil = await inventoryService.GetVariantByIdAsync(rawOilVar.Id);
        var checkAlcohol = await inventoryService.GetVariantByIdAsync(alcoholVar.Id);
        var checkBottle = await inventoryService.GetVariantByIdAsync(emptyBottleVar.Id);

        Assert.Equal(485m, checkOil!.StockQuantity);
        Assert.Equal(1965m, checkAlcohol!.StockQuantity);
        Assert.Equal(49m, checkBottle!.StockQuantity);

        // =========================================================================
        // الاختبار 7: سداد دين الزبون (سند قبض)
        // =========================================================================
        var debtPayment = await customerService.ProcessDebtPaymentAsync(customer.Id, 2000m, loggedInUser.Id, shift.Id, "سداد دفعة نقدية عند الصندوق");
        Assert.Equal(5000m, debtPayment.BalanceAfter);

        var finalCustomer = await customerService.GetCustomerByIdAsync(customer.Id);
        Assert.Equal(5000m, finalCustomer!.CurrentDebtDzd);

        // =========================================================================
        // الاختبار 8: نظام كراء فساتين الأعراس، فحص التوفر، والضمان وغرامات التأخير
        // =========================================================================
        var bridalCategory = (await inventoryService.GetAllCategoriesAsync()).First(c => c.Name.Contains("كراء"));
        var dressProduct = await inventoryService.CreateProductAsync(new Product { Name = "فستان نايلي أصيل مطرز حر حرير", CategoryId = bridalCategory.Id, ProductType = ProductType.RentalAsset });
        var dressVariant = await inventoryService.CreateVariantAsync(new ProductVariant { ProductId = dressProduct.Id, VariantName = "أبيض عرائسي - مقاس 42", RentalDailyRate = 8000m });

        var dressAsset = await rentalService.RegisterNewRentalAssetAsync(new RentalAssetItem
        {
            ProductVariantId = dressVariant.Id,
            AssetSerialTag = "TAG-RN-NAYLI-01",
            AssetNameDescription = "فستان نايلي أبيض حرير - القطعة رقم 1",
            Status = RentalAssetStatus.AvailableForRent,
            CurrentCondition = ItemConditionGrade.BrandNew,
            StorageLocker = "خزانة العرائس 01"
        });

        // التحقق من توفر القطعة لموعد قادم
        DateTime eventStart = DateTime.UtcNow.Date.AddDays(2);
        DateTime returnDate = DateTime.UtcNow.Date.AddDays(5);
        bool isAvail = await rentalService.IsAssetAvailableForDatesAsync(dressAsset.Id, eventStart, returnDate);
        Assert.True(isAvail);

        // إنشاء عقد كراء مع رهن بطاقة التعريف الوطنية البيومترية
        var rentalOrder = await rentalService.CreateRentalOrderAsync(new RentalCheckoutRequestDto
        {
            CustomerId = customer.Id,
            CashierUserId = loggedInUser.Id,
            ActiveCashShiftId = shift.Id,
            SelectedRentalAssetIds = new List<int> { dressAsset.Id },
            EventStartDate = eventStart,
            ExpectedReturnDate = returnDate,
            TotalRentFeeDzd = 12000m,
            AdvancePaidDzd = 4000m,
            GuaranteeType = GuaranteeDocumentType.BiometricNationalIdCardCNI,
            GuaranteeDocumentNumber = "112233445566",
            GuaranteeSafeLocation = "الخزنة الفولاذية رقم 1 - ملف الأعراس"
        });

        Assert.Equal(8000m, rentalOrder.RemainingRentFeeDzd);

        // فحص أن القطعة أصبحت غير متاحة في نفس التاريخ
        bool isAvailNow = await rentalService.IsAssetAvailableForDatesAsync(dressAsset.Id, eventStart, returnDate);
        Assert.False(isAvailNow);

        // محاكاة إرجاع الفستان بعد موعده بيومين مع ضرر خفيف وإرساله للمصبغة
        DateTime lateReturnDate = returnDate.AddDays(2);
        var returnInspection = new RentalReturnInspectionDto
        {
            RentalOrderId = rentalOrder.Id,
            CashierUserId = loggedInUser.Id,
            ActiveCashShiftId = shift.Id,
            ActualReturnDate = lateReturnDate,
            AdditionalDamageCostDzd = 500m,
            AdditionalDryCleaningFeeDzd = 800m,
            ReturnGuaranteeDocumentToCustomer = true,
            AmountPaidByCustomerOnReturn = 11300m, // 8000 متبقي + 2000 غرامة تأخير يومين + 500 ضرر + 800 مصبغة
            ReturnedItems = new List<RentalItemReturnStateDto>
            {
                new RentalItemReturnStateDto
                {
                    RentalItemId = rentalOrder.RentalItems.First().Id,
                    ConditionAtReturn = ItemConditionGrade.StainedNeedsCleaning,
                    SendDirectlyToDryCleaning = true
                }
            }
        };

        var settledOrder = await rentalService.ProcessReturnInspectionAsync(returnInspection);
        Assert.True(settledOrder.IsOrderCompleted);
        Assert.Equal(2000m, settledOrder.CalculatedLateFeesDzd); // 2 يوم * 1000 دج
        Assert.True(settledOrder.IsGuaranteeReturnedToCustomer);

        // فحص أن حالة القطعة تحولت تلقائياً إلى "في المصبغة"
        var inspectedAsset = await rentalService.GetRentalAssetByTagAsync("TAG-RN-NAYLI-01");
        Assert.Equal(RentalAssetStatus.InDryCleaningPressing, inspectedAsset!.Status);

        // =========================================================================
        // الاختبار 9: إغلاق الوردية والمطابقة المحاسبية Z-Report
        // =========================================================================
        // الحسابات المتوقعة بالدرج:
        // العهدة الافتتاحية: 5000 دج
        // - المصروفات النثرية: 1500 دج
        // + مبيعات الكاش من الفاتورة (دفعة الكارني): 3000 دج
        // + تحصيل دين الكارني (سند قبض): 2000 دج
        // + عربون الكراء المستلم نقداً: 4000 دج
        // + تسوية الكراء المستلمة نقداً عند الإرجاع: 11300 دج
        // الإجمالي المتوقع بالدرج = 5000 - 1500 + 3000 + 2000 + 4000 + 11300 = 23800 دج
        var summaryZ = await cashShiftService.GenerateShiftSummaryAsync(shift.Id);
        Assert.Equal(23800m, summaryZ.ExpectedCashInDrawerDzd);

        // إغلاق الصندوق فعلياً بعد العد (الكاشير وجد 23800 دج مطابقة 100%)
        var zReport = await cashShiftService.CloseShiftAsync(shift.Id, 23800m, "إغلاق وردية ممتاز - مطابق تماماً");
        Assert.Equal(0m, zReport.VarianceDifferenceDzd);

        // =========================================================================
        // الاختبار 10: النسخ الاحتياطي التلقائي
        // =========================================================================
        // التحقق من خلو ملفات الاختبار وإغلاق الاتصالات
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists($"{testDbName}.db"))
            File.Delete($"{testDbName}.db");
    }
}
