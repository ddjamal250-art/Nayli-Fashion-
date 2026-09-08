using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Common;
using NayliFashion.Core.Models.Rentals;

namespace NayliFashion.Core.Models.Catalog;

/// <summary>
/// النسخة الفرعية للمنتج (Product Variant) التي تحمل المقاس واللون والباركود الفريد والكمية الدقيقة
/// </summary>
public class ProductVariant : BaseEntity
{
    public int ProductId { get; set; }
    public virtual Product Product { get; set; } = null!;

    public string VariantName { get; set; } = string.Empty; // مثال: "مقاس L / كحلي" أو "50 مل / بخاخ"
    public string Barcode { get; set; } = string.Empty;     // باركود فريد يقرأ مباشرة بالكاشير
    public string? Sku { get; set; }                        // رمز تعريفي داخلي
    public string? ImagePath { get; set; }                  // مسار صورة هذا المتغير محلياً

    // --- معايير الألبسة والأحذية ---
    public ApparelStandardSize ApparelSize { get; set; } = ApparelStandardSize.None;
    public ApparelNumericSize NumericSize { get; set; } = ApparelNumericSize.None;
    public FootwearSizeEu ShoeSizeEu { get; set; } = FootwearSizeEu.None;

    // --- معايير أقمشة وأثواب الصلاة (المقاس المزدوج) ---
    public QamisLength QamisLength { get; set; } = QamisLength.None;
    public QamisChestWidth QamisWidth { get; set; } = QamisChestWidth.None;
    public QamisCollarType QamisCollar { get; set; } = QamisCollarType.None;
    public QamisSleeveType QamisSleeve { get; set; } = QamisSleeveType.PlainOpen;

    // --- معايير العطور والزيوت والمسك ---
    public PerfumeVolumeStandard PerfumeVolume { get; set; } = PerfumeVolumeStandard.CustomFreeMl;
    public PerfumeConcentration PerfumeConcentration { get; set; } = PerfumeConcentration.EauDeParfum;
    public PerfumeBottleType BottleType { get; set; } = PerfumeBottleType.SprayVaporisateur;
    public decimal? VolumeInMl { get; set; }               // الحجم بالمليلتر بدقة (مثل 12.5 مل)
    public decimal? WeightInGrams { get; set; }            // الوزن بالغرام بدقة (مثل 15.8 غرام)

    // --- معايير الأفرشة ---
    public BeddingDimensionStandard BeddingDimension { get; set; } = BeddingDimensionStandard.Custom;

    // --- معايير اللون والواجهة ---
    public string? ColorName { get; set; }                 // اسم اللون (مثل: أبيض ناصع، وبري، كحلي)
    public string? ColorHexCode { get; set; }              // كود اللون الست عشري للعرض في الواجهة (مثل: #FFFFFF)

    // --- الأسعار (بالدينار الجزائري DZD) ---
    public decimal PurchaseCostPrice { get; set; } = 0m;   // سعر الشراء أو متوسط التكلفة المرجح WAC
    public decimal RetailPrice { get; set; } = 0m;         // سعر البيع بالتجزئة
    public decimal WholesalePrice { get; set; } = 0m;      // سعر البيع بالجملة
    public decimal RentalDailyRate { get; set; } = 0m;     // سعر الكراء لليوم الواحد أو للمناسبة
    public decimal RentalSecurityDeposit { get; set; } = 0m; // مبلغ التأمين المقترح للقطعة

    // --- المخزون (عشري إلزامي للأقمشة والزيوت) ---
    public decimal StockQuantity { get; set; } = 0m;       // الكمية الحالية (1.75 متر قماش، 12.5 مل عطر...)
    public decimal MinStockAlertQuantity { get; set; } = 0m;// حد الأمان للتنبيه عند قرب النفاد

    // طاقات القماش المرتبطة بهذا المتغير (في حال كان قماشاً بالمتر)
    public virtual ICollection<FabricRoll> FabricRolls { get; set; } = new List<FabricRoll>();

    // مكونات الوصفة إذا كان هذا المتغير عطراً مركباً (BOM Recipe)
    public virtual ICollection<CompositeRecipeItem> CompositeRecipeItems { get; set; } = new List<CompositeRecipeItem>();

    // الأصول المادية المرقمة للكراء (الفساتين والبدلات المحددة)
    public virtual ICollection<RentalAssetItem> RentalAssets { get; set; } = new List<RentalAssetItem>();
}
