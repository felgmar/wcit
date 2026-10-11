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
- **Visual Studio** (Community Edition or higher) with the `.NET desktop development` and `WinUI application development` workloads to build the graphical app
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

### WinUI preview

The `WindowsInstallerUI` project is an early WinUI 3 front end with a fixed 1024x768 window and system, light, and dark themes under the Appearance menu. It can mount a Windows ISO or search an already-mounted drive recursively for `install.wim` and `install.esd` files, then scan a selected image for Windows editions. It also lists detected physical disks and unused drive letters for the planned Windows and EFI volumes, rechecking selected letters as you choose them and before review. The app requests administrator privileges to inspect image editions and disks. This preview does **not** format disks or deploy Windows yet.

Open `Windows Installer.sln` in Visual Studio with the workloads above, select the `WindowsInstallerUI` startup project and the `x64` platform, then run it. You can also use:

```powershell
dotnet run --project .\WindowsInstallerUI\WindowsInstallerUI.csproj -p:Platform=x64
```

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
