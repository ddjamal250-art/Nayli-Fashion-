using NayliFashion.Core.Models.Purchasing;

namespace NayliFashion.Services.Interfaces;

/// <summary>
/// واجهة خدمة المشتريات والموردين وحساب متوسط التكلفة المرجح (WAC)
/// </summary>
public interface IPurchasingService
{
    Task<List<Supplier>> GetAllSuppliersAsync();
    Task<Supplier?> GetSupplierByIdAsync(int id);
    Task<Supplier> CreateSupplierAsync(Supplier supplier);
    Task<Supplier> UpdateSupplierAsync(Supplier supplier);

    // تسجيل فاتورة شراء جديدة وتحديث المخزون ومتوسط التكلفة WAC
    Task<PurchaseInvoice> CreatePurchaseInvoiceAsync(PurchaseInvoice invoice);

    // سداد دفعة للمورد
    Task<SupplierTransaction> RecordSupplierPaymentAsync(int supplierId, decimal amountDzd, int userId, int? activeShiftId, string? notes = null);

    // كشف حساب المورد
    Task<List<SupplierTransaction>> GetSupplierStatementAsync(int supplierId);
}
