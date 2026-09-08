namespace NayliFashion.Core.Enums;

/// <summary>
/// المقاسات الأوروبية القياسية للأحذية (مع أنصاف المقاسات وأحذية الأطفال)
/// </summary>
public enum FootwearSizeEu
{
    None = 0,
    // مقاسات المواليد والأطفال
    Size19 = 19,
    Size20 = 20,
    Size21 = 21,
    Size22 = 22,
    Size23 = 23,
    Size24 = 24,
    Size25 = 25,
    Size26 = 26,
    Size27 = 27,
    Size28 = 28,
    Size29 = 29,
    Size30 = 30,
    Size31 = 31,
    Size32 = 32,
    Size33 = 33,
    Size34 = 34,

    // مقاسات الكبار (رجال، نساء، مراهقين)
    Size35 = 35,
    Size36 = 36,
    Size37 = 37,
    Size38 = 38,
    Size39 = 39,
    Size39_5 = 395,
    Size40 = 40,
    Size40_5 = 405,
    Size41 = 41,
    Size41_5 = 415,
    Size42 = 42,
    Size42_5 = 425,
    Size43 = 43,
    Size43_5 = 435,
    Size44 = 44,
    Size44_5 = 445,
    Size45 = 45,
    Size46 = 46,
    Size47 = 47,
    Size48 = 48
}

/// <summary>
/// الفئة المستهدفة للأحذية
/// </summary>
public enum FootwearGender
{
    Unisex = 1,
    Men = 2,
    Women = 3,
    Boys = 4,
    Girls = 5,
    Baby = 6
}

/// <summary>
/// تصنيفات الأحذية الشائعة بالسوق الجزائري
/// </summary>
public enum FootwearType
{
    ClassicShoe = 1,           // حذاء كلاسيك رسمي (صباط)
    SneakersBasket = 2,        // حذاء رياضي (باسكات)
    TraditionalBalgha = 3,     // بلغة / شربيل تقليدي
    SummerSandal = 4,          // صندالة صيفية
    HomeSlippers = 5,          // شبشب منزلي (كلاكيت/بانتوفة)
    WinterBoots = 6,           // بوتين شتوي
    MedicalOrthopedic = 7      // حذاء طبي
}

/// <summary>
/// نوع خامة الحذاء
/// </summary>
public enum FootwearMaterial
{
    GenuineLeather = 1,        // جلد طبيعي أصلي (Cuir véritable)
    SyntheticLeather = 2,      // جلد صناعي (سيميلي)
    SuedeNubuck = 3,           // شمواه / نوبوك
    CanvasFabric = 4,          // قماش قطني
    RubberPVC = 5              // مطاط / بلاستيك
}
