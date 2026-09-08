using NayliFashion.Core.Enums;

namespace NayliFashion.Wpf.ViewModels;

public static class PaymentMethodEnumWrapper
{
    public static PaymentMethod CashDZD => PaymentMethod.CashDZD;
    public static PaymentMethod BaridiMobRIP => PaymentMethod.BaridiMobRIP;
    public static PaymentMethod BankCardTPE => PaymentMethod.BankCardTPE;
    public static PaymentMethod CustomerDebtCarnet => PaymentMethod.CustomerDebtCarnet;
}
