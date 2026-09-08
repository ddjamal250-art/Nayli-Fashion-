using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Common;
using NayliFashion.Core.Models.Users;

namespace NayliFashion.Core.Models.Finance;

/// <summary>
/// وردية الصندوق اليومية للكاشير (Shift & Cash Drawer)
/// </summary>
public class CashShift : BaseEntity
{
    public string ShiftNumber { get; set; } = string.Empty; // رقم الوردية (SHF-2026-00001)
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public CashShiftStatus Status { get; set; } = CashShiftStatus.OpenActive;

    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    // العهدة والنقدية
    public decimal OpeningFloatBalanceDzd { get; set; } = 0m; // عهدة بداية اليوم

    // العمليات المنفذة في هذه الوردية
    public decimal TotalCashSalesDzd { get; set; } = 0m;
    public decimal TotalBaridiMobSalesDzd { get; set; } = 0m;
    public decimal TotalCardSalesDzd { get; set; } = 0m;
    public decimal TotalDebtCollectionsDzd { get; set; } = 0m;// مقبوضات ديون الكارني السابقة
    public decimal TotalExpensesOutDzd { get; set; } = 0m;    // مصاريف الصندوق النثرية
    public decimal TotalSupplierPayoutsDzd { get; set; } = 0m;// دفعات مسددة للموردين نقداً من الدرج

    // المطابقة المحاسبية والإغلاق الأعمى (Blind Close)
    public decimal ExpectedCashInDrawerDzd { get; set; } = 0m;// النقد المفترض محاسبياً بالدرج
    public decimal ActualCountedCashDzd { get; set; } = 0m;   // النقد الفعلي الذي عده الكاشير بيده
    public decimal CashVarianceDifferenceDzd { get; set; } = 0m;// الفارق (+ زيادة، - عجز بالصندوق)

    public string? ClosingNotes { get; set; }

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public virtual ICollection<CashTransaction> CashTransactions { get; set; } = new List<CashTransaction>();
}

/// <summary>
/// المصروفات النثرية والتشغيلية للمحل
/// </summary>
public class Expense : BaseEntity
{
    public int? CashShiftId { get; set; }
    public virtual CashShift? CashShift { get; set; }

    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
    public string CategoryName { get; set; } = "عام"; // كهرباء، غداء/ضيافة، صيانة، مصبغة فساتين، كراء المحل...
    public decimal AmountDzd { get; set; } = 0m;
    public string? BeneficiaryPerson { get; set; }   // الشخص أو الجهة المستلمة للمبلغ
    public string? Notes { get; set; }
}

/// <summary>
/// سجل الحركات النقدية للصندوق (إيداع وسحب مباشر)
/// </summary>
public class CashTransaction : BaseEntity
{
    public int CashShiftId { get; set; }
    public virtual CashShift CashShift { get; set; } = null!;

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string TransactionType { get; set; } = "إيداع"; // "إيداع نقدي", "سحب يدوي", "مصروف"
    public decimal AmountDzd { get; set; }
    public string Reason { get; set; } = string.Empty;
}
