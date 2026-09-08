using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Catalog;
using NayliFashion.Core.Models.Customers;
using NayliFashion.Core.Models.Finance;
using NayliFashion.Services.DTOs;
using NayliFashion.Services.Helpers;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Wpf.ViewModels;

/// <summary>
/// نموذج عرض نقطة البيع POS (الكاشير السريع، السلة، الباركود، والدفع)
/// </summary>
public partial class PosViewModel : ViewModelBase
{
    private readonly IPosService _posService;
    private readonly IInventoryService _inventoryService;
    private readonly ICustomerService _customerService;
    private readonly ICashShiftService _cashShiftService;
    private readonly IReceiptPrinterService _printerService;
    private readonly IAuthService _authService;

    // الفئات وقائمة الأصناف المعروضة
    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private string _searchProductTerm = string.Empty;

    [ObservableProperty]
    private string _scannedBarcode = string.Empty;

    // سلة المشتريات الحالية
    [ObservableProperty]
    private ObservableCollection<CartItemDto> _cartItems = new();

    [ObservableProperty]
    private CartItemDto? _selectedCartItem;

    // الملخص المالي اللحظي
    [ObservableProperty]
    private decimal _subTotalGross;

    [ObservableProperty]
    private decimal _overallDiscount;

    [ObservableProperty]
    private decimal _netTotalDzd;

    [ObservableProperty]
    private string _netTotalInWords = string.Empty;

    [ObservableProperty]
    private string _netTotalPopularCentimes = string.Empty;

    // الدفع والزبائن
    [ObservableProperty]
    private ObservableCollection<Customer> _customers = new();

    [ObservableProperty]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private PaymentMethod _selectedPaymentMethod = PaymentMethod.CashDZD;

    [ObservableProperty]
    private decimal _paidAmount;

    [ObservableProperty]
    private decimal _changeDueDzd;

    [ObservableProperty]
    private decimal _remainingDebtDzd;

    [ObservableProperty]
    private string _baridiMobTransactionNumber = string.Empty;

    [ObservableProperty]
    private CashShift? _activeShift;

    // نافذة اختيار المقاس/اللون المنبثقة عند النقر على المنتج
    [ObservableProperty]
    private bool _isVariantDialogVisible;

    [ObservableProperty]
    private Product? _dialogProduct;

    [ObservableProperty]
    private ObservableCollection<ProductVariant> _dialogVariants = new();

    public PosViewModel(
        IPosService posService,
        IInventoryService inventoryService,
        ICustomerService customerService,
        ICashShiftService cashShiftService,
        IReceiptPrinterService printerService,
        IAuthService authService)
    {
        _posService = posService;
        _inventoryService = inventoryService;
        _customerService = customerService;
        _cashShiftService = cashShiftService;
        _printerService = printerService;
        _authService = authService;
    }

    public async Task InitializeAsync()
    {
        IsBusy = true;
        BusyMessage = "جاري تحميل بيانات الكاشير...";

        try
        {
            var user = _authService.CurrentUser;
            if (user != null)
            {
                ActiveShift = await _cashShiftService.GetActiveShiftForUserAsync(user.Id);
            }

            var cats = await _inventoryService.GetAllCategoriesAsync();
            Categories = new ObservableCollection<Category>(cats);

            var custs = await _customerService.SearchCustomersAsync();
            Customers = new ObservableCollection<Customer>(custs);

            await LoadProductsAsync();

            // فحص وجود سلة مسودة محفوظة للتعافي عند انقطاع الكهرباء
            if (user != null)
            {
                var draftItems = await _posService.LoadActiveDraftCartAsync(user.Id, "Cart-1");
                if (draftItems != null && draftItems.Any())
                {
                    CartItems = new ObservableCollection<CartItemDto>(draftItems);
                    UpdateCartSummary();
                    SuccessMessage = "تم استرجاع السلة السابقة تلقائياً!";
                }
            }
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
        int? catId = SelectedCategory?.Id;
        var prods = await _inventoryService.GetProductsAsync(catId, SearchProductTerm);
        Products = new ObservableCollection<Product>(prods);
    }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        _ = LoadProductsAsync();
    }

    partial void OnSearchProductTermChanged(string value)
    {
        _ = LoadProductsAsync();
    }

    partial void OnPaidAmountChanged(decimal value)
    {
        CalculateChangeAndDebt();
    }

    private void CalculateChangeAndDebt()
    {
        if (PaidAmount >= NetTotalDzd)
        {
            ChangeDueDzd = PaidAmount - NetTotalDzd;
            RemainingDebtDzd = 0m;
        }
        else
        {
            RemainingDebtDzd = NetTotalDzd - PaidAmount;
            ChangeDueDzd = 0m;
        }
    }

    [RelayCommand]
    private async Task ProcessScannedBarcodeAsync()
    {
        if (string.IsNullOrWhiteSpace(ScannedBarcode)) return;

        string barcode = ScannedBarcode.Trim();
        ScannedBarcode = string.Empty;

        var variant = await _inventoryService.GetVariantByBarcodeAsync(barcode);
        if (variant == null)
        {
            ErrorMessage = $"الباركود ({barcode}) غير معرف في النظام!";
            return;
        }

        AddVariantToCart(variant);
    }

    [RelayCommand]
    private void ProductCardClicked(Product product)
    {
        if (!product.Variants.Any())
        {
            ErrorMessage = "هذا المنتج لا يحتوي على مقاسات أو نسخ مسجلة بالمخزن!";
            return;
        }

        if (product.Variants.Count == 1)
        {
            // منتج يملك متغيراً واحداً، يضاف مباشرة
            AddVariantToCart(product.Variants.First());
        }
        else
        {
            // فتح حوار اختيار المقاس واللون
            DialogProduct = product;
            DialogVariants = new ObservableCollection<ProductVariant>(product.Variants);
            IsVariantDialogVisible = true;
        }
    }

    [RelayCommand]
    private void SelectVariantFromDialog(ProductVariant variant)
    {
        AddVariantToCart(variant);
        IsVariantDialogVisible = false;
    }

    [RelayCommand]
    private void CloseVariantDialog()
    {
        IsVariantDialogVisible = false;
    }

    public void AddVariantToCart(ProductVariant variant, decimal quantity = 1.0m)
    {
        ClearMessages();

        var existingItem = CartItems.FirstOrDefault(i => i.ProductVariantId == variant.Id);
        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
        }
        else
        {
            CartItems.Add(new CartItemDto
            {
                ProductVariantId = variant.Id,
                ProductName = variant.Product?.Name ?? "منتج",
                VariantName = variant.VariantName,
                Barcode = variant.Barcode,
                ImagePath = variant.ImagePath ?? variant.Product?.MainImagePath,
                ProductType = variant.Product?.ProductType ?? ProductType.ReadyToWearClothing,
                UnitPrice = variant.RetailPrice,
                UnitCostPrice = variant.PurchaseCostPrice,
                Quantity = quantity,
                DiscountAmount = 0m
            });
        }

        UpdateCartSummary();
        AutoSaveDraft();
    }

    [RelayCommand]
    private void IncrementItemQuantity(CartItemDto item)
    {
        item.Quantity += 1.0m;
        UpdateCartSummary();
        AutoSaveDraft();
    }

    [RelayCommand]
    private void DecrementItemQuantity(CartItemDto item)
    {
        if (item.Quantity > 1.0m)
        {
            item.Quantity -= 1.0m;
        }
        else
        {
            CartItems.Remove(item);
        }
        UpdateCartSummary();
        AutoSaveDraft();
    }

    [RelayCommand]
    private void AddFabricDecimalMeters(string metersStr)
    {
        if (SelectedCartItem != null && decimal.TryParse(metersStr, out decimal additionalMeters))
        {
            SelectedCartItem.Quantity += additionalMeters;
            UpdateCartSummary();
            AutoSaveDraft();
        }
    }

    [RelayCommand]
    private void RemoveCartItem(CartItemDto item)
    {
        CartItems.Remove(item);
        UpdateCartSummary();
        AutoSaveDraft();
    }

    [RelayCommand]
    private void ClearCart()
    {
        CartItems.Clear();
        PaidAmount = 0m;
        ChangeDueDzd = 0m;
        RemainingDebtDzd = 0m;
        UpdateCartSummary();

        var user = _authService.CurrentUser;
        if (user != null)
        {
            _ = _posService.ClearActiveDraftCartAsync(user.Id, "Cart-1");
        }
    }

    private void UpdateCartSummary()
    {
        var summary = _posService.CalculateSummary(CartItems.ToList(), OverallDiscount);
        SubTotalGross = summary.SubTotalGross;
        NetTotalDzd = summary.NetTotalDzd;
        NetTotalInWords = summary.NetTotalInWords;
        NetTotalPopularCentimes = summary.NetTotalInPopularCentimes;

        if (PaidAmount == 0m || PaidAmount < NetTotalDzd)
        {
            PaidAmount = NetTotalDzd; // الافتراضي هو دفع المبلغ كاملاً كاش
        }

        CalculateChangeAndDebt();
    }

    private void AutoSaveDraft()
    {
        var user = _authService.CurrentUser;
        if (user != null && CartItems.Any())
        {
            _ = _posService.SaveActiveDraftCartAsync(user.Id, "Cart-1", CartItems.ToList(), SelectedCustomer?.Id);
        }
    }

    [RelayCommand]
    private async Task CheckoutAsync()
    {
        ClearMessages();

        if (!CartItems.Any())
        {
            ErrorMessage = "سلة المشتريات فارغة!";
            return;
        }

        var user = _authService.CurrentUser;
        if (user == null)
        {
            ErrorMessage = "يجب تسجيل الدخول لإتمام عملية البيع!";
            return;
        }

        IsBusy = true;
        BusyMessage = "جاري إتمام الفاتورة وتحديث المخزون...";

        try
        {
            var request = new CheckoutRequestDto
            {
                Items = CartItems.ToList(),
                CustomerId = SelectedCustomer?.Id,
                CashierUserId = user.Id,
                ActiveCashShiftId = ActiveShift?.Id,
                InvoiceType = InvoiceType.RetailSale,
                PaymentMethod = SelectedPaymentMethod,
                OverallInvoiceDiscount = OverallDiscount,
                PaidAmount = PaidAmount,
                BaridiMobTransactionNumber = BaridiMobTransactionNumber
            };

            var result = await _posService.ProcessCheckoutAsync(request);

            if (result.IsSuccess)
            {
                SuccessMessage = $"تم حفظ الفاتورة بنجاح برقم ({result.InvoiceNumber})!";

                // طباعة الإيصال الحراري
                if (result.InvoiceId.HasValue)
                {
                    var savedInvoice = await _posService.GetInvoiceByIdAsync(result.InvoiceId.Value);
                    if (savedInvoice != null)
                    {
                        _ = _printerService.PrintSaleReceiptAsync(savedInvoice);
                    }
                }

                // مسح السلة بعد نجاح العملية
                ClearCart();
            }
            else
            {
                ErrorMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ غير متوقع: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
