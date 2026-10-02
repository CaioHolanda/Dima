using Dima.Core.Common;
using Dima.Core.Enums;
using Dima.Core.Models;
using Dima.Core.Models.Vouchers;

namespace Dima.Tests.Orders;

public class VoucherDiscountCalculatorTests
{
    [Fact]
    public void Calculate_without_voucher_returns_zero()
    {
        var result =
            VoucherDiscountCalculator.Calculate(100m, null);

        Assert.Equal(0m, result);
    }

    [Fact]
    public void Calculate_fixed_amount_returns_voucher_value()
    {
        var voucher = new Voucher
        {
            DiscountType = EVoucherDiscountType.FixedAmount,
            Value = 25.50m
        };

        var result =
            VoucherDiscountCalculator.Calculate(100m, voucher);

        Assert.Equal(25.50m, result);
    }

    [Fact]
    public void Calculate_never_returns_discount_greater_than_price()
    {
        var voucher = new Voucher
        {
            DiscountType = EVoucherDiscountType.FixedAmount,
            Value = 150m
        };

        var result =
            VoucherDiscountCalculator.Calculate(100m, voucher);

        Assert.Equal(100m, result);
    }

    [Fact]
    public void Calculate_percentage_returns_percentage_of_price()
    {
        var voucher = new Voucher
        {
            DiscountType = EVoucherDiscountType.Percentage,
            Value = 15m
        };

        var result =
            VoucherDiscountCalculator.Calculate(100m, voucher);

        Assert.Equal(15m, result);
    }

    [Fact]
    public void Calculate_percentage_rounds_to_two_decimal_places()
    {
        var voucher = new Voucher
        {
            DiscountType = EVoucherDiscountType.Percentage,
            Value = 10m
        };

        var result =
            VoucherDiscountCalculator.Calculate(10.05m, voucher);

        Assert.Equal(1.01m, result);
    }
}