using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;

namespace PdfEditorApp.Services.Ocr;

public interface ITesseractModelService
{
    string TessDataDirectory { get; }
    IReadOnlyList<TesseractLanguagePackageInfo> AvailableLanguages { get; }
    bool IsLanguageInstalled(string languageCode);
    string? GetInstalledLanguagePath(string languageCode);
    Task<bool> DownloadLanguageAsync(
        string languageCode,
        IProgress<double>? progress = null,
        Action<string>? statusCallback = null,
        CancellationToken ct = default);
}

public class TesseractModelService : ITesseractModelService
{
    private static readonly HttpClient HttpClient = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(10),
        ConnectTimeout = TimeSpan.FromSeconds(15)
    })
    {
        Timeout = TimeSpan.FromMinutes(5)
    };

    static TesseractModelService()
    {
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FryPDF-OcrDownloader/1.0");
    }

    private readonly string _tessDataDir;
    private readonly List<TesseractLanguagePackageInfo> _languages;

    public string TessDataDirectory => _tessDataDir;
    public IReadOnlyList<TesseractLanguagePackageInfo> AvailableLanguages => _languages;

    public TesseractModelService(string? customTessDataDir = null)
    {
        _tessDataDir = customTessDataDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".frypdf",
            "tessdata");

        if (!Directory.Exists(_tessDataDir))
        {
            try { Directory.CreateDirectory(_tessDataDir); } catch { }
        }

        _languages = InitializeCatalog();
    }

    public bool IsLanguageInstalled(string languageCode)
    {
        string path = Path.Combine(_tessDataDir, $"{languageCode}.traineddata");
        if (File.Exists(path) && new FileInfo(path).Length > 1000)
        {
            return true;
        }

        // Fallback: check app assets/bundle
        string appPath = Path.Combine(AppContext.BaseDirectory, "tessdata", $"{languageCode}.traineddata");
        return File.Exists(appPath) && new FileInfo(appPath).Length > 1000;
    }

    public string? GetInstalledLanguagePath(string languageCode)
    {
        string userPath = Path.Combine(_tessDataDir, $"{languageCode}.traineddata");
        if (File.Exists(userPath) && new FileInfo(userPath).Length > 1000)
            return userPath;

        string appPath = Path.Combine(AppContext.BaseDirectory, "tessdata", $"{languageCode}.traineddata");
        if (File.Exists(appPath) && new FileInfo(appPath).Length > 1000)
            return appPath;

        return null;
    }

    public async Task<bool> DownloadLanguageAsync(
        string languageCode,
        IProgress<double>? progress = null,
        Action<string>? statusCallback = null,
        CancellationToken ct = default)
    {
        var lang = _languages.FirstOrDefault(l => string.Equals(l.Code, languageCode, StringComparison.OrdinalIgnoreCase));
        if (lang == null)
        {
            // Custom or dynamic language code
            lang = new TesseractLanguagePackageInfo
            {
                Code = languageCode.ToLowerInvariant(),
                DisplayName = languageCode.ToUpperInvariant(),
                DownloadUrl = $"https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main/{languageCode.ToLowerInvariant()}.traineddata",
                EstimatedSizeBytes = 4_000_000
            };
        }

        Directory.CreateDirectory(_tessDataDir);
        string targetPath = Path.Combine(_tessDataDir, lang.FileName);
        string tempPath = targetPath + ".tmp";

        if (File.Exists(targetPath) && new FileInfo(targetPath).Length > 1000)
        {
            progress?.Report(1.0);
            statusCallback?.Invoke($"{lang.DisplayName} is already installed.");
            lang.IsInstalled = true;
            return true;
        }

        statusCallback?.Invoke($"Connecting to download {lang.DisplayName} ({lang.Code})...");
        progress?.Report(0.05);

        try
        {
            using var response = await HttpClient.GetAsync(lang.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            long totalExpected = response.Content.Headers.ContentLength ?? lang.EstimatedSizeBytes;
            if (totalExpected <= 0) totalExpected = 4_000_000;

            using var sourceStream = await response.Content.ReadAsStreamAsync(ct);
            using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);

            byte[] buffer = new byte[81920];
            long downloadedBytes = 0;
            int bytesRead;

            while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
                downloadedBytes += bytesRead;

                double p = Math.Min(0.98, (double)downloadedBytes / totalExpected);
                progress?.Report(p);
                statusCallback?.Invoke($"Downloading {lang.DisplayName}: {downloadedBytes / (1024 * 1024.0):F1} MB / {totalExpected / (1024 * 1024.0):F1} MB");
            }

            fileStream.Close();

            if (File.Exists(targetPath))
                File.Delete(targetPath);

            File.Move(tempPath, targetPath);

            lang.IsInstalled = true;
            progress?.Report(1.0);
            statusCallback?.Invoke($"{lang.DisplayName} language pack installed successfully.");
            return true;
        }
        catch (Exception ex)
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
            statusCallback?.Invoke($"Download failed: {ex.Message}");
            return false;
        }
    }

    private List<TesseractLanguagePackageInfo> InitializeCatalog()
    {
        string BaseUrl(string code) => $"https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main/{code}.traineddata";

        var list = new List<TesseractLanguagePackageInfo>
        {
            new() { Code = "eng", DisplayName = "English", NativeName = "English", FlagEmoji = "🇺🇸", EstimatedSizeBytes = 4_113_000, DownloadUrl = BaseUrl("eng") },
            new() { Code = "spa", DisplayName = "Spanish", NativeName = "Español", FlagEmoji = "🇪🇸", EstimatedSizeBytes = 1_470_000, DownloadUrl = BaseUrl("spa") },
            new() { Code = "fra", DisplayName = "French", NativeName = "Français", FlagEmoji = "🇫🇷", EstimatedSizeBytes = 1_870_000, DownloadUrl = BaseUrl("fra") },
            new() { Code = "deu", DisplayName = "German", NativeName = "Deutsch", FlagEmoji = "🇩🇪", EstimatedSizeBytes = 2_200_000, DownloadUrl = BaseUrl("deu") },
            new() { Code = "ita", DisplayName = "Italian", NativeName = "Italiano", FlagEmoji = "🇮🇹", EstimatedSizeBytes = 1_680_000, DownloadUrl = BaseUrl("ita") },
            new() { Code = "por", DisplayName = "Portuguese", NativeName = "Português", FlagEmoji = "🇵🇹", EstimatedSizeBytes = 1_620_000, DownloadUrl = BaseUrl("por") },
            new() { Code = "nld", DisplayName = "Dutch", NativeName = "Nederlands", FlagEmoji = "🇳🇱", EstimatedSizeBytes = 1_600_000, DownloadUrl = BaseUrl("nld") },
            new() { Code = "rus", DisplayName = "Russian", NativeName = "Русский", FlagEmoji = "🇷🇺", EstimatedSizeBytes = 3_800_000, DownloadUrl = BaseUrl("rus") },
            new() { Code = "chi_sim", DisplayName = "Chinese Simplified", NativeName = "简体中文", FlagEmoji = "🇨🇳", EstimatedSizeBytes = 11_200_000, DownloadUrl = BaseUrl("chi_sim") },
            new() { Code = "chi_tra", DisplayName = "Chinese Traditional", NativeName = "繁體中文", FlagEmoji = "🇹🇼", EstimatedSizeBytes = 11_100_000, DownloadUrl = BaseUrl("chi_tra") },
            new() { Code = "jpn", DisplayName = "Japanese", NativeName = "日本語", FlagEmoji = "🇯🇵", EstimatedSizeBytes = 8_200_000, DownloadUrl = BaseUrl("jpn") },
            new() { Code = "kor", DisplayName = "Korean", NativeName = "한국어", FlagEmoji = "🇰🇷", EstimatedSizeBytes = 4_900_000, DownloadUrl = BaseUrl("kor") },
            new() { Code = "hin", DisplayName = "Hindi", NativeName = "हिन्दी", FlagEmoji = "🇮🇳", EstimatedSizeBytes = 3_600_000, DownloadUrl = BaseUrl("hin") },
            new() { Code = "ara", DisplayName = "Arabic", NativeName = "العربية", FlagEmoji = "🇸🇦", EstimatedSizeBytes = 2_100_000, DownloadUrl = BaseUrl("ara") },
            new() { Code = "tur", DisplayName = "Turkish", NativeName = "Türkçe", FlagEmoji = "🇹🇷", EstimatedSizeBytes = 1_500_000, DownloadUrl = BaseUrl("tur") },
            new() { Code = "pol", DisplayName = "Polish", NativeName = "Polski", FlagEmoji = "🇵🇱", EstimatedSizeBytes = 1_800_000, DownloadUrl = BaseUrl("pol") },
            new() { Code = "vie", DisplayName = "Vietnamese", NativeName = "Tiếng Việt", FlagEmoji = "🇻🇳", EstimatedSizeBytes = 1_900_000, DownloadUrl = BaseUrl("vie") },
            new() { Code = "tha", DisplayName = "Thai", NativeName = "ไทย", FlagEmoji = "🇹🇭", EstimatedSizeBytes = 2_500_000, DownloadUrl = BaseUrl("tha") }
        };

        foreach (var item in list)
        {
            item.IsInstalled = IsLanguageInstalled(item.Code);
        }

        return list;
    }
}
