using System.Text;
using Microsoft.EntityFrameworkCore;
using NayliFashion.Core.Models.Customers;
using NayliFashion.Core.Models.Rentals;
using NayliFashion.Core.Models.Sales;
using NayliFashion.Data.Context;
using NayliFashion.Services.DTOs;
using NayliFashion.Services.Helpers;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Services.Implementations;

/// <summary>
/// تطبيق خدمة تجهيز وطباعة الإيصالات الحرارية (80mm / 58mm) وأوامر درج النقود
/// متوافق مع معايير الفوترة الجزائرية والدفع عبر بريدي موب
/// </summary>
public class ReceiptPrinterService : IReceiptPrinterService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ReceiptPrinterService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<bool> PrintSaleReceiptAsync(SaleInvoice invoice)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var setting = await context.AppSettings.FirstOrDefaultAsync();

        string receiptText = GenerateSaleReceiptText(invoice, setting);

        // 1. حفظ صورة الإيصال في مجلد الإيصالات المحلي للأرشفة والمعاينة السريعة
        string receiptsDir = Path.Combine(@"D:\repos\NayliFashion", "Receipts");
        Directory.CreateDirectory(receiptsDir);
        string filePath = Path.Combine(receiptsDir, $"{invoice.InvoiceNumber}.txt");
        await File.WriteAllTextAsync(filePath, receiptText, Encoding.UTF8);

        // 2. الطباعة المباشرة عبر أوامر ESC/POS إلى الطابعة الحرارية
        await PrintRawTicketAsync(receiptText, setting);

        // 3. فتح درج النقود تلقائياً بعد البيع إذا كان مفعلاً
        if (setting?.AutoOpenCashDrawerOnPrint == true)
        {
            await OpenCashDrawerAsync();
        }

        return true;
    }

    public async Task<bool> PrintRentalContractReceiptAsync(RentalOrder order)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var setting = await context.AppSettings.FirstOrDefaultAsync();

        var sb = new StringBuilder();
        sb.AppendLine("========================================");
        sb.AppendLine($"         {setting?.StoreName ?? "Nayli Fashion"}         ");
        sb.AppendLine("         عقد وتذكرة كراء أزياء وأفرشة      ");
        sb.AppendLine("========================================");
        sb.AppendLine($"رقم العقد: {order.ContractNumber}");
        sb.AppendLine($"التاريخ: {order.OrderDate:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"العميل: {order.Customer.FullName} {order.Customer.FamilyName}");
        sb.AppendLine($"الهاتف: {order.Customer.PhoneNumber}");
        sb.AppendLine("----------------------------------------");
        sb.AppendLine($"تاريخ الاستلام: {order.EventStartDate:yyyy-MM-dd}");
        sb.AppendLine($"تاريخ الإرجاع المتوقع: {order.ExpectedReturnDate:yyyy-MM-dd}");
        sb.AppendLine($"وثيقة الضمان: {order.GuaranteeType} ({order.GuaranteeDocumentNumber ?? "مسجلة"})");
        sb.AppendLine($"مكان الحفظ بالخزنة: {order.GuaranteeSafeLocation ?? "الخزنة المركزية"}");
        sb.AppendLine("----------------------------------------");
        sb.AppendLine($"إجمالي الكراء: {order.TotalRentFeeDzd:N2} دج");
        sb.AppendLine($"العربون المدفوع: {order.AdvancePaidDzd:N2} دج");
        sb.AppendLine($"المتبقي عند الاستلام: {order.RemainingRentFeeDzd:N2} دج");
        sb.AppendLine($"مبلغ التأمين النقدي: {order.SecurityDepositCashDzd:N2} دج");
        sb.AppendLine("----------------------------------------");
        sb.AppendLine("* تنبيه: غرامة التأخير اليومية: " + order.LateFeePerDayDzd.ToString("N0") + " دج/يوم");
        sb.AppendLine("* يمنع غسل الفستان في البيت - التنظيف على المحل");
        sb.AppendLine("========================================");

        string receiptsDir = Path.Combine(@"D:\repos\NayliFashion", "Receipts");
        Directory.CreateDirectory(receiptsDir);
        string filePath = Path.Combine(receiptsDir, $"{order.ContractNumber}.txt");
        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);

        await PrintRawTicketAsync(sb.ToString(), setting);

        return true;
    }

    public async Task<bool> PrintDebtReceiptAsync(CustomerTransaction transaction, Customer customer)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var setting = await context.AppSettings.FirstOrDefaultAsync();

        var sb = new StringBuilder();
        sb.AppendLine("========================================");
        sb.AppendLine($"         {setting?.StoreName ?? "Nayli Fashion"}         ");
        sb.AppendLine("         سند قبض وتسديد ديون (الكارني)     ");
        sb.AppendLine("========================================");
        sb.AppendLine($"رقم السند: {transaction.ReferenceNumber}");
        sb.AppendLine($"التاريخ: {transaction.TransactionDate:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"العميل: {customer.FullName} {customer.FamilyName} ({customer.Nickname ?? ""})");
        sb.AppendLine($"الهاتف: {customer.PhoneNumber}");
        sb.AppendLine("----------------------------------------");
        sb.AppendLine($"المبلغ المسدد: {transaction.CreditAmount:N2} دج");
        sb.AppendLine($"بالسنتيم: {ArabicTafqeetHelper.ToPopularCentimes(transaction.CreditAmount)}");
        sb.AppendLine($"التفقيط: {ArabicTafqeetHelper.ToWords(transaction.CreditAmount)}");
        sb.AppendLine("----------------------------------------");
        sb.AppendLine($"الرصيد المتبقي في الذمة: {transaction.BalanceAfter:N2} دج");
        sb.AppendLine("========================================");
        sb.AppendLine("        توقيع المستلم / الختم            ");
        sb.AppendLine("\n\n");

        string receiptsDir = Path.Combine(@"D:\repos\NayliFashion", "Receipts");
        Directory.CreateDirectory(receiptsDir);
        string filePath = Path.Combine(receiptsDir, $"{transaction.ReferenceNumber}.txt");
        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);

        await PrintRawTicketAsync(sb.ToString(), setting);

        return true;
    }

    public async Task<bool> PrintZReportAsync(ZReportDto z)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var setting = await context.AppSettings.FirstOrDefaultAsync();

        var sb = new StringBuilder();
        sb.AppendLine("========================================");
        sb.AppendLine("         تقرير Z اليومي لإغلاق الصندوق     ");
        sb.AppendLine("========================================");
        sb.AppendLine($"رقم الوردية: {z.ShiftNumber}");
        sb.AppendLine($"الكاشير: {z.CashierName}");
        sb.AppendLine($"تاريخ الفتح: {z.OpenedAt:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"تاريخ الإغلاق: {z.ClosedAt:yyyy-MM-dd HH:mm}");
        sb.AppendLine("----------------------------------------");
        sb.AppendLine($"العهدة الافتتاحية: {z.OpeningFloatDzd:N2} دج");
        sb.AppendLine($"مبيعات الكاش النقدية: {z.TotalCashSalesDzd:N2} دج");
        sb.AppendLine($"مبيعات بريدي موب: {z.TotalBaridiMobSalesDzd:N2} دج");
        sb.AppendLine($"مبيعات البطاقة TPE: {z.TotalCardSalesDzd:N2} دج");
        sb.AppendLine($"تحصيلات ديون الكارني: {z.TotalDebtCollectionsDzd:N2} دج");
        sb.AppendLine($"المصروفات النثرية: -{z.TotalExpensesDzd:N2} دج");
        sb.AppendLine($"مدفوعات الموردين: -{z.TotalSupplierPayoutsDzd:N2} دج");
        sb.AppendLine("----------------------------------------");
        sb.AppendLine($"النقد المحسوب بالدرج: {z.ExpectedCashInDrawerDzd:N2} دج");
        sb.AppendLine($"النقد الفعلي المعدود: {z.ActualCountedCashDzd:N2} دج");
        sb.AppendLine($"الفارق (عجز/زيادة): {z.VarianceDifferenceDzd:N2} دج");
        sb.AppendLine($"عدد الفواتير المنفذة: {z.InvoicesCount}");
        sb.AppendLine($"عدد عقود الكراء: {z.RentalOrdersCount}");
        sb.AppendLine("========================================");

        string receiptsDir = Path.Combine(@"D:\repos\NayliFashion", "Receipts");
        Directory.CreateDirectory(receiptsDir);
        string filePath = Path.Combine(receiptsDir, $"Z_{z.ShiftNumber}.txt");
        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);

        await PrintRawTicketAsync(sb.ToString(), setting);

        return true;
    }

    public async Task<bool> OpenCashDrawerAsync()
    {
        // أمر فتح الدرج القياسي لطابعات ESC/POS: ESC p 0 25 250 (0x1B, 0x70, 0x00, 0x19, 0xFA)
        byte[] drawerKickCommand = new byte[] { 27, 112, 0, 25, 250 };
        
        using var context = await _contextFactory.CreateDbContextAsync();
        var setting = await context.AppSettings.FirstOrDefaultAsync();

        if (!string.IsNullOrWhiteSpace(setting?.ThermalPrinterName))
        {
            RawPrinterHelper.SendBytesToPrinter(setting.ThermalPrinterName, drawerKickCommand);
        }

        return true;
    }

    private Task<bool> PrintRawTicketAsync(string text, NayliFashion.Core.Models.System.AppSetting? setting)
    {
        if (string.IsNullOrWhiteSpace(setting?.ThermalPrinterName) || string.IsNullOrWhiteSpace(text))
            return Task.FromResult(false);

        try
        {
            var printBytes = new List<byte>();
            // تهيئة الطابعة: ESC @
            printBytes.AddRange(new byte[] { 27, 64 });

            // ضبط المحاذاة للوسط: ESC a 1
            printBytes.AddRange(new byte[] { 27, 97, 1 });

            // الترميز
            Encoding encoding;
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                encoding = Encoding.GetEncoding(1256); // Arabic Windows-1256
            }
            catch
            {
                encoding = Encoding.UTF8;
            }

            printBytes.AddRange(encoding.GetBytes(text));
            printBytes.AddRange(new byte[] { 10, 10, 10, 10 }); // أسطر فارغة

            // قص الورق الجزئي: GS V 66 0
            printBytes.AddRange(new byte[] { 29, 86, 66, 0 });

            bool result = RawPrinterHelper.SendBytesToPrinter(setting.ThermalPrinterName, printBytes.ToArray());
            return Task.FromResult(result);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private string GenerateSaleReceiptText(SaleInvoice invoice, NayliFashion.Core.Models.System.AppSetting? setting)
    {
        var sb = new StringBuilder();
        int width = setting?.ThermalPaperWidthMm == 58 ? 32 : 42;

        sb.AppendLine("========================================");
        sb.AppendLine($"         {setting?.StoreName ?? "Nayli Fashion"}         ");
        if (!string.IsNullOrWhiteSpace(setting?.StoreSlogan))
            sb.AppendLine($"       {setting.StoreSlogan}       ");
        sb.AppendLine($"الهاتف: {setting?.StorePhoneNumber ?? "027 00 00 00"}");
        sb.AppendLine($"العنوان: {setting?.StoreAddress ?? "الجلفة"}");

        if (!string.IsNullOrWhiteSpace(setting?.TaxNumberNif))
            sb.AppendLine($"NIF: {setting.TaxNumberNif} | RC: {setting.CommercialRegisterRc}");

        sb.AppendLine("========================================");
        sb.AppendLine($"فاتورة رقم: {invoice.InvoiceNumber}");
        sb.AppendLine($"التاريخ: {invoice.InvoiceDate:yyyy-MM-dd HH:mm}");
        if (invoice.Customer != null)
            sb.AppendLine($"الزبون: {invoice.Customer.FullName} {invoice.Customer.FamilyName}");
        sb.AppendLine("----------------------------------------");

        sb.AppendLine(string.Format("{0,-18} {1,5} {2,8} {3,8}", "الصنف", "الكمية", "السعر", "الإجمالي"));
        sb.AppendLine("----------------------------------------");

        foreach (var item in invoice.Items)
        {
            string name = item.ProductVariant?.VariantName ?? "صنف";
            if (name.Length > 18) name = name.Substring(0, 18);

            sb.AppendLine(string.Format("{0,-18} {1,5:0.##} {2,8:N0} {3,8:N0}",
                name,
                item.Quantity,
                item.UnitPrice,
                item.TotalLineAmount));
        }

        sb.AppendLine("----------------------------------------");
        sb.AppendLine($"المجموع الإجمالي: {invoice.GrossTotalAmount:N2} دج");
        if (invoice.DiscountAmount > 0)
            sb.AppendLine($"الخصم: -{invoice.DiscountAmount:N2} دج");

        sb.AppendLine($"الصافي للدفع: {invoice.NetTotalAmount:N2} دج");
        sb.AppendLine($"بالسنتيم: [{ArabicTafqeetHelper.ToPopularCentimes(invoice.NetTotalAmount)}]");
        sb.AppendLine($"التفقيط: {ArabicTafqeetHelper.ToWords(invoice.NetTotalAmount)}");
        sb.AppendLine("----------------------------------------");
        sb.AppendLine($"المدفوع: {invoice.PaidAmount:N2} دج ({invoice.PaymentMethod})");

        if (invoice.ChangeDueAmount > 0)
            sb.AppendLine($"الصرف المتبقي للزبون: {invoice.ChangeDueAmount:N2} دج");

        if (invoice.RemainingDebtAmount > 0)
            sb.AppendLine($"المتبقي كدين (كريدي): {invoice.RemainingDebtAmount:N2} دج");

        if (!string.IsNullOrWhiteSpace(setting?.BaridiMobRip))
        {
            sb.AppendLine("----------------------------------------");
            sb.AppendLine("للدفع عبر بريدي موب (RIP):");
            sb.AppendLine($"{setting.BaridiMobRip}");
        }

        sb.AppendLine("========================================");
        sb.AppendLine(setting?.ReceiptFooterMessage ?? "شكراً لزيارتكم");
        sb.AppendLine("========================================");

        return sb.ToString();
    }
}
