using NayliFashion.Core.Models.Finance;
using NayliFashion.Services.DTOs;

namespace NayliFashion.Services.Interfaces;

/// <summary>
/// واجهة خدمة إدارة الصندوق، الورديات، والمطابقة المحاسبية (Z-Report)
/// </summary>
public interface ICashShiftService
{
    Task<CashShift?> GetActiveShiftForUserAsync(int userId);
    Task<CashShift?> GetCurrentActiveStoreShiftAsync();

    Task<CashShift> OpenShiftAsync(int userId, decimal openingFloatDzd);
    Task<ZReportDto> CloseShiftAsync(int shiftId, decimal actualCountedCashDzd, string? closingNotes);

    Task RecordExpenseAsync(int? shiftId, int userId, string category, decimal amountDzd, string? beneficiary, string? notes);
    Task RecordManualCashMovementAsync(int shiftId, string movementType, decimal amountDzd, string reason);

    Task<List<CashShift>> GetShiftHistoryAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<ZReportDto> GenerateShiftSummaryAsync(int shiftId);
}
