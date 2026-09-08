using NayliFashion.Core.Enums;

namespace NayliFashion.Services.DTOs;

/// <summary>
/// بند في سلة مشتريات نقطة البيع (الكاشير)
/// </summary>
public class CartItemDto
{
    public int ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string VariantName { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string? ImagePath { get; set; }

    public ProductType ProductType { get; set; }

    public decimal Quantity { get; set; } = 1.0m;
    public decimal UnitPrice { get; set; }
    public decimal UnitCostPrice { get; set; }
    public decimal DiscountAmount { get; set; } = 0m;

    public int? SelectedFabricRollId { get; set; }
    public string? RollCode { get; set; }

    public decimal LineTotal => Math.Max(0, (Quantity * UnitPrice) - DiscountAmount);
    public decimal LineNetProfit => LineTotal - (Quantity * UnitCostPrice);
}

/// <summary>
/// ملخص مالي لسلة المشتريات
/// </summary>
public class CartSummaryDto
{
    public decimal SubTotalGross { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TaxVatAmount { get; set; }
    public decimal NetTotalDzd { get; set; }

    public string NetTotalInWords => Helpers.ArabicTafqeetHelper.ToWords(NetTotalDzd);
    public string NetTotalInPopularCentimes => Helpers.ArabicTafqeetHelper.ToPopularCentimes(NetTotalDzd);
}

/// <summary>
/// بيانات طلب إتمام الفاتورة والدفع من الكاشير
/// </summary>
public class CheckoutRequestDto
{
    public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
    public int? CustomerId { get; set; }
    public int CashierUserId { get; set; }
    public int? ActiveCashShiftId { get; set; }

    public InvoiceType InvoiceType { get; set; } = InvoiceType.RetailSale;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashDZD;

    public decimal OverallInvoiceDiscount { get; set; } = 0m;
    public decimal PaidAmount { get; set; } = 0m;
    public string? BaridiMobTransactionNumber { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// نتيجة إتمام الفاتورة
/// </summary>
public class CheckoutResultDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal TotalAmountDzd { get; set; }
    public decimal PaidAmountDzd { get; set; }
    public decimal ChangeDueDzd { get; set; }
    public decimal RemainingDebtDzd { get; set; }
}
