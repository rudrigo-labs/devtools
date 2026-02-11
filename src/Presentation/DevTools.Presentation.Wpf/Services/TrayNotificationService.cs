using System;
using System.Linq;
using H.NotifyIcon;

namespace DevTools.Presentation.Wpf.Services;

internal sealed class TrayNotificationService
{
    private readonly TaskbarIcon _taskbarIcon;

    public TrayNotificationService(TaskbarIcon taskbarIcon)
    {
        _taskbarIcon = taskbarIcon;
    }

    public void UpdateTooltip(string text)
    {
        _taskbarIcon.ToolTipText = text;
    }

    public bool TryShowNotification(string title, string message, bool success)
    {
        try
        {
            var iconName = success ? "Info" : "Error";
            var methods = _taskbarIcon.GetType()
                .GetMethods()
                .Where(m => m.Name == "ShowNotification" && m.GetParameters().Length == 3)
                .ToArray();

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                if (parameters[0].ParameterType != typeof(string) ||
                    parameters[1].ParameterType != typeof(string))
                {
                    continue;
                }

                if (!parameters[2].ParameterType.IsEnum)
                {
                    continue;
                }

                var iconValue = Enum.Parse(parameters[2].ParameterType, iconName, true);
                method.Invoke(_taskbarIcon, new[] { title, message, iconValue });
                return true;
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error("Falha ao exibir notificacao no tray.", ex);
        }

        return false;
    }
}
