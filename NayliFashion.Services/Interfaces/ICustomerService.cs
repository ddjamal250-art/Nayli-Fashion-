using NayliFashion.Core.Models.Customers;

namespace NayliFashion.Services.Interfaces;

/// <summary>
/// واجهة خدمة إدارة العملاء ودفتر الديون (Carnet) وتنبيهات الفيرمون
/// </summary>
public interface ICustomerService
{
    Task<List<Customer>> SearchCustomersAsync(string? searchTerm = null);
    Task<Customer?> GetCustomerByIdAsync(int id);
    Task<Customer> CreateCustomerAsync(Customer customer);
    Task<Customer> UpdateCustomerAsync(Customer customer);
    Task<bool> DeleteCustomerAsync(int id);

    // سداد الديون وإصدار سند القبض
    Task<CustomerTransaction> ProcessDebtPaymentAsync(int customerId, decimal paymentAmountDzd, int cashierUserId, int? activeShiftId, string? notes = null);

    // كشف حساب تفصيلي لحركات العميل
    Task<List<CustomerTransaction>> GetCustomerStatementAsync(int customerId);

    // تنبيهات الزبائن المستحقة ديونهم بالتزامن مع تواريخ الفيرمون ونزول الرواتب
    Task<List<Customer>> GetCustomersWithApproachingSalaryDueAsync(int targetDayThreshold = 3);
}
