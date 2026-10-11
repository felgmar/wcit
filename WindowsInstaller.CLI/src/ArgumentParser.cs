using System;
using System.Runtime.Versioning;
using WindowsInstaller.Core;

namespace WindowsInstaller.CLI
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
