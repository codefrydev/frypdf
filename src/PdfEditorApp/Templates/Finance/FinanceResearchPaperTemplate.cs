using System.Collections.Generic;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;

namespace PdfEditorApp.Templates.Finance;

public class FinanceResearchPaperTemplate : ITemplateDefinition
{
    public string Id => "financeresearch";
    public string Name => "Quantitative Finance & Econometrics Paper";
    public string Description => "Full 2-page quantitative finance paper with stochastic jump-diffusion SDEs, econometric calibration tables, and backtest metrics";
    public string Category => "Finance";
    public string IconKind => "ChartLineVariant";
    public string AccentColorHex => "#15803D";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Quantitative_Finance_Jump_Diffusion_2026.pdf",
            Author = "Dr. John Doe, CFA & Dr. Jane Doe",
            Subject = "Quantitative Finance, Stochastic Volatility & High-Frequency Risk Modeling"
        };

        // =========================================================================
        // PAGE 1: Title, Abstract, SDEs, Estimation & Model Benchmark Table
        // =========================================================================
        var page1 = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "JOURNAL OF FINANCIAL ECONOMETRICS • VOL. 34, ISSUE 2 • JEL: G12, G13, C58, C63",
            FooterCenter = "QUANTITATIVE RESEARCH ARTICLE",
            FooterRight = "Page 1 of 2",
            Elements = new List<PdfElementBase>
            {
                // Top Header Rule & Journal Metadata
                new PdfTextElement
                {
                    X = 55,
                    Y = 32,
                    Width = 690,
                    Height = 18,
                    Text = "JOURNAL OF FINANCIAL ECONOMETRICS, VOL. 34 (2026), PP. 240–288 • DOI: 10.1093/jjfinec/nbad024",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    TextColorHex = "#166534",
                    Alignment = TextAlignmentMode.Center
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 52,
                    Width = 690,
                    Height = 1,
                    Thickness = 0.75,
                    ColorHex = "#86EFAC"
                },

                // Main Paper Title
                new PdfTextElement
                {
                    X = 55,
                    Y = 58,
                    Width = 690,
                    Height = 48,
                    Text = "Multi-Factor Jump-Diffusion Asset Pricing and Stochastic Volatility in High-Frequency Futures Markets",
                    FontSize = 16,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#14532D",
                    Alignment = TextAlignmentMode.Center
                },

                // Authors & Affiliation
                new PdfTextElement
                {
                    X = 55,
                    Y = 108,
                    Width = 690,
                    Height = 34,
                    Text = "John Doe, Ph.D., CFA¹   and   Jane Doe, Ph.D.²\n¹Quantitative Investment Strategies, CodeFryDev Capital Management   •   ²Department of Finance, CodeFryDev Institute",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Center
                },

                // Abstract & JEL Codes Card
                new PdfShapeElement
                {
                    X = 75,
                    Y = 145,
                    Width = 650,
                    Height = 104,
                    CornerRadius = 4,
                    FillColorHex = "#F0FDF4",
                    StrokeColorHex = "#BBF7D0",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 90,
                    Y = 150,
                    Width = 620,
                    Height = 68,
                    Text = "Abstract— We develop a generalized continuous-time Bates jump-diffusion framework with state-dependent jump intensities calibrated to 100-microsecond order-book data for S&P 500 E-mini and Treasury futures. Out-of-sample hedging tests demonstrate a 32.8% reduction in pricing root-mean-square error (RMSE) during high-volatility regime shifts compared to classical local volatility surfaces.",
                    FontSize = 9,
                    FontFamily = "Times New Roman",
                    IsItalic = true,
                    LineHeight = 1.35,
                    TextColorHex = "#166534",
                    Alignment = TextAlignmentMode.Justify
                },
                new PdfTextElement
                {
                    X = 90,
                    Y = 222,
                    Width = 620,
                    Height = 22,
                    Text = "JEL Classification— G12 (Asset Pricing), G13 (Contingent Pricing), C58 (Financial Econometrics), C63 (Computational Techniques).",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#15803D"
                },

                // Column Split Divider
                new PdfDividerElement
                {
                    X = 55,
                    Y = 254,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#86EFAC"
                },

                // ==========================================
                // COLUMN 1 (Left: X = 55, Width = 330)
                // ==========================================
                new PdfTextElement
                {
                    X = 55,
                    Y = 262,
                    Width = 330,
                    Height = 22,
                    Text = "1. STOCHASTIC DYNAMICS SPECIFICATION",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#14532D"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 286,
                    Width = 330,
                    Height = 90,
                    Text = "Let (Ω, ℱ, {ℱ_t}, ℙ) represent a filtered probability space satisfying usual conditions. Under the physical probability measure ℙ, the spot asset price process S_t and variance process V_t follow the coupled stochastic differential equations [1, 2]:",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                // SDE Equation Box
                new PdfShapeElement
                {
                    X = 55,
                    Y = 380,
                    Width = 330,
                    Height = 52,
                    CornerRadius = 3,
                    FillColorHex = "#F0FDF4",
                    StrokeColorHex = "#BBF7D0",
                    StrokeThickness = 0.5
                },
                new PdfTextElement
                {
                    X = 65,
                    Y = 386,
                    Width = 310,
                    Height = 40,
                    Text = "dS_t = (μ - λ k̄) S_t dt + √V_t S_t dW_t^S + (e^J - 1) S_t dN_t\ndV_t = κ(θ - V_t) dt + σ_v √V_t dW_t^V      (1)",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#14532D",
                    Alignment = TextAlignmentMode.Center
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 438,
                    Width = 330,
                    Height = 110,
                    Text = "where d⟨W^S, W^V⟩_t = ρ dt represents the leverage correlation, N_t is a Poisson counting process with intensity λ_t = λ₀ + λ₁ V_t, and J ~ 𝒩(μ_J, σ_J²) denotes the log-jump amplitude distribution with mean percentage jump size k̄ = exp(μ_J + ½σ_J²) - 1.",
                    FontSize = 9,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 554,
                    Width = 330,
                    Height = 22,
                    Text = "2. QUASI-MAXIMUM LIKELIHOOD ESTIMATION",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#14532D"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 578,
                    Width = 330,
                    Height = 130,
                    Text = "Parameter estimation is performed via Sequential Monte Carlo (particle filtering) combined with Fourier transform inversion of the characteristic function ϕ(u; t) = 𝔼[exp(i u ln S_t)]. The Feller condition 2κθ > σ_v² is strictly enforced across optimization iterations to ensure that the variance process V_t remains strictly positive almost surely [3].",
                    FontSize = 9,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 712,
                    Width = 330,
                    Height = 22,
                    Text = "3. EMPIRICAL VOLATILITY SURFACE SMILE",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#14532D"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 736,
                    Width = 330,
                    Height = 110,
                    Text = "Short-maturity options exhibit pronounced implied volatility skews that cannot be replicated by diffusion-only models. Incorporating compound Poisson jump arrivals resolves the term-structure flattening problem and matches empirical kurtosis.",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                // ==========================================
                // COLUMN 2 (Right: X = 415, Width = 330)
                // ==========================================
                new PdfTextElement
                {
                    X = 415,
                    Y = 262,
                    Width = 330,
                    Height = 22,
                    Text = "4. OUT-OF-SAMPLE MODEL BENCHMARKS",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#14532D"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 286,
                    Width = 330,
                    Height = 58,
                    Text = "Table I reports out-of-sample pricing RMSE, annualized Sharpe ratio, maximum portfolio drawdown, and 99% daily Value-at-Risk (VaR) exception rates across 10 years of tick data (2016–2026).",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                // Econometric Performance Table
                new PdfTableElement
                {
                    X = 415,
                    Y = 348,
                    Width = 330,
                    Height = 145,
                    Headers = new List<string> { "Model Specification", "RMSE", "Sharpe", "Max DD", "VaR 99%" },
                    Rows = new List<List<string>>
                    {
                        new() { "Black-Scholes (1973)", "$4.12", "0.84", "-28.4%", "3.8%" },
                        new() { "Heston SV (1993)", "$1.89", "1.42", "-16.2%", "1.6%" },
                        new() { "Rough Bergomi (2019)", "$1.34", "1.78", "-11.5%", "1.1%" },
                        new() { "Proposed Jump-Diff", "$0.92", "2.14", "-7.8%", "0.9%" }
                    },
                    HeaderBackgroundHex = "#15803D",
                    HeaderTextHex = "#FFFFFF",
                    AlternateRowBackgroundHex = "#F0FDF4",
                    BorderColorHex = "#CBD5E1"
                },

                new PdfTextElement
                {
                    X = 415,
                    Y = 502,
                    Width = 330,
                    Height = 22,
                    Text = "5. RISK PARITY BACKTESTING",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#14532D"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 526,
                    Width = 330,
                    Height = 125,
                    Text = "Dynamic delta-gamma-vega hedging strategies derived from the calibrated jump-diffusion parameters achieved superior risk-adjusted alpha during market stress periods (e.g. Flash Crash events and interest rate spikes), cutting tail drawdown risk by 72% [4].",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                new PdfTextElement
                {
                    X = 415,
                    Y = 658,
                    Width = 330,
                    Height = 22,
                    Text = "6. CROSS-ASSET CALIBRATION METRICS",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#14532D"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 682,
                    Width = 330,
                    Height = 145,
                    Text = "Calibrated mean-reversion speeds κ ranged from 3.42 for S&P 500 index options to 7.85 for Crude Oil (WTI) futures, reflecting rapid information assimilation in physical commodity derivative markets.",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                }
            }
        };

        // =========================================================================
        // PAGE 2: Order-Book Microstructure, Basel IV, Author Bios & References
        // =========================================================================
        var page2 = new PdfPageModel
        {
            PageNumber = 2,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "JOURNAL OF FINANCIAL ECONOMETRICS • VOL. 34, ISSUE 2",
            FooterRight = "Page 2 of 2",
            Elements = new List<PdfElementBase>
            {
                // Top Header Metadata Strip
                new PdfTextElement
                {
                    X = 55,
                    Y = 32,
                    Width = 690,
                    Height = 18,
                    Text = "VANCE & HOLMSTRÖM: MULTI-FACTOR JUMP-DIFFUSION IN FUTURES MARKETS",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    TextColorHex = "#166534",
                    Alignment = TextAlignmentMode.Center
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 52,
                    Width = 690,
                    Height = 1,
                    Thickness = 0.75,
                    ColorHex = "#86EFAC"
                },

                // ==========================================
                // COLUMN 1 (Left: X = 55, Width = 330)
                // ==========================================
                new PdfTextElement
                {
                    X = 55,
                    Y = 62,
                    Width = 330,
                    Height = 22,
                    Text = "7. ORDER-BOOK LEVEL II MICROSTRUCTURE",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#14532D"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 86,
                    Width = 330,
                    Height = 135,
                    Text = "By linking Poisson arrival intensities λ_t directly to limit order book imbalance (OFI_t = (V_t^bid - V_t^ask) / (V_t^bid + V_t^ask)), the model captures toxic flow arrivals preceding market-maker spread widening. Empirical tests indicate that order-flow toxicity accounts for 44.2% of jump arrival variance [5].",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 228,
                    Width = 330,
                    Height = 22,
                    Text = "8. BASEL IV & FRTB CAPITAL ADEQUACY",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#14532D"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 252,
                    Width = 330,
                    Height = 135,
                    Text = "Under the Fundamental Review of the Trading Book (FRTB), banks must compute Expected Shortfall (ES) under liquidity horizons ranging from 10 to 120 days. Incorporating heavy-tailed jump dynamics prevents systematic undercapitalization during market dislocation regimes [6].",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                // Author Biographies
                new PdfTextElement
                {
                    X = 55,
                    Y = 395,
                    Width = 330,
                    Height = 22,
                    Text = "AUTHOR BIOGRAPHIES",
                    FontSize = 10.5,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#14532D"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 415,
                    Width = 330,
                    Height = 1,
                    Thickness = 0.75,
                    ColorHex = "#86EFAC"
                },

                new PdfShapeElement
                {
                    X = 55,
                    Y = 425,
                    Width = 36,
                    Height = 36,
                    CornerRadius = 18,
                    FillColorHex = "#15803D",
                    Label = "JV",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 12
                },
                new PdfTextElement
                {
                    X = 100,
                    Y = 423,
                    Width = 285,
                    Height = 70,
                    Text = "John Doe, CFA, received his Ph.D. in Finance. He is a Managing Director in Quantitative Strategies at CodeFryDev Capital Management.",
                    FontSize = 8,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.3,
                    TextColorHex = "#334155"
                },

                new PdfShapeElement
                {
                    X = 55,
                    Y = 500,
                    Width = 36,
                    Height = 36,
                    CornerRadius = 18,
                    FillColorHex = "#166534",
                    Label = "JD",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 12
                },
                new PdfTextElement
                {
                    X = 100,
                    Y = 498,
                    Width = 285,
                    Height = 70,
                    Text = "Jane Doe is Associate Professor of Financial Econometrics at CodeFryDev Institute, specializing in continuous-time asset pricing and rough volatility surfaces.",
                    FontSize = 8,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.3,
                    TextColorHex = "#334155"
                },

                // ==========================================
                // COLUMN 2 (Right: X = 415, Width = 330)
                // ==========================================
                new PdfTextElement
                {
                    X = 415,
                    Y = 62,
                    Width = 330,
                    Height = 22,
                    Text = "REFERENCES",
                    FontSize = 10.5,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#14532D"
                },
                new PdfDividerElement
                {
                    X = 415,
                    Y = 85,
                    Width = 330,
                    Height = 1,
                    Thickness = 0.75,
                    ColorHex = "#86EFAC"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 90,
                    Width = 330,
                    Height = 270,
                    Text = "[1] R. C. Merton, \"Option pricing when underlying stock returns are discontinuous,\" J. Financ. Econ., vol. 3, pp. 125–144, 1976.\n[2] S. L. Heston, \"A closed-form solution for options with stochastic volatility,\" Rev. Financ. Stud., vol. 6, pp. 327–343, 1993.\n[3] D. S. Bates, \"Jumps and stochastic volatility: Exchange rate processes implicit in Deutsche Mark options,\" Rev. Financ. Stud., vol. 9, pp. 69–107, 1996.\n[4] R. Cont and P. Tankov, Financial Modelling with Jump Processes, Chapman & Hall/CRC, 2004.\n[5] J. Gatheral, The Volatility Surface: A Practitioner's Guide, John Wiley & Sons, 2006.\n[6] Basel Committee on Banking Supervision, Minimum Capital Requirements for Market Risk (FRTB), Bank for International Settlements, 2019.\n[7] C. Bayer, P. Friz, and J. Gatheral, \"Pricing under rough volatility,\" Quant. Finance, vol. 16, pp. 887–904, 2016.\n[8] D. Duffie, J. Pan, and K. Singleton, \"Transform analysis and asset pricing for affine jump-diffusions,\" Econometrica, vol. 68, pp. 1343–1376, 2000.",
                    FontSize = 8,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#475569",
                    Alignment = TextAlignmentMode.Justify
                }
            }
        };

        doc.Pages.Add(page1);
        doc.Pages.Add(page2);

        return doc;
    }
}
