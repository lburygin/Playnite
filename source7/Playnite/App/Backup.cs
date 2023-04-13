﻿using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;

namespace Playnite;

public enum BackupDataItem
{
    [Description(LocId.backup_option_settings)]         Settings = 0,
    [Description(LocId.backup_option_library)]          Library = 1,
    [Description(LocId.backup_option_game_media)]       LibraryFiles = 2,
    [Description(LocId.backup_option_extensions)]       Extensions = 3,
    [Description(LocId.backup_option_themes)]           Themes = 4,
    [Description(LocId.backup_option_extensions_data)]  ExtensionsData = 5
}

public class BackupOptions
{
    public string? DataDir { get; set; }
    public string? LibraryDir { get; set; }
    public string? OutputFile { get; set; }
    public string? OutputDir { get; set; }
    public List<BackupDataItem> BackupItems { get; set; } = new();
    public bool ClosedWhenDone  { get; set; }
    public bool CancelIfGameRunning { get; set; }
    public int RotatingBackups { get; set; } = 0;
}

public class BackupRestoreOptions
{
    public string? BackupFile { get; set; }
    public string? DataDir { get; set; }
    public string? LibraryDir { get; set; }
    public List<BackupDataItem> RestoreItems { get; set; } = new();
    public bool ClosedWhenDone { get; set; }
    public bool CancelIfGameRunning { get; set; }
    public string? RestoreLibrarySettingsPath { get; set; }
}

public class Backup
{
    private const string backupDateFormat = "yyyy-MM-dd-HH-mm-ss";
    private const string autoBackupFilePattern = autoBackupFileName + @"\-\d{4}\-\d{2}-\d{2}-\d{2}-\d{2}-\d{2}";
    private const string autoBackupFileName = "PlayniteBackup";
    private const string libraryEntryRoot = "library";
    private const string libraryFilesEntryRoot = "libraryfiles";
    private const string extensionsDataEntryRoot = "extensiondata";
    private const string extensionsEntryRoot = "extension";
    private const string themesEntryRoot = "themes";
    private static readonly string[] configFilesNames = new string[] { PlaynitePaths.ConfigFileName, PlaynitePaths.FullscreenConfigFileName };

    public static void BackupData(string optionsFile, CancellationToken cancelToken)
    {
        var backupOptions = Serialization.FromJsonFile<BackupOptions>(optionsFile);
        if (backupOptions is null)
        {
            throw new Exception("No backup option found in specified file");
        }
        else
        {
            BackupData(backupOptions, cancelToken);
        }
    }

    public static void BackupData(BackupOptions options, CancellationToken cancelToken)
    {
        if (options.DataDir.IsNullOrWhiteSpace())
        {
            throw new Exception("User data directory folder not specified.");
        }

        if (options.OutputDir.IsNullOrWhiteSpace() && options.OutputFile.IsNullOrWhiteSpace())
        {
            throw new Exception("Backup output path not specified!");
        }

        if (!options.OutputDir.IsNullOrWhiteSpace())
        {
            options.OutputFile = Path.Combine(options.OutputDir, GetAutoBackupFileName());
        }

        var tmpFile = options.OutputFile + ".tmp";
        FileSystem.PrepareSaveFile(tmpFile);

        using (var zipFile = new FileStream(tmpFile, FileMode.Create))
        using (var archive = new ZipArchive(zipFile, ZipArchiveMode.Create))
        {
            // Settings
            foreach (var config in configFilesNames)
            {
                var fullPath = Path.Combine(options.DataDir, config);
                if (File.Exists(fullPath))
                {
                    archive.CreateEntryFromFile(fullPath, config);
                }
            };

            if (Directory.Exists(options.LibraryDir))
            {
                // Library
                foreach (var libFile in Directory.GetFiles(Path.Combine(options.LibraryDir)))
                {
                    if (cancelToken.IsCancellationRequested)
                    {
                        goto archiveDone;
                    }

                    archive.CreateEntryFromFile(libFile, Path.Combine(libraryEntryRoot, Path.GetFileName(libFile)));
                }

                if (cancelToken.IsCancellationRequested)
                {
                    goto archiveDone;
                }

                // Library files
                var libFilesDir = Path.Combine(options.LibraryDir, Library.FilesDirName);
                if (options.BackupItems.Contains(BackupDataItem.LibraryFiles) && Directory.Exists(libFilesDir))
                {
                    archive.CreateEntryFromDirectory(libFilesDir, libraryFilesEntryRoot, cancelToken);
                }

                if (cancelToken.IsCancellationRequested)
                {
                    goto archiveDone;
                }
            }

            // Extensions
            var addonsDir = Path.Combine(options.DataDir, PlaynitePaths.ExtensionsDirName);
            if (options.BackupItems.Contains(BackupDataItem.Extensions) && Directory.Exists(addonsDir))
            {
                archive.CreateEntryFromDirectory(addonsDir, extensionsEntryRoot, cancelToken);
            }

            if (cancelToken.IsCancellationRequested)
            {
                goto archiveDone;
            }

            // Extensions data
            var extDataDir = Path.Combine(options.DataDir, PlaynitePaths.ExtensionsDataDirName);
            if (options.BackupItems.Contains(BackupDataItem.ExtensionsData) && Directory.Exists(extDataDir))
            {
                archive.CreateEntryFromDirectory(extDataDir, extensionsDataEntryRoot, cancelToken);
            }

            if (cancelToken.IsCancellationRequested)
            {
                goto archiveDone;
            }

            // Themes
            var themesDir = Path.Combine(options.DataDir, PlaynitePaths.ThemesDirName);
            if (options.BackupItems.Contains(BackupDataItem.Themes) && Directory.Exists(themesDir))
            {
                void packThemes(ApplicationMode mode)
                {
                    var themeRootDir = Themes.GetThemeRootDirName(mode);
                    var modeThemesDir = Path.Combine(themesDir, themeRootDir);
                    if (Directory.Exists(modeThemesDir))
                    {
                        foreach (var themeDir in Directory.GetDirectories(modeThemesDir))
                        {
                            var themeDirName = Path.GetFileName(themeDir);
                            // Never backup default themes
                            if (themeDirName == Themes.DefaultThemeDirName)
                            {
                                continue;
                            }

                            archive.CreateEntryFromDirectory(themeDir, Path.Combine(themesEntryRoot, themeRootDir, themeDirName), cancelToken);
                        }
                    }
                }

                packThemes(ApplicationMode.Desktop);
                packThemes(ApplicationMode.Fullscreen);
            }
        }

    archiveDone:
        if (cancelToken.IsCancellationRequested)
        {
            FileSystem.DeleteFile(tmpFile);
        }
        else
        {
            FileSystem.DeleteFile(options.OutputFile!);
            File.Move(tmpFile, options.OutputFile!);

            var backupDir = Path.GetDirectoryName(options.OutputFile)!;
            var files = Directory.GetFiles(backupDir, $"{autoBackupFileName}*.zip").Where(a => Regex.IsMatch(a, autoBackupFilePattern)).OrderBy(a => a).ToArray();
            if (files.Length > options.RotatingBackups + 1)
            {
                for (int i = 0; i < files.Length - (options.RotatingBackups + 1); i++)
                {
                    File.Delete(files[i]);
                }
            }
        }
    }

    public static void RestoreBackup(string optionsFile)
    {
        var restoreOptions = Serialization.FromJsonFile<BackupRestoreOptions>(optionsFile);
        if (restoreOptions is null)
        {
            throw new Exception("No backup restore option found in specified file");
        }
        else
        {
            RestoreBackup(restoreOptions);
        }
    }

    public static void RestoreBackup(BackupRestoreOptions options)
    {
        if (options.RestoreItems == null)
        {
            return;
        }

        if (options.DataDir.IsNullOrWhiteSpace())
        {
            throw new Exception("No user data directory specified.");
        }

        if (options.LibraryDir.IsNullOrWhiteSpace())
        {
            throw new Exception("No library directory specified.");
        }

        if (options.BackupFile.IsNullOrWhiteSpace())
        {
            throw new Exception("No backup file specified.");
        }

        FileSystem.CreateDirectory(options.DataDir);
        FileSystem.CreateDirectory(options.LibraryDir);
        using (var zipFile = new FileStream(options.BackupFile, FileMode.Open))
        using (var archive = new ZipArchive(zipFile, ZipArchiveMode.Read))
        {
            // Settings
            if (options.RestoreItems.Contains(BackupDataItem.Settings))
            {
                foreach (var config in configFilesNames)
                {
                    var configEntry = archive.GetEntry(config);
                    if (configEntry != null)
                    {
                        var origFile = Path.Combine(options.DataDir, config);
                        FileSystem.DeleteFile(origFile);
                        configEntry.ExtractToFile(origFile);
                    }
                }

                if (!options.RestoreLibrarySettingsPath.IsNullOrEmpty())
                {
                    // We are doing direct string replacement because proper serialization might not be safe here.
                    // We don't know what settings model version is the original file and potential conversion will
                    // be left to setting load on next startup.
                    var mainConfigFile = Path.Combine(options.DataDir, PlaynitePaths.ConfigFileName);
                    var resultLine = $"\"{nameof(PlayniteSettings.DatabasePath)}\": {Serialization.ToJson(options.RestoreLibrarySettingsPath)},";
                    var configContent = File.ReadAllLines(mainConfigFile);
                    for (int i = 0; i < configContent.Length; i++)
                    {
                        if (configContent[i].Contains($"\"{nameof(PlayniteSettings.DatabasePath)}\"", StringComparison.Ordinal))
                        {
                            configContent[i] = resultLine;
                        }
                    }

                    File.WriteAllLines(mainConfigFile, configContent);
                }
            }

            // Library
            var libPrefix = libraryEntryRoot + Path.DirectorySeparatorChar;
            if (options.RestoreItems.Contains(BackupDataItem.Library) && archive.Entries.Any(a => a.FullName.StartsWith(libPrefix, StringComparison.OrdinalIgnoreCase)))
            {
                foreach (var file in Directory.GetFiles(options.LibraryDir))
                {
                    File.Delete(file);
                }

                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.StartsWith(libPrefix, StringComparison.OrdinalIgnoreCase) &&
                        entry.FullName.Count(a => a == Path.DirectorySeparatorChar) == 1)
                    {
                        entry.ExtractToFile(Path.Combine(options.LibraryDir, entry.Name));
                    }
                }
            }

            void unpackBackupDir(bool restore, string outputDir, string dirPrefix)
            {
                dirPrefix = dirPrefix + Path.DirectorySeparatorChar;
                if (restore && archive.Entries.Any(a => a.FullName.StartsWith(dirPrefix, StringComparison.OrdinalIgnoreCase)))
                {
                    FileSystem.CreateDirectory(outputDir, true);
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.FullName.StartsWith(dirPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            var outFile = Path.Combine(outputDir, entry.FullName.Replace(dirPrefix, string.Empty, StringComparison.Ordinal));
                            FileSystem.PrepareSaveFile(outFile);
                            entry.ExtractToFile(outFile);
                        }
                    }
                }
            }

            void unpackThemeBackupDir(bool restore, string outputDir, string dirPrefix)
            {
                dirPrefix = dirPrefix + Path.DirectorySeparatorChar;
                if (restore && archive.Entries.Any(a => a.FullName.StartsWith(dirPrefix, StringComparison.OrdinalIgnoreCase)))
                {
                    void cleanThemeModeDir(ApplicationMode mode)
                    {
                        var modeDir = Path.Combine(outputDir, Themes.GetThemeRootDirName(mode));
                        FileSystem.CreateDirectory(modeDir, false);
                        foreach (var dir in Directory.GetDirectories(modeDir))
                        {
                            // Default themes must not be deleted since they are never included in the backup
                            if (new DirectoryInfo(dir).Name.Equals(Themes.DefaultThemeDirName, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            FileSystem.DeleteDirectory(dir);
                        }

                        foreach (var file in Directory.GetFiles(modeDir))
                        {
                            FileSystem.DeleteFile(file);
                        }
                    }

                    FileSystem.CreateDirectory(outputDir, false);
                    cleanThemeModeDir(ApplicationMode.Desktop);
                    cleanThemeModeDir(ApplicationMode.Fullscreen);
                    foreach (var entry in archive.Entries)
                    {
                        if (entry.FullName.StartsWith(dirPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            var outFile = Path.Combine(outputDir, entry.FullName.Replace(dirPrefix, string.Empty, StringComparison.Ordinal));
                            FileSystem.PrepareSaveFile(outFile);
                            entry.ExtractToFile(outFile);
                        }
                    }
                }
            }

            // Library files
            unpackBackupDir(options.RestoreItems.Contains(BackupDataItem.LibraryFiles), Path.Combine(options.LibraryDir, Library.FilesDirName), libraryFilesEntryRoot);

            // Extensions
            unpackBackupDir(options.RestoreItems.Contains(BackupDataItem.Extensions), Path.Combine(options.DataDir, PlaynitePaths.ExtensionsDirName), extensionsEntryRoot);

            // Extensions data
            unpackBackupDir(options.RestoreItems.Contains(BackupDataItem.ExtensionsData), Path.Combine(options.DataDir, PlaynitePaths.ExtensionsDataDirName), extensionsDataEntryRoot);

            // Themes
            unpackThemeBackupDir(options.RestoreItems.Contains(BackupDataItem.Themes), Path.Combine(options.DataDir, PlaynitePaths.ThemesDirName), themesEntryRoot);
        }
    }

    public static List<BackupDataItem> GetRestoreSelections(string backupFile)
    {
        var selections = new List<BackupDataItem> { BackupDataItem.Settings, BackupDataItem.Library };
        using (var zipFile = new FileStream(backupFile, FileMode.Open))
        using (var archive = new ZipArchive(zipFile, ZipArchiveMode.Read))
        {
            var entires = archive.Entries;
            if (entires.Any(a => a.FullName.StartsWith(libraryFilesEntryRoot, StringComparison.OrdinalIgnoreCase)))
            {
                selections.Add(BackupDataItem.LibraryFiles);
            }

            if (entires.Any(a => a.FullName.StartsWith(extensionsEntryRoot, StringComparison.OrdinalIgnoreCase)))
            {
                selections.Add(BackupDataItem.Extensions);
            }

            if (entires.Any(a => a.FullName.StartsWith(extensionsDataEntryRoot, StringComparison.OrdinalIgnoreCase)))
            {
                selections.Add(BackupDataItem.ExtensionsData);
            }

            if (entires.Any(a => a.FullName.StartsWith(themesEntryRoot, StringComparison.OrdinalIgnoreCase)))
            {
                selections.Add(BackupDataItem.Themes);
            }
        }

        return selections;
    }

    public static BackupOptions GetAutoBackupOptions(PlayniteSettings settings, string dataDir, string libraryDir)
    {
        var options = new BackupOptions
        {
            BackupItems = new List<BackupDataItem>
            {
                BackupDataItem.Settings,
                BackupDataItem.Library
            },
            DataDir = dataDir,
            LibraryDir = libraryDir,
            ClosedWhenDone = false,
            RotatingBackups = settings.RotatingBackups
        };

        if (settings.AutoBackupIncludeExtensions)
        {
            options.BackupItems.Add(BackupDataItem.Extensions);
        }

        if (settings.AutoBackupIncludeExtensionsData)
        {
            options.BackupItems.Add(BackupDataItem.ExtensionsData);
        }

        if (settings.AutoBackupIncludeThemes)
        {
            options.BackupItems.Add(BackupDataItem.Themes);
        }

        if (settings.AutoBackupIncludeLibFiles)
        {
            options.BackupItems.Add(BackupDataItem.LibraryFiles);
        }

        options.OutputDir = settings.AutoBackupDir;
        return options;
    }

    private static string GetAutoBackupFileName()
    {
        return $"{autoBackupFileName}-{DateTime.Now.ToString(backupDateFormat)}.zip";
    }
}
