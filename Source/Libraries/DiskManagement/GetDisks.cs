﻿using System;
using System.Linq;
using System.Management;

namespace wcit.Libraries.DiskManagement
{
    public static partial class SystemDrives
    {
        public static void ListAll()
        {
            WqlObjectQuery DeviceTable = new("SELECT * FROM Win32_DiskDrive");
            ManagementObjectSearcher DeviceInfo = new(DeviceTable);
            foreach (ManagementObject o in DeviceInfo.Get().Cast<ManagementObject>())
            {
                Console.WriteLine("Disk number = " + o["Index"]);
                Console.WriteLine("Model = " + o["Model"]);
                Console.WriteLine("DeviceID = " + o["DeviceID"]);
                Console.WriteLine("");
            }
        }
    }
}
