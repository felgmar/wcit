﻿using System;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace WindowsInstallerLib.Management.ProcessManager
{
    [SupportedOSPlatform("windows")]
    public static partial class Worker
    {
        public static int StartProcess(string fileName, string args)
        {
            try
            {
                using (Process process = new())
                {
                    process.StartInfo.FileName = fileName;
                    process.StartInfo.Arguments = args;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                    process.Start();
                    process.WaitForExit();
                    ExitCode = process.ExitCode;
                    process.Close();
                }

                return ExitCode;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}