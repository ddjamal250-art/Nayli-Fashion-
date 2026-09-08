using CommunityToolkit.Mvvm.ComponentModel;

namespace NayliFashion.Wpf.ViewModels;

/// <summary>
/// الفئة الأساسية لنماذج العرض (MVVM ViewModel Base)
/// </summary>
public abstract partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _busyMessage = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _successMessage = string.Empty;

    public void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
