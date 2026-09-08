using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NayliFashion.Core.Models.Users;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Wpf.ViewModels;

/// <summary>
/// نموذج عرض شاشة تسجيل الدخول
/// </summary>
public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _authService;
    private readonly Action<User> _onLoginSuccess;

    [ObservableProperty]
    private string _username = "admin";

    [ObservableProperty]
    private string _password = string.Empty;

    public LoginViewModel(IAuthService _authService, Action<User> onLoginSuccess)
    {
        this._authService = _authService;
        _onLoginSuccess = onLoginSuccess;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ClearMessages();

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "يرجى إدخال اسم المستخدم وكلمة المرور";
            return;
        }

        IsBusy = true;
        BusyMessage = "جاري التحقق من بيانات الدخول...";

        try
        {
            var user = await _authService.LoginAsync(Username, Password);
            if (user != null)
            {
                SuccessMessage = "تم تسجيل الدخول بنجاح!";
                _onLoginSuccess(user);
            }
            else
            {
                ErrorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة!";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ أثناء الاتصال: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
