using NayliFashion.Core.Models.Common;

namespace NayliFashion.Core.Models.System;

/// <summary>
/// إعدادات النظام والهوية التجارية والجبائية للمحل
/// </summary>
public class AppSetting : BaseEntity
{
    public string StoreName { get; set; } = "Nayli Fashion - نايل فاشن";
    public string StoreSlogan { get; set; } = "للألبسة والأقمشة والعطور والكراء الفاخر";
    public string StoreAddress { get; set; } = "حي برنوس، ولاية الجلفة";
    public string StorePhoneNumber { get; set; } = "027 00 00 00 / 06 00 00 00 00";
    public string? StoreEmail { get; set; }

    // البيانات الجبائية والتجارية الرسمية (للإيصالات والفواتير)
    public string CommercialRegisterRc { get; set; } = string.Empty;
    public string TaxNumberNif { get; set; } = string.Empty;
    public string StatisticalNumberNis { get; set; } = string.Empty;
    public string ArticleImposition { get; set; } = string.Empty;

    // حساب بريدي موب للمحل للدفع السريع بالـ QR
    public string BaridiMobRip { get; set; } = string.Empty;
    public string? CcpAccountNumber { get; set; }

    // إعدادات الطابعة الحرارية
    public string? ThermalPrinterName { get; set; }
    public int ThermalPaperWidthMm { get; set; } = 80; // 80mm أو 58mm
    public bool AutoOpenCashDrawerOnPrint { get; set; } = true;
    public bool PrintBaridiMobQrOnReceipt { get; set; } = true;

    // إعدادات النسخ الاحتياطي
    public string BackupFolderPath { get; set; } = @"D:\repos\NayliFashion\Backups";
    public bool AutoBackupOnShiftClose { get; set; } = true;

    // نص تذييل وصل الكاشير
    public string ReceiptFooterMessage { get; set; } = "شكراً لزيارتكم - البضاعة المباعة لا ترد ولا تستبدل إلا خلال 48 ساعة";
}
