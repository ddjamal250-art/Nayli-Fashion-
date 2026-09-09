using System;
using System.Windows;
using System.Windows.Threading;
using NayliFashion.Wpf.Services;
using NayliFashion.Wpf.ViewModels;

namespace NayliFashion.Wpf;

public partial class MainWindow : Window
{
    private DispatcherTimer? _toastTimer;

    public MainWindow()
    {
        InitializeComponent();
    }

    public void InitializeToastService(IToastNotificationService toastService)
    {
        toastService.ToastRequested += (s, e) =>
        {
            Dispatcher.Invoke(() =>
            {
                ToastText.Text = e.Message;
                switch (e.Type)
                {
                    case ToastType.Success:
                        ToastIcon.Text = "✓";
                        ToastIcon.Foreground = (System.Windows.Media.Brush)FindResource("BrushSuccess");
                        ToastContainer.BorderBrush = (System.Windows.Media.Brush)FindResource("BrushSuccess");
                        break;
                    case ToastType.Warning:
                        ToastIcon.Text = "⚠";
                        ToastIcon.Foreground = (System.Windows.Media.Brush)FindResource("BrushAccentGold");
                        ToastContainer.BorderBrush = (System.Windows.Media.Brush)FindResource("BrushAccentGold");
                        break;
                    case ToastType.Error:
                        ToastIcon.Text = "✕";
                        ToastIcon.Foreground = (System.Windows.Media.Brush)FindResource("BrushDanger");
                        ToastContainer.BorderBrush = (System.Windows.Media.Brush)FindResource("BrushDanger");
                        break;
                    default:
                        ToastIcon.Text = "ℹ";
                        ToastIcon.Foreground = (System.Windows.Media.Brush)FindResource("BrushTextPrimary");
                        ToastContainer.BorderBrush = (System.Windows.Media.Brush)FindResource("BrushBorder");
                        break;
                }

                ToastContainer.Visibility = Visibility.Visible;

                _toastTimer?.Stop();
                _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.5) };
                _toastTimer.Tick += (ts, te) =>
                {
                    ToastContainer.Visibility = Visibility.Collapsed;
                    _toastTimer?.Stop();
                };
                _toastTimer.Start();
            });
        };
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