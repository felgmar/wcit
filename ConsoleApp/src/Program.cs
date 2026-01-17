using System;
using System.Runtime.Versioning;
using WindowsInstallerLib;

namespace ConsoleApp
{
    [SupportedOSPlatform("windows")]
    internal sealed class Program
    {
        [MTAThread]
        internal static int Main(string[] args)
        {
            Parameters parameters = new()
            {
                DiskNumber = -1,
                ImageIndex = -1,
            };

            try
            {
#if DEBUG
                Console.Title = $"[{ProgramInfo.GetConfigurationMode()}] {ProgramInfo.GetName()}";
                Console.WriteLine($"Welcome to the {ProgramInfo.GetName()} tool!");
                Console.WriteLine($"Current version: {ProgramInfo.GetVersion()}-{ProgramInfo.GetConfigurationMode()}");
                Console.WriteLine($"Created by {ProgramInfo.GetAuthor()}");
#else
                Console.Title = $"{ProgramInfo.GetName()}";
                Console.WriteLine($"Welcome to the {ProgramInfo.GetName()} tool!");
                Console.WriteLine($"Current version: {ProgramInfo.GetVersion()}");
                Console.WriteLine($"Created by {ProgramInfo.GetAuthor()}");
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error has occurred: {ex.Message}");
            }

            try
            {
                ArgumentParser.ParseArgs(ref parameters, args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error has occurred: {ex.Message}");
            }

            try
            {
                InstallerManager.Configure(ref parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error has occurred: {ex.Message}");
            }

            try
            {
                InstallerManager.Prepare(ref parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error has occurred: {ex.Message}");
            }

            try
            {
                InstallerManager.InstallWindows(ref parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error has occurred: {ex.Message}");
            }

            return 0;
        }
    }
}
