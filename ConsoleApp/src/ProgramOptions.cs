using CommandLine;
using System;
using WindowsInstallerLib;

namespace ConsoleApp
{
    [Verb("install", HelpText = "Deploy Windows to a target disk")]
    internal sealed class ProgramOptions
    {
        [Option(
            'd',
            "destination-drive",
            Required = false,
            HelpText = "Specifies the mountpoint to use for deploying Windows.")]
        internal string? DestinationDrive { get; set; }

        [Option(
            'e',
            "efi-drive",
            Required = false,
            HelpText = "Specifies the mountpoint to use for the EFI partition.")]
        internal string? EfiDrive { get; set; }

        [Option(
            'n',
            "disk-number",
            Required = false,
            HelpText = "Specifies the disk number to use for deploying Windows.")]
        internal int? DiskNumber { get; set; }

        [Option(
            's',
            "source-drive",
            Required = false,
            HelpText = "Specifies the mountpoint to use for the Windows image.")]
        internal string? SourceDrive { get; set; }

        [Option(
            'i',
            "image-index",
            Required = false,
            HelpText = "Specifies the index of the Windows image to deploy.")]
        internal int? ImageIndex { get; set; }

        [Option(
            'p',
            "image-file-path",
            Required = false,
            HelpText = "Specifies the path to the Windows image to deploy.")]
        internal string? ImageFilePath { get; set; }

        [Option(
            'a',
            "additional-drivers-drive",
            Required = false,
            HelpText = "Specifies the mountpoint to use for additional drivers.")]
        internal string? AdditionalDrive { get; set; }

        [Option(
            'f',
            "firmware-type",
            Required = false,
            HelpText = "Specifies the firmware type to use for the deployment (UEFI or BIOS).")]

        internal string? FirmwareType { get; set; }

        internal static void PrintHelp()
        {
            Console.WriteLine($"\nUsage: {ProgramInfo.GetName()} [options]");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("  -d, --destination-drive <drive>       Specifies the mountpoint to use for deploying Windows.");
            Console.WriteLine("  -e, --efi-drive <drive>               Specifies the mountpoint to use for the EFI partition.");
            Console.WriteLine("  -n, --disk-number <number>            Specifies the disk number to use for deploying Windows.");
            Console.WriteLine("  -s, --source-drive <drive>            Specifies the mountpoint to use for the Windows image.");
            Console.WriteLine("  -i, --image-index <number>            Specifies the index of the Windows image to deploy.");
            Console.WriteLine("  -p, --image-file-path <path>          Specifies the path to the Windows image to deploy.");
            Console.WriteLine("  -a, --additional-drivers-drive <path> Specifies the mountpoint to use for additional drivers.");
            Console.WriteLine("  -f, --firmware-type <type>            Specifies the firmware type to use for the deployment.");
            Console.WriteLine("  -h, --help                            Displays this help message.\n");
        }

        internal static void ApplyOptions(Parameters parameters, ProgramOptions options)
        {
            if (!string.IsNullOrEmpty(options.DestinationDrive))
                parameters.DestinationDrive = options.DestinationDrive.ToUpperInvariant();

            if (!string.IsNullOrEmpty(options.EfiDrive))
                parameters.EfiDrive = options.EfiDrive.ToUpperInvariant();

            if (options.DiskNumber.HasValue)
                parameters.DiskNumber = options.DiskNumber.Value;

            if (!string.IsNullOrEmpty(options.SourceDrive))
                parameters.SourceDrive = options.SourceDrive.ToUpperInvariant();

            if (options.ImageIndex.HasValue)
                parameters.ImageIndex = options.ImageIndex.Value;

            if (!string.IsNullOrEmpty(options.ImageFilePath))
                parameters.ImageFilePath = options.ImageFilePath.ToLowerInvariant();

            if (!string.IsNullOrEmpty(options.AdditionalDrive))
                parameters.AdditionalDrive = options.AdditionalDrive.ToLowerInvariant();

            if (!string.IsNullOrEmpty(options.FirmwareType))
                parameters.FirmwareType = options.FirmwareType.ToUpperInvariant();
        }

        internal static void ParseArgs(Parameters parameters, string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            ParserResult<ProgramOptions>? result = Parser.Default.ParseArguments<ProgramOptions>(args);

            try
            {
                result
                    .WithParsed(options => ApplyOptions(parameters, options))
                    .WithNotParsed(errors =>
                    {
                        foreach (Error? error in errors)
                        {
                            throw error.Tag switch
                            {
                                _ => new ArgumentException($"{error}")
                            };
                        }
                    });
            }
            catch (ArgumentException)
            {
                throw;
            }

#if DEBUG
            Console.WriteLine("Parameters:");
            Console.WriteLine($"  Destination Drive: {parameters.DestinationDrive}");
            Console.WriteLine($"  EFI Drive: {parameters.EfiDrive}");
            Console.WriteLine($"  Disk Number: {parameters.DiskNumber}");
            Console.WriteLine($"  Source Drive: {parameters.SourceDrive}");
            Console.WriteLine($"  Image Index: {parameters.ImageIndex}");
            Console.WriteLine($"  Image File Path: {parameters.ImageFilePath}");
            Console.WriteLine($"  Additional Drivers Drive: {parameters.AdditionalDrive}");
            Console.WriteLine($"  Firmware Type: {parameters.FirmwareType}");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
#endif
        }
    }
}
