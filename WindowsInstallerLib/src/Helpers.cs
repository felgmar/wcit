using Microsoft.Dism;
using System;
using System.Collections.Generic;

namespace WindowsInstallerLib
{
    namespace Helpers
    {
        public class Privileges
        {
            public static bool IsAdmin() { return PrivilegesManager.IsAdmin(); }
        }

        public class Devices
        {
            public static List<Tuple<int, string, string>> GetDisks() { return DiskManager.GetDisks(); }
        }

        public class SystemInfo
        {
            public static bool SystemSupportsEFI() { return SystemInfoManager.IsEFI(); }
        }

        public class Deployment
        {
            public static int ExitCode { get; private set; }
            public static DismImageInfoCollection GetImageInfo(ref Parameters parameters) { return DeployManager.GetImageInfoD(ref parameters); }
            public static void FormatDisk(ref Parameters parameters) { DiskManager.FormatDisk(ref parameters); }
            public static void ApplyImage(ref Parameters parameters) { DeployManager.ApplyImage(ref parameters); }
            public static void InstallBootloader(ref Parameters parameters) { DeployManager.InstallBootloader(ref parameters); }
        }
    }
}
