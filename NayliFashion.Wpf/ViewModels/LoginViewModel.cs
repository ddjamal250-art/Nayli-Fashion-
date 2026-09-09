using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Users;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Wpf.ViewModels;

/// <summary>
/// عنصر مستخدم في قائمة التبديل السريع للموظفين والكاشير
/// </summary>
public class StaffUserItem
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    public string RoleDisplay => Role switch
    {
        UserRole.SuperAdmin => "مدير النظام",
        UserRole.StoreManager => "مسؤول المحل",
        UserRole.Cashier => "كاشير الصندوق",
        UserRole.StockKeeper => "أمين المخزن",
        _ => "مستخدم"
    };

    public string RoleBadgeColor => Role switch
    {
        UserRole.SuperAdmin => "#C5A059", // Imperial Matte Gold
        UserRole.StoreManager => "#3B82F6", // Royal Blue
        UserRole.Cashier => "#10B981", // Emerald Green
        UserRole.StockKeeper => "#8B5CF6", // Purple
        _ => "#9CA3AF"
    };

    public string Initials => string.IsNullOrWhiteSpace(FullName)
        ? (!string.IsNullOrWhiteSpace(Username) ? Username[..1].ToUpper() : "U")
        : FullName.Trim()[..1].ToUpper();
}

/// <summary>
/// نموذج عرض شاشة تسجيل الدخول المطور بمعايير نقاط البيع والـ ERP العالمية
/// </summary>
public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly Action<User> _onLoginSuccess;
    private readonly DispatcherTimer _clockTimer;

    [ObservableProperty]
    private string _username = "admin";

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isPasswordVisible;

    [ObservableProperty]
    private bool _isPinMode;

    [ObservableProperty]
    private string _pinCode = string.Empty;

    [ObservableProperty]
    private string _pinMaskedDisplay = "○   ○   ○   ○";

    [ObservableProperty]
    private bool _isCapsLockOn;

    [ObservableProperty]
    private string _currentTime = string.Empty;

    [ObservableProperty]
    private string _currentDate = string.Empty;

    [ObservableProperty]
    private StaffUserItem? _selectedUser;

    public ObservableCollection<StaffUserItem> Users { get; } = new();

    public LoginViewModel(IAuthService authService, Action<User> onLoginSuccess)
    {
        _authService = authService;
        _onLoginSuccess = onLoginSuccess;

        UpdateClock();
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (s, e) => UpdateClock();
        _clockTimer.Start();
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        var culture = new CultureInfo("ar-DZ");
        CurrentTime = now.ToString("HH:mm:ss");
        CurrentDate = now.ToString("dddd، d MMMM yyyy", culture);
    }

    public async Task InitializeAsync()
    {
        try
        {
            var dbUsers = await _authService.GetAllUsersAsync();
            Users.Clear();
            foreach (var u in dbUsers.Where(x => x.IsActive && !x.IsDeleted))
            {
                Users.Add(new StaffUserItem
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = u.FullName,
                    Role = u.Role
                });
            }

            var defaultUser = Users.FirstOrDefault(u => u.Username.Equals(Username, StringComparison.OrdinalIgnoreCase)) ?? Users.FirstOrDefault();
            if (defaultUser != null)
            {
                SelectUser(defaultUser);
            }
        }
        catch
        {
            // صامت في حال عدم اكتمال التهيئة بعد
        }
    }

    [RelayCommand]
    public void SelectUser(StaffUserItem? user)
    {
        if (user == null) return;
        SelectedUser = user;
        Username = user.Username;
        Password = string.Empty;
        ClearPin();
        ClearMessages();
    }

    [RelayCommand]
    public void TogglePasswordVisibility()
    {
        IsPasswordVisible = !IsPasswordVisible;
    }

    [RelayCommand]
    public void SwitchToPinMode()
    {
        IsPinMode = true;
        ClearMessages();
    }

    [RelayCommand]
    public void SwitchToPasswordMode()
    {
        IsPinMode = false;
        ClearMessages();
    }

    [RelayCommand]
    public void EnterPinDigit(string? digit)
    {
        if (string.IsNullOrEmpty(digit)) return;
        if (PinCode.Length < 8)
        {
            PinCode += digit;
            UpdatePinDisplay();
            ClearMessages();
        }
    }

    [RelayCommand]
    public void BackspacePin()
    {
        if (PinCode.Length > 0)
        {
            PinCode = PinCode[..^1];
            UpdatePinDisplay();
            ClearMessages();
        }
    }

    [RelayCommand]
    public void ClearPin()
    {
        PinCode = string.Empty;
        UpdatePinDisplay();
        ClearMessages();
    }

    private void UpdatePinDisplay()
    {
        if (PinCode.Length == 0)
        {
            PinMaskedDisplay = "○   ○   ○   ○";
        }
        else
        {
            var dots = new string('●', PinCode.Length);
            PinMaskedDisplay = string.Join("   ", dots.ToCharArray());
        }
    }

    [RelayCommand]
    public async Task LoginAsync()
    {
        ClearMessages();

        string passToValidate = IsPinMode ? PinCode : Password;

        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "يرجى تحديد أو إدخال اسم المستخدم";
            return;
        }

        if (string.IsNullOrWhiteSpace(passToValidate))
        {
            ErrorMessage = IsPinMode ? "يرجى إدخال رمز PIN المكون من أرقام" : "يرجى إدخال كلمة المرور";
            return;
        }

        IsBusy = true;
        BusyMessage = "جاري التحقق والمصادقة الأمنية...";

        try
        {
            var user = await _authService.LoginAsync(Username, passToValidate);
            if (user != null)
            {
                SuccessMessage = $"مرحباً بك مجدداً: {user.FullName}";
                _clockTimer.Stop();
                _onLoginSuccess(user);
            }
            else
            {
                ErrorMessage = IsPinMode
                    ? "رمز PIN غير صحيح لهذا المستخدم!"
                    : "اسم المستخدم أو كلمة المرور غير صحيحة!";
                if (IsPinMode)
                {
                    ClearPin();
                }
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
