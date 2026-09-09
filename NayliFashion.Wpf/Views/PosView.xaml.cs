using System.Windows;
using System.Windows.Controls;
using NayliFashion.Wpf.Helpers;
using NayliFashion.Wpf.ViewModels;

namespace NayliFashion.Wpf.Views;

/// <summary>
/// تفاعل الـ Code-Behind لواجهة PosView مع ربط مستمع الباركود العام
/// </summary>
public partial class PosView : UserControl
{
    private readonly GlobalBarcodeListener _barcodeListener = new();

    public PosView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _barcodeListener.Attach(this);
        _barcodeListener.BarcodeScanned += OnGlobalBarcodeScanned;

        if (DataContext is PosViewModel vm)
        {
            vm.RequestBarcodeFocus += FocusBarcodeBox;
            FocusBarcodeBox();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _barcodeListener.Detach(this);
        _barcodeListener.BarcodeScanned -= OnGlobalBarcodeScanned;

        if (DataContext is PosViewModel vm)
        {
            vm.RequestBarcodeFocus -= FocusBarcodeBox;
        }
    }

    private async void OnGlobalBarcodeScanned(string barcode)
    {
        if (DataContext is PosViewModel vm)
        {
            await vm.ProcessBarcodeDirectAsync(barcode);
        }
    }

    private void FocusBarcodeBox()
    {
        Dispatcher.InvokeAsync(() =>
        {
            BarcodeBox?.Focus();
            BarcodeBox?.SelectAll();
        });
    }
}
