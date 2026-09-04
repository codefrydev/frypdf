using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace PdfEditorApp.Services;

/// <summary>
/// Provides cross-platform utility methods for file operations:
/// renaming, duplication, deletion, file name validation, and revealing in OS file managers.
/// </summary>
public static class FileOperationHelper
{
    private static readonly char[] CrossPlatformInvalidChars = new[]
    {
        '/', '\\', ':', '*', '?', '"', '<', '>', '|', '\0'
    };

    /// <summary>
    /// Validates whether the proposed file name is valid across Windows, macOS, and Linux file systems.
    /// </summary>
    public static bool ValidateFileName(string? fileName, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(fileName))
        {
            errorMessage = "Document name cannot be empty.";
            return false;
        }

        var trimmed = fileName.Trim();
        if (trimmed.Length == 0 || trimmed == "." || trimmed == "..")
        {
            errorMessage = "Document name cannot be empty or a navigation dot.";
            return false;
        }

        if (trimmed.IndexOfAny(CrossPlatformInvalidChars) >= 0 || trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            errorMessage = "Document name contains invalid characters (e.g. / \\ : * ? \" < > |).";
            return false;
        }

        if (trimmed.Length > 255)
        {
            errorMessage = "Document name is too long (maximum 255 characters).";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Renames a physical file on disk to a new file name, preserving the original extension if not explicitly typed.
    /// </summary>
    public static bool RenameFile(string oldPath, string newName, out string? newPath, out string? errorMessage)
    {
        newPath = null;
        errorMessage = null;

        if (!File.Exists(oldPath))
        {
            errorMessage = "Original file could not be found on disk.";
            return false;
        }

        if (!ValidateFileName(newName, out errorMessage))
        {
            return false;
        }

        try
        {
            var dir = Path.GetDirectoryName(oldPath);
            if (string.IsNullOrEmpty(dir))
            {
                errorMessage = "Could not determine parent directory.";
                return false;
            }

            var originalExt = Path.GetExtension(oldPath);
            var trimmedName = newName.Trim();

            // If user typed the extension explicitly, keep it; otherwise append original extension
            string finalFileName;
            if (!string.IsNullOrEmpty(originalExt) && trimmedName.EndsWith(originalExt, StringComparison.OrdinalIgnoreCase))
            {
                finalFileName = trimmedName;
            }
            else
            {
                finalFileName = trimmedName + originalExt;
            }

            var destinationPath = Path.Combine(dir, finalFileName);

            // If same file path, no rename required
            if (string.Equals(oldPath, destinationPath, StringComparison.OrdinalIgnoreCase))
            {
                newPath = oldPath;
                return true;
            }

            // Check if another file with this name already exists
            if (File.Exists(destinationPath))
            {
                errorMessage = $"A file named \"{finalFileName}\" already exists in this folder.";
                return false;
            }

            File.Move(oldPath, destinationPath);
            newPath = destinationPath;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to rename file: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Creates a duplicate copy of a file in the same directory (e.g. "Document (Copy).pdf").
    /// </summary>
    public static bool DuplicateFile(string sourcePath, out string? newPath, out string? errorMessage)
    {
        newPath = null;
        errorMessage = null;

        if (!File.Exists(sourcePath))
        {
            errorMessage = "Source file does not exist on disk.";
            return false;
        }

        try
        {
            var dir = Path.GetDirectoryName(sourcePath);
            if (string.IsNullOrEmpty(dir))
            {
                errorMessage = "Could not determine parent directory.";
                return false;
            }

            var nameWithoutExt = Path.GetFileNameWithoutExtension(sourcePath);
            var ext = Path.GetExtension(sourcePath);

            string targetPath = Path.Combine(dir, $"{nameWithoutExt} (Copy){ext}");
            int copyIndex = 2;

            while (File.Exists(targetPath) && copyIndex <= 999)
            {
                targetPath = Path.Combine(dir, $"{nameWithoutExt} (Copy {copyIndex}){ext}");
                copyIndex++;
            }

            File.Copy(sourcePath, targetPath, false);
            newPath = targetPath;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to create duplicate: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Safely deletes a file from the disk.
    /// </summary>
    public static bool DeleteFile(string filePath, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            return true;
        }

        if (!File.Exists(filePath))
        {
            return true; // Already deleted
        }

        try
        {
            File.Delete(filePath);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to delete file: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Cross-platform helper to highlight/select a file in macOS Finder, Windows Explorer, or Linux file managers.
    /// </summary>
    public static bool RevealInFileManager(string filePath, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            errorMessage = "File path is empty.";
            return false;
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"-R \"{filePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi);
                return true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{filePath}\"",
                    UseShellExecute = true
                };
                Process.Start(psi);
                return true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var folder = File.Exists(filePath) ? Path.GetDirectoryName(filePath) : filePath;
                if (string.IsNullOrEmpty(folder)) folder = ".";

                var psi = new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = $"\"{folder}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi);
                return true;
            }
            else
            {
                errorMessage = "Unsupported operating system.";
                return false;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Could not open file manager: {ex.Message}";
            return false;
        }
    }
}
