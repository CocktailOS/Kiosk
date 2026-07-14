namespace CocktailOS.Kiosk.Models;

public static class RaspberryPiHeaderPins
{
    public static readonly IReadOnlyDictionary<int, int> BoardToBcm = new Dictionary<int, int>
    {
        [3] = 2, [5] = 3, [7] = 4, [8] = 14, [10] = 15, [11] = 17, [12] = 18, [13] = 27, [15] = 22,
        [16] = 23, [18] = 24, [19] = 10, [21] = 9, [22] = 25, [23] = 11, [24] = 8, [26] = 7, [27] = 0,
        [28] = 1, [29] = 5, [31] = 6, [32] = 12, [33] = 13, [35] = 19, [36] = 16, [37] = 26, [38] = 20, [40] = 21
    };

    public static bool IsConfigurablePin(string scheme, int pin) => scheme.Equals(PinNumberingSchemes.Board, StringComparison.OrdinalIgnoreCase)
        ? BoardToBcm.ContainsKey(pin)
        : pin is >= 0 and <= 27;
}
