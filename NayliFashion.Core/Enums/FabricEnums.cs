namespace NayliFashion.Core.Enums;

/// <summary>
/// عروض الأقمشة القياسية في التجارة (عرض القماش بالسانتيمتر)
/// </summary>
public enum FabricWidthStandard
{
    CustomWidth = 0,
    Width90cm = 90,
    Width110cm = 110,
    Width140cm = 140,          // العرض الكلاسيكي للألبسة والبدلات
    Width150cm = 150,          // العرض الأكثر انتشاراً في الأقمشة
    Width180cm = 180,
    Width280cm = 280,          // عرض مزدوج للأفرشة والستائر واللحف
    Width300cm = 300           // عرض 3 أمتار لأفرشة الأعراس الفاخرة
}

/// <summary>
/// خامات الأقمشة والصوف والوبر
/// </summary>
public enum FabricMaterialType
{
    Cotton = 1,                // قطن طبيعي
    Silk = 2,                  // حرير طبيعي أو صناعي
    Linen = 3,                 // كتان / لينين
    Wool = 4,                  // صوف عادي
    Polyester = 5,             // بوليستر
    VelvetMobar = 6,           // مخمل / موبرة (للجباب والأفرشة)
    Chiffon = 7,               // شيفون خفيف
    Satin = 8,                 // ساتان حريري
    Denim = 9,                 // جينز
    Crepe = 10,                // كريب
    Cashmere = 11,             // كشمير فاخر
    Georgette = 12,            // جورجيت
    Organza = 13,              // أورجانزا
    PureCamelHairOuaber = 14,  // وبر الإبل الجلفاوي الحر الصافي
    TraditionalWoolSouf = 15   // صوف جلفاوي مغزول تقليدياً
}

/// <summary>
/// تصنيف جودة وحياكة القطع التراثية الجلفاوية (البرنوس والڨشابية)
/// </summary>
public enum TraditionalGarmentGrade
{
    NotTraditional = 0,
    PureCamelOuaberGrade1 = 1, // وبر إبل خالص نقي 100% درجة أولى
    CamelOuaberGrade2 = 2,     // وبر إبل درجة ثانية
    CamelAndWoolBlend = 3,     // خليط وبر وصوف
    PureSheepWool = 4,         // صوف غنم نقي
    HandWovenDjelfaOriginal = 5,// حياكة يدوية تقليدية أصلية (منطقة الجلفة)
    IndustrialMachineWoven = 6 // نسيج ميكانيكي حديث
}

/// <summary>
/// القيسات الجاهزة الشائعة في محلات الأقمشة الجزائرية
/// </summary>
public enum PrecutFabricCoupe
{
    CustomMeter = 0,           // بيع حر بالأمتار العشرية
    Coupe2_5m = 250,           // قيسة 2.5 متر
    Coupe3_0m = 300,           // قيسة 3 أمتار (الجبة والفستان الكلاسيكي)
    Coupe3_5m = 350,           // قيسة 3.5 أمتار (بلوزة وقندورة واسعة)
    Coupe4_0m = 400,           // قيسة 4 أمتار (أطقم وتطريز كامل)
    SuitPiece3_2m = 320        // قيسة بدلة رجالية رسمية 3.20 متر
}
