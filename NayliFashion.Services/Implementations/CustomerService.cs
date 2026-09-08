using Microsoft.EntityFrameworkCore;
using NayliFashion.Core.Models.Customers;
using NayliFashion.Data.Context;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Services.Implementations;

/// <summary>
/// تطبيق خدمة إدارة العملاء ودفتر الديون (Carnet) وسندات القبض وتنبيهات الفيرمون
/// </summary>
public class CustomerService : ICustomerService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public CustomerService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Customer>> SearchCustomersAsync(string? searchTerm = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Customers.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(c => c.FullName.ToLower().Contains(term) ||
                                     c.FamilyName.ToLower().Contains(term) ||
                                     (c.Nickname != null && c.Nickname.ToLower().Contains(term)) ||
                                     c.PhoneNumber.Contains(term) ||
                                     (c.NationalIdCardNumber != null && c.NationalIdCardNumber.Contains(term)));
        }

        return await query.OrderBy(c => c.FamilyName).ThenBy(c => c.FullName).ToListAsync();
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Customers
                            .Include(c => c.Transactions)
                            .Include(c => c.SaleInvoices)
                            .Include(c => c.RentalOrders)
                            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    public async Task<Customer> UpdateCustomerAsync(Customer customer)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        context.Customers.Update(customer);
        await context.SaveChangesAsync();
        return customer;
    }

    public async Task<bool> DeleteCustomerAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var customer = await context.Customers.FindAsync(id);
        if (customer == null) return false;

        customer.IsDeleted = true;
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<CustomerTransaction> ProcessDebtPaymentAsync(
        int customerId,
        decimal paymentAmountDzd,
        int cashierUserId,
        int? activeShiftId,
        string? notes = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        var customer = await context.Customers.FindAsync(customerId);
        if (customer == null) throw new InvalidOperationException("العميل غير موجود");

        if (paymentAmountDzd <= 0) throw new ArgumentException("مبلغ السداد يجب أن يكون أكبر من الصفر");

        // خصم المبلغ المسدد من رصيد دين العميل
        customer.CurrentDebtDzd = Math.Max(0m, customer.CurrentDebtDzd - paymentAmountDzd);

        var customerTx = new CustomerTransaction
        {
            CustomerId = customer.Id,
            TransactionDate = DateTime.UtcNow,
            TransactionType = "سند قبض نقدي (تسديد دين)",
            ReferenceNumber = $"REC-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(100, 999)}",
            DebitAmount = 0m,
            CreditAmount = paymentAmountDzd,
            BalanceAfter = customer.CurrentDebtDzd,
            Notes = notes ?? "تسديد دفعة نقدية من حساب الكارني"
        };

        await context.CustomerTransactions.AddAsync(customerTx);

        // إضافة المبلغ المحصل لخزينة وردية الكاشير النشطة
        if (activeShiftId.HasValue)
        {
            var shift = await context.CashShifts.FindAsync(activeShiftId.Value);
            if (shift != null)
            {
                shift.TotalDebtCollectionsDzd += paymentAmountDzd;
            }
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        return customerTx;
    }

    public async Task<List<CustomerTransaction>> GetCustomerStatementAsync(int customerId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.CustomerTransactions
                            .Where(t => t.CustomerId == customerId)
                            .OrderByDescending(t => t.TransactionDate)
                            .ToListAsync();
    }

    public async Task<List<Customer>> GetCustomersWithApproachingSalaryDueAsync(int targetDayThreshold = 3)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        int currentDay = DateTime.UtcNow.Day;

        // استخراج العملاء الذين لديهم رصيد دين وموعد الفيرمون يقترب أو حل موعده
        return await context.Customers
                            .Where(c => c.CurrentDebtDzd > 0 && c.SalaryVirementDay.HasValue)
                            .Where(c => Math.Abs(c.SalaryVirementDay!.Value - currentDay) <= targetDayThreshold ||
                                        (currentDay >= c.SalaryVirementDay.Value && currentDay <= c.SalaryVirementDay.Value + 5))
                            .OrderBy(c => c.SalaryVirementDay)
                            .ToListAsync();
    }
}
