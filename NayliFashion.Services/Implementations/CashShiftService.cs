using Microsoft.EntityFrameworkCore;
using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Finance;
using NayliFashion.Data.Context;
using NayliFashion.Services.DTOs;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Services.Implementations;

/// <summary>
/// تطبيق خدمة إدارة الصندوق والورديات اليومية والمطابقة المحاسبية (Z-Report)
/// </summary>
public class CashShiftService : ICashShiftService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public CashShiftService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CashShift?> GetActiveShiftForUserAsync(int userId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.CashShifts
                            .Include(s => s.User)
                            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == CashShiftStatus.OpenActive);
    }

    public async Task<CashShift?> GetCurrentActiveStoreShiftAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.CashShifts
                            .Include(s => s.User)
                            .OrderByDescending(s => s.OpenedAt)
                            .FirstOrDefaultAsync(s => s.Status == CashShiftStatus.OpenActive);
    }

    public async Task<CashShift> OpenShiftAsync(int userId, decimal openingFloatDzd)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // فحص وجود وردية نشطة مفتوحة بالفعل
        var existing = await context.CashShifts.FirstOrDefaultAsync(s => s.UserId == userId && s.Status == CashShiftStatus.OpenActive);
        if (existing != null)
            return existing;

        int todayCount = await context.CashShifts.CountAsync(s => s.CreatedAt.Date == DateTime.UtcNow.Date);
        string shiftNumber = $"SHF-{DateTime.UtcNow:yyyyMMdd}-{(todayCount + 1):D3}";

        var shift = new CashShift
        {
            ShiftNumber = shiftNumber,
            UserId = userId,
            OpenedAt = DateTime.UtcNow,
            Status = CashShiftStatus.OpenActive,
            OpeningFloatBalanceDzd = openingFloatDzd,
            ExpectedCashInDrawerDzd = openingFloatDzd
        };

        await context.CashShifts.AddAsync(shift);
        await context.SaveChangesAsync();
        return shift;
    }

    public async Task<ZReportDto> CloseShiftAsync(int shiftId, decimal actualCountedCashDzd, string? closingNotes)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var shift = await context.CashShifts
                                 .Include(s => s.User)
                                 .FirstOrDefaultAsync(s => s.Id == shiftId);

        if (shift == null) throw new InvalidOperationException("الوردية غير موجودة");

        // الحساب الدقيق للنقد المفترض وجوده بالدرج
        decimal expectedCash = shift.OpeningFloatBalanceDzd
                             + shift.TotalCashSalesDzd
                             + shift.TotalDebtCollectionsDzd
                             - shift.TotalExpensesOutDzd
                             - shift.TotalSupplierPayoutsDzd;

        decimal variance = actualCountedCashDzd - expectedCash; // + زيادة، - عجز

        shift.ClosedAt = DateTime.UtcNow;
        shift.Status = CashShiftStatus.ClosedReconciled;
        shift.ExpectedCashInDrawerDzd = expectedCash;
        shift.ActualCountedCashDzd = actualCountedCashDzd;
        shift.CashVarianceDifferenceDzd = variance;
        shift.ClosingNotes = closingNotes;

        await context.SaveChangesAsync();

        int invoicesCount = await context.SaleInvoices.CountAsync(i => i.CashShiftId == shift.Id);
        int rentalsCount = await context.RentalOrders.CountAsync(r => r.CashShiftId == shift.Id);

        return new ZReportDto
        {
            ShiftNumber = shift.ShiftNumber,
            CashierName = shift.User.FullName,
            OpenedAt = shift.OpenedAt,
            ClosedAt = shift.ClosedAt.Value,
            OpeningFloatDzd = shift.OpeningFloatBalanceDzd,
            TotalCashSalesDzd = shift.TotalCashSalesDzd,
            TotalBaridiMobSalesDzd = shift.TotalBaridiMobSalesDzd,
            TotalCardSalesDzd = shift.TotalCardSalesDzd,
            TotalDebtCollectionsDzd = shift.TotalDebtCollectionsDzd,
            TotalExpensesDzd = shift.TotalExpensesOutDzd,
            TotalSupplierPayoutsDzd = shift.TotalSupplierPayoutsDzd,
            ExpectedCashInDrawerDzd = expectedCash,
            ActualCountedCashDzd = actualCountedCashDzd,
            VarianceDifferenceDzd = variance,
            InvoicesCount = invoicesCount,
            RentalOrdersCount = rentalsCount
        };
    }

    public async Task RecordExpenseAsync(int? shiftId, int userId, string category, decimal amountDzd, string? beneficiary, string? notes)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        var expense = new Expense
        {
            CashShiftId = shiftId,
            UserId = userId,
            ExpenseDate = DateTime.UtcNow,
            CategoryName = category,
            AmountDzd = amountDzd,
            BeneficiaryPerson = beneficiary,
            Notes = notes
        };

        await context.Expenses.AddAsync(expense);

        // خصم المصروف من رصيد الوردية المفتوحة
        if (shiftId.HasValue)
        {
            var shift = await context.CashShifts.FindAsync(shiftId.Value);
            if (shift != null)
            {
                shift.TotalExpensesOutDzd += amountDzd;
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task RecordManualCashMovementAsync(int shiftId, string movementType, decimal amountDzd, string reason)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var shift = await context.CashShifts.FindAsync(shiftId);
        if (shift == null) return;

        var tx = new CashTransaction
        {
            CashShiftId = shiftId,
            TransactionDate = DateTime.UtcNow,
            TransactionType = movementType,
            AmountDzd = amountDzd,
            Reason = reason
        };

        await context.CashTransactions.AddAsync(tx);

        if (movementType.Contains("إيداع"))
        {
            shift.TotalCashSalesDzd += amountDzd;
        }
        else if (movementType.Contains("سحب"))
        {
            shift.TotalExpensesOutDzd += amountDzd;
        }

        await context.SaveChangesAsync();
    }

    public async Task<List<CashShift>> GetShiftHistoryAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.CashShifts.Include(s => s.User).AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(s => s.OpenedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(s => s.OpenedAt <= toDate.Value);

        return await query.OrderByDescending(s => s.OpenedAt).ToListAsync();
    }

    public async Task<ZReportDto> GenerateShiftSummaryAsync(int shiftId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        var shift = await context.CashShifts.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == shiftId);
        if (shift == null) throw new InvalidOperationException("الوردية غير موجودة");

        decimal expected = shift.OpeningFloatBalanceDzd
                         + shift.TotalCashSalesDzd
                         + shift.TotalDebtCollectionsDzd
                         - shift.TotalExpensesOutDzd
                         - shift.TotalSupplierPayoutsDzd;

        int invoicesCount = await context.SaleInvoices.CountAsync(i => i.CashShiftId == shift.Id);
        int rentalsCount = await context.RentalOrders.CountAsync(r => r.CashShiftId == shift.Id);

        return new ZReportDto
        {
            ShiftNumber = shift.ShiftNumber,
            CashierName = shift.User.FullName,
            OpenedAt = shift.OpenedAt,
            ClosedAt = shift.ClosedAt ?? DateTime.UtcNow,
            OpeningFloatDzd = shift.OpeningFloatBalanceDzd,
            TotalCashSalesDzd = shift.TotalCashSalesDzd,
            TotalBaridiMobSalesDzd = shift.TotalBaridiMobSalesDzd,
            TotalCardSalesDzd = shift.TotalCardSalesDzd,
            TotalDebtCollectionsDzd = shift.TotalDebtCollectionsDzd,
            TotalExpensesDzd = shift.TotalExpensesOutDzd,
            TotalSupplierPayoutsDzd = shift.TotalSupplierPayoutsDzd,
            ExpectedCashInDrawerDzd = expected,
            ActualCountedCashDzd = shift.ActualCountedCashDzd,
            VarianceDifferenceDzd = shift.CashVarianceDifferenceDzd,
            InvoicesCount = invoicesCount,
            RentalOrdersCount = rentalsCount
        };
    }
}
