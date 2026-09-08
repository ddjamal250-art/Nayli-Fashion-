using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Finance;
using NayliFashion.Services.DTOs;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Wpf.ViewModels;

/// <summary>
/// نموذج عرض إدارة المالية والورديات، المصروفات النثرية، ومطابقة الصندوق Z-Report
/// </summary>
public partial class FinanceViewModel : ViewModelBase
{
    private readonly ICashShiftService _cashShiftService;
    private readonly IReceiptPrinterService _printerService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private CashShift? _activeShift;

    [ObservableProperty]
    private bool _hasActiveShift;

    [ObservableProperty]
    private ObservableCollection<CashShift> _shiftHistory = new();

    // حوار فتح وردية جديدة
    [ObservableProperty]
    private bool _isOpenShiftDialogVisible;

    [ObservableProperty]
    private decimal _openingFloatAmount = 5000m; // عهدة بداية اليوم الافتراضية

    // حوار إغلاق الوردية الأعمى (Blind Close)
    [ObservableProperty]
    private bool _isCloseShiftDialogVisible;

    [ObservableProperty]
    private decimal _actualCountedCash;

    [ObservableProperty]
    private string _closingNotes = string.Empty;

    [ObservableProperty]
    private ZReportDto? _lastGeneratedZReport;

    // حوار تسجيل مصروف جديد
    [ObservableProperty]
    private bool _isExpenseDialogVisible;

    [ObservableProperty]
    private string _expenseCategory = "مصبغة فساتين";

    [ObservableProperty]
    private decimal _expenseAmount = 1000m;

    [ObservableProperty]
    private string _expenseBeneficiary = string.Empty;

    [ObservableProperty]
    private string _expenseNotes = string.Empty;

    public FinanceViewModel(
        ICashShiftService cashShiftService,
        IReceiptPrinterService printerService,
        IAuthService authService)
    {
        _cashShiftService = cashShiftService;
        _printerService = printerService;
        _authService = authService;
    }

    public async Task InitializeAsync()
    {
        IsBusy = true;
        BusyMessage = "جاري تحميل بيانات الصندوق والورديات...";

        try
        {
            await RefreshActiveShiftAsync();
            var history = await _cashShiftService.GetShiftHistoryAsync();
            ShiftHistory = new ObservableCollection<CashShift>(history);
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

    public async Task RefreshActiveShiftAsync()
    {
        var user = _authService.CurrentUser;
        if (user != null)
        {
            ActiveShift = await _cashShiftService.GetActiveShiftForUserAsync(user.Id);
            HasActiveShift = (ActiveShift != null && ActiveShift.Status == CashShiftStatus.OpenActive);
        }
    }

    [RelayCommand]
    private void OpenOpenShiftDialog()
    {
        OpeningFloatAmount = 5000m;
        IsOpenShiftDialogVisible = true;
    }

    [RelayCommand]
    private async Task ConfirmOpenShiftAsync()
    {
        ClearMessages();

        var user = _authService.CurrentUser;
        if (user == null) return;

        try
        {
            var shift = await _cashShiftService.OpenShiftAsync(user.Id, OpeningFloatAmount);
            SuccessMessage = $"تم فتح الوردية برقم ({shift.ShiftNumber}) بعهدة ({OpeningFloatAmount:N2} دج)!";
            IsOpenShiftDialogVisible = false;

            await RefreshActiveShiftAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"فشل فتح الوردية: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenCloseShiftDialog()
    {
        if (ActiveShift == null) return;
        ActualCountedCash = 0m;
        ClosingNotes = string.Empty;
        IsCloseShiftDialogVisible = true;
    }

    [RelayCommand]
    private async Task ConfirmCloseShiftAsync()
    {
        ClearMessages();

        if (ActiveShift == null) return;

        try
        {
            var zReport = await _cashShiftService.CloseShiftAsync(ActiveShift.Id, ActualCountedCash, ClosingNotes);
            LastGeneratedZReport = zReport;

            SuccessMessage = $"تم إغلاق الوردية بنجاح! الفارق: {zReport.VarianceDifferenceDzd:N2} دج";
            IsCloseShiftDialogVisible = false;

            // طباعة تقرير Z
            _ = _printerService.PrintZReportAsync(zReport);

            await InitializeAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"فشل إغلاق الوردية: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenExpenseDialog()
    {
        ExpenseAmount = 0m;
        ExpenseBeneficiary = string.Empty;
        ExpenseNotes = string.Empty;
        IsExpenseDialogVisible = true;
    }

    [RelayCommand]
    private async Task SaveExpenseAsync()
    {
        ClearMessages();

        if (ExpenseAmount <= 0)
        {
            ErrorMessage = "يرجى كتابة مبلغ صالح للمصروف!";
            return;
        }

        var user = _authService.CurrentUser;
        if (user == null) return;

        try
        {
            await _cashShiftService.RecordExpenseAsync(
                ActiveShift?.Id,
                user.Id,
                ExpenseCategory,
                ExpenseAmount,
                ExpenseBeneficiary,
                ExpenseNotes);

            SuccessMessage = $"تم تسجيل المصروف بمبلغ ({ExpenseAmount:N2} دج) وخصمه من الدرج!";
            IsExpenseDialogVisible = false;

            await RefreshActiveShiftAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"فشل تسجيل المصروف: {ex.Message}";
        }
    }
}
