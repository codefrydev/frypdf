using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Templates;

public class ResumeAcademicCvTemplate : ITemplateDefinition
{
    public string Id => "resumeacademic";
    public string Name => "Academic & Scientific Curriculum Vitae";
    public string Description => "Comprehensive 2-page academic CV with grants table, peer-reviewed publications with DOIs, student advising, and editorial service";
    public string Category => "Career";
    public string IconKind => "SchoolOutline";
    public string AccentColorHex => "#78350F";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Prof_John_Doe_Academic_CV.pdf",
            Author = "Prof. John Doe, Ph.D.",
            Subject = "Curriculum Vitae • Computational Neuroscience & Applied Mathematics"
        };

        // =========================================================================
        // PAGE 1: Header, Appointments, Education, Grants Table & Major Publications
        // =========================================================================
        var page1 = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "Prof. John Doe, Ph.D. • Curriculum Vitae | ORCID: 0000-0002-8910-4492",
            FooterCenter = "FACULTY DOSSIER",
            FooterRight = "Page 1 of 2",
            Elements = new List<PdfElementBase>
            {
                // Top Header Rule
                new PdfDividerElement
                {
                    X = 55,
                    Y = 35,
                    Width = 690,
                    Height = 2,
                    Thickness = 2,
                    ColorHex = "#78350F"
                },

                // Scholar Name
                new PdfTextElement
                {
                    X = 55,
                    Y = 45,
                    Width = 520,
                    Height = 34,
                    Text = "JOHN DOE, Ph.D.",
                    FontSize = 24,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#1E293B"
                },

                // Academic Title & Institutional Affiliation
                new PdfTextElement
                {
                    X = 55,
                    Y = 78,
                    Width = 520,
                    Height = 44,
                    Text = "Associate Professor of Applied Mathematics & Computational Neuroscience\nInstitute for Advanced Theoretical Studies • CodeFryDev Institute of Technology\nEmail: j.doe@codefrydev.in • Web: codefrydev.in/people/john-doe",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#475569"
                },

                // Top Right ORCID / Google Scholar QR Code
                new PdfQrCodeElement
                {
                    X = 645,
                    Y = 42,
                    Width = 100,
                    Height = 85,
                    Content = "https://orcid.org/0000-0002-8910-4492",
                    Label = "ORCID RECORD",
                    DarkColorHex = "#78350F",
                    LightColorHex = "#FFFFFF"
                },

                // Header Divider
                new PdfDividerElement
                {
                    X = 55,
                    Y = 126,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#CBD5E1"
                },

                // Section: Research Interests
                new PdfTextElement
                {
                    X = 55,
                    Y = 135,
                    Width = 690,
                    Height = 20,
                    Text = "PRIMARY RESEARCH FIELDS",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#78350F"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 155,
                    Width = 690,
                    Height = 34,
                    Text = "Topological Data Analysis, Neural Differential Equations, High-Dimensional Stochastic Dynamical Systems, Geometric Deep Learning, and Spectral Graph Theory on Cortical Manifolds.",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    IsItalic = true,
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },

                // Section: Academic Appointments
                new PdfTextElement
                {
                    X = 55,
                    Y = 190,
                    Width = 690,
                    Height = 20,
                    Text = "ACADEMIC APPOINTMENTS & POSITIONS",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#78350F"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 208,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#E2E8F0"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 214,
                    Width = 690,
                    Height = 68,
                    Text = "• Associate Professor (Tenured) — Mathematical Institute, Oxford University (2022 – Present)\n• Assistant Professor of Applied Mathematics — Stanford University (2018 – 2022)\n• Postdoctoral Research Fellow — Center for Brain Science, Harvard University (2015 – 2018)\n• Visiting Scholar — Max Planck Institute for Mathematics in the Sciences, Leipzig (2017)",
                    FontSize = 9,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.4,
                    TextColorHex = "#334155"
                },

                // Section: Education
                new PdfTextElement
                {
                    X = 55,
                    Y = 285,
                    Width = 690,
                    Height = 20,
                    Text = "EDUCATION & DEGREES CONFERRED",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#78350F"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 303,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#E2E8F0"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 309,
                    Width = 690,
                    Height = 58,
                    Text = "• Ph.D. in Applied Mathematics — Harvard University (2011 – 2015)\n   Dissertation: Spectral Geometry and Persistent Homology on Cortical Surfaces. Advisor: Prof. Shing-Tung Yau.\n• M.Sc. in Theoretical Physics — Cambridge University (Part III of Mathematical Tripos, Distinction, 2010 – 2011)\n• B.Sc. in Mathematics & Physics — MIT (Summa Cum Laude, GPA 4.00/4.00, 2006 – 2010)",
                    FontSize = 9,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },

                // Section: Major Research Grants & Funding
                new PdfTextElement
                {
                    X = 55,
                    Y = 370,
                    Width = 690,
                    Height = 20,
                    Text = "MAJOR FUNDED RESEARCH GRANTS ($4.2M TOTAL AWARDS)",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#78350F"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 388,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#E2E8F0"
                },

                // Table of Grants
                new PdfTableElement
                {
                    X = 55,
                    Y = 394,
                    Width = 690,
                    Height = 115,
                    Headers = new List<string> { "Grant Agency & Title", "Grant Number", "Role", "Funding Period", "Total Amount" },
                    Rows = new List<List<string>>
                    {
                        new() { "ERC Consolidator Grant (Topological Brain Dynamics)", "ERC-2023-COG-948", "Principal Investigator", "2024 – 2029", "€2,000,000" },
                        new() { "NSF DMS CAREER: Geometric Deep Learning on Manifolds", "DMS-2048910", "Sole PI", "2020 – 2025", "$520,000" },
                        new() { "NIH BRAIN Initiative: High-Dimensional Cortical Tracking", "U01-MH12498", "Co-PI (with Stanford Med)", "2019 – 2023", "$1,450,000" }
                    },
                    HeaderBackgroundHex = "#78350F",
                    HeaderTextHex = "#FFFFFF",
                    AlternateRowBackgroundHex = "#FFFDF9",
                    BorderColorHex = "#E2E8F0"
                },

                // Section: Selected Peer-Reviewed Publications (Part 1)
                new PdfTextElement
                {
                    X = 55,
                    Y = 520,
                    Width = 690,
                    Height = 20,
                    Text = "SELECTED PEER-REVIEWED PUBLICATIONS (OVER 45 PEER-REVIEWED PAPERS, h-index: 28)",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#78350F"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 538,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#E2E8F0"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 544,
                    Width = 690,
                    Height = 185,
                    Text = "1. Doe, J., & Chen, E. (2025). \"Persistent Spectral Cohomology on High-Dimensional Simplicial Manifolds.\" Journal of the American Mathematical Society, 38(2), pp. 412–458. DOI: 10.1090/jams/9842.\n2. Doe, J., Thorne, M., & Jenkins, S. (2024). \"Neural Differential Equations with Guaranteed Stability via Hodge Decomposition.\" Nature Machine Intelligence, 6(8), pp. 892–906. DOI: 10.1038/s42256-024-00892.\n3. Doe, J. (2023). \"Spectral Gap Concentration on Random Simplicial Complexes.\" Communications on Pure and Applied Mathematics, 76(4), pp. 789–834. DOI: 10.1002/cpa.22019.\n4. Vance, E., & Doe, J. (2022). \"Topological Decoding of Multi-Electrode Array Cortical Spikes.\" Proceedings of the National Academy of Sciences (PNAS), 119(14), e2119842119. DOI: 10.1073/pnas.2119842119.\n5. Doe, J., et al. (2020). \"Continuous Invertible Neural Flows on Riemannian Manifolds.\" IEEE Transactions on Pattern Analysis and Machine Intelligence (TPAMI), 42(11), pp. 2780–2794. DOI: 10.1109/TPAMI.2020.29841.",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                }
            }
        };

        // =========================================================================
        // PAGE 2: Additional Publications, Keynotes, Teaching, Advising & Service
        // =========================================================================
        var page2 = new PdfPageModel
        {
            PageNumber = 2,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "Prof. John Doe, Ph.D. • Curriculum Vitae (Page 2)",
            FooterCenter = "FACULTY DOSSIER",
            FooterRight = "Page 2 of 2",
            Elements = new List<PdfElementBase>
            {
                // Top Header Rule
                new PdfDividerElement
                {
                    X = 55,
                    Y = 35,
                    Width = 690,
                    Height = 2,
                    Thickness = 2,
                    ColorHex = "#78350F"
                },

                // Section: Additional Publications (Part 2)
                new PdfTextElement
                {
                    X = 55,
                    Y = 48,
                    Width = 690,
                    Height = 20,
                    Text = "ADDITIONAL PEER-REVIEWED ARTICLES & MONOGRAPHS",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#78350F"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 70,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#E2E8F0"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 76,
                    Width = 690,
                    Height = 150,
                    Text = "6. Doe, J., & Sterling, C. (2020). \"Discrete Bochner Laplacians and Curvature Bounds on Cell Complexes.\" SIAM Journal on Applied Mathematics, 80(3), pp. 1120–1145. DOI: 10.1137/19M1284102.\n7. Doe, J., & Tanaka, H. (2019). \"Diffusion Geometry on Non-Compact Symmetric Spaces.\" Journal of Machine Learning Research (JMLR), 20(84), pp. 1–38.\n8. Doe, J. (2018). \"Spectral Geometry of Cortical Folding Patterns.\" Cambridge University Press Research Monograph Series in Mathematical Biology, Vol. 14, 280 pages. ISBN 978-1-108-49210-4.\n9. Thorne, J., & Doe, J. (2017). \"Persistent Homology of Neural Population Codes.\" Physical Review E, 96(4), 042412. DOI: 10.1103/PhysRevE.96.042412.",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },

                // Section: Invited Keynotes & Plenaries
                new PdfTextElement
                {
                    X = 55,
                    Y = 232,
                    Width = 690,
                    Height = 20,
                    Text = "INVITED KEYNOTES & PLENARY ADDRESSES",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#78350F"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 254,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#E2E8F0"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 260,
                    Width = 690,
                    Height = 65,
                    Text = "• Plenary Speaker, International Congress of Mathematicians (ICM 2026, Section on Applied Topology, Helsinki)\n• Invited Keynote, NeurIPS 2024 Workshop on Differential Geometry in Deep Learning (Vancouver, BC)\n• Euler Lecturer, Zurich Mathematics Colloquium, ETH Zurich (2023)\n• Courant Institute Annual Distinguished Lecture in Applied Mathematics, NYU (2022)\n• Invited Plenary, SIAM Conference on Mathematics of Data Science (MDS 2020)",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },

                // Section: Ph.D. Advising & Postdoctoral Mentorship
                new PdfTextElement
                {
                    X = 55,
                    Y = 330,
                    Width = 330,
                    Height = 20,
                    Text = "PH.D. STUDENTS & POSTDOCS SUPERVISED",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#78350F"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 352,
                    Width = 330,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#E2E8F0"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 358,
                    Width = 330,
                    Height = 90,
                    Text = "• Dr. Clara Sterling (Ph.D. 2023, now Assistant Professor at Cambridge)\n• Dr. Julian Thorne (Ph.D. 2024, Postdoc at MIT Mathematics)\n• Dr. Hiroshi Tanaka (Postdoc 2021–2024, now Faculty at Tokyo Univ)\n• Currently advising 4 Ph.D. students and 2 postdoctoral fellows.",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },

                // Section: Editorial Boards & Review Service
                new PdfTextElement
                {
                    X = 415,
                    Y = 330,
                    Width = 330,
                    Height = 20,
                    Text = "EDITORIAL BOARDS & SERVICE",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#78350F"
                },
                new PdfDividerElement
                {
                    X = 415,
                    Y = 352,
                    Width = 330,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#E2E8F0"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 358,
                    Width = 330,
                    Height = 90,
                    Text = "• Associate Editor, SIAM Journal on Applied Mathematics (2021–Present)\n• Action Editor, Journal of Machine Learning Research (JMLR)\n• Program Chair, Computational Topology & Geometry 2025\n• NSF & ERC Panel Reviewer (Mathematics & Computing)",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                },

                // Section: University Teaching & Courses Taught
                new PdfTextElement
                {
                    X = 55,
                    Y = 455,
                    Width = 690,
                    Height = 20,
                    Text = "UNIVERSITY TEACHING & CURRICULUM DEVELOPMENT",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#78350F"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 477,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#E2E8F0"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 483,
                    Width = 690,
                    Height = 65,
                    Text = "• MATH 842: High-Dimensional Stochastic Differential Equations (Oxford, Graduate Level, 2022–2026)\n• MATH 510: Computational Topology and Persistent Homology (Stanford / Oxford, 2019–2025)\n• Recipient of Oxford University Outstanding Graduate Teaching Award (2024)",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155"
                }
            }
        };

        doc.Pages.Add(page1);
        doc.Pages.Add(page2);

        return doc;
    }
}
