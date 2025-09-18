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
- **.NET SDK** (version 8.0 or later — replace with your actual target)  
- **Visual Studio** (Community Edition or higher) with `.NET desktop development` workload  
- *(Optional)* [Inno Setup](https://jrsoftware.org/isinfo.php) if you want to compile the installer  

---

### Build Scripts Overview  

All build scripts are in the root folder. Run them from **Command Prompt** or **PowerShell**.  

| Script | What it does |
|--------|--------------|
| `build.bat` | Builds the project in Release mode |
| `build-clean.bat` | Cleans previous build artifacts, then builds |
| `publish.bat` | Publishes the project (self-contained build) |
| `publish-cleanup.bat` | Cleans, then publishes |
| `compile.bat` | Compiles without publishing |
| `compile-installer.bat` | Builds and packages an installer using Inno Setup |
| `cleanup.bat` | Removes build artifacts |
| `patch-installer-scripts.ps1` | Updates installer scripts before compiling |
| `installer-scripts.ps1` | Helper script for installer creation |

---

### Example Build Commands  
> **Note**: These commands must be ran at the root directory of the repository.

- To build and cleanup:
```powershell
.\scripts\build-clean.bat
```
- To compile the installer:
```powershell
.\scripts\publish-cleanup.bat
```
