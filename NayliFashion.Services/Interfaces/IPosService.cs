using NayliFashion.Core.Models.Sales;
using NayliFashion.Services.DTOs;

namespace NayliFashion.Services.Interfaces;

/// <summary>
/// واجهة خدمة نقطة البيع والكاشير، إدارة السلال، والدفع
/// </summary>
public interface IPosService
{
    // حساب الملخص المالي اللحظي للسلة
    CartSummaryDto CalculateSummary(List<CartItemDto> items, decimal overallDiscount = 0m, decimal vatRatePercentage = 0m);

    // إتمام الفاتورة وتحديث المخزون والديون وحركة الصندوق
    Task<CheckoutResultDto> ProcessCheckoutAsync(CheckoutRequestDto request);

    // حفظ واسترجاع مسودات السلال (التعافي من انقطاع الكهرباء وتعليق السلال)
    Task SaveActiveDraftCartAsync(int userId, string cartIdentifier, List<CartItemDto> items, int? customerId);
    Task<List<CartItemDto>?> LoadActiveDraftCartAsync(int userId, string cartIdentifier);
    Task ClearActiveDraftCartAsync(int userId, string cartIdentifier);

    // إدارة المرتجعات
    Task<SaleInvoice> ProcessReturnAsync(int originalInvoiceId, List<CartItemDto> returnedItems, int cashierUserId, int? activeShiftId, string? reason);

    // الاستعلام عن الفواتير
    Task<SaleInvoice?> GetInvoiceByIdAsync(int invoiceId);
    Task<List<SaleInvoice>> GetInvoicesListAsync(DateTime? fromDate = null, DateTime? toDate = null, int? customerId = null);
}
