using System;

namespace NayliFashion.Wpf.Services;

public class ToastNotificationService : IToastNotificationService
{
    public event EventHandler<ToastMessageEventArgs>? ToastRequested;

    public void Show(string message, ToastType type = ToastType.Info)
    {
        ToastRequested?.Invoke(this, new ToastMessageEventArgs(message, type));
    }

    public void ShowSuccess(string message) => Show(message, ToastType.Success);
    public void ShowWarning(string message) => Show(message, ToastType.Warning);
    public void ShowError(string message) => Show(message, ToastType.Error);
}