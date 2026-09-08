using System.Text;

namespace NayliFashion.Services.Helpers;

/// <summary>
/// محرك التفقيط المالي باللغة العربية (تحويل الأرقام إلى نصوص رسمية بالدينار الجزائري والسنتيم)
/// مخصص لفواتير المبيعات، عقود الكراء، وسندات قبض الديون
/// </summary>
public static class ArabicTafqeetHelper
{
    private static readonly string[] Ones =
    {
        "", "واحد", "اثنان", "ثلاثة", "أربعة", "خمسة", "ستة", "سبعة", "ثمانية", "تسعة",
        "عشرة", "أحد عشر", "اثنا عشر", "ثلاثة عشر", "أربعة عشر", "خمسة عشر", "ستة عشر", "سبعة عشر", "ثمانية عشر", "تسعة عشر"
    };

    private static readonly string[] Tens =
    {
        "", "عشرة", "عشرون", "ثلاثون", "أربعون", "خمسون", "ستون", "سبعون", "ثمانون", "تسعون"
    };

    private static readonly string[] Hundreds =
    {
        "", "مائة", "مائتان", "ثلاثمائة", "أربعمائة", "خمسمائة", "ستمائة", "سبعمائة", "ثمانمائة", "تسعمائة"
    };

    /// <summary>
    /// تفقيط المبلغ بالدينار الجزائري مع السنتيم العشري
    /// </summary>
    public static string ToWords(decimal amount, string currencyMain = "دينار جزائري", string currencySub = "سنتيم")
    {
        if (amount == 0)
            return "صفر " + currencyMain;

        long integerPart = (long)Math.Floor(amount);
        int decimalPart = (int)Math.Round((amount - integerPart) * 100);

        var sb = new StringBuilder();
        sb.Append("فقط ");
        sb.Append(ConvertIntegerPart(integerPart));
        sb.Append(" ");
        sb.Append(currencyMain);

        if (decimalPart > 0)
        {
            sb.Append(" و ");
            sb.Append(ConvertIntegerPart(decimalPart));
            sb.Append(" ");
            sb.Append(currencySub);
        }

        sb.Append(" لا غير");
        return sb.ToString();
    }

    /// <summary>
    /// إرجاع القيمة الشعبية التقريبية بالسنتيم الجزائري (للعرض التوضيحي بالكاشير)
    /// مثال: 3,500 دج -> "350 ألف"
    /// 25,000 دج -> "مليونين ونصف"
    /// </summary>
    public static string ToPopularCentimes(decimal amountInDzd)
    {
        decimal centimes = amountInDzd * 100m;

        if (centimes < 1000m)
            return $"{centimes:N0} سنتيم";

        if (centimes < 1000000m)
        {
            decimal thousands = centimes / 1000m;
            return $"{thousands:0.#} آلاف سنتيم";
        }

        decimal millions = centimes / 1000000m;
        return $"{millions:0.##} مليون سنتيم";
    }

    private static string ConvertIntegerPart(long number)
    {
        if (number == 0)
            return "";

        if (number < 0)
            return "سالب " + ConvertIntegerPart(Math.Abs(number));

        var parts = new List<string>();

        // ملايير (Billions)
        if (number >= 1000000000)
        {
            long billions = number / 1000000000;
            parts.Add(FormatUnit(billions, "مليار", "ملياران", "مليارات", "مليار"));
            number %= 1000000000;
        }

        // ملايين (Millions)
        if (number >= 1000000)
        {
            long millions = number / 1000000;
            parts.Add(FormatUnit(millions, "مليون", "مليونان", "ملايين", "مليون"));
            number %= 1000000;
        }

        // آلاف (Thousands)
        if (number >= 1000)
        {
            long thousands = number / 1000;
            parts.Add(FormatUnit(thousands, "ألف", "ألفان", "آلاف", "ألف"));
            number %= 1000;
        }

        // المئات والآحاد والعشرات
        if (number > 0)
        {
            parts.Add(ConvertHundredsAndBelow((int)number));
        }

        return string.Join(" و ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static string FormatUnit(long value, string singular, string dual, string plural, string overTen)
    {
        if (value == 1) return singular;
        if (value == 2) return dual;
        if (value >= 3 && value <= 10) return ConvertHundredsAndBelow((int)value) + " " + plural;
        return ConvertHundredsAndBelow((int)value) + " " + overTen;
    }

    private static string ConvertHundredsAndBelow(int number)
    {
        if (number == 0)
            return "";

        var parts = new List<string>();

        int hundreds = number / 100;
        int remainder = number % 100;

        if (hundreds > 0)
            parts.Add(Hundreds[hundreds]);

        if (remainder > 0)
        {
            if (remainder < 20)
            {
                parts.Add(Ones[remainder]);
            }
            else
            {
                int tens = remainder / 10;
                int ones = remainder % 10;

                if (ones > 0)
                    parts.Add(Ones[ones] + " و " + Tens[tens]);
                else
                    parts.Add(Tens[tens]);
            }
        }

        return string.Join(" و ", parts);
    }
}
