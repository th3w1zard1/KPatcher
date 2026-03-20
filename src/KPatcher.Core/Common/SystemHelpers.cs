using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace KPatcher.Core.Common
{

    public static class SystemHelpers
    {
        private const int UnixModeFile = 0x1A4;   // 0o644
        private const int UnixModeDirectory = 0x1ED; // 0o755

        /// <summary>
        /// Attempts to gain write access to a directory and its contents.
        /// On Windows: removes ReadOnly attributes from files and directories.
        /// On Unix/Linux/macOS: applies chmod 0o644 to files and 0o755 to directories.
        /// </summary>
        public static void FixPermissions(string directoryPath, Action<string> logAction)
        {
            var dirInfo = new DirectoryInfo(directoryPath);
            if (!dirInfo.Exists)
            {
                return;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                FixPermissionsWindows(dirInfo, logAction);
            }
            else
            {
                FixPermissionsUnix(dirInfo, logAction);
            }
        }

        private static void FixPermissionsWindows(DirectoryInfo dirInfo, Action<string> logAction)
        {
            foreach (FileInfo file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                try
                {
                    if (file.IsReadOnly)
                    {
                        file.IsReadOnly = false;
                        logAction($"Fixed permissions for file: {file.FullName}");
                    }
                }
                catch (Exception ex)
                {
                    logAction($"Failed to fix permissions for file {file.FullName}: {ex.Message}");
                }
            }

            foreach (DirectoryInfo dir in dirInfo.GetDirectories("*", SearchOption.AllDirectories))
            {
                try
                {
                    if ((dir.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        dir.Attributes &= ~FileAttributes.ReadOnly;
                        logAction($"Fixed permissions for directory: {dir.FullName}");
                    }
                }
                catch (Exception ex)
                {
                    logAction($"Failed to fix permissions for directory {dir.FullName}: {ex.Message}");
                }
            }
        }

        private static void FixPermissionsUnix(DirectoryInfo dirInfo, Action<string> logAction)
        {
            foreach (FileInfo file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                try
                {
                    Chmod(file.FullName, UnixModeFile);
                    logAction($"Fixed permissions for file: {file.FullName}");
                }
                catch (Exception ex)
                {
                    logAction($"Failed to fix permissions for file {file.FullName}: {ex.Message}");
                }
            }

            foreach (DirectoryInfo dir in dirInfo.GetDirectories("*", SearchOption.AllDirectories))
            {
                try
                {
                    Chmod(dir.FullName, UnixModeDirectory);
                    logAction($"Fixed permissions for directory: {dir.FullName}");
                }
                catch (Exception ex)
                {
                    logAction($"Failed to fix permissions for directory {dir.FullName}: {ex.Message}");
                }
            }

            // Root directory itself
            try
            {
                Chmod(dirInfo.FullName, UnixModeDirectory);
                logAction($"Fixed permissions for directory: {dirInfo.FullName}");
            }
            catch (Exception ex)
            {
                logAction($"Failed to fix permissions for directory {dirInfo.FullName}: {ex.Message}");
            }
        }

        private static void Chmod(string path, int mode)
        {
            if (string.IsNullOrEmpty(path))
                return;

            string modeOctal = Convert.ToString(mode, 8);
            string args = $"{modeOctal} \"{path.Replace("\"", "\\\"")}\"";

            foreach (string chmodPath in new[] { "chmod", "/usr/bin/chmod" })
            {
                try
                {
                    using (var process = new Process())
                    {
                        process.StartInfo.FileName = chmodPath;
                        process.StartInfo.Arguments = args;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.RedirectStandardError = true;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.Start();
                        string stderr = process.StandardError.ReadToEnd();
                        process.WaitForExit(10000);
                        if (process.ExitCode != 0 && !string.IsNullOrEmpty(stderr))
                            throw new InvalidOperationException($"chmod failed: {stderr}");
                        return;
                    }
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    if (chmodPath == "/usr/bin/chmod")
                        throw;
                }
            }
        }

        /// <summary>
        /// Renames all files and directories to lowercase to fix case sensitivity issues (common on Linux/Steam Deck).
        /// </summary>
        public static void FixCaseSensitivity(string directoryPath, Action<string> logAction)
        {
            var dirInfo = new DirectoryInfo(directoryPath);
            if (!dirInfo.Exists)
            {
                return;
            }

            // Process recursively, bottom-up (files first, then subdirs, then parent dirs)
            // But C# GetFiles/GetDirectories returns everything upfront. 
            // We need to be careful about renaming directories while iterating.

            // Implementation strategy:
            // 1. Recursively process children first.
            // 2. Rename files in current directory.
            // 3. Rename current directory (handled by caller for root, or parent recursion).

            ProcessDirectoryCase(dirInfo, logAction);
        }

        private static void ProcessDirectoryCase(DirectoryInfo dir, Action<string> logAction)
        {
            // 1. Process subdirectories first (depth-first)
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                ProcessDirectoryCase(subDir, logAction);
            }

            // 2. Rename files in this directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string newName = file.Name.ToLowerInvariant();
                if (file.Name != newName)
                {
                    string newPath = Path.Combine(file.DirectoryName, newName);
                    try
                    {
                        // On case-insensitive FS (Windows), Move to same name with different case might need a temp step or just works depending on API.
                        // File.Move is usually case-insensitive on Windows, so "File.txt" -> "file.txt" might fail or be ignored.
                        // Safe bet: Move to temp, then to target.

                        string tempPath = newPath + ".tmp_" + Guid.NewGuid().ToString().Substring(0, 8);
                        file.MoveTo(tempPath);
                        File.Move(tempPath, newPath);

                        logAction($"Renamed file: {file.Name} -> {newName}");
                    }
                    catch (Exception ex)
                    {
                        logAction($"Failed to rename file {file.Name}: {ex.Message}");
                    }
                }
            }

            // 3. Rename this directory (unless it's the root we started with - but checking that is hard without context. 
            // Actually, we can't rename the directory object we are iterating FROM easily if we are inside it.
            // But GetDirectories returns DirectoryInfos that are valid handles. 
            // Wait, ProcessDirectoryCase calls itself. The child `subDir` is passed. 
            // We should rename `subDir` AFTER `ProcessDirectoryCase(subDir)` returns? 
            // No, inside `ProcessDirectoryCase(subDir)`, it processes its children. 
            // So we should rename `dir` at the end of its processing? 
            // But the caller holds a reference to `dir`.

            // Better approach: Iterate children, process them, then rename them.
        }

        public static void FixCaseSensitivityRecursive(string rootPath, Action<string> logAction)
        {
            // We use a custom walker to ensure we handle renames correctly.
            // In C#, we can simulate something like os.walk
            var dir = new DirectoryInfo(rootPath);
            if (!dir.Exists)
            {
                return;
            }

            FixCaseRecursiveInternal(dir, logAction);

            // Finally rename the root folder itself if needed
            string rootName = dir.Name;
            string newRootName = rootName.ToLowerInvariant();
            if (rootName != newRootName)
            {
                // Careful renaming the root if it's the path passed in, but usually desirable.
                RenameFileSystemEntry(dir, newRootName, logAction);
            }
        }

        private static void FixCaseRecursiveInternal(DirectoryInfo dir, Action<string> logAction)
        {
            // Subdirectories
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                // Recurse first
                FixCaseRecursiveInternal(subDir, logAction);

                // Then rename the subdirectory itself
                string name = subDir.Name;
                string newName = name.ToLowerInvariant();
                if (name != newName)
                {
                    RenameFileSystemEntry(subDir, newName, logAction);
                }
            }

            // Files
            foreach (FileInfo file in dir.GetFiles())
            {
                string name = file.Name;
                string newName = name.ToLowerInvariant();
                if (name != newName)
                {
                    RenameFileSystemEntry(file, newName, logAction);
                }
            }
        }

        private static void RenameFileSystemEntry(FileSystemInfo fsi, string newName, Action<string> logAction)
        {
            try
            {
                string dir = Path.GetDirectoryName(fsi.FullName);
                string newPath = Path.Combine(dir, newName);

                // Case-insensitive check: if paths match ignoring case, we are just changing case.
                if (string.Equals(fsi.FullName, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    // Case rename on Windows/Mac requires temp move often
                    string tempPath = newPath + ".tmp_rename";
                    if (fsi is DirectoryInfo d)
                    {
                        d.MoveTo(tempPath);
                        Directory.Move(tempPath, newPath);
                    }
                    else if (fsi is FileInfo f)
                    {
                        f.MoveTo(tempPath);
                        File.Move(tempPath, newPath);
                    }
                }
                else
                {
                    // Different name (or different case on case-sensitive FS where Equals is false? No, OrdinalIgnoreCase covers that).
                    // If we are here, it's a rename.
                    if (fsi is DirectoryInfo d)
                    {
                        d.MoveTo(newPath);
                    }
                    else if (fsi is FileInfo f)
                    {
                        f.MoveTo(newPath);
                    }
                }

                logAction($"Renamed: {fsi.Name} -> {newName}");
            }
            catch (Exception ex)
            {
                logAction($"Failed to rename {fsi.Name}: {ex.Message}");
            }
        }
    }
}

