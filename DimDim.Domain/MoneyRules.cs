namespace DimDim.Domain;
public static class MoneyRules
{
    public static bool IsValid(decimal value) => value is >= -9999999999999999.99m and <= 9999999999999999.99m && decimal.Round(value, 2) == value;
}
