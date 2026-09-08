namespace NayliFashion.Core.Enums;

/// <summary>
/// الأبعاد المعيارية للأفرشة وأسرّة النوم (بالسانتيمتر)
/// </summary>
public enum BeddingDimensionStandard
{
    Custom = 0,
    Single_90x190 = 1,         // سرير فردي (1 بلاصة)
    SingleLarge_120x200 = 2,    // سرير فردي عريض (بلاصة ونص)
    DoubleStandard_140x190 = 3, // سرير مزدوج كلاسيكي (2 بلايص)
    QueenSize_160x200 = 4,     // كوين سايز
    KingSize_180x200 = 5,      // كينغ سايز
    SuperKing_200x220 = 6,     // سوبر كينغ (أفرشة الأعراس الفاخرة)
    BridalSpread_220x240 = 7,  // غطاء عرائسي واسع
    LuxurySpread_240x260 = 8   // لحاف فندقي واسع جداً
}

/// <summary>
/// نوع منتج الأفرشة ومفارش العرائس
/// </summary>
public enum BeddingPieceType
{
    BridalTrousseauComplete = 1,// جهاز العروسة المتكامل (طقم فاخر 6 إلى 12 قطعة)
    DuvetCoverCouvreLit = 2,    // كوفريلي ومفرش سرير مطرز
    FittedSheetDrapHousse = 3,  // درا أوس (شرشف مطاط)
    FlatSheetDrapPlat = 4,      // درا عادي مسطح
    PillowcasePair = 5,         // طقم وسائد (2 مخاد)
    WinterBlanketCouverture = 6,// بطانية شتوية ثقيلة (كوفيرطة)
    SummerQuiltBoutis = 7,      // لحاف صيفي خفيف (بوتيس)
    OrthopedicMattressMatlas = 8 // ماطلا طبية
}
