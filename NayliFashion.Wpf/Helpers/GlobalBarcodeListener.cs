using System;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace NayliFashion.Wpf.Helpers;

/// <summary>
/// مستمع الباركود العام فائق السرعة
/// يلتقط إشارات أجهزة قراءة الباركود الليزرية تلقائياً من أي مكان في الواجهة
/// دون الحاجة للنقر بالفأرة أو التركيز على حقل محدد (Zero-Click Global Barcode Scanner)
/// </summary>
public class GlobalBarcodeListener
{
    private readonly StringBuilder _buffer = new();
    private DateTime _lastKeystrokeTime = DateTime.MinValue;
    private const int MaxInterKeyDelayMilliseconds = 60; // فارق توقيت ضربات القارئ الليزري مقارنة بالإنسان

    public event Action<string>? BarcodeScanned;

    public void Attach(UIElement element)
    {
        element.PreviewTextInput += OnPreviewTextInput;
        element.PreviewKeyDown += OnPreviewKeyDown;
    }

    public void Detach(UIElement element)
    {
        element.PreviewTextInput -= OnPreviewTextInput;
        element.PreviewKeyDown -= OnPreviewKeyDown;
    }

    private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        var now = DateTime.Now;
        var elapsed = (now - _lastKeystrokeTime).TotalMilliseconds;
        _lastKeystrokeTime = now;

        if (elapsed > MaxInterKeyDelayMilliseconds && _buffer.Length > 0)
        {
            // انقضى وقت طويل (إدخال يدوي من المستخدم وليس قارئ باركود)
            _buffer.Clear();
        }

        if (!string.IsNullOrEmpty(e.Text))
        {
            _buffer.Append(e.Text);
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return || e.Key == Key.Enter)
        {
            var now = DateTime.Now;
            var elapsed = (now - _lastKeystrokeTime).TotalMilliseconds;

            if (_buffer.Length >= 3 && elapsed < 200)
            {
                string scanned = _buffer.ToString().Trim();
                _buffer.Clear();

                if (!string.IsNullOrEmpty(scanned))
                {
                    e.Handled = true;
                    BarcodeScanned?.Invoke(scanned);
                }
            }
            else
            {
                _buffer.Clear();
            }
        }
    }
}