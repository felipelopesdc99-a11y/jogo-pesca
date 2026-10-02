using FishingIdle.Texts;
using Xunit;

namespace FishingIdle.GameService.Tests;

public sealed class FormatTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(999, "999")]
    [InlineData(1234567, "1.234.567")]
    public void Numbers_use_a_dot_for_thousands(long value, string expected) => Assert.Equal(expected, Format.Number(value));

    [Fact]
    public void Decimals_use_a_comma()
    {
        Assert.Equal("35,3", Format.Decimal(35.25, 1));
        Assert.Equal("1.234,5", Format.Decimal(1234.5, 1));
        Assert.Equal("10,3%", Format.Percent(0.103, 1));
        Assert.Equal("42,0 cm", Format.SizeCm(42));
    }

    [Fact]
    public void Dates_are_day_month_year_with_24h_time()
    {
        Assert.Equal("28/09/2026 21:05", Format.DateTime(new DateTime(2026, 9, 28, 21, 5, 0)));
    }

    [Fact]
    public void Countdowns_and_durations_read_naturally()
    {
        Assert.Equal("0:07", Format.Countdown(6.2));
        Assert.Equal("1:02:03", Format.Countdown(3723));
        Assert.Equal("30 segundos", Format.Duration(30));
        Assert.Equal("2 minutos", Format.Duration(120));
    }
}
