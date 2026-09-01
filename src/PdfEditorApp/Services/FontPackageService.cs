using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Analysis;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Services;

namespace PdfEditorApp.Services;

/// <summary>
/// Professional on-demand font package management service.
/// Keeps FryPDF lightweight while giving users on-demand access to worldwide language packs.
/// </summary>
public class FontPackageService : IFontPackageService
{
    private static readonly HttpClient HttpClient = CreateHttpClient();

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(3)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("FryPDF-FontDownloader/1.0");
        return client;
    }

    private readonly string _userFontDir;
    private readonly List<FontPackageInfo> _catalog;

    public event Action? FontLibraryChanged;

    public FontPackageService()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _userFontDir = Path.Combine(localAppData, "FryPDF", "Fonts");

        if (!Directory.Exists(_userFontDir))
        {
            try { Directory.CreateDirectory(_userFontDir); } catch { }
        }

        _catalog = BuildCatalog();

        // Register any already-cached fonts on startup
        RegisterAllDownloadedFontsWithQuestPdf();
    }

    public IReadOnlyList<FontPackageInfo> GetAllPackages() => _catalog;

    public string GetUserFontDirectory() => _userFontDir;

    public bool IsPackageInstalled(FontPackageInfo package)
    {
        if (package.Files.Count == 0) return true;

        foreach (var file in package.Files)
        {
            string userPath = Path.Combine(_userFontDir, file.FileName);
            if (File.Exists(userPath) && new FileInfo(userPath).Length > 0)
                continue;

            // Also check embedded fallback
            string appPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", file.FileName);
            if (File.Exists(appPath) && new FileInfo(appPath).Length > 0)
                continue;

            return false;
        }

        return true;
    }

    public async Task<bool> DownloadPackageAsync(
        FontPackageInfo package,
        IProgress<double>? progress = null,
        Action<string>? statusCallback = null,
        CancellationToken ct = default)
    {
        if (package.Files.Count == 0)
        {
            progress?.Report(1.0);
            return true;
        }

        Directory.CreateDirectory(_userFontDir);

        long totalBytesExpected = package.TotalEstimatedSizeBytes;
        long totalBytesDownloaded = 0;
        int completedFiles = 0;

        foreach (var file in package.Files)
        {
            if (ct.IsCancellationRequested) return false;

            string targetPath = Path.Combine(_userFontDir, file.FileName);
            string tempPath = targetPath + ".tmp";

            // If already downloaded and valid size, count as downloaded
            if (File.Exists(targetPath) && new FileInfo(targetPath).Length > 1000)
            {
                totalBytesDownloaded += new FileInfo(targetPath).Length;
                completedFiles++;
                progress?.Report((double)totalBytesDownloaded / Math.Max(1, totalBytesExpected));
                continue;
            }

            statusCallback?.Invoke($"Downloading {file.FileName}...");

            try
            {
                using var response = await HttpClient.GetAsync(file.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();

                long? contentLength = response.Content.Headers.ContentLength;
                using var sourceStream = await response.Content.ReadAsStreamAsync(ct);
                using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);

                byte[] buffer = new byte[81920];
                int bytesRead;

                while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, ct);
                    totalBytesDownloaded += bytesRead;
                    if (totalBytesExpected > 0)
                    {
                        double p = Math.Min(0.99, (double)totalBytesDownloaded / totalBytesExpected);
                        progress?.Report(p);
                    }
                }

                fileStream.Close();

                if (File.Exists(targetPath))
                    File.Delete(targetPath);

                File.Move(tempPath, targetPath);

                // Register with QuestPDF dynamically
                try
                {
                    using var regStream = File.OpenRead(targetPath);
                    QuestPDF.Drawing.FontManager.RegisterFont(regStream);
                }
                catch { }

                completedFiles++;
            }
            catch (Exception ex)
            {
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }

                statusCallback?.Invoke($"Error downloading {file.FileName}: {ex.Message}");
                return false;
            }
        }

        progress?.Report(1.0);
        statusCallback?.Invoke("Installed successfully!");
        FontLibraryChanged?.Invoke();
        return true;
    }

    public async Task<bool> DeletePackageAsync(FontPackageInfo package)
    {
        return await Task.Run(() =>
        {
            try
            {
                foreach (var file in package.Files)
                {
                    string targetPath = Path.Combine(_userFontDir, file.FileName);
                    if (File.Exists(targetPath))
                    {
                        File.Delete(targetPath);
                    }
                }

                FontLibraryChanged?.Invoke();
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public async Task<long> GetTotalCacheSizeBytesAsync()
    {
        return await Task.Run(() =>
        {
            if (!Directory.Exists(_userFontDir)) return 0L;
            try
            {
                var dir = new DirectoryInfo(_userFontDir);
                return dir.GetFiles("*.*", SearchOption.TopDirectoryOnly).Sum(f => f.Length);
            }
            catch
            {
                return 0L;
            }
        });
    }

    public async Task ClearAllCacheAsync()
    {
        await Task.Run(() =>
        {
            if (!Directory.Exists(_userFontDir)) return;
            try
            {
                foreach (var file in Directory.GetFiles(_userFontDir, "*.*"))
                {
                    try { File.Delete(file); } catch { }
                }
                FontLibraryChanged?.Invoke();
            }
            catch { }
        });
    }

    public async Task<bool> ImportCustomFontAsync(string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath)) return false;

        string ext = Path.GetExtension(sourceFilePath).ToLowerInvariant();
        if (ext != ".ttf" && ext != ".otf") return false;

        return await Task.Run(() =>
        {
            try
            {
                Directory.CreateDirectory(_userFontDir);
                string fileName = Path.GetFileName(sourceFilePath);
                string destPath = Path.Combine(_userFontDir, fileName);

                File.Copy(sourceFilePath, destPath, true);

                using (var stream = File.OpenRead(destPath))
                {
                    QuestPDF.Drawing.FontManager.RegisterFont(stream);
                }

                FontLibraryChanged?.Invoke();
                return true;
            }
            catch
            {
                return false;
            }
        });
    }

    public FontPackageInfo? DetectMissingPackageForText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        if (UnicodeScriptDetector.ContainsCjk(text))
        {
            string detectedFont = UnicodeScriptDetector.DetectScriptFontFamily(text);
            var matchingPack = _catalog.FirstOrDefault(p => p.IncludedFontFamilies.Contains(detectedFont));
            if (matchingPack != null && !IsPackageInstalled(matchingPack))
            {
                return matchingPack;
            }
        }
        else if (UnicodeScriptDetector.ContainsDevanagari(text))
        {
            var indicPack = _catalog.FirstOrDefault(p => p.Id == "indic");
            if (indicPack != null && !IsPackageInstalled(indicPack))
                return indicPack;
        }
        else if (UnicodeScriptDetector.IsRtlText(text))
        {
            var arabicPack = _catalog.FirstOrDefault(p => p.Id == "arabic");
            if (arabicPack != null && !IsPackageInstalled(arabicPack))
                return arabicPack;
        }

        return null;
    }

    public IEnumerable<string> GetAllAvailableFontFilePaths()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // User cache
        if (Directory.Exists(_userFontDir))
        {
            foreach (var f in Directory.GetFiles(_userFontDir, "*.*"))
            {
                string ext = Path.GetExtension(f).ToLowerInvariant();
                if (ext == ".ttf" || ext == ".otf")
                    paths.Add(f);
            }
        }

        // App assets
        string[] searchPaths =
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Assets", "Fonts"),
            Path.Combine(Directory.GetCurrentDirectory(), "src", "PdfEditorApp", "Assets", "Fonts"),
            Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Fonts")
        };

        foreach (var basePath in searchPaths)
        {
            if (Directory.Exists(basePath))
            {
                foreach (var f in Directory.GetFiles(basePath, "*.ttf"))
                    paths.Add(f);
                break;
            }
        }

        return paths;
    }

    public void RegisterAllDownloadedFontsWithQuestPdf()
    {
        try
        {
            foreach (var fontPath in GetAllAvailableFontFilePaths())
            {
                try
                {
                    using var stream = File.OpenRead(fontPath);
                    QuestPDF.Drawing.FontManager.RegisterFont(stream);
                }
                catch { }
            }
        }
        catch { }
    }

    /// <summary>
    /// Base URL for the official FryPDF font repository on GitHub.
    /// Eliminates Google Fonts CDN hash breakage and ensures 100% reliable downloads worldwide.
    /// </summary>
    public const string FontCdnBaseUrl = "https://raw.githubusercontent.com/codefrydev/PDFCreator-resources/refs/heads/main/fonts";

    private static string CdnUrl(string fileName) => $"{FontCdnBaseUrl}/{fileName}";

    private static List<FontPackageInfo> BuildCatalog()
    {
        return new List<FontPackageInfo>
        {
            // ── 1. Simplified Chinese ──────────────────────────────────────────
            new()
            {
                Id = "zh-hans",
                Name = "Simplified Chinese",
                NativeName = "简体中文",
                FlagEmoji = "🇨🇳",
                Region = "China, Singapore, Global",
                Category = FontPackageCategory.EastAsia,
                Description = "Complete Simplified Chinese typography with full GB2312/GBK coverage for reports, contracts, and documents.",
                SampleText = "创造世界一流的 PDF 编辑与文档处理体验。",
                SupportedLanguages = new() { "Chinese (Simplified)", "Mandarin" },
                IncludedFontFamilies = new() { "Noto Sans SC" },
                TotalEstimatedSizeBytes = 21_071_052,
                Files = new()
                {
                    new() { FileName = "NotoSansSC.ttf", DownloadUrl = CdnUrl("NotoSansSC.ttf"), FontFamilyName = "Noto Sans SC", FileSizeBytes = 10_540_644 },
                    new() { FileName = "NotoSansSC-Bold.ttf", DownloadUrl = CdnUrl("NotoSansSC-Bold.ttf"), FontFamilyName = "Noto Sans SC", IsBold = true, FileSizeBytes = 10_530_408 }
                }
            },

            // ── 2. Traditional Chinese ─────────────────────────────────────────
            new()
            {
                Id = "zh-hant",
                Name = "Traditional Chinese",
                NativeName = "繁體中文",
                FlagEmoji = "🇹🇼",
                Region = "Taiwan, Hong Kong, Macau",
                Category = FontPackageCategory.EastAsia,
                Description = "High-precision Traditional Chinese typography with Big5 and full character coverage for legal, business, and publishing.",
                SampleText = "創造世界一流的 PDF 編輯與文件處理體驗。",
                SupportedLanguages = new() { "Chinese (Traditional)", "Cantonese", "Taiwanese" },
                IncludedFontFamilies = new() { "Noto Sans TC" },
                TotalEstimatedSizeBytes = 14_176_420,
                Files = new()
                {
                    new() { FileName = "NotoSansTC.ttf", DownloadUrl = CdnUrl("NotoSansTC.ttf"), FontFamilyName = "Noto Sans TC", FileSizeBytes = 7_090_820 },
                    new() { FileName = "NotoSansTC-Bold.ttf", DownloadUrl = CdnUrl("NotoSansTC-Bold.ttf"), FontFamilyName = "Noto Sans TC", IsBold = true, FileSizeBytes = 7_085_600 }
                }
            },

            // ── 3. Japanese ───────────────────────────────────────────────────
            new()
            {
                Id = "ja",
                Name = "Japanese",
                NativeName = "日本語",
                FlagEmoji = "🇯🇵",
                Region = "Japan",
                Category = FontPackageCategory.EastAsia,
                Description = "Includes both modern Sans-Serif and elegant Serif Japanese fonts for Hiragana, Katakana, and Kanji.",
                SampleText = "世界最高峰のPDF編集およびドキュメント作成体験を提供します。",
                SupportedLanguages = new() { "Japanese" },
                IncludedFontFamilies = new() { "Noto Sans JP", "Noto Serif JP" },
                TotalEstimatedSizeBytes = 18_121_784,
                Files = new()
                {
                    new() { FileName = "NotoSansJP.ttf", DownloadUrl = CdnUrl("NotoSansJP.ttf"), FontFamilyName = "Noto Sans JP", FileSizeBytes = 5_324_144 },
                    new() { FileName = "NotoSansJP-Bold.ttf", DownloadUrl = CdnUrl("NotoSansJP-Bold.ttf"), FontFamilyName = "Noto Sans JP", IsBold = true, FileSizeBytes = 5_319_680 },
                    new() { FileName = "NotoSerifJP.ttf", DownloadUrl = CdnUrl("NotoSerifJP.ttf"), FontFamilyName = "Noto Serif JP", FileSizeBytes = 7_477_960 }
                }
            },

            // ── 4. Korean ─────────────────────────────────────────────────────
            new()
            {
                Id = "ko",
                Name = "Korean",
                NativeName = "한국어",
                FlagEmoji = "🇰🇷",
                Region = "South Korea",
                Category = FontPackageCategory.EastAsia,
                Description = "Complete Hangul syllable and Jamo character set with Noto Sans KR and Nanum Gothic for professional Korean documents.",
                SampleText = "세계 최고 수준의 PDF 편집 및 문서 작성 경험을 선사합니다.",
                SupportedLanguages = new() { "Korean" },
                IncludedFontFamilies = new() { "Noto Sans KR", "Nanum Gothic" },
                TotalEstimatedSizeBytes = 14_375_144,
                Files = new()
                {
                    new() { FileName = "NotoSansKR.ttf", DownloadUrl = CdnUrl("NotoSansKR.ttf"), FontFamilyName = "Noto Sans KR", FileSizeBytes = 6_163_256 },
                    new() { FileName = "NotoSansKR-Bold.ttf", DownloadUrl = CdnUrl("NotoSansKR-Bold.ttf"), FontFamilyName = "Noto Sans KR", IsBold = true, FileSizeBytes = 6_159_248 },
                    new() { FileName = "NanumGothic.ttf", DownloadUrl = CdnUrl("NanumGothic.ttf"), FontFamilyName = "Nanum Gothic", FileSizeBytes = 2_052_640 }
                }
            },

            // ── 5. Indic Languages ────────────────────────────────────────────
            new()
            {
                Id = "indic",
                Name = "Indic Languages Pack",
                NativeName = "भारतीय भाषाएँ",
                FlagEmoji = "🇮🇳",
                Region = "India, Nepal, Sri Lanka, Bangladesh",
                Category = FontPackageCategory.SouthAsia,
                Description = "Comprehensive Indian subcontinent typography: Devanagari (Hindi/Marathi/Sanskrit), Tamil, Telugu, Bengali, Gujarati, Kannada, Malayalam, and Tiro Hindi.",
                SampleText = "भारत का सबसे तेज़ और सुरक्षित PDF एडिटर और क्रिएटर।",
                SupportedLanguages = new() { "Hindi", "Marathi", "Sanskrit", "Tamil", "Telugu", "Bengali", "Gujarati", "Kannada", "Malayalam" },
                IncludedFontFamilies = new() { "Noto Sans Devanagari", "Tiro Devanagari Hindi", "Noto Sans Tamil", "Noto Sans Telugu", "Noto Sans Bengali", "Noto Sans Gujarati", "Noto Sans Kannada", "Noto Sans Malayalam" },
                TotalEstimatedSizeBytes = 1_414_088,
                Files = new()
                {
                    new() { FileName = "NotoSansDevanagari.ttf", DownloadUrl = CdnUrl("NotoSansDevanagari.ttf"), FontFamilyName = "Noto Sans Devanagari", FileSizeBytes = 219_460 },
                    new() { FileName = "TiroDevanagariHindi.ttf", DownloadUrl = CdnUrl("TiroDevanagariHindi.ttf"), FontFamilyName = "Tiro Devanagari Hindi", FileSizeBytes = 376_908 },
                    new() { FileName = "NotoSansTamil.ttf", DownloadUrl = CdnUrl("NotoSansTamil.ttf"), FontFamilyName = "Noto Sans Tamil", FileSizeBytes = 77_724 },
                    new() { FileName = "NotoSansTelugu.ttf", DownloadUrl = CdnUrl("NotoSansTelugu.ttf"), FontFamilyName = "Noto Sans Telugu", FileSizeBytes = 178_492 },
                    new() { FileName = "NotoSansBengali.ttf", DownloadUrl = CdnUrl("NotoSansBengali.ttf"), FontFamilyName = "Noto Sans Bengali", FileSizeBytes = 138_780 },
                    new() { FileName = "NotoSansGujarati.ttf", DownloadUrl = CdnUrl("NotoSansGujarati.ttf"), FontFamilyName = "Noto Sans Gujarati", FileSizeBytes = 173_540 },
                    new() { FileName = "NotoSansKannada.ttf", DownloadUrl = CdnUrl("NotoSansKannada.ttf"), FontFamilyName = "Noto Sans Kannada", FileSizeBytes = 143_576 },
                    new() { FileName = "NotoSansMalayalam.ttf", DownloadUrl = CdnUrl("NotoSansMalayalam.ttf"), FontFamilyName = "Noto Sans Malayalam", FileSizeBytes = 105_608 }
                }
            },

            // ── 6. Arabic, Persian & Urdu ─────────────────────────────────────
            new()
            {
                Id = "arabic",
                Name = "Arabic, Persian & Urdu",
                NativeName = "العربية / فارسی / اردو",
                FlagEmoji = "🇸🇦",
                Region = "Middle East, North Africa, Pakistan, Iran",
                Category = FontPackageCategory.MiddleEast,
                Description = "RTL-optimized font suite featuring Noto Sans Arabic, Vazirmatn (Farsi), and calligraphic Noto Nastaliq Urdu.",
                SampleText = "محرر ومُنشئ مستندات PDF الاحترافي والأكثر أماناً وسرعة.",
                SupportedLanguages = new() { "Arabic", "Persian (Farsi)", "Urdu", "Pashto", "Kurdish" },
                IncludedFontFamilies = new() { "Noto Sans Arabic", "Vazirmatn", "Noto Nastaliq Urdu" },
                TotalEstimatedSizeBytes = 824_404,
                Files = new()
                {
                    new() { FileName = "NotoSansArabic.ttf", DownloadUrl = CdnUrl("NotoSansArabic.ttf"), FontFamilyName = "Noto Sans Arabic", FileSizeBytes = 192_144 },
                    new() { FileName = "Vazirmatn.ttf", DownloadUrl = CdnUrl("Vazirmatn.ttf"), FontFamilyName = "Vazirmatn", FileSizeBytes = 104_640 },
                    new() { FileName = "NotoNastaliqUrdu.ttf", DownloadUrl = CdnUrl("NotoNastaliqUrdu.ttf"), FontFamilyName = "Noto Nastaliq Urdu", FileSizeBytes = 527_620 }
                }
            },

            // ── 7. Hebrew ─────────────────────────────────────────────────────
            new()
            {
                Id = "hebrew",
                Name = "Hebrew",
                NativeName = "עברית",
                FlagEmoji = "🇮🇱",
                Region = "Israel, Global",
                Category = FontPackageCategory.MiddleEast,
                Description = "Modern and classic Hebrew typography featuring Noto Sans Hebrew and Heebo for documents and books.",
                SampleText = "עורך ומציג מסמכי ה-PDF המקצועי והמהיר ביותר.",
                SupportedLanguages = new() { "Hebrew", "Yiddish" },
                IncludedFontFamilies = new() { "Noto Sans Hebrew", "Heebo" },
                TotalEstimatedSizeBytes = 90_452,
                Files = new()
                {
                    new() { FileName = "NotoSansHebrew.ttf", DownloadUrl = CdnUrl("NotoSansHebrew.ttf"), FontFamilyName = "Noto Sans Hebrew", FileSizeBytes = 46_496 },
                    new() { FileName = "Heebo.ttf", DownloadUrl = CdnUrl("Heebo.ttf"), FontFamilyName = "Heebo", FileSizeBytes = 43_956 }
                }
            },

            // ── 8. Southeast Asian ────────────────────────────────────────────
            new()
            {
                Id = "southeast-asia",
                Name = "Southeast Asian Pack",
                NativeName = "ไทย, မြန်မာ, ខ្មែរ, ລາວ",
                FlagEmoji = "🇹🇭",
                Region = "Thailand, Myanmar, Cambodia, Laos, Sri Lanka, Vietnam",
                Category = FontPackageCategory.SoutheastAsia,
                Description = "Complete Southeast Asian fonts: Thai (Sarabun & Noto), Burmese (Myanmar), Khmer, Lao, Sinhala, and Vietnamese (Be Vietnam Pro).",
                SampleText = "โปรแกรมสร้างและแก้ไขไฟล์ PDF ระดับมืออาชีพที่ปลอดภัยที่สุด",
                SupportedLanguages = new() { "Thai", "Burmese", "Khmer", "Lao", "Sinhala", "Vietnamese" },
                IncludedFontFamilies = new() { "Noto Sans Thai", "Sarabun", "Noto Sans Myanmar", "Noto Sans Khmer", "Noto Sans Lao", "Noto Sans Sinhala", "Be Vietnam Pro" },
                TotalEstimatedSizeBytes = 820_332,
                Files = new()
                {
                    new() { FileName = "NotoSansThai.ttf", DownloadUrl = CdnUrl("NotoSansThai.ttf"), FontFamilyName = "Noto Sans Thai", FileSizeBytes = 45_660 },
                    new() { FileName = "Sarabun.ttf", DownloadUrl = CdnUrl("Sarabun.ttf"), FontFamilyName = "Sarabun", FileSizeBytes = 81_516 },
                    new() { FileName = "NotoSansMyanmar.ttf", DownloadUrl = CdnUrl("NotoSansMyanmar.ttf"), FontFamilyName = "Noto Sans Myanmar", FileSizeBytes = 181_864 },
                    new() { FileName = "NotoSansKhmer.ttf", DownloadUrl = CdnUrl("NotoSansKhmer.ttf"), FontFamilyName = "Noto Sans Khmer", FileSizeBytes = 104_132 },
                    new() { FileName = "NotoSansLao.ttf", DownloadUrl = CdnUrl("NotoSansLao.ttf"), FontFamilyName = "Noto Sans Lao", FileSizeBytes = 51_004 },
                    new() { FileName = "NotoSansSinhala.ttf", DownloadUrl = CdnUrl("NotoSansSinhala.ttf"), FontFamilyName = "Noto Sans Sinhala", FileSizeBytes = 235_928 },
                    new() { FileName = "BeVietnamPro.ttf", DownloadUrl = CdnUrl("BeVietnamPro.ttf"), FontFamilyName = "Be Vietnam Pro", FileSizeBytes = 120_228 }
                }
            },

            // ── 9. Eurasian & Eastern Europe ──────────────────────────────────
            new()
            {
                Id = "eurasia",
                Name = "Eurasian & Eastern European",
                NativeName = "Русский, Ελληνικά, ქართული, Հայերեն",
                FlagEmoji = "🌍",
                Region = "Eastern Europe, Caucasus, Horn of Africa",
                Category = FontPackageCategory.EuropeAndEurasia,
                Description = "Cyrillic (Golos & Russo One), Greek (GFS Neohellenic), Georgian, Armenian, and Ethiopic (Amharic).",
                SampleText = "Самый быстрый и безопасный редактор и создатель PDF документов.",
                SupportedLanguages = new() { "Russian", "Ukrainian", "Bulgarian", "Greek", "Georgian", "Armenian", "Amharic" },
                IncludedFontFamilies = new() { "Golos Text", "Russo One", "GFS Neohellenic", "Noto Sans Georgian", "Noto Sans Armenian", "Noto Sans Ethiopic" },
                TotalEstimatedSizeBytes = 1_016_072,
                Files = new()
                {
                    new() { FileName = "GolosText.ttf", DownloadUrl = CdnUrl("GolosText.ttf"), FontFamilyName = "Golos Text", FileSizeBytes = 64_240 },
                    new() { FileName = "RussoOne.ttf", DownloadUrl = CdnUrl("RussoOne.ttf"), FontFamilyName = "Russo One", FileSizeBytes = 36_816 },
                    new() { FileName = "GFSNeohellenic.ttf", DownloadUrl = CdnUrl("GFSNeohellenic.ttf"), FontFamilyName = "GFS Neohellenic", FileSizeBytes = 440_376 },
                    new() { FileName = "NotoSansGeorgian.ttf", DownloadUrl = CdnUrl("NotoSansGeorgian.ttf"), FontFamilyName = "Noto Sans Georgian", FileSizeBytes = 60_888 },
                    new() { FileName = "NotoSansArmenian.ttf", DownloadUrl = CdnUrl("NotoSansArmenian.ttf"), FontFamilyName = "Noto Sans Armenian", FileSizeBytes = 48_388 },
                    new() { FileName = "NotoSansEthiopic.ttf", DownloadUrl = CdnUrl("NotoSansEthiopic.ttf"), FontFamilyName = "Noto Sans Ethiopic", FileSizeBytes = 365_364 }
                }
            },

            // ── 10. Creative & Typography Pack ─────────────────────────────────
            new()
            {
                Id = "creative-design",
                Name = "Creative & Typography Pack",
                NativeName = "Modern Display & Script",
                FlagEmoji = "🎨",
                Region = "Global",
                Category = FontPackageCategory.DesignAndTypography,
                Description = "Expanded design fonts for brochures, certificates, flyers, and branding: Poppins, Lato, Raleway, Nunito, Ubuntu, Playfair Display, Cinzel, Orbitron, Lobster, Pacifico, Dancing Script, and Titillium Web.",
                SampleText = "Design stunning certificates, invoices, and branded documents.",
                SupportedLanguages = new() { "All Latin Languages" },
                IncludedFontFamilies = new() { "Poppins", "Lato", "Raleway", "Nunito", "Ubuntu", "Playfair Display", "Cinzel", "Orbitron", "Lobster", "Pacifico", "Dancing Script", "Titillium Web", "Exo 2", "Cabin" },
                TotalEstimatedSizeBytes = 1_264_788,
                Files = new()
                {
                    new() { FileName = "Poppins.ttf", DownloadUrl = CdnUrl("Poppins.ttf"), FontFamilyName = "Poppins", FileSizeBytes = 154_628 },
                    new() { FileName = "Poppins-Bold.ttf", DownloadUrl = CdnUrl("Poppins-Bold.ttf"), FontFamilyName = "Poppins", IsBold = true, FileSizeBytes = 150_292 },
                    new() { FileName = "Lato.ttf", DownloadUrl = CdnUrl("Lato.ttf"), FontFamilyName = "Lato", FileSizeBytes = 72_312 },
                    new() { FileName = "Lato-Bold.ttf", DownloadUrl = CdnUrl("Lato-Bold.ttf"), FontFamilyName = "Lato", IsBold = true, FileSizeBytes = 70_576 },
                    new() { FileName = "Raleway.ttf", DownloadUrl = CdnUrl("Raleway.ttf"), FontFamilyName = "Raleway", FileSizeBytes = 138_808 },
                    new() { FileName = "Nunito.ttf", DownloadUrl = CdnUrl("Nunito.ttf"), FontFamilyName = "Nunito", FileSizeBytes = 125_528 },
                    new() { FileName = "Ubuntu.ttf", DownloadUrl = CdnUrl("Ubuntu.ttf"), FontFamilyName = "Ubuntu", FileSizeBytes = 280_328 },
                    new() { FileName = "TitilliumWeb.ttf", DownloadUrl = CdnUrl("TitilliumWeb.ttf"), FontFamilyName = "Titillium Web", FileSizeBytes = 53_980 },
                    new() { FileName = "Exo2.ttf", DownloadUrl = CdnUrl("Exo2.ttf"), FontFamilyName = "Exo 2", FileSizeBytes = 143_780 },
                    new() { FileName = "Cabin.ttf", DownloadUrl = CdnUrl("Cabin.ttf"), FontFamilyName = "Cabin", FileSizeBytes = 74_556 }
                }
            }
        };
    }
}
