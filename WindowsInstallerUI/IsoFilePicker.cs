using System.Runtime.InteropServices;

namespace WindowsInstallerUI;

internal static class IsoFilePicker
{
    private static readonly Guid FileOpenDialogClassId = new("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");
    private static readonly Guid FileOpenDialogInterfaceId = new("D57C7288-D4AD-4768-BE02-9D969532D960");

    internal static string? Pick(IntPtr ownerWindow)
    {
        Guid classId = FileOpenDialogClassId;
        Guid interfaceId = FileOpenDialogInterfaceId;
        CoCreateInstance(
            ref classId,
            IntPtr.Zero,
            0x1 | 0x4,
            ref interfaceId,
            out IFileOpenDialog dialog);

        try
        {
            FilterSpec[] filters = [new("Windows ISO images (*.iso)", "*.iso")];
            Marshal.ThrowExceptionForHR(dialog.SetFileTypes((uint)filters.Length, filters));
            Marshal.ThrowExceptionForHR(dialog.SetFileTypeIndex(1));
            Marshal.ThrowExceptionForHR(dialog.SetDefaultExtension("iso"));
            Marshal.ThrowExceptionForHR(dialog.SetTitle("Select a Windows ISO"));
            Marshal.ThrowExceptionForHR(dialog.SetOptions(
                0x00000040 | // Force filesystem items
                0x00000800 | // Path must exist
                0x00001000)); // File must exist

            int result = dialog.Show(ownerWindow);
            if (result == unchecked((int)0x800704C7))
            {
                return null;
            }

            Marshal.ThrowExceptionForHR(result);
            Marshal.ThrowExceptionForHR(dialog.GetResult(out IShellItem item));

            try
            {
                Marshal.ThrowExceptionForHR(item.GetDisplayName(0x80058000, out IntPtr displayName));
                try
                {
                    return Marshal.PtrToStringUni(displayName);
                }
                finally
                {
                    Marshal.FreeCoTaskMem(displayName);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(item);
            }
        }
        finally
        {
            Marshal.ReleaseComObject(dialog);
        }
    }

    [DllImport("ole32.dll", PreserveSig = false)]
    private static extern void CoCreateInstance(
        ref Guid classId,
        IntPtr outer,
        uint context,
        ref Guid interfaceId,
        [MarshalAs(UnmanagedType.Interface)] out IFileOpenDialog instance);

    [ComImport]
    [Guid("D57C7288-D4AD-4768-BE02-9D969532D960")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOpenDialog
    {
        [PreserveSig] int Show(IntPtr owner);
        [PreserveSig] int SetFileTypes(uint count, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] FilterSpec[] filters);
        [PreserveSig] int SetFileTypeIndex(uint index);
        [PreserveSig] int GetFileTypeIndex(out uint index);
        [PreserveSig] int Advise(IntPtr events, out uint cookie);
        [PreserveSig] int Unadvise(uint cookie);
        [PreserveSig] int SetOptions(uint options);
        [PreserveSig] int GetOptions(out uint options);
        [PreserveSig] int SetDefaultFolder(IntPtr folder);
        [PreserveSig] int SetFolder(IntPtr folder);
        [PreserveSig] int GetFolder(out IntPtr folder);
        [PreserveSig] int GetCurrentSelection(out IntPtr item);
        [PreserveSig] int SetFileName([MarshalAs(UnmanagedType.LPWStr)] string name);
        [PreserveSig] int GetFileName(out IntPtr name);
        [PreserveSig] int SetTitle([MarshalAs(UnmanagedType.LPWStr)] string title);
        [PreserveSig] int SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string text);
        [PreserveSig] int SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string label);
        [PreserveSig] int GetResult(out IShellItem item);
        [PreserveSig] int AddPlace(IntPtr item, uint location);
        [PreserveSig] int SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string extension);
        [PreserveSig] int Close(int result);
        [PreserveSig] int SetClientGuid(ref Guid guid);
        [PreserveSig] int ClearClientData();
        [PreserveSig] int SetFilter(IntPtr filter);
        [PreserveSig] int GetResults(out IntPtr items);
        [PreserveSig] int GetSelectedItems(out IntPtr items);
    }

    [ComImport]
    [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        [PreserveSig] int BindToHandler(IntPtr bindContext, ref Guid handler, ref Guid interfaceId, out IntPtr result);
        [PreserveSig] int GetParent(out IntPtr parent);
        [PreserveSig] int GetDisplayName(uint nameType, out IntPtr name);
        [PreserveSig] int GetAttributes(uint mask, out uint attributes);
        [PreserveSig] int Compare(IShellItem item, uint hint, out int order);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct FilterSpec(
        [MarshalAs(UnmanagedType.LPWStr)] string name,
        [MarshalAs(UnmanagedType.LPWStr)] string spec)
    {
        public string Name = name;
        public string Spec = spec;
    }
}
