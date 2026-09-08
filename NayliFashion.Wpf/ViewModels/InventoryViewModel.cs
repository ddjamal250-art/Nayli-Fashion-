using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Catalog;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Wpf.ViewModels;

/// <summary>
/// نموذج عرض إدارة المخزون، المنتجات، المتغيرات، بكرات الأقمشة، ووصفات العطور BOM
/// </summary>
public partial class InventoryViewModel : ViewModelBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IBarcodeService _barcodeService;

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private ObservableCollection<Brand> _brands = new();

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private ProductVariant? _selectedVariant;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    [ObservableProperty]
    private Category? _filterCategory;

    // بيانات إضافة منتج جديد
    [ObservableProperty]
    private string _newProductName = string.Empty;

    [ObservableProperty]
    private int _newProductCategoryId;

    [ObservableProperty]
    private int? _newProductBrandId;

    [ObservableProperty]
    private ProductType _newProductType = ProductType.ReadyToWearClothing;

    [ObservableProperty]
    private bool _isNewProductDialogVisible;

    // بيانات إضافة متغير جديد
    [ObservableProperty]
    private string _newVariantName = string.Empty;

    [ObservableProperty]
    private string _newVariantColor = string.Empty;

    [ObservableProperty]
    private decimal _newVariantRetailPrice;

    [ObservableProperty]
    private decimal _newVariantWholesalePrice;

    [ObservableProperty]
    private decimal _newVariantCostPrice;

    [ObservableProperty]
    private decimal _newVariantInitialStock;

    [ObservableProperty]
    private bool _isNewVariantDialogVisible;

    // بيانات إضافة طاقة قماش لبكرة
    [ObservableProperty]
    private string _newRollCode = string.Empty;

    [ObservableProperty]
    private decimal _newRollMeters = 50m;

    [ObservableProperty]
    private decimal _newRollWidthCm = 150m;

    [ObservableProperty]
    private bool _isNewRollDialogVisible;

    // تنبيهات النواقص بالمخزن
    [ObservableProperty]
    private ObservableCollection<ProductVariant> _lowStockAlerts = new();

    public InventoryViewModel(IInventoryService inventoryService, IBarcodeService barcodeService)
    {
        _inventoryService = inventoryService;
        _barcodeService = barcodeService;
    }

    public async Task InitializeAsync()
    {
        IsBusy = true;
        BusyMessage = "جاري تحميل المخزون والكتالوج...";

        try
        {
            var cats = await _inventoryService.GetAllCategoriesAsync();
            Categories = new ObservableCollection<Category>(cats);
            if (Categories.Any())
                NewProductCategoryId = Categories.First().Id;

            var brs = await _inventoryService.GetAllBrandsAsync();
            Brands = new ObservableCollection<Brand>(brs);

            await LoadProductsAsync();
            await LoadLowStockAlertsAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ في التحميل: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task LoadProductsAsync()
    {
        int? catId = FilterCategory?.Id;
        var list = await _inventoryService.GetProductsAsync(catId, SearchTerm);
        Products = new ObservableCollection<Product>(list);

        if (Products.Any() && SelectedProduct == null)
        {
            SelectedProduct = Products.First();
        }
    }

    public async Task LoadLowStockAlertsAsync()
    {
        var alerts = await _inventoryService.GetLowStockAlertsAsync();
        LowStockAlerts = new ObservableCollection<ProductVariant>(alerts);
    }

    partial void OnFilterCategoryChanged(Category? value)
    {
        _ = LoadProductsAsync();
    }

    partial void OnSearchTermChanged(string value)
    {
        _ = LoadProductsAsync();
    }

    [RelayCommand]
    private void OpenNewProductDialog()
    {
        NewProductName = string.Empty;
        IsNewProductDialogVisible = true;
    }

    [RelayCommand]
    private async Task SaveNewProductAsync()
    {
        ClearMessages();

        if (string.IsNullOrWhiteSpace(NewProductName))
        {
            ErrorMessage = "يرجى كتابة اسم المنتج!";
            return;
        }

        try
        {
            var prod = new Product
            {
                Name = NewProductName.Trim(),
                CategoryId = NewProductCategoryId,
                BrandId = NewProductBrandId,
                ProductType = NewProductType,
                IsSaleAllowed = true,
                IsRentalAllowed = (NewProductType == ProductType.RentalAsset || NewProductType == ProductType.TraditionalGarment)
            };

            await _inventoryService.CreateProductAsync(prod);
            SuccessMessage = $"تم إضافة المنتج ({prod.Name}) بنجاح!";
            IsNewProductDialogVisible = false;

            await LoadProductsAsync();
            SelectedProduct = prod;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"فشل إضافة المنتج: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenNewVariantDialog()
    {
        if (SelectedProduct == null)
        {
            ErrorMessage = "يرجى تحديد المنتج أولاً لإضافة مقاس أو نسخة فرعية له!";
            return;
        }

        NewVariantName = string.Empty;
        NewVariantColor = string.Empty;
        NewVariantRetailPrice = 0m;
        NewVariantWholesalePrice = 0m;
        NewVariantCostPrice = 0m;
        NewVariantInitialStock = 0m;
        IsNewVariantDialogVisible = true;
    }

    [RelayCommand]
    private async Task SaveNewVariantAsync()
    {
        ClearMessages();

        if (SelectedProduct == null) return;

        if (string.IsNullOrWhiteSpace(NewVariantName))
        {
            ErrorMessage = "يرجى إدخال اسم المقاس / المتغير!";
            return;
        }

        try
        {
            var variant = new ProductVariant
            {
                ProductId = SelectedProduct.Id,
                VariantName = NewVariantName.Trim(),
                ColorName = NewVariantColor.Trim(),
                Barcode = _barcodeService.GenerateUniqueBarcode("200"),
                PurchaseCostPrice = NewVariantCostPrice,
                RetailPrice = NewVariantRetailPrice,
                WholesalePrice = NewVariantWholesalePrice,
                StockQuantity = NewVariantInitialStock,
                MinStockAlertQuantity = 2m
            };

            await _inventoryService.CreateVariantAsync(variant);
            SuccessMessage = $"تم إضافة المقاس بنجاح مع باركود ({variant.Barcode})!";
            IsNewVariantDialogVisible = false;

            // تحديث بيانات المنتج المحدد لعرض المتغيرات
            SelectedProduct = await _inventoryService.GetProductByIdAsync(SelectedProduct.Id);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"فشل إضافة المتغير: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenNewRollDialog()
    {
        if (SelectedVariant == null)
        {
            ErrorMessage = "يرجى تحديد متغير قماش لإضافة بكرة له!";
            return;
        }

        NewRollCode = $"ROL-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(10, 99)}";
        NewRollMeters = 50m;
        NewRollWidthCm = 150m;
        IsNewRollDialogVisible = true;
    }

    [RelayCommand]
    private async Task SaveNewRollAsync()
    {
        ClearMessages();

        if (SelectedVariant == null) return;

        try
        {
            var roll = new FabricRoll
            {
                ProductVariantId = SelectedVariant.Id,
                RollCode = NewRollCode.Trim(),
                InitialMeters = NewRollMeters,
                RemainingMeters = NewRollMeters,
                WidthCm = NewRollWidthCm,
                CostPerMeter = SelectedVariant.PurchaseCostPrice,
                IsExhausted = false
            };

            await _inventoryService.AddFabricRollAsync(roll);
            SuccessMessage = $"تم تسجيل طاقة القماش برقم ({roll.RollCode}) وطول ({roll.InitialMeters} متر)!";
            IsNewRollDialogVisible = false;

            // إعادة تحميل المتغير
            SelectedProduct = await _inventoryService.GetProductByIdAsync(SelectedProduct!.Id);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"فشل إضافة البكرة: {ex.Message}";
        }
    }
}
