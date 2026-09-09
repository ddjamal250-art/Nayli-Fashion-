using System;

namespace NayliFashion.Wpf.Services;

public enum ToastType
{
    Success,
    Warning,
    Error,
    Info
}

public class ToastMessageEventArgs : EventArgs
{
    public string Message { get; }
    public ToastType Type { get; }

    public ToastMessageEventArgs(string message, ToastType type)
    {
        Message = message;
        Type = type;
    }
}

public interface IToastNotificationService
{
    event EventHandler<ToastMessageEventArgs>? ToastRequested;
    void Show(string message, ToastType type = ToastType.Info);
    void ShowSuccess(string message);
    void ShowWarning(string message);
    void ShowError(string message);
}