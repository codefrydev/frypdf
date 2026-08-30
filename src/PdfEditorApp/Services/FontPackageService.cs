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
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(3)
    };

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
                TotalEstimatedSizeBytes = 20_971_520, // ~20 MB
                Files = new()
                {
                    new() { FileName = "NotoSansSC.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosanssc/v36/k3kXo84MPvpKitbxOm7bN5WG.ttf", FontFamilyName = "Noto Sans SC", FileSizeBytes = 10_485_760 },
                    new() { FileName = "NotoSansSC-Bold.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosanssc/v36/k3kUo84MPvpKitbxOm7bN5WCT5tT.ttf", FontFamilyName = "Noto Sans SC", IsBold = true, FileSizeBytes = 10_485_760 }
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
                TotalEstimatedSizeBytes = 14_260_633, // ~13.6 MB
                Files = new()
                {
                    new() { FileName = "NotoSansTC.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosanstc/v35/-nF7OG829Oo296awhR075Upt.ttf", FontFamilyName = "Noto Sans TC", FileSizeBytes = 7_130_316 },
                    new() { FileName = "NotoSansTC-Bold.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosanstc/v35/-nF5OG829Oo296awhR075Uq9K6A-.ttf", FontFamilyName = "Noto Sans TC", IsBold = true, FileSizeBytes = 7_130_317 }
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
                TotalEstimatedSizeBytes = 17_825_792, // ~17 MB
                Files = new()
                {
                    new() { FileName = "NotoSansJP.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosansjp/v53/-F6jfjtqLzI2JPCgQBnw7HFQ.ttf", FontFamilyName = "Noto Sans JP", FileSizeBytes = 5_347_737 },
                    new() { FileName = "NotoSansJP-Bold.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosansjp/v53/-F6hfjtqLzI2JPCgQBnw7HFY209E.ttf", FontFamilyName = "Noto Sans JP", IsBold = true, FileSizeBytes = 5_347_737 },
                    new() { FileName = "NotoSerifJP.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notoserifjp/v30/xn71YHs72GKoTvER4Gn3b5eMRtWG.ttf", FontFamilyName = "Noto Serif JP", FileSizeBytes = 7_130_318 }
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
                TotalEstimatedSizeBytes = 14_470_144, // ~13.8 MB
                Files = new()
                {
                    new() { FileName = "NotoSansKR.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosanskr/v36/PbykFmXiEBPT4ITbgNA5Cgm2.ttf", FontFamilyName = "Noto Sans KR", FileSizeBytes = 6_186_598 },
                    new() { FileName = "NotoSansKR-Bold.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosanskr/v36/PbyiFmXiEBPT4ITbgNA5Cgmx0aU4.ttf", FontFamilyName = "Noto Sans KR", IsBold = true, FileSizeBytes = 6_186_598 },
                    new() { FileName = "NanumGothic.ttf", DownloadUrl = "https://fonts.gstatic.com/s/nanumgothic/v23/PN_3Rfi-oW3hYwmKDpxS7F_z-7rVxCX9.ttf", FontFamilyName = "Nanum Gothic", FileSizeBytes = 2_096_948 }
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
                TotalEstimatedSizeBytes = 3_984_588, // ~3.8 MB
                Files = new()
                {
                    new() { FileName = "NotoSansDevanagari.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosansdevanagari/v27/5aU69_m6W52OnE9uhPSc5BpmdTag7K_4.ttf", FontFamilyName = "Noto Sans Devanagari", FileSizeBytes = 647_168 },
                    new() { FileName = "TiroDevanagariHindi.ttf", DownloadUrl = "https://fonts.gstatic.com/s/tirodevanagarihindi/v7/0ybuGDq4nOxJm8hT_7iTf8u84U15_V9T.ttf", FontFamilyName = "Tiro Devanagari Hindi", FileSizeBytes = 376_832 },
                    new() { FileName = "NotoSansTamil.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosanstamil/v27/ieVi2YdCLzn3FsNQDtpmCSWDZw.ttf", FontFamilyName = "Noto Sans Tamil", FileSizeBytes = 184_320 },
                    new() { FileName = "NotoSansTelugu.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosanstelugu/v25/0QI6MX5E4PnMG6qD4U3N7y4S.ttf", FontFamilyName = "Noto Sans Telugu", FileSizeBytes = 184_320 },
                    new() { FileName = "NotoSansBengali.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosansbengali/v23/Cn-8JitSh7y11iWiaP3VnArZcw.ttf", FontFamilyName = "Noto Sans Bengali", FileSizeBytes = 184_320 },
                    new() { FileName = "NotoSansGujarati.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosansgujarati/v25/nwpStKqkOQDViPby41pD6U0s.ttf", FontFamilyName = "Noto Sans Gujarati", FileSizeBytes = 184_320 },
                    new() { FileName = "NotoSansKannada.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosanskannada/v26/A2BEn5hXhN95j5-nUu3yY4w.ttf", FontFamilyName = "Noto Sans Kannada", FileSizeBytes = 184_320 },
                    new() { FileName = "NotoSansMalayalam.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosansmalayalam/v26/92zqtSCy4qO9x2L6nEvv3e_V.ttf", FontFamilyName = "Noto Sans Malayalam", FileSizeBytes = 184_320 }
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
                TotalEstimatedSizeBytes = 1_887_436, // ~1.8 MB
                Files = new()
                {
                    new() { FileName = "NotoSansArabic.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosansarabic/v28/nwpStKqkOQDViPby41pD6U0s.ttf", FontFamilyName = "Noto Sans Arabic", FileSizeBytes = 184_320 },
                    new() { FileName = "Vazirmatn.ttf", DownloadUrl = "https://fonts.gstatic.com/s/vazirmatn/v13/D5Hzw-G0WmjD2hB2G1Te3A.ttf", FontFamilyName = "Vazirmatn", FileSizeBytes = 204_800 },
                    new() { FileName = "NotoNastaliqUrdu.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notonastaliqurdu/v23/L0xoDF02iomPmqK9lP2m.ttf", FontFamilyName = "Noto Nastaliq Urdu", FileSizeBytes = 527_360 }
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
                TotalEstimatedSizeBytes = 409_600, // ~400 KB
                Files = new()
                {
                    new() { FileName = "NotoSansHebrew.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosanshebrew/v38/nwpStKqkOQDViPby41pD6U0s.ttf", FontFamilyName = "Noto Sans Hebrew", FileSizeBytes = 184_320 },
                    new() { FileName = "Heebo.ttf", DownloadUrl = "https://fonts.gstatic.com/s/heebo/v26/NGSpv5_NC0k9P_v6ZUCb.ttf", FontFamilyName = "Heebo", FileSizeBytes = 225_280 }
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
                TotalEstimatedSizeBytes = 1_433_600, // ~1.4 MB
                Files = new()
                {
                    new() { FileName = "NotoSansThai.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosansthai/v27/ieVi2YdCLzn3FsNQDtpmCSWDZw.ttf", FontFamilyName = "Noto Sans Thai", FileSizeBytes = 184_320 },
                    new() { FileName = "Sarabun.ttf", DownloadUrl = "https://fonts.gstatic.com/s/sarabun/v14/DtVkJxWL0Z6lFPW4.ttf", FontFamilyName = "Sarabun", FileSizeBytes = 184_320 },
                    new() { FileName = "NotoSansMyanmar.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosansmyanmar/v26/A2BEn5hXhN95j5-nUu3yY4w.ttf", FontFamilyName = "Noto Sans Myanmar", FileSizeBytes = 184_320 },
                    new() { FileName = "NotoSansKhmer.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosanskhmer/v26/92zqtSCy4qO9x2L6nEvv3e_V.ttf", FontFamilyName = "Noto Sans Khmer", FileSizeBytes = 184_320 },
                    new() { FileName = "NotoSansLao.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosanslao/v26/nwpStKqkOQDViPby41pD6U0s.ttf", FontFamilyName = "Noto Sans Lao", FileSizeBytes = 184_320 },
                    new() { FileName = "NotoSansSinhala.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosanssinhala/v26/0QI6MX5E4PnMG6qD4U3N7y4S.ttf", FontFamilyName = "Noto Sans Sinhala", FileSizeBytes = 235_520 },
                    new() { FileName = "BeVietnamPro.ttf", DownloadUrl = "https://fonts.gstatic.com/s/bevietnampro/v11/FeVQS0BCb2974re5P5tKoQV3.ttf", FontFamilyName = "Be Vietnam Pro", FileSizeBytes = 276_480 }
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
                TotalEstimatedSizeBytes = 1_843_200, // ~1.8 MB
                Files = new()
                {
                    new() { FileName = "GolosText.ttf", DownloadUrl = "https://fonts.gstatic.com/s/golostext/v4/pe1rFNWeHI2R8wU03YtK.ttf", FontFamilyName = "Golos Text", FileSizeBytes = 194_560 },
                    new() { FileName = "RussoOne.ttf", DownloadUrl = "https://fonts.gstatic.com/s/russoone/v16/Z9XUDmZqWgRLWhGzF18.ttf", FontFamilyName = "Russo One", FileSizeBytes = 122_880 },
                    new() { FileName = "GFSNeohellenic.ttf", DownloadUrl = "https://fonts.gstatic.com/s/gfsneohellenic/v24/0QI6MX5E4PnMG6qD4U3N7y4S.ttf", FontFamilyName = "GFS Neohellenic", FileSizeBytes = 440_320 },
                    new() { FileName = "NotoSansGeorgian.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosansgeorgian/v26/A2BEn5hXhN95j5-nUu3yY4w.ttf", FontFamilyName = "Noto Sans Georgian", FileSizeBytes = 184_320 },
                    new() { FileName = "NotoSansArmenian.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosansarmenian/v26/92zqtSCy4qO9x2L6nEvv3e_V.ttf", FontFamilyName = "Noto Sans Armenian", FileSizeBytes = 184_320 },
                    new() { FileName = "NotoSansEthiopic.ttf", DownloadUrl = "https://fonts.gstatic.com/s/notosansethiopic/v26/0QI6MX5E4PnMG6qD4U3N7y4S.ttf", FontFamilyName = "Noto Sans Ethiopic", FileSizeBytes = 368_640 }
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
                TotalEstimatedSizeBytes = 5_242_880, // ~5 MB
                Files = new()
                {
                    new() { FileName = "Poppins.ttf", DownloadUrl = "https://fonts.gstatic.com/s/poppins/v21/pxiEyp8kv8JHgFVrJJfecg.ttf", FontFamilyName = "Poppins", FileSizeBytes = 163_840 },
                    new() { FileName = "Poppins-Bold.ttf", DownloadUrl = "https://fonts.gstatic.com/s/poppins/v21/pxiByp8kv8JHgFVrLCz7Z1xlFQ.ttf", FontFamilyName = "Poppins", IsBold = true, FileSizeBytes = 163_840 },
                    new() { FileName = "Lato.ttf", DownloadUrl = "https://fonts.gstatic.com/s/lato/v24/S6uyw4BMUTPHjx4wXg.ttf", FontFamilyName = "Lato", FileSizeBytes = 655_360 },
                    new() { FileName = "Lato-Bold.ttf", DownloadUrl = "https://fonts.gstatic.com/s/lato/v24/S6u9w4BMUTPHh6UVSwiPGQ.ttf", FontFamilyName = "Lato", IsBold = true, FileSizeBytes = 655_360 },
                    new() { FileName = "Raleway.ttf", DownloadUrl = "https://fonts.gstatic.com/s/raleway/v28/1Crz2gBr3IWQqnJt63GDpQ.ttf", FontFamilyName = "Raleway", FileSizeBytes = 317_440 },
                    new() { FileName = "Nunito.ttf", DownloadUrl = "https://fonts.gstatic.com/s/nunito/v26/XRXV3I6Li01BKofINeaB.ttf", FontFamilyName = "Nunito", FileSizeBytes = 276_480 },
                    new() { FileName = "Ubuntu.ttf", DownloadUrl = "https://fonts.gstatic.com/s/ubuntu/v20/4iCs6KVjbNBYlgo6eA.ttf", FontFamilyName = "Ubuntu", FileSizeBytes = 358_400 },
                    new() { FileName = "TitilliumWeb.ttf", DownloadUrl = "https://fonts.gstatic.com/s/titilliumweb/v17/NaPecZTIAOhVxoIx-40FFIH_VAc.ttf", FontFamilyName = "Titillium Web", FileSizeBytes = 184_320 },
                    new() { FileName = "Exo2.ttf", DownloadUrl = "https://fonts.gstatic.com/s/exo2/v21/7cH1v4okm5zmbt6PFIFu.ttf", FontFamilyName = "Exo 2", FileSizeBytes = 184_320 },
                    new() { FileName = "Cabin.ttf", DownloadUrl = "https://fonts.gstatic.com/s/cabin/v27/u-4X0qWljRw-PfU81xCK.ttf", FontFamilyName = "Cabin", FileSizeBytes = 184_320 }
                }
            }
        };
    }
}
