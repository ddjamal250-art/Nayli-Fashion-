using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NayliFashion.Core.Models.Finance;
using NayliFashion.Core.Models.Users;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Wpf.ViewModels;

/// <summary>
/// نموذج العرض الرئيسي للشل (Main Navigation & Shell ViewModel)
/// يتحكم في التنقل بين أقسام البرنامج وعرض حالة الوردية والمستخدم
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly ICashShiftService _cashShiftService;
    private readonly IServiceProvider _serviceProvider;
    private readonly Action _onLogoutAction;

    [ObservableProperty]
    private User? _currentUser;

    [ObservableProperty]
    private CashShift? _activeShift;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _activeSectionTitle = "نقطة البيع (الكاشير)";

    public MainViewModel(
        IAuthService authService,
        ICashShiftService cashShiftService,
        IServiceProvider serviceProvider,
        Action onLogoutAction)
    {
        _authService = authService;
        _cashShiftService = cashShiftService;
        _serviceProvider = serviceProvider;
        _onLogoutAction = onLogoutAction;

        CurrentUser = _authService.CurrentUser;
    }

    public async Task InitializeAsync()
    {
        CurrentUser = _authService.CurrentUser;
        await RefreshActiveShiftAsync();
        NavigateToPos();
    }

    public async Task RefreshActiveShiftAsync()
    {
        if (CurrentUser != null)
        {
            ActiveShift = await _cashShiftService.GetActiveShiftForUserAsync(CurrentUser.Id);
        }
    }

    [RelayCommand]
    public void NavigateToPos()
    {
        ActiveSectionTitle = "نقطة البيع (الكاشير)";
        var posVm = (PosViewModel)_serviceProvider.GetService(typeof(PosViewModel))!;
        _ = posVm.InitializeAsync();
        CurrentView = posVm;
    }

    [RelayCommand]
    public void NavigateToInventory()
    {
        ActiveSectionTitle = "إدارة المخزون والمعايير";
        var invVm = (InventoryViewModel)_serviceProvider.GetService(typeof(InventoryViewModel))!;
        _ = invVm.InitializeAsync();
        CurrentView = invVm;
    }

    [RelayCommand]
    public void NavigateToRentals()
    {
        ActiveSectionTitle = "نظام كراء الأزياء والأفرشة";
        var rntVm = (RentalsViewModel)_serviceProvider.GetService(typeof(RentalsViewModel))!;
        _ = rntVm.InitializeAsync();
        CurrentView = rntVm;
    }

    [RelayCommand]
    public void NavigateToCustomers()
    {
        ActiveSectionTitle = "إدارة العملاء ودفتر الديون (الكارني)";
        var cstVm = (CustomersViewModel)_serviceProvider.GetService(typeof(CustomersViewModel))!;
        _ = cstVm.InitializeAsync();
        CurrentView = cstVm;
    }

    [RelayCommand]
    public void NavigateToFinance()
    {
        ActiveSectionTitle = "المالية والورديات والصندوق";
        var finVm = (FinanceViewModel)_serviceProvider.GetService(typeof(FinanceViewModel))!;
        _ = finVm.InitializeAsync();
        CurrentView = finVm;
    }

    [RelayCommand]
    public void NavigateToSettings()
    {
        ActiveSectionTitle = "إعدادات النظام والطباعة";
        var setVm = (SettingsViewModel)_serviceProvider.GetService(typeof(SettingsViewModel))!;
        _ = setVm.InitializeAsync();
        CurrentView = setVm;
    }

    [RelayCommand]
    public void Logout()
    {
        _authService.Logout();
        _onLogoutAction();
    }
}
