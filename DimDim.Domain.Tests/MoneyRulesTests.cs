using DimDim.Domain;
using Xunit;
public class MoneyRulesTests
{
    [Theory]
    [InlineData("0", true)] [InlineData("-10.25", true)] [InlineData("9999999999999999.99", true)]
    [InlineData("0.001", false)] [InlineData("10000000000000000", false)]
    public void ValidatesSqlMoneyPrecision(string input, bool expected) => Assert.Equal(expected, MoneyRules.IsValid(decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture)));
}
