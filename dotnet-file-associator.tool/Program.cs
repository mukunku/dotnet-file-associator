using System.CommandLine;

namespace DotnetFileAssociator.Tool;

internal class Program
{
    private static int Main(string[] args)
    {
        // Common options
        var pathToExecutableOption = new Option<string>(
            aliases: ["-p"],
            name: "--pathToExecutable"
        )
        {
            Required = true,
            Description = "Path to the executable to associate."
        };

        var extensionOption = new Option<string>(
            aliases: ["-e"],
            name: "--extension"
        )
        {
            Required = true,
            Description = "File extension to associate, with or without the 'dot' (e.g., .abc)."
        };

        var longNameOption = new Option<string>(
            aliases: ["-l"],
            name: "--longName"
        )
        {
            Required = false,
            Description = "Expanded name of the extension (optional)."
        };

        // set command
        var setCommand = new Command("set", "Set a file association (requires administrator privileges)")
        {
            pathToExecutableOption,
            extensionOption,
            longNameOption
        };

        setCommand.SetAction((ParseResult parseResult) =>
        {
            var exePath = parseResult.GetRequiredValue<string>(pathToExecutableOption);
            var extension = parseResult.GetRequiredValue<string>(extensionOption);
            var longName = parseResult.GetValue<string>(longNameOption);

            try
            {
                FileAssociator.SetFileAssociation(exePath, extension, longName);
                Console.WriteLine($"Associated '{extension}' with '{exePath}'{(string.IsNullOrWhiteSpace(longName) ? string.Empty : $" ({longName})")}.");
                return 0;
            }
            catch (FileNotFoundException)
            {
                Console.Error.WriteLine($"Executable does not exist. File not found: {exePath} ");
                return -1;
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or ArgumentException or ArgumentNullException or NotRunningAsAdministratorException)
            {
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
        });

        // remove command
        var removeCommand = new Command("remove", "Remove a file association (requires administrator privileges)")
        {
            pathToExecutableOption,
            extensionOption
        };
        removeCommand.SetAction((ParseResult parseResult) =>
        {
            var exePath = parseResult.GetRequiredValue<string>(pathToExecutableOption);
            var extension = parseResult.GetRequiredValue<string>(extensionOption);

            try
            {
                FileAssociator.RemoveFileAssociation(exePath, extension);
                Console.WriteLine($"Removed association for '{extension}' with '{exePath}'.");
                return 0;
            }
            catch (FileNotFoundException)
            {
                Console.Error.WriteLine($"Executable does not exist. File not found: {exePath} ");
                return -1;
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException or ArgumentException or ArgumentNullException or NotRunningAsAdministratorException)
            {
                Console.Error.WriteLine(ex.Message);
                return -1;
            }
        });

        // check command (does NOT require admin privileges)
        var checkCommand = new Command("check", "Check if a file association is set (status code: 0 = exists, 1 = doesn't exist)")
        {
            pathToExecutableOption,
            extensionOption
        };
        checkCommand.SetAction((ParseResult parseResult) =>
        {
            var exePath = parseResult.GetRequiredValue<string>(pathToExecutableOption);
            var extension = parseResult.GetRequiredValue<string>(extensionOption);

            try
            {
                bool isSet = FileAssociator.IsFileAssociationSet(exePath, extension);
                Console.WriteLine(isSet
                    ? $"Association exists for '{extension}' with '{exePath}'."
                    : $"No association found for '{extension}' with '{exePath}'.");

                return isSet ? 0 : 1;
            }
            catch (FileNotFoundException)
            {
                Console.Error.WriteLine($"Executable does not exist. File not found: {exePath} ");
                return -1;
            }
        });

        // Root command automatically contains --help and --version options
        var rootCommand = new RootCommand("dotnet-file-associator CLI utility")
        {
            setCommand,
            removeCommand,
            checkCommand
        };

        return rootCommand.Parse(args).Invoke();
    }
}
