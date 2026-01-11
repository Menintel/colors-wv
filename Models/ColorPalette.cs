using System;
using System.Collections.Generic;

namespace colors.Models;

public class ColorPalette
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public List<ColorItem> Colors { get; set; } = new List<ColorItem>();
    public string ReferenceImageUrl { get; set; } = string.Empty; // Firebase Storage URL
    public string ReferenceImagePath { get; set; } = string.Empty; // Local file path
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ColorPalette() { }

    public ColorPalette(string name, string projectId)
    {
        Name = name;
        ProjectId = projectId;
    }
}