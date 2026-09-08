using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NayliFashion.Core.Models.Customers;
using NayliFashion.Core.Models.Finance;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Wpf.ViewModels;

/// <summary>
/// نموذج عرض إدارة العملاء، دفتر ديون الكارني، وسندات القبض ومواعيد الفيرمون
/// </summary>
public partial class CustomersViewModel : ViewModelBase
{
    private readonly ICustomerService _customerService;
    private readonly ICashShiftService _cashShiftService;
    private readonly IReceiptPrinterService _printerService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private ObservableCollection<Customer> _customers = new();

    [ObservableProperty]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private string _searchTerm = string.Empty;

    // كشف الحساب وسندات القبض
    [ObservableProperty]
    private ObservableCollection<CustomerTransaction> _customerStatement = new();

    // العملاء الذين حان موعد نزول رواتبهم والفيرمون
    [ObservableProperty]
    private ObservableCollection<Customer> _dueSalaryCustomers = new();

    // حوار تسجيل عميل جديد
    [ObservableProperty]
    private bool _isNewCustomerDialogVisible;

    [ObservableProperty]
    private string _newFirstName = string.Empty;

    [ObservableProperty]
    private string _newFamilyName = string.Empty;

    [ObservableProperty]
    private string _newNickname = string.Empty;

    [ObservableProperty]
    private string _newPhone = string.Empty;

    [ObservableProperty]
    private string _newNeighborhood = "حي برنوس، الجلفة";

    [ObservableProperty]
    private decimal _newCreditLimit = 50000m;

    [ObservableProperty]
    private int _newSalaryVirementDay = 22; // تاريخ الفيرمون الشائع (20 إلى 26)

    // حوار سداد دفعة نقدية (سند قبض)
    [ObservableProperty]
    private bool _isPaymentDialogVisible;

    [ObservableProperty]
    private decimal _paymentAmount;

    [ObservableProperty]
    private string _paymentNotes = string.Empty;

    public CustomersViewModel(
        ICustomerService customerService,
        ICashShiftService cashShiftService,
        IReceiptPrinterService printerService,
        IAuthService authService)
    {
        _customerService = customerService;
        _cashShiftService = cashShiftService;
        _printerService = printerService;
        _authService = authService;
    }

    public async Task InitializeAsync()
    {
        IsBusy = true;
        BusyMessage = "جاري تحميل سجل العملاء ودفتر الديون...";

        try
        {
            await LoadCustomersAsync();

            var dues = await _customerService.GetCustomersWithApproachingSalaryDueAsync(3);
            DueSalaryCustomers = new ObservableCollection<Customer>(dues);
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

    public async Task LoadCustomersAsync()
    {
        var list = await _customerService.SearchCustomersAsync(SearchTerm);
        Customers = new ObservableCollection<Customer>(list);

        if (Customers.Any() && SelectedCustomer == null)
        {
            SelectedCustomer = Customers.First();
        }
    }

    partial void OnSearchTermChanged(string value)
    {
        _ = LoadCustomersAsync();
    }

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        if (value != null)
        {
            _ = LoadStatementAsync(value.Id);
        }
    }

    private async Task LoadStatementAsync(int customerId)
    {
        var list = await _customerService.GetCustomerStatementAsync(customerId);
        CustomerStatement = new ObservableCollection<CustomerTransaction>(list);
    }

    [RelayCommand]
    private void OpenNewCustomerDialog()
    {
        NewFirstName = string.Empty;
        NewFamilyName = string.Empty;
        NewNickname = string.Empty;
        NewPhone = string.Empty;
        IsNewCustomerDialogVisible = true;
    }

    [RelayCommand]
    private async Task SaveNewCustomerAsync()
    {
        ClearMessages();

        if (string.IsNullOrWhiteSpace(NewFirstName) || string.IsNullOrWhiteSpace(NewFamilyName))
        {
            ErrorMessage = "يرجى كتابة الاسم الشخصي واللقب العائلي!";
            return;
        }

        try
        {
            var customer = new Customer
            {
                FullName = NewFirstName.Trim(),
                FamilyName = NewFamilyName.Trim(),
                Nickname = string.IsNullOrWhiteSpace(NewNickname) ? null : NewNickname.Trim(),
                PhoneNumber = NewPhone.Trim(),
                AddressNeighborhood = NewNeighborhood.Trim(),
                MaxCreditLimitDzd = NewCreditLimit,
                SalaryVirementDay = NewSalaryVirementDay,
                CurrentDebtDzd = 0m
            };

            await _customerService.CreateCustomerAsync(customer);
            SuccessMessage = $"تم تسجيل العميل ({customer.FullName} {customer.FamilyName}) بنجاح!";
            IsNewCustomerDialogVisible = false;

            await LoadCustomersAsync();
            SelectedCustomer = customer;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"فشل إضافة العميل: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenPaymentDialog()
    {
        if (SelectedCustomer == null)
        {
            ErrorMessage = "يرجى اختيار العميل أولاً لتسجيل دفعة سداد!";
            return;
        }

        PaymentAmount = SelectedCustomer.CurrentDebtDzd;
        PaymentNotes = "تسديد دفعة من حساب الكارني";
        IsPaymentDialogVisible = true;
    }

    [RelayCommand]
    private async Task SaveDebtPaymentAsync()
    {
        ClearMessages();

        if (SelectedCustomer == null) return;

        if (PaymentAmount <= 0)
        {
            ErrorMessage = "يرجى إدخال مبلغ سداد صالح أكبر من الصفر!";
            return;
        }

        var user = _authService.CurrentUser;
        if (user == null) return;

        try
        {
            var shift = await _cashShiftService.GetActiveShiftForUserAsync(user.Id);

            var tx = await _customerService.ProcessDebtPaymentAsync(
                SelectedCustomer.Id,
                PaymentAmount,
                user.Id,
                shift?.Id,
                PaymentNotes);

            SuccessMessage = $"تم استلام مبلغ ({PaymentAmount:N2} دج) بنجاح! الرصيد المتبقي: {SelectedCustomer.CurrentDebtDzd:N2} دج";
            IsPaymentDialogVisible = false;

            // طباعة وصل القبض
            _ = _printerService.PrintDebtReceiptAsync(tx, SelectedCustomer);

            // تحديث كشف الحساب
            await LoadStatementAsync(SelectedCustomer.Id);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"فشل تسجيل السند: {ex.Message}";
        }
    }
}
