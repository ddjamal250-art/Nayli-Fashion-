namespace NayliFashion.Core.Enums;

/// <summary>
/// التصنيف النوعي للمنتج في النظام لتحديد طريقة البيع والخصم المخزني
/// </summary>
public enum ProductType
{
    ReadyToWearClothing = 1,   // ألبسة جاهزة (مقاس ولون عادي)
    IslamicQamisWear = 2,      // أقمصة صلاة شرعية (طول + عرض)
    FootwearShoes = 3,         // أحذية ومقاسات أوروبية
    FabricByMeter = 4,         // قماش بالمتر العشري والبكرات
    TraditionalGarment = 5,    // ألبسة تراثية جلفاوية (برنوس وڨشابية)
    PerfumeFinishedPack = 6,   // عطر أصلي مغلف جاهز
    PerfumeRawOil = 7,         // زيت عطر خام للتعبئة (بالغرام أو المليلتر)
    PerfumeAlcohol = 8,        // كحول عطور خام (للمخفف والتركيب)
    PerfumeEmptyBottle = 9,    // زجاجات عطور فارغة (فابو، رول، تولة)
    PerfumeComposite = 10,     // عطر مركب بتعبئة المحل (BOM)
    MuskAndIncense = 11,       // مسك وبخور
    Siwak = 12,                // سواك طبيعي
    BeddingAndTrousseau = 13,  // أفرشة ومفارش ومفروشات
    RentalAsset = 14           // قطع مخصصة للكراء فقط
}

/// <summary>
/// حالات دورة حياة أصل الكراء (الفساتين، البدلات، أطقم الأفرشة)
/// </summary>
public enum RentalAssetStatus
{
    AvailableForRent = 1,       // متاح وجاهز في الصالة للكراء
    BookedReserved = 2,         // محجوز لمناسبة قادمة ومثبت بعربون
    RentedOutWithCustomer = 3,  // مؤجر حالياً وموجود مع الزبون
    ReturnedPendingInspection = 4,// تم إرجاعه ويخضع لمعاينة الفحص
    InDryCleaningPressing = 5,  // موجود حالياً في المصبغة للتنظيف والكي
    InMaintenanceRepair = 6,    // قيد الصيانة أو إعادة التطريز
    RetiredDecommissioned = 7   // خرج من الخدمة أو بيع كقطعة مستعملة
}

/// <summary>
/// حالة فحص وجودة القطعة المؤجرة قبل وبعد الاستلام
/// </summary>
public enum ItemConditionGrade
{
    BrandNew = 1,               // جديدة تماماً (أول لبسة)
    ExcellentAsNew = 2,         // ممتازة جداً بدون أي ملاحظات
    GoodMinorWear = 3,          // جيدة مع استهلاك طبيعي خفيف
    StainedNeedsCleaning = 4,   // بها بقع وتحتاج غسيل احترافي عاجل
    DamagedRequiresRepair = 5,  // بها تمزق أو سقوط أحجار تحتاج تصليح
    SeverelyRuined = 6          // تالفة تماماً وتستوجب تعويضاً كاملاً
}

/// <summary>
/// نوع وثيقة الضمان المستلمة في الكراء (العرف الجزائري)
/// </summary>
public enum GuaranteeDocumentType
{
    None = 0,
    BiometricNationalIdCardCNI = 1, // بطاقة التعريف الوطنية البيومترية الأصلية
    DrivingLicensePermis = 2,       // رخصة السياقة الأصلية
    PassportPasseport = 3,          // جواز السفر
    FinancialCashDeposit = 4,       // كفالة أو تأمين مالي نقدي
    ChequeGarantie = 5              // صك ضمان بنكي أو بريدي
}

/// <summary>
/// طرق الدفع المعتمدة محلياً
/// </summary>
public enum PaymentMethod
{
    CashDZD = 1,                // نقداً بالدينار الجزائري (الكاش)
    BaridiMobRIP = 2,           // بريدي موب عبر رمز QR أو تحويل RIP
    BankCardTPE = 3,            // بطاقة الذهبية / CIB عبر جهاز الدفع TPE
    PostalCheckChèque = 4,       // صك بريدي أو بنكي
    CustomerDebtCarnet = 5,     // على الحساب (كريدي / في الكارني)
    SplitPayment = 6            // دفع مركب (كاش + بريدي موب + كريدي)
}

/// <summary>
/// نوع الفاتورة الصادرة
/// </summary>
public enum InvoiceType
{
    RetailSale = 1,             // بيع بالتجزئة
    WholesaleSale = 2,          // بيع بالجملة
    CustomerReturn = 3          // مرتجع مبيعات واسترداد
}

/// <summary>
/// حالة وردية الصندوق (الكاشير)
/// </summary>
public enum CashShiftStatus
{
    OpenActive = 1,             // وردية مفتوحة نشطة
    ClosedReconciled = 2        // وردية مغلقة ومطابقة محاسبياً
}

/// <summary>
/// أسباب تسوية المخزون والفروقات
/// </summary>
public enum StockAdjustmentReason
{
    PhysicalStocktakeVariance = 1, // فرق جرد دوري (عجز أو زيادة)
    FabricCutEndWaste = 2,         // هالك قص نهايات أقمشة
    PerfumeEvaporationLeakage = 3, // تبخر طبيعي أو تسريب في زيوت العطور
    DamagedSpoiledGoods = 4,       // بضاعة تالفة أو تعرضت لماء/غبار
    GiftOrSamplePromotion = 5,     // عينات تسويقية مجانية
    InitialStockSetup = 6          // رصيد افتتاحي للمخزن
}

/// <summary>
/// أدوار المستخدمين وصلاحياتهم
/// </summary>
public enum UserRole
{
    SuperAdmin = 1,             // مدير النظام (كامل الصلاحيات)
    StoreManager = 2,           // مسؤول المحل (إدارة المخزون والمشتريات والتقارير)
    Cashier = 3                 // كاشير (نقطة البيع، الكراء، والقبض فقط)
}
