
using colors.Models;
using System;
using System.Runtime.InteropServices;

namespace colors.Services;

public class ScreenColorPickerService
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    public ColorItem? GetColorAtCursor()
    {
        try
        {
            POINT point;
            if (!GetCursorPos(out point))
                return null;

            IntPtr hdc = GetDC(IntPtr.Zero);
            uint pixel = GetPixel(hdc, point.X, point.Y);
            ReleaseDC(IntPtr.Zero, hdc);

            int r = (int)(pixel & 0x000000FF);
            int g = (int)((pixel & 0x0000FF00) >> 8);
            int b = (int)((pixel & 0x00FF0000) >> 16);

            return new ColorItem(r, g, b);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting color at cursor: {ex.Message}");
            return null;
        }
    }

    public (int X, int Y) GetCursorPosition()
    {
        POINT point;
        GetCursorPos(out point);
        return (point.X, point.Y);
    }
}
