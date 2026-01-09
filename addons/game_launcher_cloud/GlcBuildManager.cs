#if TOOLS
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace GameLauncherCloud;

/// <summary>
/// Build manager for Game Launcher Cloud.
/// Handles project export and compression for upload.
/// </summary>
public class GlcBuildManager
{
    private const string BuildsFolder = "Builds";
    private const string GlcUploadFolder = "GLC_Upload";

    /// <summary>
    /// Get the builds directory path
    /// </summary>
    public static string GetBuildsPath()
    {
        var projectPath = ProjectSettings.GlobalizePath("res://");
        return Path.Combine(projectPath, BuildsFolder);
    }

    /// <summary>
    /// Get the GLC upload directory path
    /// </summary>
    public static string GetUploadPath()
    {
        return Path.Combine(GetBuildsPath(), GlcUploadFolder);
    }

    /// <summary>
    /// Get the ZIP file path for upload
    /// </summary>
    public static string GetZipPath()
    {
        var projectName = ProjectSettings.GetSetting("application/config/name").AsString();
        if (string.IsNullOrEmpty(projectName))
        {
            projectName = "GodotProject";
        }

        // Sanitize project name for file system
        projectName = SanitizeFileName(projectName);

        return Path.Combine(GetBuildsPath(), $"{projectName}_upload.zip");
    }

    /// <summary>
    /// Sanitize a file name by removing invalid characters
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
    }

    /// <summary>
    /// Get available export presets
    /// </summary>
    public static List<ExportPresetInfo> GetExportPresets()
    {
        var presets = new List<ExportPresetInfo>();

        // Read export_presets.cfg if it exists
        var presetsPath = ProjectSettings.GlobalizePath("res://export_presets.cfg");
        if (!File.Exists(presetsPath))
        {
            GD.Print("[GLC] No export_presets.cfg found");
            return presets;
        }

        try
        {
            var content = File.ReadAllText(presetsPath);
            var lines = content.Split('\n');

            ExportPresetInfo? currentPreset = null;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("[preset."))
                {
                    // Save previous preset
                    if (currentPreset != null)
                    {
                        presets.Add(currentPreset);
                    }

                    // Extract preset index
                    var indexStr = trimmedLine.Replace("[preset.", "").Replace("]", "");
                    if (int.TryParse(indexStr, out var index))
                    {
                        currentPreset = new ExportPresetInfo { Index = index };
                    }
                }
                else if (currentPreset != null)
                {
                    if (trimmedLine.StartsWith("name="))
                    {
                        currentPreset.Name = trimmedLine.Replace("name=", "").Trim('"');
                    }
                    else if (trimmedLine.StartsWith("platform="))
                    {
                        currentPreset.Platform = trimmedLine.Replace("platform=", "").Trim('"');
                    }
                    else if (trimmedLine.StartsWith("export_path="))
                    {
                        currentPreset.ExportPath = trimmedLine.Replace("export_path=", "").Trim('"');
                    }
                }
            }

            // Add last preset
            if (currentPreset != null)
            {
                presets.Add(currentPreset);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[GLC] Error reading export presets: {ex.Message}");
        }

        return presets;
    }

    /// <summary>
    /// Export the project using the specified preset
    /// </summary>
    public static async Task<(bool Success, string Message, string? OutputPath)> ExportProjectAsync(
        int presetIndex, Action<string>? statusCallback = null)
    {
        var presets = GetExportPresets();
        if (presetIndex < 0 || presetIndex >= presets.Count)
        {
            return (false, "Invalid export preset index", null);
        }

        var preset = presets[presetIndex];
        GD.Print($"[GLC] Exporting project with preset: {preset.Name} ({preset.Platform})");
        statusCallback?.Invoke($"Exporting with preset: {preset.Name}...");

        var uploadPath = GetUploadPath();

        // Create upload directory if it doesn't exist
        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }
        else
        {
            // Clean existing files
            try
            {
                Directory.Delete(uploadPath, true);
                Directory.CreateDirectory(uploadPath);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[GLC] Error cleaning upload directory: {ex.Message}");
            }
        }

        // Determine output file name based on platform
        var projectName = ProjectSettings.GetSetting("application/config/name").AsString();
        if (string.IsNullOrEmpty(projectName))
        {
            projectName = "GodotProject";
        }

        projectName = SanitizeFileName(projectName);

        var extension = GetPlatformExtension(preset.Platform);
        var outputPath = Path.Combine(uploadPath, $"{projectName}{extension}");

        try
        {
            // Use Godot's command line export
            // This requires running Godot with --export or --export-release
            // For editor plugin, we'll use EditorInterface

            // Get the export platform
            var exportPlatform = EditorInterface.Singleton.GetEditorPaths();

            // Unfortunately, we cannot directly call export from C# in the editor
            // We need to use the command line or OS.Execute

            var godotPath = OS.GetExecutablePath();
            var projectPath = ProjectSettings.GlobalizePath("res://");

            var arguments = new List<string>
            {
                "--headless",
                "--path", projectPath,
                "--export-release", preset.Name, outputPath
            };

            GD.Print($"[GLC] Running: {godotPath} {string.Join(" ", arguments)}");

            // Run export in background without blocking the UI
            int exitCode = 0;
            Godot.Collections.Array output = new();
            
            await Task.Run(() =>
            {
                exitCode = OS.Execute(godotPath, arguments.ToArray(), output, true, false);
            });

            if (exitCode == 0)
            {
                // Verify output exists
                if (File.Exists(outputPath) || Directory.Exists(outputPath))
                {
                    GD.Print($"[GLC] Export successful: {outputPath}");
                    return (true, "Export successful", outputPath);
                }
                else
                {
                    // Check if it's a macOS .app bundle
                    var appPath = outputPath.Replace(".app", "") + ".app";
                    if (Directory.Exists(appPath))
                    {
                        return (true, "Export successful", appPath);
                    }

                    return (false, "Export completed but output not found", null);
                }
            }
            else
            {
                var outputStr = string.Join("\n", output);
                GD.PrintErr($"[GLC] Export failed with exit code {exitCode}");
                GD.PrintErr($"[GLC] Output: {outputStr}");
                return (false, $"Export failed with exit code {exitCode}", null);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[GLC] Export error: {ex.Message}");
            return (false, $"Export error: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Get file extension for platform
    /// </summary>
    private static string GetPlatformExtension(string platform)
    {
        return platform.ToLower() switch
        {
            "windows desktop" => ".exe",
            "windows" => ".exe",
            "linux/x11" => "",
            "linux" => "",
            "macos" => ".app",
            "mac os x" => ".app",
            "web" => ".html",
            "android" => ".apk",
            _ => ""
        };
    }

    /// <summary>
    /// Check if there's an existing build ready for upload
    /// </summary>
    public static BuildInfo? GetExistingBuild()
    {
        var zipPath = GetZipPath();
        var uploadPath = GetUploadPath();

        // Check for ZIP file first
        if (File.Exists(zipPath))
        {
            var zipInfo = new FileInfo(zipPath);

            // Try to get uncompressed size from ZIP
            long uncompressedSize = 0;
            int fileCount = 0;

            try
            {
                using var zip = ZipFile.OpenRead(zipPath);
                uncompressedSize = zip.Entries.Sum(e => e.Length);
                fileCount = zip.Entries.Count;
            }
            catch
            {
                // Ignore errors reading ZIP
            }

            return new BuildInfo
            {
                Path = zipPath,
                Size = zipInfo.Length,
                UncompressedSize = uncompressedSize,
                FileCount = fileCount,
                LastModified = zipInfo.LastWriteTime,
                IsCompressed = true
            };
        }

        // Check for uncompressed build directory
        if (Directory.Exists(uploadPath))
        {
            var dirInfo = new DirectoryInfo(uploadPath);
            var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
            var totalSize = files.Sum(f => f.Length);

            return new BuildInfo
            {
                Path = uploadPath,
                Size = totalSize,
                UncompressedSize = totalSize,
                FileCount = files.Length,
                LastModified = dirInfo.LastWriteTime,
                IsCompressed = false
            };
        }

        return null;
    }

    /// <summary>
    /// Compress the build directory into a ZIP file
    /// </summary>
    public static async Task<(bool Success, string Message, string? ZipPath, long? UncompressedSize)> CompressBuildAsync(
        Action<string>? statusCallback = null)
    {
        var uploadPath = GetUploadPath();
        var zipPath = GetZipPath();

        if (!Directory.Exists(uploadPath))
        {
            return (false, "Build directory not found", null, null);
        }

        statusCallback?.Invoke("Compressing build...");

        try
        {
            // Delete existing ZIP if exists
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            // Calculate uncompressed size before compression
            var dirInfo = new DirectoryInfo(uploadPath);
            var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
            var uncompressedSize = files.Sum(f => f.Length);

            GD.Print($"[GLC] Compressing {files.Length} files ({GlcMultipartUploadHelper.FormatFileSize(uncompressedSize)})...");

            // Compress in a separate task to not block the UI
            await Task.Run(() =>
            {
                ZipFile.CreateFromDirectory(uploadPath, zipPath, CompressionLevel.Optimal, false);
            });

            var zipInfo = new FileInfo(zipPath);
            var compressionRatio = (1 - (double)zipInfo.Length / uncompressedSize) * 100;

            GD.Print($"[GLC] Compression complete: {GlcMultipartUploadHelper.FormatFileSize(zipInfo.Length)} ({compressionRatio:F1}% saved)");

            return (true, "Build compressed successfully", zipPath, uncompressedSize);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[GLC] Compression error: {ex.Message}");
            return (false, $"Compression error: {ex.Message}", null, null);
        }
    }
}

/// <summary>
/// Information about an export preset
/// </summary>
public class ExportPresetInfo
{
    public int Index { get; set; }
    public string Name { get; set; } = "";
    public string Platform { get; set; } = "";
    public string ExportPath { get; set; } = "";

    public override string ToString()
    {
        return $"{Name} ({Platform})";
    }
}

/// <summary>
/// Information about an existing build
/// </summary>
public class BuildInfo
{
    public string Path { get; set; } = "";
    public long Size { get; set; }
    public long UncompressedSize { get; set; }
    public int FileCount { get; set; }
    public DateTime LastModified { get; set; }
    public bool IsCompressed { get; set; }
}
#endif
