using ECS.Domain.Exceptions;
using ECS.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace ECS.Domain.UnitTests;

public class ValueObjectTests
{
    [Fact]
    public void Money_rounds_to_two_decimals()
        => Money.Create(10.005m).Amount.Should().Be(10.01m);

    [Fact]
    public void Money_rejects_negative_amount()
    {
        var act = () => Money.Create(-1);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Money_add_requires_same_currency()
    {
        var a = Money.Create(10, "TRY");
        var b = Money.Create(5, "EUR");

        var act = () => a.Add(b);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Money_add_same_currency_sums()
        => Money.Create(10).Add(Money.Create(5)).Amount.Should().Be(15);

    [Theory]
    [InlineData(" 34 abc 123 ", "34ABC123")]
    [InlineData("06bk1234", "06BK1234")]
    public void PlateNumber_normalizes(string raw, string expected)
        => PlateNumber.Normalize(raw).Should().Be(expected);

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("ab")]
    public void PlateNumber_rejects_invalid(string raw)
    {
        var act = () => PlateNumber.Normalize(raw);
        act.Should().Throw<DomainException>();
    }
}
