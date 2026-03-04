using System.Collections.Generic;
using DevTools.Core.Models;

namespace DevTools.Core.Configuration;

public interface IProfileStore
{
    List<ToolProfile> LoadProfiles(string toolName);
    void SaveProfiles(string toolName, List<ToolProfile> profiles);
}

