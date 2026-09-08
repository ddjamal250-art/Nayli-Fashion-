namespace NayliFashion.Core.Enums;

/// <summary>
/// الأحجام القياسية للعطور والزيوت والتعبئة
/// </summary>
public enum PerfumeVolumeStandard
{
    CustomFreeMl = 0,          // بالمليلتر الحر
    CustomFreeGrams = 1,       // بالغرام الحر على الميزان
    QuarterTola = 2,           // ربع تولة (~3 مل / 2.9 غرام)
    HalfTola = 3,              // نصف تولة (~6 مل / 5.8 غرام)
    OneTola = 4,               // تولة كاملة (~12 مل / 11.66 غرام)
    Bottle15ml = 15,           // زجاجة 15 مل
    Bottle30ml = 30,           // زجاجة 30 مل
    Bottle50ml = 50,           // زجاجة 50 مل
    Bottle75ml = 75,           // زجاجة 75 مل
    Bottle100ml = 100,         // زجاجة 100 مل
    Bottle125ml = 125,         // زجاجة 125 مل
    Bottle200ml = 200,         // زجاجة 200 مل
    Liter1000ml = 1000         // لتر زيت خام / كحول (للمخازن والمواد الأولية)
}

/// <summary>
/// درجات تركيز العطور المعيارية عالمياً
/// </summary>
public enum PerfumeConcentration
{
    PureOil100 = 1,            // زيت مركز خام 100% (بدون كحول)
    ExtraitDeParfum = 2,       // خلاصة عطر فاخرة (20% - 40% زيت)
    EauDeParfum = 3,           // ماء عطر مركز (15% - 20% زيت)
    EauDeToilette = 4,         // ماء تواليت خفيف (5% - 15% زيت)
    EauDeCologne = 5,          // كولونيا منعشة (2% - 4% زيت)
    EauFraiche = 6             // ماء منعش خفيف جداً (1% - 3%)
}

/// <summary>
/// نوع زجاجة / عبوة العطر في التعبئة
/// </summary>
public enum PerfumeBottleType
{
    SprayVaporisateur = 1,     // بخاخ (رشاش)
    RollOn = 2,                // رول دوار (شائع للزيوت والمسك)
    DropperTola = 3,           // مرود زجاجي / قطارة
    PocketSpray = 4,           // بخاخ جيب صغير (قلم)
    AmberGlassStorage = 5      // قارورة عنبرية لحفظ الزيوت الخام
}

/// <summary>
/// أنواع المسك والبخور
/// </summary>
public enum MuskType
{
    WhiteTaharaMusk = 1,       // مسك الطهارة الأبيض الأصلي
    BlackRoyalMusk = 2,        // المسك الأسود الملكي
    AmberMusk = 3,             // مسك العنبر
    PomegranateMusk = 4,       // مسك الرمان المنعش
    StoneCubesMusk = 5,        // مكعبات مسك جامد (حجر)
    NaturalIncenseOud = 6      // عود وبخور طبيعي
}
