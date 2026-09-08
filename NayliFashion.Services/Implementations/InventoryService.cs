using Microsoft.EntityFrameworkCore;
using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Catalog;
using NayliFashion.Core.Models.Inventory;
using NayliFashion.Data.Context;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Services.Implementations;

/// <summary>
/// تطبيق خدمة إدارة المخزون، المنتجات، المتغيرات، وصفات عطور التعبئة، وبكرات الأقمشة
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IBarcodeService _barcodeService;

    public InventoryService(IDbContextFactory<AppDbContext> contextFactory, IBarcodeService barcodeService)
    {
        _contextFactory = contextFactory;
        _barcodeService = barcodeService;
    }

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Categories
                            .OrderBy(c => c.DisplayOrder)
                            .ThenBy(c => c.Name)
                            .ToListAsync();
    }

    public async Task<List<Brand>> GetAllBrandsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Brands
                            .OrderBy(b => b.Name)
                            .ToListAsync();
    }

    public async Task<List<Product>> GetProductsAsync(int? categoryId = null, string? searchTerm = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Products
                           .Include(p => p.Category)
                           .Include(p => p.Brand)
                           .Include(p => p.Variants)
                           .AsQueryable();

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) ||
                                     (p.CodeSku != null && p.CodeSku.ToLower().Contains(term)) ||
                                     p.Variants.Any(v => v.Barcode.Contains(term) || v.VariantName.ToLower().Contains(term)));
        }

        return await query.OrderByDescending(p => p.Id).ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Products
                            .Include(p => p.Category)
                            .Include(p => p.Brand)
                            .Include(p => p.Variants)
                                .ThenInclude(v => v.FabricRolls)
                            .Include(p => p.Variants)
                                .ThenInclude(v => v.CompositeRecipeItems)
                            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();
        return product;
    }

    public async Task<Product> UpdateProductAsync(Product product)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Products.Update(product);
        await context.SaveChangesAsync();
        return product;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var product = await context.Products.FindAsync(id);
        if (product == null) return false;

        product.IsDeleted = true;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<ProductVariant?> GetVariantByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;

        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProductVariants
                            .Include(v => v.Product)
                                .ThenInclude(p => p.Category)
                            .Include(v => v.FabricRolls)
                            .Include(v => v.CompositeRecipeItems)
                                .ThenInclude(r => r.ComponentVariant)
                            .FirstOrDefaultAsync(v => v.Barcode == barcode.Trim());
    }

    public async Task<ProductVariant?> GetVariantByIdAsync(int variantId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.ProductVariants
                            .Include(v => v.Product)
                            .Include(v => v.FabricRolls)
                            .Include(v => v.CompositeRecipeItems)
                            .FirstOrDefaultAsync(v => v.Id == variantId);
    }

    public async Task<ProductVariant> CreateVariantAsync(ProductVariant variant)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        if (string.IsNullOrWhiteSpace(variant.Barcode))
        {
            variant.Barcode = _barcodeService.GenerateUniqueBarcode("200");
        }

        await context.ProductVariants.AddAsync(variant);
        await context.SaveChangesAsync();
        return variant;
    }

    public async Task<ProductVariant> UpdateVariantAsync(ProductVariant variant)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.ProductVariants.Update(variant);
        await context.SaveChangesAsync();
        return variant;
    }

    public async Task<bool> DeleteVariantAsync(int variantId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var variant = await context.ProductVariants.FindAsync(variantId);
        if (variant == null) return false;

        variant.IsDeleted = true;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ProductVariant>> GenerateVariantsMatrixAsync(
        int productId,
        List<string> sizes,
        List<string> colors,
        decimal retailPrice,
        decimal wholesalePrice,
        decimal costPrice)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var product = await context.Products.FindAsync(productId);
        if (product == null) throw new InvalidOperationException("المنتج غير موجود");

        var createdVariants = new List<ProductVariant>();

        // في حال عدم إدخال ألوان أو مقاسات يتم افتراض عنصر أساسي
        var safeSizes = sizes.Any() ? sizes : new List<string> { "Standard" };
        var safeColors = colors.Any() ? colors : new List<string> { "افتراضي" };

        foreach (var size in safeSizes)
        {
            foreach (var color in safeColors)
            {
                string variantName = $"{size} / {color}".Trim();
                string barcode = _barcodeService.GenerateUniqueBarcode("200");

                var variant = new ProductVariant
                {
                    ProductId = productId,
                    VariantName = variantName,
                    ColorName = color,
                    Barcode = barcode,
                    PurchaseCostPrice = costPrice,
                    RetailPrice = retailPrice,
                    WholesalePrice = wholesalePrice,
                    StockQuantity = 0m,
                    MinStockAlertQuantity = 2m
                };

                await context.ProductVariants.AddAsync(variant);
                createdVariants.Add(variant);
            }
        }

        await context.SaveChangesAsync();
        return createdVariants;
    }

    public async Task DeductStockForSaleItemAsync(int productVariantId, decimal quantity, int? fabricRollId = null, AppDbContext? existingContext = null)
    {
        bool disposeContext = false;
        var context = existingContext;
        if (context == null)
        {
            context = await _contextFactory.CreateDbContextAsync();
            disposeContext = true;
        }

        try
        {
            var variant = await context.ProductVariants
                                       .Include(v => v.Product)
                                       .Include(v => v.CompositeRecipeItems)
                                       .FirstOrDefaultAsync(v => v.Id == productVariantId);

            if (variant == null) return;

            // 1. خصم من طاقة القماش المحددة إذا كان البيع من بكرة
            if (fabricRollId.HasValue)
            {
                var roll = await context.FabricRolls.FindAsync(fabricRollId.Value);
                if (roll != null)
                {
                    roll.RemainingMeters = Math.Max(0m, roll.RemainingMeters - quantity);
                    if (roll.RemainingMeters <= 0.05m) // إذا تبقى أقل من 5 سم تعتبر منتهية
                    {
                        roll.IsExhausted = true;
                    }
                }
            }

            // 2. معالجة المنتجات المركبة (عطور التعبئة والـ BOM)
            // خصم المكونات الأولية (زيت العطر الخام بالمليلتر/الغرام + الكحول + الزجاجة الفارغة)
            if (variant.CompositeRecipeItems.Any())
            {
                foreach (var component in variant.CompositeRecipeItems)
                {
                    var compVariant = await context.ProductVariants.FindAsync(component.ComponentVariantId);
                    if (compVariant != null)
                    {
                        decimal requiredComponentQty = component.QuantityRequired * quantity;
                        compVariant.StockQuantity = Math.Max(0m, compVariant.StockQuantity - requiredComponentQty);
                    }
                }
            }
            else
            {
                // الخصم الطبيعي للمتغير
                variant.StockQuantity -= quantity;
            }

            if (disposeContext)
            {
                await context.SaveChangesAsync();
            }
        }
        finally
        {
            if (disposeContext)
            {
                await context.DisposeAsync();
            }
        }
    }

    public async Task RestoreStockForReturnedItemAsync(int productVariantId, decimal quantity, int? fabricRollId = null, AppDbContext? existingContext = null)
    {
        bool disposeContext = false;
        var context = existingContext;
        if (context == null)
        {
            context = await _contextFactory.CreateDbContextAsync();
            disposeContext = true;
        }

        try
        {
            var variant = await context.ProductVariants
                                       .Include(v => v.CompositeRecipeItems)
                                       .FirstOrDefaultAsync(v => v.Id == productVariantId);

            if (variant == null) return;

            if (fabricRollId.HasValue)
            {
                var roll = await context.FabricRolls.FindAsync(fabricRollId.Value);
                if (roll != null)
                {
                    roll.RemainingMeters += quantity;
                    roll.IsExhausted = false;
                }
            }

            if (variant.CompositeRecipeItems.Any())
            {
                foreach (var component in variant.CompositeRecipeItems)
                {
                    var compVariant = await context.ProductVariants.FindAsync(component.ComponentVariantId);
                    if (compVariant != null)
                    {
                        compVariant.StockQuantity += (component.QuantityRequired * quantity);
                    }
                }
            }
            else
            {
                variant.StockQuantity += quantity;
            }

            if (disposeContext)
            {
                await context.SaveChangesAsync();
            }
        }
        finally
        {
            if (disposeContext)
            {
                await context.DisposeAsync();
            }
        }
    }

    public async Task<List<FabricRoll>> GetAvailableFabricRollsAsync(int variantId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var rolls = await context.FabricRolls
                                 .Where(r => r.ProductVariantId == variantId && !r.IsExhausted && r.RemainingMeters > 0)
                                 .ToListAsync();
        return rolls.OrderByDescending(r => r.RemainingMeters).ToList();
    }

    public async Task<FabricRoll> AddFabricRollAsync(FabricRoll roll)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        await context.FabricRolls.AddAsync(roll);

        // إضافة أمتار البكرة لإجمالي رصيد المتغير
        var variant = await context.ProductVariants.FindAsync(roll.ProductVariantId);
        if (variant != null)
        {
            variant.StockQuantity += roll.InitialMeters;
        }

        await context.SaveChangesAsync();
        return roll;
    }

    public async Task SetCompositeRecipeAsync(int parentVariantId, List<CompositeRecipeItem> recipeItems)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // مسح البنود القديمة
        var existing = await context.CompositeRecipeItems.Where(r => r.ParentVariantId == parentVariantId).ToListAsync();
        context.CompositeRecipeItems.RemoveRange(existing);

        // إضافة البنود الجديدة
        foreach (var item in recipeItems)
        {
            item.ParentVariantId = parentVariantId;
            await context.CompositeRecipeItems.AddAsync(item);
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<CompositeRecipeItem>> GetCompositeRecipeAsync(int parentVariantId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.CompositeRecipeItems
                            .Include(r => r.ComponentVariant)
                                .ThenInclude(v => v.Product)
                            .Where(r => r.ParentVariantId == parentVariantId)
                            .ToListAsync();
    }

    public async Task<StockAdjustment> CreateStockAdjustmentAsync(StockAdjustment adjustment)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        adjustment.AdjustmentNumber = $"ADJ-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100, 999)}";
        await context.StockAdjustments.AddAsync(adjustment);

        // تعديل أرصدة المتغيرات مباشرة بناءً على الفوارق
        foreach (var item in adjustment.Items)
        {
            var variant = await context.ProductVariants.FindAsync(item.ProductVariantId);
            if (variant != null)
            {
                item.SystemQuantityBefore = variant.StockQuantity;
                item.DifferenceQuantity = item.ActualCountedQuantity - item.SystemQuantityBefore;
                item.UnitCost = variant.PurchaseCostPrice;
                item.TotalFinancialImpact = item.DifferenceQuantity * item.UnitCost;

                variant.StockQuantity = item.ActualCountedQuantity;
            }
        }

        await context.SaveChangesAsync();
        return adjustment;
    }

    public async Task<List<ProductVariant>> GetLowStockAlertsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var variants = await context.ProductVariants
                                    .Include(v => v.Product)
                                    .Where(v => v.StockQuantity <= v.MinStockAlertQuantity)
                                    .ToListAsync();
        return variants.OrderBy(v => v.StockQuantity).ToList();
    }
}
