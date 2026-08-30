using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Templates;

public class MathResearchPaperTemplate : ITemplateDefinition
{
    public string Id => "mathresearch";
    public string Name => "Mathematics Research Paper";
    public string Description => "Full 2-page pure & applied mathematics paper with discrete Hodge Laplacians, theorem proofs, spectral tables, and citations";
    public string Category => "Academic";
    public string IconKind => "MathCompass";
    public string AccentColorHex => "#4338CA";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Discrete_Hodge_Laplacians_Simplicial_Manifolds_2026.pdf",
            Author = "Prof. David H. Eisenbud & Dr. Claire Montgomery",
            Subject = "Discrete Differential Geometry & Topological Data Analysis"
        };

        // =========================================================================
        // PAGE 1: Title, Abstract, Discrete Hodge Formulation & Proofs
        // =========================================================================
        var page1 = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "ANNALS OF MATHEMATICS • VOL. 198, NO. 3 • MSC2020: 55N31, 58J50, 05E45",
            FooterCenter = "MATHEMATICAL RESEARCH ARTICLE",
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
                    Text = "ANNALS OF MATHEMATICS, VOL. 198 (2026), ISSUE 3, PP. 711–754",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    TextColorHex = "#64748B",
                    Alignment = TextAlignmentMode.Center
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 52,
                    Width = 690,
                    Height = 1,
                    Thickness = 0.75,
                    ColorHex = "#CBD5E1"
                },

                // Main Paper Title
                new PdfTextElement
                {
                    X = 55,
                    Y = 58,
                    Width = 690,
                    Height = 48,
                    Text = "Discrete Hodge Laplacians and Persistent Cohomology on High-Dimensional Simplicial Manifolds",
                    FontSize = 17,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#1E1B4B",
                    Alignment = TextAlignmentMode.Center
                },

                // Authors & Institutional Affiliation
                new PdfTextElement
                {
                    X = 55,
                    Y = 108,
                    Width = 690,
                    Height = 34,
                    Text = "David H. Eisenbud¹   and   Claire Montgomery²\n¹Department of Mathematics, UC Berkeley   •   ²Mathematical Institute, University of Oxford",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Center
                },

                // Abstract & MSC Classification Card
                new PdfShapeElement
                {
                    X = 75,
                    Y = 145,
                    Width = 650,
                    Height = 104,
                    CornerRadius = 4,
                    FillColorHex = "#F5F3FF",
                    StrokeColorHex = "#DDD6FE",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 90,
                    Y = 150,
                    Width = 620,
                    Height = 68,
                    Text = "Abstract— We establish spectral convergence bounds for higher-order discrete Hodge Laplacians Δ_k acting on real cochain complexes of triangulated Riemannian manifolds. We prove that the persistent harmonic cochains of a filtration asymptotically recover the continuous L² de Rham cohomology, and provide explicit lower bounds on the first non-zero eigenvalue λ₁(Δ_k) in terms of combinatorial Ricci curvature.",
                    FontSize = 9,
                    FontFamily = "Times New Roman",
                    IsItalic = true,
                    LineHeight = 1.35,
                    TextColorHex = "#1E1B4B",
                    Alignment = TextAlignmentMode.Justify
                },
                new PdfTextElement
                {
                    X = 90,
                    Y = 222,
                    Width = 620,
                    Height = 22,
                    Text = "MSC 2020 Classification— 55N31 (Persistent homology), 58J50 (Spectral theory on manifolds), 05E45 (Simplicial complexes).",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#4338CA"
                },

                // Column Split Divider
                new PdfDividerElement
                {
                    X = 55,
                    Y = 254,
                    Width = 690,
                    Height = 1,
                    Thickness = 1,
                    ColorHex = "#CBD5E1"
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
                    Text = "1. INTRODUCTION & PRELIMINARIES",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#1E1B4B"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 286,
                    Width = 330,
                    Height = 110,
                    Text = "Let K be a finite, oriented simplicial complex of dimension n. For each integer k ∈ {0, …, n}, let C_k(K; ℝ) denote the vector space of real k-chains, endowed with the canonical inner product making the oriented simplices an orthonormal basis. The combinatorial coboundary operator δ_k: C^k(K; ℝ) → C^{k+1}(K; ℝ) is the adjoint of the boundary operator ∂_{k+1}.",
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
                    Text = "2. DISCRETE HODGE LAPLACIAN",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#1E1B4B"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 426,
                    Width = 330,
                    Height = 45,
                    Text = "Definition 2.1. The k-th discrete Hodge Laplacian Δ_k: C^k(K; ℝ) → C^k(K; ℝ) is the self-adjoint, positive semi-definite operator defined by:",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                // Formal Equation Box
                new PdfShapeElement
                {
                    X = 55,
                    Y = 475,
                    Width = 330,
                    Height = 36,
                    CornerRadius = 3,
                    FillColorHex = "#F5F3FF",
                    StrokeColorHex = "#DDD6FE",
                    StrokeThickness = 0.5
                },
                new PdfTextElement
                {
                    X = 65,
                    Y = 481,
                    Width = 310,
                    Height = 24,
                    Text = "Δ_k = δ_{k-1} ∂_k + ∂_{k+1} δ_k = Δ_k^{down} + Δ_k^{up}      (2.1)",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#1E1B4B",
                    Alignment = TextAlignmentMode.Center
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 516,
                    Width = 330,
                    Height = 22,
                    Text = "Theorem 2.2 (Discrete Hodge Decomposition).",
                    FontSize = 10,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#1E1B4B"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 540,
                    Width = 330,
                    Height = 145,
                    Text = "For any finite simplicial complex K, there exists an orthogonal direct sum decomposition:\n\nC^k(K; ℝ) = im(δ_{k-1}) ⊕ im(∂_{k+1}) ⊕ ker(Δ_k).\n\nFurthermore, there is a canonical isomorphism between the space of harmonic k-cochains and the k-th simplicial cohomology group: ker(Δ_k) ≅ H^k(K; ℝ), yielding dim ker(Δ_k) = β_k(K), where β_k is the k-th Betti number.",
                    FontSize = 9,
                    FontFamily = "Times New Roman",
                    IsItalic = true,
                    LineHeight = 1.35,
                    TextColorHex = "#1F2937",
                    Alignment = TextAlignmentMode.Justify
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 690,
                    Width = 330,
                    Height = 22,
                    Text = "3. SPECTRAL GAP & CURVATURE BOUNDS",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#1E1B4B"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 714,
                    Width = 330,
                    Height = 140,
                    Text = "Lemma 3.1 (Bochner-Weitzenböck Formula). Let Ric_k(K) denote the Forman combinatorial Ricci curvature on k-simplices [4]. If Ric_k(K) ≥ κ > 0 for all σ ∈ K_k, then the first non-zero eigenvalue satisfies λ₁(Δ_k) ≥ κ. Consequently, whenever κ > 0, the k-th reduced cohomology group vanishes: H̃^k(K; ℝ) = 0.",
                    FontSize = 9,
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
                    Text = "4. NUMERICAL EIGENVALUE COMPUTATION",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#1E1B4B"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 286,
                    Width = 330,
                    Height = 58,
                    Text = "We evaluated the spectral spectrum of Δ₁ on fine triangulations of standard smooth surfaces (2-Sphere S², Torus T², Klein Bottle K², and 3-Torus T³). Sparse Lanczos iterations were computed via ARPACK.",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                // Analytical Spectral Table
                new PdfTableElement
                {
                    X = 415,
                    Y = 348,
                    Width = 330,
                    Height = 145,
                    Headers = new List<string> { "Manifold (M)", "Dim", "β₁(M)", "λ₁(Δ₁)", "ρ(Δ₁)" },
                    Rows = new List<List<string>>
                    {
                        new() { "Sphere S²", "2", "0", "1.9842", "12.00" },
                        new() { "Torus T²", "2", "2", "0.4931", "11.85" },
                        new() { "Klein Bottle K²", "2", "1", "0.4810", "11.90" },
                        new() { "3-Torus T³", "3", "3", "0.2465", "18.00" }
                    },
                    HeaderBackgroundHex = "#4338CA",
                    HeaderTextHex = "#FFFFFF",
                    AlternateRowBackgroundHex = "#F5F3FF",
                    BorderColorHex = "#CBD5E1"
                },

                new PdfTextElement
                {
                    X = 415,
                    Y = 502,
                    Width = 330,
                    Height = 22,
                    Text = "5. PERSISTENT COHOMOLOGY ALGORITHM",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#1E1B4B"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 526,
                    Width = 330,
                    Height = 115,
                    Text = "Given a filtered simplicial complex ∅ = K₀ ⊂ K₁ ⊂ ⋯ ⊂ K_m = K, the persistence diagram Dgm_k is computed via matrix reduction on the boundary matrix. The harmonic representatives ω_i ∈ ker(Δ_k(K_t)) provide geometric localization for persistent topological features across filtration scales [3].",
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
                    Text = "6. CONTINUOUS ASYMPTOTIC CONVERGENCE",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#1E1B4B"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 672,
                    Width = 330,
                    Height = 145,
                    Text = "Theorem 6.1 (Spectral Convergence). Let (M, g) be a smooth compact Riemannian manifold and let {K_h}_{h>0} be a family of Delaunay triangulations with mesh size h → 0 satisfying the minimal angle condition. Then for each k, the spectrum of Δ_k(K_h) converges in the Hausdorff metric to the spectrum of the continuous Hodge-de Rham Laplacian Δ_k^M on differential k-forms.",
                    FontSize = 9,
                    FontFamily = "Times New Roman",
                    IsItalic = true,
                    LineHeight = 1.35,
                    TextColorHex = "#1F2937",
                    Alignment = TextAlignmentMode.Justify
                }
            }
        };

        // =========================================================================
        // PAGE 2: Heat Trace, Zeta Functions, Author Bios & References
        // =========================================================================
        var page2 = new PdfPageModel
        {
            PageNumber = 2,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "ANNALS OF MATHEMATICS • VOL. 198, NO. 3",
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
                    Text = "EISENBUD & MONTGOMERY: DISCRETE HODGE LAPLACIANS ON MANIFOLDS",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    TextColorHex = "#64748B",
                    Alignment = TextAlignmentMode.Center
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 52,
                    Width = 690,
                    Height = 1,
                    Thickness = 0.75,
                    ColorHex = "#CBD5E1"
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
                    Text = "7. DISCRETE HEAT KERNEL & SPECTRAL ZETA",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#1E1B4B"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 86,
                    Width = 330,
                    Height = 70,
                    Text = "The discrete heat operator H_k(t) = exp(-t Δ_k) admits a spectral expansion governed by the eigenvalues 0 ≤ λ₁ ≤ λ₂ ≤ ⋯ ≤ λ_N. The combinatorial spectral zeta function is defined for Re(s) > 0 by:",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                // Formal Equation Box
                new PdfShapeElement
                {
                    X = 55,
                    Y = 160,
                    Width = 330,
                    Height = 36,
                    CornerRadius = 3,
                    FillColorHex = "#F5F3FF",
                    StrokeColorHex = "#DDD6FE",
                    StrokeThickness = 0.5
                },
                new PdfTextElement
                {
                    X = 65,
                    Y = 166,
                    Width = 310,
                    Height = 24,
                    Text = "ζ_k(s) = Tr( (Δ_k|_{ker(Δ_k)^⊥})^{-s} ) = ∑_{j=1}^{N - β_k} λ_j^{-s}      (7.1)",
                    FontSize = 9,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#1E1B4B",
                    Alignment = TextAlignmentMode.Center
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 202,
                    Width = 330,
                    Height = 120,
                    Text = "The derivative at zero, ζ'_k(0), yields the discrete Ray-Singer analytic torsion, which we prove is a topological invariant of the complex K under simplicial subdivision and Reidemeister moves [5]. This extends Cheeger's theorem to combinatorial cell complexes.",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 330,
                    Width = 330,
                    Height = 22,
                    Text = "8. CONCLUDING THEORETICAL REMARKS",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#1E1B4B"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 354,
                    Width = 330,
                    Height = 125,
                    Text = "Our results establish that higher-order discrete Hodge Laplacians faithfully reflect the underlying geometry and topology of continuous Riemannian manifolds. Future extensions will investigate discrete spin Dirac operators D on simplicial spin manifolds and non-linear p-Laplacian eigenvalues.",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                // Author Biographies (Left Column)
                new PdfTextElement
                {
                    X = 55,
                    Y = 490,
                    Width = 330,
                    Height = 22,
                    Text = "AUTHOR BIOGRAPHIES",
                    FontSize = 10.5,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#1E1B4B"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 510,
                    Width = 330,
                    Height = 1,
                    Thickness = 0.75,
                    ColorHex = "#CBD5E1"
                },

                // Bio 1: David Eisenbud
                new PdfShapeElement
                {
                    X = 55,
                    Y = 520,
                    Width = 34,
                    Height = 34,
                    CornerRadius = 17,
                    FillColorHex = "#4338CA",
                    Label = "DE",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 12
                },
                new PdfTextElement
                {
                    X = 98,
                    Y = 518,
                    Width = 287,
                    Height = 65,
                    Text = "David H. Eisenbud is Professor of Mathematics at UC Berkeley and former Director of MSRI. His work spans commutative algebra, algebraic geometry, and discrete Hodge theory.",
                    FontSize = 8,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.3,
                    TextColorHex = "#334155"
                },

                // Bio 2: Claire Montgomery
                new PdfShapeElement
                {
                    X = 55,
                    Y = 590,
                    Width = 34,
                    Height = 34,
                    CornerRadius = 17,
                    FillColorHex = "#6366F1",
                    Label = "CM",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 12
                },
                new PdfTextElement
                {
                    X = 98,
                    Y = 588,
                    Width = 287,
                    Height = 65,
                    Text = "Claire Montgomery is a Royal Society University Research Fellow at Oxford University. Her research focuses on persistent spectral geometry and high-dimensional topological data analysis.",
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
                    TextColorHex = "#1E1B4B"
                },
                new PdfDividerElement
                {
                    X = 415,
                    Y = 85,
                    Width = 330,
                    Height = 1,
                    Thickness = 0.75,
                    ColorHex = "#CBD5E1"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 90,
                    Width = 330,
                    Height = 270,
                    Text = "[1] W. V. D. Hodge, The Theory and Applications of Harmonic Integrals, Cambridge University Press, 1941.\n[2] H. Edelsbrunner and J. L. Harer, Computational Topology: An Introduction, American Mathematical Society, 2010.\n[3] G. Carlsson, \"Topology and data,\" Bull. Amer. Math. Soc., vol. 46, no. 2, pp. 255–308, 2009.\n[4] R. Forman, \"Bochner's method for cell complexes and combinatorial Ricci curvature,\" Discrete Comput. Geom., vol. 29, pp. 323–374, 2003.\n[5] M. F. Atiyah and I. M. Singer, \"The index of elliptic operators: I,\" Ann. of Math., vol. 87, pp. 484–530, 1968.\n[6] J. Cheeger, \"Analytic torsion and the heat equation,\" Ann. of Math., vol. 109, pp. 259–322, 1979.\n[7] D. B. Ray and I. M. Singer, \"R-torsion and the Laplacian on Riemannian manifolds,\" Adv. Math., vol. 7, pp. 145–210, 1971.\n[8] J. Dodziuk, \"Finite-difference approach to the Hodge theory of harmonic forms,\" Amer. J. Math., vol. 98, pp. 79–104, 1976.",
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
