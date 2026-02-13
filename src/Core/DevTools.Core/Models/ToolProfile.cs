using System;
using System.Collections.Generic;

namespace DevTools.Core.Models;

public class ToolProfile
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Options { get; set; } = new();
    public DateTime UpdatedUtc { get; set; }
}

public class ToolProfileContainer
{
    public List<ToolProfile> Profiles { get; set; } = new();
}
