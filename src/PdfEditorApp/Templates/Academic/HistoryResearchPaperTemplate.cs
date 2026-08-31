using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Templates;

public class HistoryResearchPaperTemplate : ITemplateDefinition
{
    public string Id => "historyresearch";
    public string Name => "Historical Research & Archival Monograph";
    public string Description => "Full 2-page historical research monograph with primary ledger records, bullion flow tables, notary archives, and manuscript citations";
    public string Category => "Academic";
    public string IconKind => "Bookshelf";
    public string AccentColorHex => "#92400E";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Mediterranean_Maritime_Trade_1250_1450_Research.pdf",
            Author = "Prof. Jane Doe",
            Subject = "Economic History & Medieval Mediterranean Archival Trade Networks"
        };

        // =========================================================================
        // PAGE 1: Title, Abstract, Archival Context, Bullion & Port Ledger Table
        // =========================================================================
        var page1 = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFDF9",
            FooterLeft = "MEDIEVAL MEDITERRANEAN MONOGRAPHS • VOL. 48 • ISSN 0928-5520",
            FooterCenter = "HISTORICAL RESEARCH MONOGRAPH",
            FooterRight = "Page 1 of 2",
            Elements = new List<PdfElementBase>
            {
                // Top Header Rule & Monograph Series Metadata
                new PdfTextElement
                {
                    X = 55,
                    Y = 32,
                    Width = 690,
                    Height = 18,
                    Text = "JOURNAL OF MEDIEVAL ECONOMIC HISTORY, VOL. 48, PP. 112–148 • DOI: 10.1163/15700674-12345",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    TextColorHex = "#78350F",
                    Alignment = TextAlignmentMode.Center
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 52,
                    Width = 690,
                    Height = 1,
                    Thickness = 0.75,
                    ColorHex = "#D97706"
                },

                // Main Paper Title
                new PdfTextElement
                {
                    X = 55,
                    Y = 58,
                    Width = 690,
                    Height = 48,
                    Text = "Maritime Trade Networks, Bullion Flows, and Commercial Treaties in the Mediterranean Basin (1250–1450 CE)",
                    FontSize = 16,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#451A03",
                    Alignment = TextAlignmentMode.Center
                },

                // Author & Institutional Affiliation
                new PdfTextElement
                {
                    X = 55,
                    Y = 108,
                    Width = 690,
                    Height = 34,
                    Text = "Jane Doe, Ph.D.\nChair of Medieval Economic History, CodeFryDev Institute of Historical Studies",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    TextColorHex = "#522504",
                    Alignment = TextAlignmentMode.Center
                },

                // Abstract & Archival Keywords Card
                new PdfShapeElement
                {
                    X = 75,
                    Y = 145,
                    Width = 650,
                    Height = 104,
                    CornerRadius = 4,
                    FillColorHex = "#FEF3C7",
                    StrokeColorHex = "#FDE68A",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 90,
                    Y = 150,
                    Width = 620,
                    Height = 68,
                    Text = "Abstract— Drawing upon newly transcribed notary cartularies from the Archivio di Stato di Venezia and the Datini Archives in Prato, this study examines the velocity of gold specie (ducats and florins) in financing Levantine spice and silk commerce between 1250 and 1450 CE. We demonstrate that merchant syndicates mitigated currency depreciation through sophisticated maritime loans and reciprocal credit instruments.",
                    FontSize = 9,
                    FontFamily = "Times New Roman",
                    IsItalic = true,
                    LineHeight = 1.35,
                    TextColorHex = "#78350F",
                    Alignment = TextAlignmentMode.Justify
                },
                new PdfTextElement
                {
                    X = 90,
                    Y = 222,
                    Width = 620,
                    Height = 22,
                    Text = "Keywords— Mediterranean trade, Venetian notary registers, bullion flows, bills of exchange, Pax Mongolica.",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#92400E"
                },

                // Column Split Divider
                new PdfDividerElement
                {
                    X = 55,
                    Y = 254,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#D97706"
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
                    Text = "I. HISTORIOGRAPHICAL CONTEXT & ARCHIVES",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#451A03"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 286,
                    Width = 330,
                    Height = 110,
                    Text = "The Commercial Revolution of the thirteenth century reshaped the financial structures of Western Christendom [1]. Venetian and Genoese maritime hegemony relied not merely on naval superiority, but on legal innovations such as the colleganza and the development of double-entry bookkeeping recorded in notarized registers (cartulari notarili) across Mediterranean emporia [2].",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 402,
                    Width = 330,
                    Height = 22,
                    Text = "II. BULLION FLOWS & CURRENCY SPECIE",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#451A03"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 426,
                    Width = 330,
                    Height = 120,
                    Text = "The introduction of the gold ducat (ducato d'oro) in 1284 established an international standard of exchange. Our quantitative examination of 2,400 notarized contracts in Famagusta and Alexandria reveals that an estimated 350,000 to 500,000 gold ducats flowed annually through Levantine ports to purchase Indonesian spices and Persian silks [3].",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                // Archival Quotation Callout
                new PdfShapeElement
                {
                    X = 55,
                    Y = 552,
                    Width = 330,
                    Height = 70,
                    CornerRadius = 3,
                    FillColorHex = "#FEF3C7",
                    StrokeColorHex = "#FDE68A",
                    StrokeThickness = 0.5
                },
                new PdfTextElement
                {
                    X = 68,
                    Y = 558,
                    Width = 304,
                    Height = 58,
                    Text = "\"In nomine Domini, anno 1342... Ego Nicoleto de Contareno fateor recepisse a te... ducatos auri ducentos quinquaginta in colleganza pro navigando versus Alexandriam.\"\n— ASVe, Notarile, Busta 142, fol. 18r.",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    IsItalic = true,
                    LineHeight = 1.3,
                    TextColorHex = "#78350F"
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 632,
                    Width = 330,
                    Height = 22,
                    Text = "III. COMMERCIAL TREATIES & GUILD PRIVILEGES",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#451A03"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 656,
                    Width = 330,
                    Height = 160,
                    Text = "Bilateral treaties signed between the Republic of Venice and Mamluk Sultans in Cairo (notably the 1345 and 1375 chrysobulls) fixed customs tariffs at 5% ad valorem for pepper and ginger, while granting extraterritorial legal jurisdiction to the Venetian consul (bailo) stationed in Alexandria and Aleppo [4].",
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
                    Text = "IV. QUANTITATIVE TRADE DATA ACROSS PORTS",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#451A03"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 286,
                    Width = 330,
                    Height = 58,
                    Text = "Table I details the estimated annual gold specie inflows, primary exported commodities, and average customs duty rates across major Mediterranean trade depots based on consular customs ledgers (1300–1400 CE).",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                // Historical Archival Table
                new PdfTableElement
                {
                    X = 415,
                    Y = 348,
                    Width = 330,
                    Height = 145,
                    Headers = new List<string> { "Trading Port", "Commodity", "Gold Flow (Ducats)", "Tariff" },
                    Rows = new List<List<string>>
                    {
                        new() { "Alexandria (Egypt)", "Spices / Pepper", "380,000", "5.0%" },
                        new() { "Constantinople", "Silk / Grain", "240,000", "3.5%" },
                        new() { "Famagusta (Cyprus)", "Sugar / Cotton", "160,000", "4.0%" },
                        new() { "Ragusa (Dubrovnik)", "Silver / Timber", "120,000", "2.0%" }
                    },
                    HeaderBackgroundHex = "#92400E",
                    HeaderTextHex = "#FFFFFF",
                    AlternateRowBackgroundHex = "#FEF3C7",
                    BorderColorHex = "#CBD5E1"
                },

                new PdfTextElement
                {
                    X = 415,
                    Y = 502,
                    Width = 330,
                    Height = 22,
                    Text = "V. MARITIME LAW & BILLS OF EXCHANGE",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#451A03"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 526,
                    Width = 330,
                    Height = 115,
                    Text = "The emergence of the lettera di cambio (bill of exchange) permitted merchant bankers in Florence, Venice, and Genoa to settle accounts without physically transporting silver and gold across pirate-infested shipping lanes, effectively multiplying liquidity across Europe [5].",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                new PdfTextElement
                {
                    X = 415,
                    Y = 648,
                    Width = 330,
                    Height = 22,
                    Text = "VI. CONVOY SHIPPING & THE MUDA SYSTEM",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#451A03"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 672,
                    Width = 330,
                    Height = 145,
                    Text = "The Venetian State organized biannual state-sponsored galley convoys (mude) departing for Flanders, Beirut, and Alexandria. Auctioning cargo space on armed great galleys minimized piracy risks and created standardized freight insurance rates of 2.5%–4.0% [6].",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                }
            }
        };

        // =========================================================================
        // PAGE 2: Silk Road Links, Datini Archives, Author Bio & References
        // =========================================================================
        var page2 = new PdfPageModel
        {
            PageNumber = 2,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFDF9",
            FooterLeft = "MEDIEVAL MEDITERRANEAN MONOGRAPHS • VOL. 48",
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
                    Text = "DE MONTMIRAIL: MEDITERRANEAN MARITIME TRADE (1250–1450 CE)",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    TextColorHex = "#78350F",
                    Alignment = TextAlignmentMode.Center
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 52,
                    Width = 690,
                    Height = 1,
                    Thickness = 0.75,
                    ColorHex = "#D97706"
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
                    Text = "VII. OVERLAND SILK ROAD LINKAGES",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#451A03"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 86,
                    Width = 330,
                    Height = 135,
                    Text = "During the Pax Mongolica, maritime routes converged with overland caravan routes terminating at Trebizond, Tabriz, and Ayas. Venetian and Genoese factors established permanent trading houses (fondachi) in the Black Sea port of Caffa, exporting Crimean wheat and raw silk westward in exchange for Flemish woolen textiles and German silver ingots [7].",
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
                    Text = "VIII. CONCLUSION & HISTORIOGRAPHY",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#451A03"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 252,
                    Width = 330,
                    Height = 135,
                    Text = "The pre-modern Mediterranean was not an arena of irreconcilable religious clash, but a deeply integrated commercial network held together by institutional legal norms, shared currency weights, and mutual commercial interest. These notary registers prove that capital velocity was far higher than traditional historiography assumed.",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                // Author Biography Header
                new PdfTextElement
                {
                    X = 55,
                    Y = 395,
                    Width = 330,
                    Height = 22,
                    Text = "AUTHOR BIOGRAPHY",
                    FontSize = 10.5,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#451A03"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 415,
                    Width = 330,
                    Height = 1,
                    Thickness = 0.75,
                    ColorHex = "#D97706"
                },

                new PdfShapeElement
                {
                    X = 55,
                    Y = 425,
                    Width = 36,
                    Height = 36,
                    CornerRadius = 18,
                    FillColorHex = "#92400E",
                    Label = "HM",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 12
                },
                new PdfTextElement
                {
                    X = 100,
                    Y = 423,
                    Width = 285,
                    Height = 90,
                    Text = "Henriette de Montmirail is Professor of Medieval Economic History at EHESS Paris and Fellow of the Royal Historical Society. She has authored four books on Mediterranean maritime law and medieval banking cartularies.",
                    FontSize = 8.5,
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
                    Text = "PRIMARY SOURCES & BIBLIOGRAPHY",
                    FontSize = 10.5,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#451A03"
                },
                new PdfDividerElement
                {
                    X = 415,
                    Y = 85,
                    Width = 330,
                    Height = 1,
                    Thickness = 0.75,
                    ColorHex = "#D97706"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 90,
                    Width = 330,
                    Height = 270,
                    Text = "[1] F. Braudel, The Mediterranean and the Mediterranean World in the Age of Philip II, Harper & Row, 1972.\n[2] R. S. Lopez, The Commercial Revolution of the Middle Ages, 950–1350, Cambridge Univ. Press, 1976.\n[3] E. Ashtor, Levant Trade in the Later Middle Ages, Princeton Univ. Press, 1983.\n[4] F. C. Lane, Venice: A Maritime Republic, Johns Hopkins Univ. Press, 1973.\n[5] R. de Roover, The Rise and Decline of the Medici Bank, 1397–1494, Harvard Univ. Press, 1963.\n[6] D. Jacoby, Commercial Exchange Across the Mediterranean, Variorum, 2005.\n[7] M. Balard, La Romanie Génoise (XIIe–début du XVe siècle), École Française de Rome, 1978.\n[8] Archivio di Stato di Venezia (ASVe), Procuratori di San Marco, de Citra, Buste 140–162.",
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
