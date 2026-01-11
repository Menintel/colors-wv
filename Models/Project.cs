using System;
using System.Collections.Generic;

namespace colors.Models;

public class Project
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = "";
    public List<string> PaletteIds { get; set; } = new List<string>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Project() { }

    public Project(string name, string description = "")
    {
        Name = name;
        Description = description;
    }
}
