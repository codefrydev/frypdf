using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Models;

namespace PdfEditorApp.Services.Ocr;

public interface ITesseractModelService
{
    event Action? LanguageLibraryChanged;

    string TessDataDirectory { get; }
    IReadOnlyList<TesseractLanguagePackageInfo> AvailableLanguages { get; }

    bool IsLanguageInstalled(string languageCode);
    string? GetInstalledLanguagePath(string languageCode);

    Task<bool> DownloadLanguageAsync(
        string languageCode,
        IProgress<double>? progress = null,
        Action<string>? statusCallback = null,
        CancellationToken ct = default);

    Task<bool> DeleteLanguageAsync(string languageCode);
    Task ClearAllCacheAsync();
    Task<long> GetTotalCacheSizeBytesAsync();

    bool IsTesseractCliAvailable();
    string? GetTesseractCliPath();
    void RefreshInstalledStatuses();
}

public class TesseractModelService : ITesseractModelService
{
    public event Action? LanguageLibraryChanged;

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

    public void RefreshInstalledStatuses()
    {
        foreach (var lang in _languages)
        {
            lang.IsInstalled = IsLanguageInstalled(lang.Code);
        }
        LanguageLibraryChanged?.Invoke();
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
                EstimatedSizeBytes = 4_000_000,
                Category = "Custom"
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
            LanguageLibraryChanged?.Invoke();
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
            LanguageLibraryChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
            statusCallback?.Invoke($"Download failed: {ex.Message}");
            return false;
        }
    }

    public Task<bool> DeleteLanguageAsync(string languageCode)
    {
        try
        {
            string targetPath = Path.Combine(_tessDataDir, $"{languageCode}.traineddata");
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            var lang = _languages.FirstOrDefault(l => string.Equals(l.Code, languageCode, StringComparison.OrdinalIgnoreCase));
            if (lang != null)
            {
                lang.IsInstalled = IsLanguageInstalled(lang.Code);
            }

            LanguageLibraryChanged?.Invoke();
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task ClearAllCacheAsync()
    {
        try
        {
            if (Directory.Exists(_tessDataDir))
            {
                var files = Directory.GetFiles(_tessDataDir, "*.traineddata")
                    .Concat(Directory.GetFiles(_tessDataDir, "*.tmp"));
                foreach (var f in files)
                {
                    try { File.Delete(f); } catch { }
                }
            }

            foreach (var lang in _languages)
            {
                lang.IsInstalled = IsLanguageInstalled(lang.Code);
            }

            LanguageLibraryChanged?.Invoke();
        }
        catch { }

        return Task.CompletedTask;
    }

    public Task<long> GetTotalCacheSizeBytesAsync()
    {
        try
        {
            if (!Directory.Exists(_tessDataDir))
                return Task.FromResult(0L);

            long total = 0;
            var dirInfo = new DirectoryInfo(_tessDataDir);
            foreach (var file in dirInfo.EnumerateFiles("*.traineddata", SearchOption.TopDirectoryOnly))
            {
                total += file.Length;
            }
            return Task.FromResult(total);
        }
        catch
        {
            return Task.FromResult(0L);
        }
    }

    public bool IsTesseractCliAvailable()
    {
        return !string.IsNullOrEmpty(GetTesseractCliPath());
    }

    public string? GetTesseractCliPath()
    {
        string[] candidates = OperatingSystem.IsWindows()
            ? new[] { "tesseract.exe", @"C:\Program Files\Tesseract-OCR\tesseract.exe", @"C:\Program Files (x86)\Tesseract-OCR\tesseract.exe" }
            : new[] { "tesseract", "/opt/homebrew/bin/tesseract", "/usr/local/bin/tesseract", "/usr/bin/tesseract" };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate)) return candidate;
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "where" : "which",
                Arguments = "tesseract",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                string path = p.StandardOutput.ReadLine()?.Trim() ?? "";
                p.WaitForExit(1000);
                if (File.Exists(path)) return path;
            }
        }
        catch { }

        return null;
    }

    private List<TesseractLanguagePackageInfo> InitializeCatalog()
    {
        string BaseUrl(string code) => $"https://raw.githubusercontent.com/tesseract-ocr/tessdata_fast/main/{code}.traineddata";

        var list = new List<TesseractLanguagePackageInfo>
        {
            // === 1. Latin & European ===
            new() { Code = "eng", DisplayName = "English", NativeName = "English", FlagEmoji = "🇺🇸", Category = "Latin & European", EstimatedSizeBytes = 4_113_000, DownloadUrl = BaseUrl("eng"), Description = "Fast LSTM neural model for English documents, forms, reports, and books.", SampleText = "The quick brown fox jumps over the lazy dog." },
            new() { Code = "spa", DisplayName = "Spanish", NativeName = "Español", FlagEmoji = "🇪🇸", Category = "Latin & European", EstimatedSizeBytes = 1_470_000, DownloadUrl = BaseUrl("spa"), Description = "Modern Spanish language model with accented characters and punctuation.", SampleText = "El veloz murciélago hindú comía feliz cardillo y kiwi." },
            new() { Code = "fra", DisplayName = "French", NativeName = "Français", FlagEmoji = "🇫🇷", Category = "Latin & European", EstimatedSizeBytes = 1_870_000, DownloadUrl = BaseUrl("fra"), Description = "French language model supporting cedillas, ligatures, and acute accents.", SampleText = "Voix ambiguë d'un cœur qui au zéphyr préfère les jattes de kiwis." },
            new() { Code = "deu", DisplayName = "German", NativeName = "Deutsch", FlagEmoji = "🇩🇪", Category = "Latin & European", EstimatedSizeBytes = 2_200_000, DownloadUrl = BaseUrl("deu"), Description = "German language model with umlauts (ä, ö, ü) and Eszett (ß).", SampleText = "Victor jagt zwölf Boxkämpfer quer über den großen Sylter Deich." },
            new() { Code = "ita", DisplayName = "Italian", NativeName = "Italiano", FlagEmoji = "🇮🇹", Category = "Latin & European", EstimatedSizeBytes = 1_680_000, DownloadUrl = BaseUrl("ita"), Description = "Italian language model for high-fidelity document OCR.", SampleText = "Quel vituperio di un pazzo fa sbadigliare la gente con zelo." },
            new() { Code = "por", DisplayName = "Portuguese", NativeName = "Português", FlagEmoji = "🇵🇹", Category = "Latin & European", EstimatedSizeBytes = 1_620_000, DownloadUrl = BaseUrl("por"), Description = "Portuguese model supporting European and Brazilian orthography.", SampleText = "Um pequeno jabuti xereta viu dez cegonhas felizes." },
            new() { Code = "nld", DisplayName = "Dutch", NativeName = "Nederlands", FlagEmoji = "🇳🇱", Category = "Latin & European", EstimatedSizeBytes = 1_600_000, DownloadUrl = BaseUrl("nld"), Description = "Dutch model supporting compounding and standard orthography.", SampleText = "Pa's wijze lynx bezag vroom het fijne cactussnoeiwerk." },
            new() { Code = "rus", DisplayName = "Russian", NativeName = "Русский", FlagEmoji = "🇷🇺", Category = "Latin & European", EstimatedSizeBytes = 3_800_000, DownloadUrl = BaseUrl("rus"), Description = "Cyrillic Russian language model for print and book OCR.", SampleText = "Съешь же ещё этих мягких французских булок, да выпей чаю." },
            new() { Code = "pol", DisplayName = "Polish", NativeName = "Polski", FlagEmoji = "🇵🇱", Category = "Latin & European", EstimatedSizeBytes = 1_800_000, DownloadUrl = BaseUrl("pol"), Description = "Polish language model with ogoneks, kreskas, and diacritics.", SampleText = "Pchnąć w tę łódź jeża lub ośm skrzyń fig." },
            new() { Code = "ukr", DisplayName = "Ukrainian", NativeName = "Українська", FlagEmoji = "🇺🇦", Category = "Latin & European", EstimatedSizeBytes = 2_600_000, DownloadUrl = BaseUrl("ukr"), Description = "Cyrillic Ukrainian model with apostrophes and specific vowels.", SampleText = "Чуєш, їхній плуг замінить рясну траву шовком." },
            new() { Code = "swe", DisplayName = "Swedish", NativeName = "Svenska", FlagEmoji = "🇸🇪", Category = "Latin & European", EstimatedSizeBytes = 1_400_000, DownloadUrl = BaseUrl("swe"), Description = "Swedish model supporting å, ä, ö and Scandinavian typography.", SampleText = "Flygande bäckasiner söka hwila på mjuk tuva." },
            new() { Code = "nor", DisplayName = "Norwegian", NativeName = "Norsk", FlagEmoji = "🇳🇴", Category = "Latin & European", EstimatedSizeBytes = 1_500_000, DownloadUrl = BaseUrl("nor"), Description = "Norwegian (Bokmål & Nynorsk) model with æ, ø, å support.", SampleText = "Vår sære ørn ble mager av sjøens ti fisk." },
            new() { Code = "dan", DisplayName = "Danish", NativeName = "Dansk", FlagEmoji = "🇩🇰", Category = "Latin & European", EstimatedSizeBytes = 1_400_000, DownloadUrl = BaseUrl("dan"), Description = "Danish model supporting æ, ø, å diacritics.", SampleText = "Quizdeltagerne spiste jordbær med fløde, mens cirkusklovnen kiggede på." },
            new() { Code = "fin", DisplayName = "Finnish", NativeName = "Suomi", FlagEmoji = "🇫🇮", Category = "Latin & European", EstimatedSizeBytes = 1_800_000, DownloadUrl = BaseUrl("fin"), Description = "Finnish model tuned for agglutinative words and compound terms.", SampleText = "Viekas kettu kuoppaan lankesi heti aamutuimaan." },
            new() { Code = "ces", DisplayName = "Czech", NativeName = "Čeština", FlagEmoji = "🇨🇿", Category = "Latin & European", EstimatedSizeBytes = 1_700_000, DownloadUrl = BaseUrl("ces"), Description = "Czech language model supporting carons and acutes.", SampleText = "Příliš žluťoučký kůň úpěl ďábelské ódy." },
            new() { Code = "ell", DisplayName = "Greek", NativeName = "Ελληνικά", FlagEmoji = "🇬🇷", Category = "Latin & European", EstimatedSizeBytes = 1_800_000, DownloadUrl = BaseUrl("ell"), Description = "Modern Greek script model with monotonic accentuation.", SampleText = "Ξεσκεπάζω την ψυχοφθόρα βδελυγμία του καθενός." },
            new() { Code = "lat", DisplayName = "Latin", NativeName = "Latina", FlagEmoji = "🏛️", Category = "Latin & European", EstimatedSizeBytes = 1_300_000, DownloadUrl = BaseUrl("lat"), Description = "Classical and Ecclesiastical Latin recognition model.", SampleText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit." },
            new() { Code = "tur", DisplayName = "Turkish", NativeName = "Türkçe", FlagEmoji = "🇹🇷", Category = "Latin & European", EstimatedSizeBytes = 1_500_000, DownloadUrl = BaseUrl("tur"), Description = "Turkish model with dotted/dotless i (İ/i, I/ı), ş, ğ.", SampleText = "Pijamalı hasta yağız şoföre çabucak güvendi." },

            // === 2. East Asia (CJK) ===
            new() { Code = "chi_sim", DisplayName = "Chinese Simplified", NativeName = "简体中文", FlagEmoji = "🇨🇳", Category = "East Asia (CJK)", EstimatedSizeBytes = 11_200_000, DownloadUrl = BaseUrl("chi_sim"), Description = "Simplified Chinese neural model (Hanzi / Mandarin).", SampleText = "天地玄黄，宇宙洪荒。日月盈昃，辰宿列张。" },
            new() { Code = "chi_tra", DisplayName = "Chinese Traditional", NativeName = "繁體中文", FlagEmoji = "🇹🇼", Category = "East Asia (CJK)", EstimatedSizeBytes = 11_100_000, DownloadUrl = BaseUrl("chi_tra"), Description = "Traditional Chinese neural model for Hong Kong, Taiwan, and classic docs.", SampleText = "有朋自遠方來，不亦樂乎？博學而篤志，切問而近思。" },
            new() { Code = "jpn", DisplayName = "Japanese", NativeName = "日本語", FlagEmoji = "🇯🇵", Category = "East Asia (CJK)", EstimatedSizeBytes = 8_200_000, DownloadUrl = BaseUrl("jpn"), Description = "Japanese model supporting Kanji, Hiragana, and Katakana.", SampleText = "いろはにほへと ちりぬるを わかよたれそ つねならむ" },
            new() { Code = "kor", DisplayName = "Korean", NativeName = "한국어", FlagEmoji = "🇰🇷", Category = "East Asia (CJK)", EstimatedSizeBytes = 4_900_000, DownloadUrl = BaseUrl("kor"), Description = "Korean Hangul script neural model.", SampleText = "다람쥐 헌 쳇바퀴에 타고파. 나랏말싸미 듕귁에 달아." },

            // === 3. South Asia (Indic) ===
            new() { Code = "hin", DisplayName = "Hindi", NativeName = "हिन्दी", FlagEmoji = "🇮🇳", Category = "South Asia (Indic)", EstimatedSizeBytes = 3_600_000, DownloadUrl = BaseUrl("hin"), Description = "Devanagari Hindi model for official, legal, and published documents.", SampleText = "सत्यमेव जयते। सभी मनुष्यों को गौरव और अधिकारों के विषय में समानता प्राप्त है।" },
            new() { Code = "ben", DisplayName = "Bengali", NativeName = "বাংলা", FlagEmoji = "🇧🇩", Category = "South Asia (Indic)", EstimatedSizeBytes = 2_500_000, DownloadUrl = BaseUrl("ben"), Description = "Bengali / Bangla script model for India and Bangladesh documents.", SampleText = "সকল মানুষ সমান মর্যাদা এবং অধিকার নিয়ে জন্মগ্রহণ করে।" },
            new() { Code = "tam", DisplayName = "Tamil", NativeName = "தமிழ்", FlagEmoji = "🇮🇳", Category = "South Asia (Indic)", EstimatedSizeBytes = 2_400_000, DownloadUrl = BaseUrl("tam"), Description = "Tamil script model with complex ligature recognition.", SampleText = "யாதும் ஊரே யாவரும் கேளிர்; தீதும் நன்றும் பிறர்தர வாரா." },
            new() { Code = "tel", DisplayName = "Telugu", NativeName = "తెలుగు", FlagEmoji = "🇮🇳", Category = "South Asia (Indic)", EstimatedSizeBytes = 2_300_000, DownloadUrl = BaseUrl("tel"), Description = "Telugu Dravidian script recognition model.", SampleText = "తెలుగు భాష అమృతము వంటిది. సర్వేజనా సుఖినోభవంతు." },
            new() { Code = "mar", DisplayName = "Marathi", NativeName = "मराठी", FlagEmoji = "🇮🇳", Category = "South Asia (Indic)", EstimatedSizeBytes = 2_800_000, DownloadUrl = BaseUrl("mar"), Description = "Marathi Devanagari model for government and educational records.", SampleText = "ज्ञान हीच खरी शक्ती आहे. सर्वांना समान संधी मिळायला हवी." },
            new() { Code = "san", DisplayName = "Sanskrit", NativeName = "संस्कृतम्", FlagEmoji = "🕉️", Category = "South Asia (Indic)", EstimatedSizeBytes = 3_200_000, DownloadUrl = BaseUrl("san"), Description = "Classical Sanskrit Devanagari model with Vedic accent markings.", SampleText = "सर्वे भवन्तु सुखिनः सर्वे सन्तु निरामयाः।" },
            new() { Code = "urd", DisplayName = "Urdu", NativeName = "اردو", FlagEmoji = "🇵🇰", Category = "South Asia (Indic)", EstimatedSizeBytes = 2_400_000, DownloadUrl = BaseUrl("urd"), Description = "Urdu Nastaliq / Perso-Arabic model for books and documents.", SampleText = "تمام انسان آزادی اور حقوق کے اعتبار سے برابر پیدا ہوئے ہیں۔" },

            // === 4. Middle East & RTL ===
            new() { Code = "ara", DisplayName = "Arabic", NativeName = "العربية", FlagEmoji = "🇸🇦", Category = "Middle East & RTL", EstimatedSizeBytes = 2_100_000, DownloadUrl = BaseUrl("ara"), Description = "Modern Standard Arabic right-to-left document OCR.", SampleText = "يولد جميع الناس أحراراً ومتساوين في الكرامة والحقوق." },
            new() { Code = "heb", DisplayName = "Hebrew", NativeName = "עברית", FlagEmoji = "🇮🇱", Category = "Middle East & RTL", EstimatedSizeBytes = 1_500_000, DownloadUrl = BaseUrl("heb"), Description = "Modern and Biblical Hebrew script recognition model.", SampleText = "כל בני אדם נולדו בני חורין ושווים בכבודם ובזכויותיהם." },
            new() { Code = "fas", DisplayName = "Persian", NativeName = "فارسی", FlagEmoji = "🇮🇷", Category = "Middle East & RTL", EstimatedSizeBytes = 2_200_000, DownloadUrl = BaseUrl("fas"), Description = "Persian / Farsi Nastaliq and standard typeface model.", SampleText = "همه افراد بشر آزاد به دنیا می‌آیند و از لحاظ حیثیت و حقوق با هم برابرند." },

            // === 5. Southeast Asia ===
            new() { Code = "vie", DisplayName = "Vietnamese", NativeName = "Tiếng Việt", FlagEmoji = "🇻🇳", Category = "Southeast Asia", EstimatedSizeBytes = 1_900_000, DownloadUrl = BaseUrl("vie"), Description = "Vietnamese Quốc ngữ model with multiple stacked diacritics.", SampleText = "Mọi người sinh ra đều được tự do và bình đẳng về nhân phẩm." },
            new() { Code = "tha", DisplayName = "Thai", NativeName = "ไทย", FlagEmoji = "🇹🇭", Category = "Southeast Asia", EstimatedSizeBytes = 2_500_000, DownloadUrl = BaseUrl("tha"), Description = "Thai script model without inter-word spacing.", SampleText = "มนุษย์ทุกคนเกิดมามีอิสระและเสมอภาคกันในศักดิ์ศรีและสิทธิ" },
            new() { Code = "ind", DisplayName = "Indonesian", NativeName = "Bahasa Indonesia", FlagEmoji = "🇮🇩", Category = "Southeast Asia", EstimatedSizeBytes = 1_400_000, DownloadUrl = BaseUrl("ind"), Description = "Bahasa Indonesia model for contracts, receipts, and forms.", SampleText = "Semua orang dilahirkan merdeka dan mempunyai martabat yang sama." },

            // === 6. Specialized & Other ===
            new() { Code = "osd", DisplayName = "Orientation & Script", NativeName = "OSD", FlagEmoji = "🧭", Category = "Specialized & Other", EstimatedSizeBytes = 10_500_000, DownloadUrl = BaseUrl("osd"), Description = "Orientation and script detection neural model (detects page angle & script).", SampleText = "0° / 90° / 180° / 270° Orientation & Script Classifier" },
            new() { Code = "equ", DisplayName = "Math & Equations", NativeName = "Equation", FlagEmoji = "📐", Category = "Specialized & Other", EstimatedSizeBytes = 2_400_000, DownloadUrl = BaseUrl("equ"), Description = "Mathematical notation, operators, formulas, and equation recognition.", SampleText = "f(x) = ∫ e^(-x²) dx, ∑(a_n * x^n)" }
        };

        foreach (var item in list)
        {
            item.IsInstalled = IsLanguageInstalled(item.Code);
        }

        return list;
    }
}
