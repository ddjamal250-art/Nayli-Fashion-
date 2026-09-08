using System.Windows;
using NayliFashion.Wpf.ViewModels;

namespace NayliFashion.Wpf.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
    }

    private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            vm.Password = TxtPassword.Password;
        }
    }
}
