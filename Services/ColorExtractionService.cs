using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using colors.Models;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace colors.Services;

public class ColorExtractionService
{
    public async Task<List<ColorItem>> ExtractDominantColorsAsync(StorageFile imageFile, int colorCount)
    {
        if (imageFile == null)
            throw new ArgumentNullException(nameof(imageFile));
        if (colorCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(colorCount), "Color count must be positive.");
        
        try
        {
            using (IRandomAccessStream stream = await imageFile.OpenAsync(FileAccessMode.Read))
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Rgba8,
                    BitmapAlphaMode.Straight,
                    new BitmapTransform(),
                    ExifOrientationMode.RespectExifOrientation,
                    ColorManagementMode.ColorManageToSRgb
                );

                byte[] pixels = pixelData.DetachPixelData();

                // Use k-means clustering to find dominant colors
                var dominantColors = ExtractColorsUsingKMeans(pixels, colorCount);

                return dominantColors.Select(c => new ColorItem(c.R, c.G, c.B)).ToList();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error extracting colors: {ex.Message}");
            return new List<ColorItem>();
        }
    }

    private List<(int R, int G, int B)> ExtractColorsUsingKMeans(byte[] pixels, int k)
    {
        // Sample pixels (every 40th pixel for performance, 4 bytes per pixel * 10 steps = 40)
        var samples = new List<(int R, int G, int B)>();
        for (int i = 0; i < pixels.Length; i += 40) 
        {
            if (i + 2 < pixels.Length)
            {
                samples.Add((pixels[i], pixels[i + 1], pixels[i + 2]));
            }
        }

        if (samples.Count == 0)
            return new List<(int R, int G, int B)>();

        // Ensure k doesn't exceed available samples
        k = Math.Min(k, samples.Count);

        // Initialize centroids randomly
        var random = new Random();
        var centroids = samples.OrderBy(x => random.Next()).Take(k).ToList();

        // K-means iterations
        for (int iteration = 0; iteration < 20; iteration++)
        {
            var clusters = new List<List<(int R, int G, int B)>>();
            for (int i = 0; i < k; i++)
            {
                clusters.Add(new List<(int R, int G, int B)>());
            }

            // Assign samples to nearest centroid
            foreach (var sample in samples)
            {
                int nearestIndex = 0;
                double nearestDistance = double.MaxValue;

                for (int i = 0; i < centroids.Count; i++)
                {
                    double distance = ColorDistance(sample, centroids[i]);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestIndex = i;
                    }
                }

                clusters[nearestIndex].Add(sample);
            }

            // Update centroids
            for (int i = 0; i < k; i++)
            {
                if (clusters[i].Count > 0)
                {
                    int avgR = (int)clusters[i].Average(c => c.R);
                    int avgG = (int)clusters[i].Average(c => c.G);
                    int avgB = (int)clusters[i].Average(c => c.B);
                    centroids[i] = (avgR, avgG, avgB);
                }
            }
        }

        return centroids;
    }

    private double ColorDistance((int R, int G, int B) c1, (int R, int G, int B) c2)
    {
        int dr = c1.R - c2.R;
        int dg = c1.G - c2.G;
        int db = c1.B - c2.B;
        return Math.Sqrt(dr * dr + dg * dg + db * db);
    }

    public async Task<ColorItem?> GetColorAtPositionAsync(StorageFile imageFile, int x, int y)
    {
        if (imageFile == null)
             throw new ArgumentNullException(nameof(imageFile));

        try
        {
            using (IRandomAccessStream stream = await imageFile.OpenAsync(FileAccessMode.Read))
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                if (x >= decoder.PixelWidth || y >= decoder.PixelHeight || x < 0 || y < 0)
                    return null;

                PixelDataProvider pixelData = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Rgba8,
                    BitmapAlphaMode.Straight,
                    new BitmapTransform(),
                    ExifOrientationMode.RespectExifOrientation,
                    ColorManagementMode.ColorManageToSRgb
                );

                byte[] pixels = pixelData.DetachPixelData();
                int pixelIndex = (y * (int)decoder.PixelWidth + x) * 4;

                if (pixelIndex >= 0 && pixelIndex + 2 < pixels.Length)
                {
                    return new ColorItem(pixels[pixelIndex], pixels[pixelIndex + 1], pixels[pixelIndex + 2]);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting color at position: {ex.Message}");
        }

        return null;
    }
}
