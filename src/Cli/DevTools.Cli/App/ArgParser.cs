namespace DevTools.Cli.App;

public static class ArgParser
{
    public static CliLaunchOptions Parse(string[] args)
    {
        var options = new CliLaunchOptions();
        
        if (args.Length == 0)
            return options;

        // First arg is command name if it doesn't start with -
        int startIndex = 0;
        if (!args[0].StartsWith("-"))
        {
            options.CommandName = args[0];
            startIndex = 1;
        }

        for (int i = startIndex; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("-"))
                continue; // Skip positional args that are not values (shouldn't happen with this simple parser)

            var key = arg.TrimStart('-');
            
            // Check for --non-interactive flag
            if (string.Equals(key, "non-interactive", StringComparison.OrdinalIgnoreCase))
            {
                options.IsNonInteractive = true;
                continue;
            }

            // Check if next arg is value
            if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
            {
                options.Options[key] = args[i + 1];
                i++; // Consume value
            }
            else
            {
                // Boolean flag
                options.Options[key] = "true";
            }
        }

        return options;
    }
}
