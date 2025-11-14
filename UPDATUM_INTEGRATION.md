# Updatum Integration Guide

This document explains how the automatic update system has been integrated into Windows Edge Light using [Updatum](https://github.com/sn4k3/Updatum).

## What is Updatum?

Updatum is a lightweight C# library that automates application updates using GitHub Releases. It handles:
- Checking for new versions
- Downloading updates with progress tracking
- Installing updates automatically
- Displaying release notes

## How It Works

### 1. Update Checking

When the application starts, it automatically checks for updates after a 2-second delay:

```csharp
// In App.xaml.cs
protected override async void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    _ = CheckForUpdatesAsync();
}
```

### 2. Update Dialog

If a new version is available, a beautiful dialog appears showing:
- The new version number
- Release notes formatted from GitHub
- Three action buttons:
  - **Download & Install** - Downloads and installs the update
  - **Remind Me Later** - Skips this time, will check again next launch
  - **Skip This Version** - Ignores this version

### 3. Download Progress

When downloading an update, a progress dialog shows:
- Download progress bar
- Download size (MB downloaded / Total MB)
- Percentage complete

### 4. Installation

After downloading, the user is asked to confirm installation. If confirmed:
- For single-file executables: Replaces the current executable
- For MSI installers: Launches the installer
- For ZIP files: Extracts and updates files

## Configuration

### Setting Your GitHub Repository

**IMPORTANT:** Update the repository information in `App.xaml.cs`:

```csharp
internal static readonly UpdatumManager AppUpdater = new("YOUR_GITHUB_USERNAME", "WindowsEdgeLight")
```

Replace `YOUR_GITHUB_USERNAME` with your actual GitHub username.

### Asset Naming Convention

For Updatum to find your releases, name your GitHub Release assets following this pattern:

**Current naming (used by the GitHub Actions workflow):**

**For Single-File Executables:**
```
WindowsEdgeLight-v0.7.0-win-x64.exe
```

**For ZIP Files (Portable):**
```
WindowsEdgeLight-v0.7.0-win-x64.zip
```

The asset pattern is configured in `App.xaml.cs`:
```csharp
AssetRegexPattern = $"WindowsEdgeLight.*win-x64"
AssetExtensionFilter = "zip"  // Prefers ZIP files for easier updates
```

**Note:** The app is configured to prefer ZIP files because they allow Updatum to extract and update the executable more reliably. If only EXE files are present, those will be used instead.

### Publishing Configuration

When you publish your application, make sure the version number matches:

```xml
<!-- In WindowsEdgeLight.csproj -->
<AssemblyVersion>0.7.0.0</AssemblyVersion>
<FileVersion>0.7.0.0</FileVersion>
<Version>0.7</Version>
```

## Creating a GitHub Release

1. **Build and publish your app:**
   ```powershell
   dotnet publish -c Release
   ```

2. **Create a GitHub Release:**
   - Go to your repository on GitHub
   - Click "Releases" → "Draft a new release"
   - Tag: `v0.7.0` (must start with 'v')
   - Title: `Version 0.7.0`
   - Description: Add your release notes in Markdown format

3. **Upload the asset:**
   - Drag and drop your published executable/installer
   - Name it: `WindowsEdgeLight_win-x64_v0.7.0.exe` (or .msi or .zip)

4. **Publish the release**

## Release Notes Format

Updatum will automatically parse and display your GitHub release notes. Use Markdown for formatting:

```markdown
## v0.7.0

### New Features
- ✨ Automatic update system
- 📝 Release notes display

### Improvements
- 🚀 Better performance
- 🎨 Updated UI

### Bug Fixes
- 🐛 Fixed brightness control
- 🔧 Fixed memory leak
```

## Customization Options

### Update Check Timing

To check for updates at different times:

```csharp
// Check on startup with a delay
await Task.Delay(5000); // 5 second delay
var updateFound = await AppUpdater.CheckForUpdatesAsync();

// Periodic checks
AppUpdater.AutoUpdateCheckTimer.Interval = TimeSpan.FromHours(1).TotalMilliseconds;
AppUpdater.AutoUpdateCheckTimer.Start();
```

### MSI Installer Arguments

For silent/quiet MSI installations:

```csharp
// Current: Shows basic UI (/qb)
InstallUpdateWindowsInstallerArguments = "/qb",

// Silent with no UI:
InstallUpdateWindowsInstallerArguments = "/quiet",

// Full UI (default installer behavior):
InstallUpdateWindowsInstallerArguments = "",
```

### Asset Filtering

If you have multiple assets (e.g., portable and installer):

```csharp
// Prefer MSI files
AppUpdater.AssetExtensionFilter = "msi";

// Or prefer ZIP files
AppUpdater.AssetExtensionFilter = "zip";
```

## Testing the Update System

### Test with a Different Repository

To test without publishing your own releases:

```csharp
// Use the Updatum test repository
internal static readonly UpdatumManager AppUpdater = new("sn4k3", "UVtools")
{
    AssetRegexPattern = $"UVtools.*win-x64",
    InstallUpdateWindowsInstallerArguments = "/qb",
};
```

This will show you real update dialogs with the UVtools releases.

### Manual Testing

1. Set your version to something lower (e.g., 0.1.0)
2. Create a GitHub release with version 0.2.0
3. Run your application
4. You should see the update dialog

## Files Added

The following files were added to implement the update system:

- **UpdateDialog.xaml** - Update notification dialog UI
- **UpdateDialog.xaml.cs** - Update dialog logic
- **DownloadProgressDialog.xaml** - Download progress UI
- **DownloadProgressDialog.xaml.cs** - Download progress logic
- **App.xaml.cs** (modified) - Update checker integration

## NuGet Package

The following NuGet package was added:

```xml
<PackageReference Include="Updatum" Version="1.1.6" />
```

## Troubleshooting

### Update Not Found

1. **Check version numbers**: Your current version must be lower than the release version
2. **Check asset naming**: Ensure your asset name matches the regex pattern
3. **Check internet connection**: Updatum needs to access GitHub API
4. **Check GitHub repository**: Ensure the repository is public or you have access

### Download Fails

1. **Check asset size**: GitHub has size limits for releases
2. **Check permissions**: Ensure the app can write to the temp folder
3. **Check antivirus**: Some antivirus software may block downloads

### Installation Fails

1. **For MSI**: User needs admin privileges
2. **For single-file**: App must be able to replace its own executable
3. **Check file locks**: Close all instances before installing

## Advanced Features

### Skip Version Tracking

Currently not implemented, but you can add:

```csharp
// Save skipped version
Properties.Settings.Default.SkippedVersion = version;
Properties.Settings.Default.Save();

// Check if version was skipped
if (Properties.Settings.Default.SkippedVersion == release.TagName)
    return;
```

### Custom UI

You can create your own dialogs by replacing UpdateDialog and DownloadProgressDialog with your custom implementations.

### Rate Limiting

GitHub API has rate limits. Updatum handles this gracefully, but for frequent checks, consider:

```csharp
// Check only once per day
var lastCheck = Properties.Settings.Default.LastUpdateCheck;
if ((DateTime.Now - lastCheck).TotalHours < 24)
    return;
```

## Resources

- [Updatum GitHub Repository](https://github.com/sn4k3/Updatum)
- [Updatum Documentation](https://github.com/sn4k3/Updatum#readme)
- [GitHub Releases Documentation](https://docs.github.com/en/repositories/releasing-projects-on-github)
