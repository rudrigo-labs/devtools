using System.Collections.Generic;
using DevTools.Core.Models;

namespace DevTools.Core.Configuration;

public interface IToolConfigurationStore
{
    List<ToolConfiguration> LoadConfigurations(string toolName);
    void SaveConfigurations(string toolName, List<ToolConfiguration> configurations);
}



