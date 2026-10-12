using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Windows.UI.ViewManagement;
using WindowsInstallerLib;
using WinRT.Interop;
using Microsoft.Win32;

namespace WindowsInstallerUI;

public sealed partial class MainWindow : Window
{
    private List<string> _availableDriveLetters = [];
    private int _editionScanVersion;
    private bool _isLoadingDestinationOptions;
    private bool _isUpdatingDriveChoices;
    private bool _isInstalling;
    private int? _targetDiskNumber;
    private readonly UISettings _uiSettings = new();
    private ElementTheme _selectedTheme = ElementTheme.Default;
    private readonly Dictionary<Control, bool> _enabledStateBeforeInstall = [];

    public MainWindow()
    {
        InitializeComponent();
        MediaTypeBox.SelectedIndex = 0;

        IntPtr windowHandle = WindowNative.GetWindowHandle(this);
        AppWindow appWindow = AppWindow.GetFromWindowId(
            Win32Interop.GetWindowIdFromWindow(windowHandle));

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
        }

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBarDragArea);
        appWindow.Resize(new SizeInt32(1024, 768));
        _uiSettings.ColorValuesChanged += SystemColors_Changed;
        ApplySelectedTheme();
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ApplySelectedTheme();

        if (_isLoadingDestinationOptions)
        {
            return;
        }

        _isLoadingDestinationOptions = true;
        DestinationStatus.Text = "Scanning physical disks and available drive letters...";

        try
        {
            (SortedDictionary<int, string> disks, HashSet<int> protectedDiskNumbers, List<string> driveLetters) =
                await Task.Run(() =>
                    (InstallerManager.GetDisks(),
                     InstallerManager.GetProtectedDiskNumbers(),
                     InstallerManager.GetAvailableDriveLetters()));

            TargetDiskBox.ItemsSource = disks
                .Where(disk => !protectedDiskNumbers.Contains(disk.Key))
                .Select(disk => new DiskOption(disk.Key, disk.Value))
                .ToArray();
            TargetDiskBox.IsEnabled = TargetDiskBox.Items.Count > 0;
            if (TargetDiskBox.Items.Count > 0)
            {
                TargetDiskBox.SelectedIndex = 0;
            }

            _availableDriveLetters = driveLetters;
            WindowsDriveBox.ItemsSource = _availableDriveLetters;
            WindowsDriveBox.IsEnabled = _availableDriveLetters.Count > 0;
            EfiDriveBox.ItemsSource = _availableDriveLetters;
            EfiDriveBox.IsEnabled = _availableDriveLetters.Count > 1;

            if (_availableDriveLetters.Count > 0)
            {
                WindowsDriveBox.SelectedIndex = 0;
                EfiDriveBox.SelectedIndex = _availableDriveLetters.Count > 1 ? 1 : 0;
            }

            DestinationStatus.Text = disks.Count == 0
                ? "No physical disks were found."
                : TargetDiskBox.Items.Count == 0
                    ? "No eligible target disks were found. The current Windows and boot disks are protected."
                : _availableDriveLetters.Count < 2
                    ? "At least two unused drive letters are needed."
                    : $"Found {TargetDiskBox.Items.Count} eligible disk(s); current Windows and boot disks are excluded. {_availableDriveLetters.Count} unused drive letter(s) are available.";
        }
        catch (Exception ex)
        {
            DestinationStatus.Text = $"Could not load disks or drive letters: {ex.Message}";
        }
        finally
        {
            _isLoadingDestinationOptions = false;
        }
    }

    private void SystemThemeItem_Click(object sender, RoutedEventArgs e)
    {
        SetSelectedTheme(ElementTheme.Default);
    }

    private void LightThemeItem_Click(object sender, RoutedEventArgs e)
    {
        SetSelectedTheme(ElementTheme.Light);
    }

    private void DarkThemeItem_Click(object sender, RoutedEventArgs e)
    {
        SetSelectedTheme(ElementTheme.Dark);
    }

    private void SetSelectedTheme(ElementTheme theme)
    {
        _selectedTheme = theme;
        SystemThemeItem.IsChecked = theme == ElementTheme.Default;
        LightThemeItem.IsChecked = theme == ElementTheme.Light;
        DarkThemeItem.IsChecked = theme == ElementTheme.Dark;
        RootLayout.DispatcherQueue.TryEnqueue(ApplySelectedTheme);
    }

    private void SystemColors_Changed(UISettings sender, object args)
    {
        if (_selectedTheme == ElementTheme.Default)
        {
            RootLayout.DispatcherQueue.TryEnqueue(ApplySelectedTheme);
        }
    }

    private void ApplySelectedTheme()
    {
        if (_selectedTheme != ElementTheme.Default)
        {
            RootLayout.RequestedTheme = _selectedTheme;
            return;
        }

        using RegistryKey? personalizationKey = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        object? appsUseLightTheme = personalizationKey?.GetValue("AppsUseLightTheme");
        RootLayout.RequestedTheme = appsUseLightTheme is not null &&
                                   Convert.ToInt32(appsUseLightTheme) != 0
            ? ElementTheme.Light
            : ElementTheme.Dark;
    }

    private void BrowseIso_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string? filePath = PickIsoFile();
            if (filePath is not null)
            {
                MediaTypeBox.SelectedIndex = 0;
                IsoPathBox.Text = filePath;
                ResetImageSearch();
            }
        }
        catch (Exception ex)
        {
            ShowValidationError($"Could not open the ISO file picker: {ex.Message}");
        }
    }

    private string? PickIsoFile()
    {
        StringBuilder selectedPath = new(capacity: 32768);
        OpenFileName dialog = new()
        {
            structSize = (uint)Marshal.SizeOf<OpenFileName>(),
            ownerWindow = WindowNative.GetWindowHandle(this),
            filter = "Windows ISO images (*.iso)\0*.iso\0All files (*.*)\0*.*\0\0",
            filterIndex = 1,
            file = selectedPath,
            maxFile = (uint)selectedPath.Capacity,
            title = "Select a Windows ISO",
            flags = (uint)(OpenFileNameFlags.FileMustExist |
                           OpenFileNameFlags.PathMustExist |
                           OpenFileNameFlags.NoChangeDirectory |
                           OpenFileNameFlags.Explorer),
            defaultExtension = "iso"
        };

        if (GetOpenFileName(ref dialog))
        {
            return selectedPath.ToString();
        }

        uint error = CommDlgExtendedError();
        if (error != 0)
        {
            throw new InvalidOperationException(
                $"The Windows file dialog reported error 0x{error:X8}.");
        }

        return null;
    }

    [DllImport("comdlg32.dll", EntryPoint = "GetOpenFileNameW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetOpenFileName(ref OpenFileName openFileName);

    [DllImport("comdlg32.dll", EntryPoint = "CommDlgExtendedError")]
    private static extern uint CommDlgExtendedError();

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OpenFileName
    {
        public uint structSize;
        public IntPtr ownerWindow;
        public IntPtr instance;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? filter;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? customFilter;
        public uint maxCustomFilter;
        public uint filterIndex;
        [MarshalAs(UnmanagedType.LPWStr)]
        public StringBuilder? file;
        public uint maxFile;
        [MarshalAs(UnmanagedType.LPWStr)]
        public StringBuilder? fileTitle;
        public uint maxFileTitle;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? initialDirectory;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? title;
        public uint flags;
        public ushort fileOffset;
        public ushort fileExtension;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? defaultExtension;
        public IntPtr customData;
        public IntPtr hook;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? templateName;
        public IntPtr reserved;
        public uint reservedValue;
        public uint flagsExtended;
    }

    [Flags]
    private enum OpenFileNameFlags : uint
    {
        FileMustExist = 0x00001000,
        PathMustExist = 0x00000800,
        NoChangeDirectory = 0x00000008,
        Explorer = 0x00080000
    }

    private void MediaType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool useIso = MediaTypeBox.SelectedIndex == 0;
        IsoSourcePanel.Visibility = useIso ? Visibility.Visible : Visibility.Collapsed;
        DriveSourcePanel.Visibility = useIso ? Visibility.Collapsed : Visibility.Visible;

        if (useIso)
        {
            DriveLetterBox.Text = string.Empty;
        }
        else
        {
            IsoPathBox.Text = string.Empty;
        }

        ResetImageSearch();
    }

    private void DriveLetter_TextChanged(object sender, TextChangedEventArgs e)
    {
        ResetImageSearch();
    }

    private async void FindImage_Click(object sender, RoutedEventArgs e)
    {
        FindImageButton.IsEnabled = false;
        ImageFilesBox.IsEnabled = false;
        ImageSearchStatus.Text = "Preparing installation media...";
        StatusBar.IsOpen = false;
        ReviewCard.Visibility = Visibility.Collapsed;

        try
        {
            string mediaRoot;
            if (MediaTypeBox.SelectedIndex == 0)
            {
                string isoPath = IsoPathBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(isoPath))
                {
                    ShowValidationError("Choose a Windows ISO file.");
                    return;
                }

                if (!File.Exists(isoPath) ||
                    !string.Equals(Path.GetExtension(isoPath), ".iso", StringComparison.OrdinalIgnoreCase))
                {
                    throw new FileNotFoundException("Select an existing .iso file.", isoPath);
                }

                mediaRoot = await Task.Run(() => MountIso(isoPath));
                ImageSearchStatus.Text = $"ISO mounted at {mediaRoot} Searching for Windows images...";
            }
            else
            {
                string driveInput = DriveLetterBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(driveInput))
                {
                    ShowValidationError("Enter the drive letter of your mounted installation media.");
                    return;
                }

                mediaRoot = NormalizeDriveRoot(driveInput);
                if (!Directory.Exists(mediaRoot))
                {
                    throw new DirectoryNotFoundException($"The drive {mediaRoot} is not available.");
                }

                ImageSearchStatus.Text = $"Searching {mediaRoot} for Windows images...";
            }

            string[] imageFiles = await Task.Run(() => FindWindowsImages(mediaRoot));
            if (imageFiles.Length == 0)
            {
                ImageFilesBox.ItemsSource = null;
                ImageFilesBox.IsEnabled = false;
                ImageSearchStatus.Text = $"No install.wim or install.esd files were found under {mediaRoot}.";
                return;
            }

            ImageFilesBox.ItemsSource = imageFiles;
            ImageFilesBox.SelectedIndex = 0;
            ImageFilesBox.IsEnabled = true;
            ImageSearchStatus.Text = $"Found {imageFiles.Length} Windows image file(s) under {mediaRoot}.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or Win32Exception or ArgumentException)
        {
            ShowValidationError(ex.Message);
        }
        finally
        {
            FindImageButton.IsEnabled = true;
        }
    }

    private async void ImageFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        InvalidateReview();
        StatusBar.IsOpen = false;

        if (ImageFilesBox.SelectedItem is string imagePath)
        {
            await ScanImageEditionsAsync(imagePath);
        }
    }

    private async void ReviewSetup_Click(object sender, RoutedEventArgs e)
    {
        InvalidateReview();
        ReviewButton.IsEnabled = false;
        SetInstallerBusy(true);
        bool reviewValidated = false;

        try
        {
            if (ImageFilesBox.SelectedItem is not string imagePath)
            {
                ShowValidationError("Find and select a Windows image file before reviewing the setup.");
                return;
            }

            if (ImageEditionBox.SelectedItem is not KeyValuePair<int, string> edition)
            {
                ShowValidationError("Scan the selected image and choose a Windows edition.");
                return;
            }

            if (_targetDiskNumber is not int targetDiskNumber)
            {
                ShowValidationError("Select a target installation disk.");
                return;
            }

            if (WindowsDriveBox.SelectedItem is not string windowsDrive ||
                EfiDriveBox.SelectedItem is not string efiDrive ||
                string.Equals(windowsDrive, efiDrive, StringComparison.OrdinalIgnoreCase))
            {
                ShowValidationError("Select different unused drive letters for Windows and EFI.");
                return;
            }

            try
            {
                if (!InstallerManager.IsDriveLetterAvailable(windowsDrive) ||
                    !InstallerManager.IsDriveLetterAvailable(efiDrive))
                {
                    DriveLetterError.Message = "A selected drive letter is now in use. Choose different unused letters.";
                    DriveLetterError.IsOpen = true;
                    RefreshDriveLetterChoices();
                    return;
                }
            }
            catch (Exception ex)
            {
                DriveLetterError.Message = $"Could not check whether the selected drive letters are available: {ex.Message}";
                DriveLetterError.IsOpen = true;
                return;
            }

            DriveLetterError.IsOpen = false;
            StatusBar.IsOpen = false;
            Parameters parameters = CreateInstallationParameters(
                imagePath,
                edition.Key,
                targetDiskNumber,
                windowsDrive,
                efiDrive);
            await Task.Run(() => ValidateInstallationParameters(parameters));

            ReviewSummary.Text =
                $"Media type: {(MediaTypeBox.SelectedIndex == 0 ? "ISO file" : "Mounted drive")}\n" +
                $"Image: {imagePath}\n" +
                $"Windows edition: {edition.Value} (index {edition.Key})\n" +
                $"Target disk number: {targetDiskNumber} - {(TargetDiskBox.SelectedItem as DiskOption)?.Model}\n" +
                $"Windows drive: {windowsDrive}\n" +
                $"Boot / EFI drive: {efiDrive}\n" +
                $"Firmware: {parameters.FirmwareType}\n" +
                $"Drivers folder: {(string.IsNullOrWhiteSpace(parameters.AdditionalDrive) ? "None" : parameters.AdditionalDrive)}";
            ConfirmDiskEraseCheckBox.Content =
                $"I understand that all data on Disk {targetDiskNumber} will be permanently erased.";
            ConfirmDiskEraseCheckBox.IsChecked = false;
            InstallButton.IsEnabled = false;
            ReviewCard.Visibility = Visibility.Visible;
            reviewValidated = true;
        }
        catch (Exception ex)
        {
            ShowValidationError($"Setup validation failed: {ex.Message}");
        }
        finally
        {
            SetInstallerBusy(false);
            ReviewButton.IsEnabled = true;
            if (reviewValidated)
            {
                ConfirmDiskEraseCheckBox.IsEnabled = true;
            }
        }
    }

    private async void InstallWindows_Click(object sender, RoutedEventArgs e)
    {
        if (_isInstalling ||
            ConfirmDiskEraseCheckBox.IsChecked != true ||
            ImageFilesBox.SelectedItem is not string imagePath ||
            ImageEditionBox.SelectedItem is not KeyValuePair<int, string> edition ||
            _targetDiskNumber is not int targetDiskNumber ||
            WindowsDriveBox.SelectedItem is not string windowsDrive ||
            EfiDriveBox.SelectedItem is not string efiDrive)
        {
            ShowValidationError("Review the setup and confirm that the selected disk can be erased before installing.");
            return;
        }

        ContentDialog confirmationDialog = new()
        {
            XamlRoot = RootLayout.XamlRoot,
            Title = $"Erase Disk {targetDiskNumber} and install Windows?",
            Content =
                $"This permanently deletes every partition and all files on Disk {targetDiskNumber} " +
                $"({(TargetDiskBox.SelectedItem as DiskOption)?.Model}). Windows {edition.Value} will be installed " +
                $"from {imagePath}. This action cannot be undone.",
            PrimaryButtonText = "Erase disk and install",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };

        if (await confirmationDialog.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        Parameters parameters;
        try
        {
            parameters = CreateInstallationParameters(
                imagePath,
                edition.Key,
                targetDiskNumber,
                windowsDrive,
                efiDrive);
        }
        catch (Exception ex)
        {
            ShowValidationError($"Could not prepare the installation settings: {ex.Message}");
            return;
        }

        _isInstalling = true;
        bool installationSucceeded = false;
        SetInstallerBusy(true);
        StatusBar.IsOpen = false;
        InstallationProgressPanel.Visibility = Visibility.Visible;
        InstallationProgressText.Text = "Starting installation...";

        try
        {
            IProgress<string> progress = new Progress<string>(
                message => InstallationProgressText.Text = message);
            await Task.Run(() => InstallWindows(parameters, progress));
            InstallationProgressText.Text = "Windows was installed successfully. You can close the installer.";
            InstallationProgressPanel.Visibility = Visibility.Collapsed;
            StatusBar.Severity = InfoBarSeverity.Success;
            StatusBar.Title = "Installation complete";
            StatusBar.Message = "Windows was applied and its boot files were installed successfully.";
            StatusBar.IsOpen = true;
            ConfirmDiskEraseCheckBox.IsChecked = false;
            InstallButton.IsEnabled = false;
            installationSucceeded = true;
        }
        catch (Exception ex)
        {
            InstallationProgressPanel.Visibility = Visibility.Collapsed;
            StatusBar.Severity = InfoBarSeverity.Error;
            StatusBar.Title = "Installation failed";
            StatusBar.Message =
                $"The installation stopped: {ex.Message} Any changes already made to Disk {targetDiskNumber} are not rolled back.";
            StatusBar.IsOpen = true;
        }
        finally
        {
            SetInstallerBusy(false);
            if (installationSucceeded)
            {
                ReviewButton.IsEnabled = false;
                ConfirmDiskEraseCheckBox.IsEnabled = false;
                InstallButton.IsEnabled = false;
            }

            _isInstalling = false;
        }
    }

    private Parameters CreateInstallationParameters(
        string imagePath,
        int imageIndex,
        int diskNumber,
        string windowsDrive,
        string efiDrive)
    {
        string fullImagePath = Path.GetFullPath(imagePath);
        string sourceDrive = Path.GetPathRoot(fullImagePath)
            ?? throw new InvalidDataException("Could not determine the source drive for the selected image.");

        return new Parameters(
            windowsDrive,
            efiDrive,
            diskNumber,
            sourceDrive,
            imageIndex,
            fullImagePath,
            DriversPathBox.Text.Trim(),
            InstallerManager.GetFirmwareType());
    }

    private static void ValidateInstallationParameters(Parameters parameters)
    {
        InstallerManager.ValidateInstallationParameters(ref parameters);
    }

    private static void InstallWindows(Parameters parameters, IProgress<string> progress)
    {
        InstallerManager.Install(ref parameters, progress);
    }

    private void SetInstallerBusy(bool isBusy)
    {
        Control[] controls =
        [
            MediaTypeBox,
            BrowseIsoButton,
            DriveLetterBox,
            FindImageButton,
            ImageFilesBox,
            ImageEditionBox,
            TargetDiskBox,
            WindowsDriveBox,
            EfiDriveBox,
            DriversPathBox,
            ReviewButton,
            ConfirmDiskEraseCheckBox,
            InstallButton
        ];

        if (isBusy)
        {
            _enabledStateBeforeInstall.Clear();
            foreach (Control control in controls)
            {
                _enabledStateBeforeInstall.Add(control, control.IsEnabled);
                control.IsEnabled = false;
            }

            return;
        }

        foreach ((Control control, bool wasEnabled) in _enabledStateBeforeInstall)
        {
            control.IsEnabled = wasEnabled;
        }

        _enabledStateBeforeInstall.Clear();
    }

    private void ConfirmDiskEraseCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        InstallButton.IsEnabled = ConfirmDiskEraseCheckBox.IsEnabled &&
                                  ConfirmDiskEraseCheckBox.IsChecked == true &&
                                  !_isInstalling;
    }

    private void ImageEdition_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        InvalidateReview();
    }

    private void DriversPath_TextChanged(object sender, TextChangedEventArgs e)
    {
        InvalidateReview();
    }

    private void InvalidateReview()
    {
        if (_isInstalling)
        {
            return;
        }

        ReviewCard.Visibility = Visibility.Collapsed;
        ConfirmDiskEraseCheckBox.IsEnabled = false;
        ConfirmDiskEraseCheckBox.IsChecked = false;
        InstallButton.IsEnabled = false;
    }

    private void TargetDisk_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _targetDiskNumber = (TargetDiskBox.SelectedItem as DiskOption)?.DiskNumber;
        InvalidateReview();
    }

    private void DriveLetter_DropDownOpened(object sender, object e)
    {
        RefreshDriveLetterChoices();
    }

    private void DriveLetter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingDestinationOptions || _isUpdatingDriveChoices)
        {
            return;
        }

        ComboBox changedBox = (ComboBox)sender;
        ComboBox otherBox = ReferenceEquals(changedBox, WindowsDriveBox)
            ? EfiDriveBox
            : WindowsDriveBox;
        string? selectedLetter = changedBox.SelectedItem as string;

        if (selectedLetter is not null)
        {
            try
            {
                if (!InstallerManager.IsDriveLetterAvailable(selectedLetter))
                {
                    DriveLetterError.Message = $"Drive {selectedLetter} is already in use. Select an unused drive letter.";
                    DriveLetterError.IsOpen = true;
                    RefreshDriveLetterChoices();
                    return;
                }
            }
            catch (Exception ex)
            {
                DriveLetterError.Message = $"Could not check drive {selectedLetter}: {ex.Message}";
                DriveLetterError.IsOpen = true;
                return;
            }
        }

        DriveLetterError.IsOpen = false;
        string? previousOtherSelection = otherBox.SelectedItem as string;

        _isUpdatingDriveChoices = true;
        try
        {
            otherBox.ItemsSource = _availableDriveLetters
                .Where(letter => !string.Equals(letter, selectedLetter, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            otherBox.SelectedItem = previousOtherSelection;
            otherBox.IsEnabled = otherBox.Items.Count > 0;
        }
        finally
        {
            _isUpdatingDriveChoices = false;
        }

        InvalidateReview();
    }

    private void RefreshDriveLetterChoices()
    {
        string? windowsSelection = WindowsDriveBox.SelectedItem as string;
        string? efiSelection = EfiDriveBox.SelectedItem as string;
        HashSet<string> availableLetters;
        try
        {
            availableLetters = InstallerManager.GetAvailableDriveLetters()
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            DriveLetterError.Message = $"Could not check drive letters: {ex.Message}";
            DriveLetterError.IsOpen = true;
            return;
        }

        _availableDriveLetters = availableLetters.OrderBy(letter => letter, StringComparer.OrdinalIgnoreCase).ToList();
        _isUpdatingDriveChoices = true;
        try
        {
            WindowsDriveBox.ItemsSource = _availableDriveLetters
                .Where(letter => !string.Equals(letter, efiSelection, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            EfiDriveBox.ItemsSource = _availableDriveLetters
                .Where(letter => !string.Equals(letter, windowsSelection, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            WindowsDriveBox.SelectedItem = windowsSelection;
            EfiDriveBox.SelectedItem = efiSelection;
            WindowsDriveBox.IsEnabled = WindowsDriveBox.Items.Count > 0;
            EfiDriveBox.IsEnabled = EfiDriveBox.Items.Count > 0;
        }
        finally
        {
            _isUpdatingDriveChoices = false;
        }

        string? occupiedSelection = new[] { windowsSelection, efiSelection }
            .FirstOrDefault(letter => letter is not null && !availableLetters.Contains(letter));
        if (occupiedSelection is not null)
        {
            DriveLetterError.Message = $"Drive {occupiedSelection} is now in use. Choose another drive letter.";
            DriveLetterError.IsOpen = true;
        }
        else
        {
            DriveLetterError.IsOpen = false;
        }
    }

    private async Task ScanImageEditionsAsync(string imagePath)
    {
        int scanVersion = ++_editionScanVersion;
        ImageEditionBox.ItemsSource = null;
        ImageEditionBox.IsEnabled = false;
        ImageSearchStatus.Text = "Scanning the image for available Windows editions...";

        try
        {
            Dictionary<int, string> editions = await Task.Run(
                () => InstallerManager.GetImageEditions(imagePath));
            if (scanVersion != _editionScanVersion ||
                !string.Equals(ImageFilesBox.SelectedItem as string, imagePath, StringComparison.Ordinal))
            {
                return;
            }

            if (editions.Count == 0)
            {
                ImageSearchStatus.Text = "No Windows editions were found in the selected image.";
                return;
            }

            ImageEditionBox.ItemsSource = editions.OrderBy(edition => edition.Key).ToArray();
            ImageEditionBox.SelectedIndex = 0;
            ImageEditionBox.IsEnabled = true;
            ImageSearchStatus.Text = $"Found {editions.Count} Windows edition(s).";
        }
        catch (Exception ex)
        {
            if (scanVersion == _editionScanVersion)
            {
                ShowValidationError($"Could not read Windows editions from this image: {ex.Message}");
            }
        }
    }

    private static string[] FindWindowsImages(string mediaRoot)
    {
        EnumerationOptions options = new()
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        return Directory.EnumerateFiles(mediaRoot, "*", options)
            .Where(path =>
                string.Equals(Path.GetFileName(path), "install.wim", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileName(path), "install.esd", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string MountIso(string isoPath)
    {
        string escapedPath = isoPath.Replace("'", "''", StringComparison.Ordinal);
        string script =
            "$ErrorActionPreference = 'Stop'; " +
            $"Mount-DiskImage -ImagePath '{escapedPath}' -PassThru | Out-Null; " +
            $"$volume = Get-DiskImage -ImagePath '{escapedPath}' | Get-Volume | Where-Object DriveLetter | Select-Object -First 1; " +
            "if ($null -eq $volume) { throw 'The ISO was mounted, but Windows did not assign it a drive letter.' }; " +
            "Write-Output ($volume.DriveLetter + ':')";

        ProcessStartInfo startInfo = new()
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(script);

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start PowerShell to mount the ISO.");
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        string output = outputTask.GetAwaiter().GetResult().Trim();
        string error = errorTask.GetAwaiter().GetResult().Trim();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error) ? "Windows could not mount the ISO." : error);
        }

        return NormalizeDriveRoot(output);
    }

    private static string NormalizeDriveRoot(string value)
    {
        string drive = value.Trim();
        if (drive.Length == 1 && char.IsAsciiLetter(drive[0]))
        {
            drive += ':';
        }

        if (drive.Length is < 2 or > 3 ||
            !char.IsAsciiLetter(drive[0]) ||
            drive[1] != ':' ||
            (drive.Length == 3 && drive[2] != '\\'))
        {
            throw new ArgumentException("Enter a drive letter in the format E:.");
        }

        return char.ToUpperInvariant(drive[0]) + @":\";
    }

    private void ResetImageSearch()
    {
        _editionScanVersion++;
        ImageFilesBox.ItemsSource = null;
        ImageFilesBox.IsEnabled = false;
        ImageEditionBox.ItemsSource = null;
        ImageEditionBox.IsEnabled = false;
        ImageSearchStatus.Text = string.Empty;
        InvalidateReview();
        StatusBar.IsOpen = false;
    }

    private void ShowValidationError(string message)
    {
        StatusBar.Severity = InfoBarSeverity.Error;
        StatusBar.Title = "Setup error";
        StatusBar.Message = message;
        StatusBar.IsOpen = true;
        InvalidateReview();
    }

    private sealed record DiskOption(int DiskNumber, string Model);

}
