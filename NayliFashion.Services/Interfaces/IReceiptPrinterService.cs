using NayliFashion.Core.Models.Customers;
using NayliFashion.Core.Models.Rentals;
using NayliFashion.Core.Models.Sales;
using NayliFashion.Services.DTOs;

namespace NayliFashion.Services.Interfaces;

/// <summary>
/// واجهة خدمة طباعة الإيصالات الحرارية (80mm / 58mm) وأوامر درج النقود
/// </summary>
public interface IReceiptPrinterService
{
    // طباعة إيصال مبيعات الكاشير الحراري
    Task<bool> PrintSaleReceiptAsync(SaleInvoice invoice);

    // طباعة عقد وإيصال الكراء
    Task<bool> PrintRentalContractReceiptAsync(RentalOrder order);

    // طباعة وصل قبض ديون الكارني
    Task<bool> PrintDebtReceiptAsync(CustomerTransaction transaction, Customer customer);

    // طباعة تقرير Z اليومي لإغلاق الصندوق
    Task<bool> PrintZReportAsync(ZReportDto zReport);

    // إرسال أمر فتح درج النقود الإلكتروني (Cash Drawer Kick)
    Task<bool> OpenCashDrawerAsync();
}
