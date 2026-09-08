namespace NayliFashion.Core.Enums;

/// <summary>
/// المقاسات العالمية بالحروف للألبسة
/// </summary>
public enum ApparelStandardSize
{
    None = 0,
    XXS = 1,
    XS = 2,
    S = 3,
    M = 4,
    L = 5,
    XL = 6,
    XXL = 7,
    XXXL = 8,
    Size4XL = 9,
    Size5XL = 10,
    OneSizeStandard = 11 // مقاس موحد (Standard)
}

/// <summary>
/// المقاسات الرقمية للألبسة والبدلات والفساتين
/// </summary>
public enum ApparelNumericSize
{
    None = 0,
    Size34 = 34,
    Size36 = 36,
    Size38 = 38,
    Size40 = 40,
    Size42 = 42,
    Size44 = 44,
    Size46 = 46,
    Size48 = 48,
    Size50 = 50,
    Size52 = 52,
    Size54 = 54,
    Size56 = 56,
    Size58 = 58,
    Size60 = 60
}

/// <summary>
/// مقاسات الطول لأقمصة الصلاة (بالإنش من الكتف إلى الكاحل)
/// معايير: الدفة، الأصيل، الحرمين، الإماراتي والسعودي
/// </summary>
public enum QamisLength
{
    None = 0,
    Length48 = 48,
    Length50 = 50,
    Length52 = 52,
    Length54 = 54,
    Length56 = 56,
    Length58 = 58,
    Length60 = 60,
    Length62 = 62,
    Length64 = 64
}

/// <summary>
/// مقاسات عرض الصدر لأقمصة الصلاة
/// </summary>
public enum QamisChestWidth
{
    None = 0,
    Small = 1,
    Medium = 2,
    Large = 3,
    XLarge = 4,
    XXLarge = 5,
    XXXLarge = 6,
    Width20 = 20,
    Width22 = 22,
    Width24 = 24,
    Width26 = 26,
    Width28 = 28
}

/// <summary>
/// نمط ياقة قميص الصلاة
/// </summary>
public enum QamisCollarType
{
    None = 0,
    RoundMandarin = 1, // ياقة دائرية سعودية سادة
    TurnDownShirt = 2, // ياقة قميص رسمية قلاب
    VNeck = 3,         // ياقة مغربية سبعة
    Embroidered = 4     // ياقة مطرزة يدوي أو ميكانيكي
}

/// <summary>
/// نمط أكمام قميص الصلاة
/// </summary>
public enum QamisSleeveType
{
    PlainOpen = 1,     // كم عادي مفتوح
    CuffLinks = 2,     // كم كبك للأزرار الفاخرة
    ElasticCuff = 3,   // كم مطاط
    ShortSleeve = 4    // نصف كم صيفي
}
