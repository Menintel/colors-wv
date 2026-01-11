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
        R = r;
        G = g;
        B = b;
        HexColor = $"#{r:X2}{g:X2}{b:X2}";
        Description = description;
    }

    public static ColorItem? FromHex(string hex, string description = "")
    {
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