using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NayliFashion.Wpf.ViewModels;

namespace NayliFashion.Wpf.Views;

/// <summary>
/// فئة الكود الخلفي لنافذة تسجيل الدخول (LoginWindow)
/// تضمن تزامن حقل كلمة المرور المشفر والمرئي وكشف حالة Caps Lock ومفاتيح الإدخال
/// </summary>
public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();

        DataContextChanged += LoginWindow_DataContextChanged;
        Loaded += LoginWindow_Loaded;
        Activated += (s, e) => CheckCapsLock();
        PreviewKeyDown += (s, e) => CheckCapsLock();
        PreviewKeyUp += (s, e) => CheckCapsLock();
    }

    private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
    {
        CheckCapsLock();
        TxtUsername.Focus();
    }

    private void LoginWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is LoginViewModel oldVm)
        {
            oldVm.PropertyChanged -= ViewModel_PropertyChanged;
        }

        if (e.NewValue is LoginViewModel newVm)
        {
            newVm.PropertyChanged += ViewModel_PropertyChanged;
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (DataContext is not LoginViewModel vm) return;

        if (e.PropertyName == nameof(LoginViewModel.IsPasswordVisible))
        {
            if (vm.IsPasswordVisible)
            {
                TxtPasswordVisible.Text = vm.Password ?? string.Empty;
                TxtPasswordVisible.Focus();
                TxtPasswordVisible.CaretIndex = TxtPasswordVisible.Text.Length;
            }
            else
            {
                TxtPassword.Password = vm.Password ?? string.Empty;
                TxtPassword.Focus();
            }
        }
        else if (e.PropertyName == nameof(LoginViewModel.Password))
        {
            if (!vm.IsPasswordVisible && TxtPassword.Password != vm.Password)
            {
                TxtPassword.Password = vm.Password ?? string.Empty;
            }
            else if (vm.IsPasswordVisible && TxtPasswordVisible.Text != vm.Password)
            {
                TxtPasswordVisible.Text = vm.Password ?? string.Empty;
            }
        }
    }

    private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && !vm.IsPasswordVisible)
        {
            vm.Password = TxtPassword.Password;
        }
    }

    private void TxtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is LoginViewModel vm && vm.IsPasswordVisible)
        {
            vm.Password = TxtPasswordVisible.Text;
        }
    }

    private void CheckCapsLock()
    {
        if (DataContext is LoginViewModel vm)
        {
            vm.IsCapsLockOn = Keyboard.IsKeyToggled(Key.CapsLock);
        }
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        CheckCapsLock();

        if (e.Key == Key.Enter)
        {
            if (DataContext is LoginViewModel vm && vm.LoginCommand.CanExecute(null))
            {
                vm.LoginCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
