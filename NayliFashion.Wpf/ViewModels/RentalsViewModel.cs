using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Customers;
using NayliFashion.Core.Models.Finance;
using NayliFashion.Core.Models.Rentals;
using NayliFashion.Services.DTOs;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Wpf.ViewModels;

/// <summary>
/// نموذج عرض نظام الكراء، تتبع الفساتين والبدلات، الودائع والضمانات، والمصبغة
/// </summary>
public partial class RentalsViewModel : ViewModelBase
{
    private readonly IRentalService _rentalService;
    private readonly ICustomerService _customerService;
    private readonly ICashShiftService _cashShiftService;
    private readonly IReceiptPrinterService _printerService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private ObservableCollection<RentalOrder> _activeOrders = new();

    [ObservableProperty]
    private ObservableCollection<RentalOrder> _overdueOrders = new();

    [ObservableProperty]
    private ObservableCollection<RentalAssetItem> _assets = new();

    [ObservableProperty]
    private RentalOrder? _selectedOrder;

    [ObservableProperty]
    private RentalAssetItem? _selectedAsset;

    // بيانات إنشاء عقد كراء جديد
    [ObservableProperty]
    private bool _isNewRentalDialogVisible;

    [ObservableProperty]
    private ObservableCollection<Customer> _customers = new();

    [ObservableProperty]
    private Customer? _newOrderCustomer;

    [ObservableProperty]
    private ObservableCollection<RentalAssetItem> _availableAssets = new();

    [ObservableProperty]
    private RentalAssetItem? _newOrderSelectedAsset;

    [ObservableProperty]
    private DateTime _newEventStartDate = DateTime.UtcNow.Date.AddDays(1);

    [ObservableProperty]
    private DateTime _newExpectedReturnDate = DateTime.UtcNow.Date.AddDays(3);

    [ObservableProperty]
    private decimal _newTotalRentFee = 5000m;

    [ObservableProperty]
    private decimal _newAdvancePaid = 2000m;

    [ObservableProperty]
    private GuaranteeDocumentType _newGuaranteeType = GuaranteeDocumentType.BiometricNationalIdCardCNI;

    [ObservableProperty]
    private string _newGuaranteeNumber = string.Empty;

    [ObservableProperty]
    private string _newGuaranteeSafeLocation = "خزنة العقود 1";

    [ObservableProperty]
    private decimal _newSecurityDepositCash = 0m;

    [ObservableProperty]
    private string _newOrderNotes = string.Empty;

    // بيانات معاينة الإرجاع والتسوية
    [ObservableProperty]
    private bool _isReturnInspectionDialogVisible;

    [ObservableProperty]
    private ItemConditionGrade _returnCondition = ItemConditionGrade.GoodMinorWear;

    [ObservableProperty]
    private bool _sendDirectlyToDryCleaning = true;

    [ObservableProperty]
    private decimal _returnDamageCost = 0m;

    [ObservableProperty]
    private decimal _returnDryCleaningCost = 0m;

    [ObservableProperty]
    private decimal _calculatedLateFees = 0m;

    [ObservableProperty]
    private decimal _totalAmountToPayOnReturn = 0m;

    [ObservableProperty]
    private bool _returnGuaranteeDocument = true;

    public RentalsViewModel(
        IRentalService rentalService,
        ICustomerService customerService,
        ICashShiftService cashShiftService,
        IReceiptPrinterService printerService,
        IAuthService authService)
    {
        _rentalService = rentalService;
        _customerService = customerService;
        _cashShiftService = cashShiftService;
        _printerService = printerService;
        _authService = authService;
    }

    public async Task InitializeAsync()
    {
        IsBusy = true;
        BusyMessage = "جاري تحميل عقود وأصول الكراء...";

        try
        {
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ أثناء التحميل: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task LoadDataAsync()
    {
        var active = await _rentalService.GetActiveRentalsAsync();
        ActiveOrders = new ObservableCollection<RentalOrder>(active);

        var overdue = await _rentalService.GetOverdueRentalsAsync();
        OverdueOrders = new ObservableCollection<RentalOrder>(overdue);

        var allAssets = await _rentalService.GetRentalAssetsAsync();
        Assets = new ObservableCollection<RentalAssetItem>(allAssets);

        var custs = await _customerService.SearchCustomersAsync();
        Customers = new ObservableCollection<Customer>(custs);

        var avail = await _rentalService.GetRentalAssetsAsync(status: RentalAssetStatus.AvailableForRent);
        AvailableAssets = new ObservableCollection<RentalAssetItem>(avail);
    }

    [RelayCommand]
    private void OpenNewRentalDialog()
    {
        IsNewRentalDialogVisible = true;
    }

    [RelayCommand]
    private async Task CreateRentalOrderAsync()
    {
        ClearMessages();

        if (NewOrderCustomer == null)
        {
            ErrorMessage = "يرجى تحديد العميل المستأجر!";
            return;
        }

        if (NewOrderSelectedAsset == null)
        {
            ErrorMessage = "يرجى اختيار القطعة / الفستان المؤجر!";
            return;
        }

        var user = _authService.CurrentUser;
        if (user == null)
        {
            ErrorMessage = "يجب تسجيل الدخول أولاً!";
            return;
        }

        try
        {
            var activeShift = await _cashShiftService.GetActiveShiftForUserAsync(user.Id);

            var request = new RentalCheckoutRequestDto
            {
                CustomerId = NewOrderCustomer.Id,
                CashierUserId = user.Id,
                ActiveCashShiftId = activeShift?.Id,
                SelectedRentalAssetIds = new List<int> { NewOrderSelectedAsset.Id },
                EventStartDate = NewEventStartDate,
                ExpectedReturnDate = NewExpectedReturnDate,
                TotalRentFeeDzd = NewTotalRentFee,
                AdvancePaidDzd = NewAdvancePaid,
                GuaranteeType = NewGuaranteeType,
                GuaranteeDocumentNumber = NewGuaranteeNumber,
                GuaranteeSafeLocation = NewGuaranteeSafeLocation,
                SecurityDepositCashDzd = NewSecurityDepositCash,
                Notes = NewOrderNotes
            };

            var order = await _rentalService.CreateRentalOrderAsync(request);

            SuccessMessage = $"تم إنشاء عقد الكراء برقم ({order.ContractNumber}) بنجاح!";
            IsNewRentalDialogVisible = false;

            // طباعة عقد الكراء وتذكرة الاستلام
            _ = _printerService.PrintRentalContractReceiptAsync(order);

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"فشل إنشاء عقد الكراء: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenReturnInspectionDialog(RentalOrder order)
    {
        SelectedOrder = order;

        // حساب غرامات التأخير التقديرية
        DateTime now = DateTime.UtcNow.Date;
        if (now > order.ExpectedReturnDate.Date)
        {
            int days = (int)(now - order.ExpectedReturnDate.Date).TotalDays;
            CalculatedLateFees = days * order.LateFeePerDayDzd;
        }
        else
        {
            CalculatedLateFees = 0m;
        }

        TotalAmountToPayOnReturn = order.RemainingRentFeeDzd + CalculatedLateFees;
        IsReturnInspectionDialogVisible = true;
    }

    [RelayCommand]
    private async Task SettleAndCloseRentalOrderAsync()
    {
        ClearMessages();

        if (SelectedOrder == null) return;

        var user = _authService.CurrentUser;
        if (user == null) return;

        try
        {
            var activeShift = await _cashShiftService.GetActiveShiftForUserAsync(user.Id);

            var inspectionDto = new RentalReturnInspectionDto
            {
                RentalOrderId = SelectedOrder.Id,
                CashierUserId = user.Id,
                ActiveCashShiftId = activeShift?.Id,
                ActualReturnDate = DateTime.UtcNow,
                AdditionalDamageCostDzd = ReturnDamageCost,
                AdditionalDryCleaningFeeDzd = ReturnDryCleaningCost,
                ReturnGuaranteeDocumentToCustomer = ReturnGuaranteeDocument,
                AmountPaidByCustomerOnReturn = TotalAmountToPayOnReturn,
                InspectionNotes = $"تم فحص القطعة وتسليمها بحالة {ReturnCondition}",
                ReturnedItems = SelectedOrder.RentalItems.Select(item => new RentalItemReturnStateDto
                {
                    RentalItemId = item.Id,
                    ConditionAtReturn = ReturnCondition,
                    SendDirectlyToDryCleaning = SendDirectlyToDryCleaning
                }).ToList()
            };

            await _rentalService.ProcessReturnInspectionAsync(inspectionDto);

            SuccessMessage = $"تمت تسوية عقد الكراء ({SelectedOrder.ContractNumber}) وإرجاع وثيقة الضمان بنجاح!";
            IsReturnInspectionDialogVisible = false;

            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"فشل تسوية عقد الكراء: {ex.Message}";
        }
    }
}
