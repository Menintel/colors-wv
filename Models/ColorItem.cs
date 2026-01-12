using System;

namespace colors.Models;

public class ColorItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string HexColor { get; set; } = "#000000";
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ColorItem() { }

    public ColorItem(int r, int g, int b, string description = "")
    {
        if (r < 0 || r > 255) throw new ArgumentOutOfRangeException(nameof(r), "Value must be between 0 and 255.");
        if (g < 0 || g > 255) throw new ArgumentOutOfRangeException(nameof(g), "Value must be between 0 and 255.");
        if (b < 0 || b > 255) throw new ArgumentOutOfRangeException(nameof(b), "Value must be between 0 and 255.");
        R = r;
        G = g;
        B = b;
        HexColor = $"#{r:X2}{g:X2}{b:X2}";
    }

    public static ColorItem? FromHex(string hex, string description = "")
    {
        if (string.IsNullOrEmpty(hex)) return null;

        hex = hex.Replace("#", "");
        if (hex.Length != 6) return null;

        try
        {
            int r = Convert.ToInt32(hex.Substring(0, 2), 16);
            int g = Convert.ToInt32(hex.Substring(2, 2), 16);
            int b = Convert.ToInt32(hex.Substring(4, 2), 16);
        return new ColorItem(r, g, b, description);
        }
        catch
        {
            return null;
        }
    }

    public Windows.UI.Color ToWindowsColor()
    {
        return Windows.UI.Color.FromArgb(255, (byte)R, (byte)G, (byte)B);
    }


}