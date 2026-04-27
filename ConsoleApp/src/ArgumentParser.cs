using System;
using System.Runtime.Versioning;
using WindowsInstallerLib;

namespace ConsoleApp
{
    [SupportedOSPlatform("windows")]
    internal sealed class ArgumentParser
    {
        internal static void ParseArgs(Parameters parameters, string[] args)
        {
            try
            {
                ProgramOptions.ParseArgs(parameters, args);
            }
            catch
            {
                throw;
            }
        }
    }
}
