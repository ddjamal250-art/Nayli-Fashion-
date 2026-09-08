using NayliFashion.Core.Models.Catalog;
using NayliFashion.Core.Models.Inventory;

namespace NayliFashion.Services.Interfaces;

/// <summary>
/// واجهة خدمة إدارة المخزون، المنتجات، المتغيرات، الأقمشة والعطور
/// </summary>
public interface IInventoryService
{
    Task<List<Category>> GetAllCategoriesAsync();
    Task<List<Brand>> GetAllBrandsAsync();

    Task<List<Product>> GetProductsAsync(int? categoryId = null, string? searchTerm = null);
    Task<Product?> GetProductByIdAsync(int id);
    Task<Product> CreateProductAsync(Product product);
    Task<Product> UpdateProductAsync(Product product);
    Task<bool> DeleteProductAsync(int id);

    Task<ProductVariant?> GetVariantByBarcodeAsync(string barcode);
    Task<ProductVariant?> GetVariantByIdAsync(int variantId);
    Task<ProductVariant> CreateVariantAsync(ProductVariant variant);
    Task<ProductVariant> UpdateVariantAsync(ProductVariant variant);
    Task<bool> DeleteVariantAsync(int variantId);

    // توليد مصفوفة المتغيرات السريعة (المقاسات × الألوان)
    Task<List<ProductVariant>> GenerateVariantsMatrixAsync(int productId, List<string> sizes, List<string> colors, decimal retailPrice, decimal wholesalePrice, decimal costPrice);

    // خصم المخزون العشري ومعالجة الأقمشة والعطور المركبة BOM
    Task DeductStockForSaleItemAsync(int productVariantId, decimal quantity, int? fabricRollId = null, NayliFashion.Data.Context.AppDbContext? existingContext = null);
    Task RestoreStockForReturnedItemAsync(int productVariantId, decimal quantity, int? fabricRollId = null, NayliFashion.Data.Context.AppDbContext? existingContext = null);

    // إدارة بكرات وطاقات الأقمشة
    Task<List<FabricRoll>> GetAvailableFabricRollsAsync(int variantId);
    Task<FabricRoll> AddFabricRollAsync(FabricRoll roll);

    // إدارة وصفات عطور التعبئة والـ BOM
    Task SetCompositeRecipeAsync(int parentVariantId, List<CompositeRecipeItem> recipeItems);
    Task<List<CompositeRecipeItem>> GetCompositeRecipeAsync(int parentVariantId);

    // الجرد وتسوية الفروقات
    Task<StockAdjustment> CreateStockAdjustmentAsync(StockAdjustment adjustment);
    Task<List<ProductVariant>> GetLowStockAlertsAsync();
}
