using System.Windows;
using NayliFashion.Wpf.ViewModels;

namespace NayliFashion.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void LockPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is System.Windows.Controls.PasswordBox pb)
        {
            vm.UnlockPassword = pb.Password;
        }
    }

    private void LockPasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter && DataContext is MainViewModel vm)
        {
            vm.UnlockScreenCommand.Execute(null);
            if (!vm.IsScreenLocked && sender is System.Windows.Controls.PasswordBox pb)
            {
                pb.Clear();
            }
        }
    }
}