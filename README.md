# wcit – Windows CLI Installer Tool  

## 📌 What is this?  
**Windows CLI Installer Tool** is a C# program that deploys Windows onto any storage device **from within an existing Windows installation**, eliminating the need to reboot into a separate installer.  

By running multiple instances, you can install Windows on several devices at once — as many as you have instances running. This can be useful for:  
- Rapid OS deployment in IT environments  
- Preparing multiple drives for testing or distribution  
- Automating repetitive installation tasks  

---

## 🚀 Getting Started  

### Download Prebuilt Release  
If you just want to use the tool, grab the latest release from the **[Releases](../../releases)** section on the right-hand side of this page.  

---

## 🛠 Building from Source  

### Prerequisites  
- **Windows 10/11**  
- **.NET SDK 10.0**
- **Visual Studio** (Community Edition or higher) with the `WinUI application development` workload to build the graphical app.
- *(Optional)* [Inno Setup](https://jrsoftware.org/isinfo.php) if you want to compile the installer  

---

### Build Scripts Overview  

All build scripts are in the root folder. Run them from **Command Prompt** or **PowerShell**.  

| Script | What it does |
|--------|--------------|
| `build.bat` | Builds the project in Release mode |
| `build-clean.bat` | Cleans previous build artifacts, then builds |
| `publish.bat` | Publishes the project (self-contained build) |
| `publish-cleanup.bat` | Cleans, then publishes the project |
| `compile-installer.bat` | Compiles without publishing |
| `compile-installer-cleanup.bat` | Cleans, then compiles without publishing |
| `cleanup.bat` | Removes build artifacts |
| `patch-installer-script.ps1` | Patches the installer script before compiling |
| `download-installer-script.ps1` | Helper script for downloading the installer script |

---

### WinUI installer

The `WindowsInstallerUI` project is a WinUI 3 frontend with WCIT branding, a custom installer icon, a fixed 1024x768 window, and File, Appearance, and Help menus. The About dialog shows the app version and creator. It can mount a Windows ISO or search an already-mounted drive recursively for `install.wim` and `install.esd` files. When mounted-drive mode is selected, the drive letter can be left blank to automatically detect standard Windows installation media in mounted drives. The app then scans a selected image for Windows editions. Select an eligible physical disk, unused Windows and boot-partition drive letters, and optionally a drivers folder. The installer validates the setup, asks you to acknowledge the data loss, and then requires a final confirmation before cleaning and repartitioning the selected disk, applying the Windows image, optionally adding drivers, and installing boot files. The running Windows/boot disk is excluded and rechecked immediately before formatting. Installation requires administrator privileges. Disk changes are destructive and cannot be rolled back; use only a disk whose contents you are prepared to lose.

Open `Windows Installer.slnx` in Visual Studio with the WinUI application development workload installed, select `WindowsInstallerUI` as the startup project and the `x64` platform, then run it. The build scripts use Visual Studio MSBuild so the Windows App SDK packaging tasks are available.

### Example Build Commands
> **Note**: These commands must be ran at the root directory of the repository.

- To build and cleanup:
```powershell
.\scripts\build-clean.bat
```
- To compile the installer:
```powershell
.\scripts\compile-installer-cleanup.bat
```
