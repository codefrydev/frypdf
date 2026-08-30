using System.Collections.Generic;
using PdfEditorApp.Models;
using PdfEditorApp.Models.Elements;

namespace PdfEditorApp.Templates;

public class PhysicsResearchPaperTemplate : ITemplateDefinition
{
    public string Id => "physicsresearch";
    public string Name => "Physics Research Paper";
    public string Description => "Full 2-page quantum physics paper with Hamiltonian equations, Lindblad dissipation, Wigner tomography, and spectroscopy data";
    public string Category => "Academic";
    public string IconKind => "Atom";
    public string AccentColorHex => "#0891B2";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Cavity_QED_Superconducting_Qubits_2026.pdf",
            Author = "Dr. Alexander Bohr, Dr. Evelyn Chen, and Prof. Julian Thorne",
            Subject = "Quantum Electrodynamics & Non-Equilibrium Phase Transitions"
        };

        // =========================================================================
        // PAGE 1: Title, Abstract, Dicke Hamiltonian, Master Equation & Data
        // =========================================================================
        var page1 = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "PHYSICAL REVIEW LETTERS • VOL. 136, 140401 (2026) • PACS: 03.67.Lx, 42.50.Pq, 85.25.Cp",
            FooterCenter = "QUANTUM PHYSICS RESEARCH",
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
                    Text = "PHYSICAL REVIEW LETTERS 136, 140401 (2026) • DOI: 10.1103/PhysRevLett.136.140401",
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
                    Text = "Cavity Quantum Electrodynamics and Non-Equilibrium Phase Transitions in Superconducting Qubits",
                    FontSize = 17,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0E7490",
                    Alignment = TextAlignmentMode.Center
                },

                // Authors & Affiliation
                new PdfTextElement
                {
                    X = 55,
                    Y = 108,
                    Width = 690,
                    Height = 34,
                    Text = "Alexander Bohr¹,   Evelyn Chen²,   and   Julian Thorne¹\n¹Quantum Photonics Laboratory, Department of Physics, Harvard University\n²Center for Quantum Information, Massachusetts Institute of Technology",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Center
                },

                // Abstract & PACS Card
                new PdfShapeElement
                {
                    X = 75,
                    Y = 145,
                    Width = 650,
                    Height = 104,
                    CornerRadius = 4,
                    FillColorHex = "#ECFEFF",
                    StrokeColorHex = "#A5F3FC",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 90,
                    Y = 150,
                    Width = 620,
                    Height = 68,
                    Text = "Abstract— We report the experimental observation of a non-equilibrium superradiant phase transition in an array of 64 transmon qubits coupled to a high-Q 3D microwave cavity. In the ultra-strong coupling regime (g/ω_c ≈ 0.18), we measure a critical threshold in the cavity transmission spectrum, observing a 12.4 dB photon squeezing and extended ensemble dephasing times T₂* exceeding 180 μs.",
                    FontSize = 9,
                    FontFamily = "Times New Roman",
                    IsItalic = true,
                    LineHeight = 1.35,
                    TextColorHex = "#164E63",
                    Alignment = TextAlignmentMode.Justify
                },
                new PdfTextElement
                {
                    X = 90,
                    Y = 222,
                    Width = 620,
                    Height = 22,
                    Text = "PACS Numbers— 03.67.Lx (Quantum computation), 42.50.Pq (Cavity QED), 85.25.Cp (Josephson devices).",
                    FontSize = 8.5,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0891B2"
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
                    Text = "I. INTRODUCTION & EXPERIMENTAL SETUP",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0E7490"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 286,
                    Width = 330,
                    Height = 110,
                    Text = "Circuit quantum electrodynamics (cQED) provides a macroscopic testbed for exploring light-matter interactions at the single-photon level [1, 2]. By embedding superconducting Josephson junction transmon qubits inside an aluminum 3D resonant cavity cooled to 12 mK in a dilution refrigerator, we suppress ambient thermal noise and achieve ultra-long photon coherence.",
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
                    Text = "II. TAVIS-CUMMINGS HAMILTONIAN",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0E7490"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 426,
                    Width = 330,
                    Height = 45,
                    Text = "The coherent dynamics of N identical two-level transmons coupled to a single cavity mode is described by the Dicke Hamiltonian:",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                // Hamiltonian Equation Box
                new PdfShapeElement
                {
                    X = 55,
                    Y = 475,
                    Width = 330,
                    Height = 42,
                    CornerRadius = 3,
                    FillColorHex = "#ECFEFF",
                    StrokeColorHex = "#A5F3FC",
                    StrokeThickness = 0.5
                },
                new PdfTextElement
                {
                    X = 65,
                    Y = 482,
                    Width = 310,
                    Height = 28,
                    Text = "Ĥ = ℏω_c â† â + ½ ℏω_q ∑ σ_z^(j) + ℏg/√N ∑ (â† σ_-^(j) + â σ_+^(j))   (1)",
                    FontSize = 9,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0E7490",
                    Alignment = TextAlignmentMode.Center
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 524,
                    Width = 330,
                    Height = 22,
                    Text = "III. LINDBLAD MASTER EQUATION",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0E7490"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 548,
                    Width = 330,
                    Height = 135,
                    Text = "Open-system quantum dissipation is treated via the Lindblad generator:\n\n∂_t ρ̂ = -i/ℏ [Ĥ, ρ̂] + κ 𝒟[â]ρ̂ + γ ∑ 𝒟[σ_-^(j)]ρ̂ + γ_φ ∑ 𝒟[σ_z^(j)]ρ̂,\n\nwhere 𝒟[L̂]ρ̂ = L̂ρ̂L̂† - ½ {L̂†L̂, ρ̂}. Here κ = 2π × 42 kHz represents the cavity photon leakage rate, γ = 2π × 8.5 kHz is the spontaneous qubit decay rate, and γ_φ is the pure dephasing rate.",
                    FontSize = 9,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 690,
                    Width = 330,
                    Height = 22,
                    Text = "IV. DECOHERENCE SUPPRESSION",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0E7490"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 714,
                    Width = 330,
                    Height = 140,
                    Text = "By tuning the qubit array frequency to the cavity anti-node (ω_q = 6.482 GHz), we observe cooperative subradiant states that effectively shield the quantum ensemble from local 1/f charge and flux noise fluctuations. This yields an order-of-magnitude enhancement in ensemble quantum state lifetime.",
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
                    Text = "V. SPECTROSCOPY & EXPERIMENTAL DATA",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0E7490"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 286,
                    Width = 330,
                    Height = 58,
                    Text = "Table I summarizes the calibrated experimental parameters across 4 sub-modules of the 64-qubit quantum processor measured at a base temperature T = 12.5 mK.",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                // Experimental cQED Table
                new PdfTableElement
                {
                    X = 415,
                    Y = 348,
                    Width = 330,
                    Height = 145,
                    Headers = new List<string> { "Module", "ω_q / 2π", "g / 2π", "T₁ (μs)", "T₂* (μs)" },
                    Rows = new List<List<string>>
                    {
                        new() { "Array α (N=16)", "6.482 GHz", "184 MHz", "192.4", "184.2" },
                        new() { "Array β (N=16)", "6.485 GHz", "181 MHz", "188.1", "176.5" },
                        new() { "Array γ (N=16)", "6.479 GHz", "186 MHz", "195.0", "189.0" },
                        new() { "Array δ (N=16)", "6.481 GHz", "183 MHz", "190.2", "182.8" }
                    },
                    HeaderBackgroundHex = "#0891B2",
                    HeaderTextHex = "#FFFFFF",
                    AlternateRowBackgroundHex = "#ECFEFF",
                    BorderColorHex = "#CBD5E1"
                },

                new PdfTextElement
                {
                    X = 415,
                    Y = 502,
                    Width = 330,
                    Height = 22,
                    Text = "VI. QUANTUM PHASE TRANSITION OBSERVATION",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0E7490"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 526,
                    Width = 330,
                    Height = 115,
                    Text = "As the drive photon amplitude exceeds the critical threshold η_c = √(κγ)/2g, we detect macroscopic photon occupation within the cavity accompanied by spontaneous symmetry breaking of the collective spin polarization vector ⟨J_x⟩ ≠ 0 [3, 4].",
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
                    Text = "VII. TIME-RESOLVED PHOTON CORRELATIONS",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0E7490"
                },
                new PdfTextElement
                {
                    X = 415,
                    Y = 672,
                    Width = 330,
                    Height = 145,
                    Text = "Second-order correlation measurements g^(2)(τ) = ⟨â†(t) â†(t+τ) â(t+τ) â(t)⟩ / ⟨â† â⟩² demonstrate strong non-classical antibunching (g^(2)(0) = 0.08 ± 0.02) below the threshold, transitioning smoothly into super-Poissonian photon bunching at criticality.",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                }
            }
        };

        // =========================================================================
        // PAGE 2: Wigner Tomography, Fault Tolerance, Author Bios & References
        // =========================================================================
        var page2 = new PdfPageModel
        {
            PageNumber = 2,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "PHYSICAL REVIEW LETTERS • VOL. 136, 140401",
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
                    Text = "BOHR, CHEN, AND THORNE: CAVITY QED IN SUPERCONDUCTING QUBITS",
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
                    Text = "VIII. WIGNER TOMOGRAPHY & SQUEEZING",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0E7490"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 86,
                    Width = 330,
                    Height = 120,
                    Text = "Full quantum state tomography of the cavity mode reveals pronounced negativity in the reconstructed Wigner quasi-probability distribution W(α). Quadrature variance satisfies (ΔX_θ)² = e^{-2r} with squeeze parameter r = 1.43, demonstrating non-Gaussian quantum resource generation in macroscopic circuits [5].",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 212,
                    Width = 330,
                    Height = 22,
                    Text = "IX. FAULT-TOLERANT QUANTUM COMPUTING",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0E7490"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 236,
                    Width = 330,
                    Height = 130,
                    Text = "The collective protection against single-qubit relaxation opens direct pathways for bosonic quantum error correction codes, specifically Gottesman-Kitaev-Preskill (GKP) grid states and cat-qubit parity measurements. With average gate fidelities ℱ_gate > 99.82%, the architecture satisfies the fault-tolerance threshold.",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                new PdfTextElement
                {
                    X = 55,
                    Y = 372,
                    Width = 330,
                    Height = 22,
                    Text = "X. CONCLUDING REMARKS",
                    FontSize = 11,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0E7490"
                },
                new PdfTextElement
                {
                    X = 55,
                    Y = 396,
                    Width = 330,
                    Height = 90,
                    Text = "In conclusion, we achieved ultra-strong collective light-matter coupling in multi-transmon systems. This platform provides an ideal simulator for non-equilibrium quantum criticality and high-fidelity quantum transducers.",
                    FontSize = 9.5,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.35,
                    TextColorHex = "#334155",
                    Alignment = TextAlignmentMode.Justify
                },

                // Author Biographies Header
                new PdfTextElement
                {
                    X = 55,
                    Y = 494,
                    Width = 330,
                    Height = 22,
                    Text = "AUTHOR BIOGRAPHIES",
                    FontSize = 10.5,
                    FontFamily = "Times New Roman",
                    IsBold = true,
                    TextColorHex = "#0E7490"
                },
                new PdfDividerElement
                {
                    X = 55,
                    Y = 515,
                    Width = 330,
                    Height = 1,
                    Thickness = 0.75,
                    ColorHex = "#CBD5E1"
                },

                // Bio 1: Alexander Bohr
                new PdfShapeElement
                {
                    X = 55,
                    Y = 525,
                    Width = 34,
                    Height = 34,
                    CornerRadius = 17,
                    FillColorHex = "#0891B2",
                    Label = "AB",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 12
                },
                new PdfTextElement
                {
                    X = 98,
                    Y = 523,
                    Width = 287,
                    Height = 65,
                    Text = "Alexander Bohr received his Ph.D. in Physics from Harvard in 2021. He leads experimental superconducting cQED research at the Harvard Quantum Optics Lab.",
                    FontSize = 8,
                    FontFamily = "Times New Roman",
                    LineHeight = 1.3,
                    TextColorHex = "#334155"
                },

                // Bio 2: Evelyn Chen
                new PdfShapeElement
                {
                    X = 55,
                    Y = 595,
                    Width = 34,
                    Height = 34,
                    CornerRadius = 17,
                    FillColorHex = "#0E7490",
                    Label = "EC",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 12
                },
                new PdfTextElement
                {
                    X = 98,
                    Y = 593,
                    Width = 287,
                    Height = 65,
                    Text = "Evelyn Chen is an Associate Professor of Electrical Engineering and Physics at MIT, focusing on Josephson junction parametric amplifiers and quantum noise limits.",
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
                    TextColorHex = "#0E7490"
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
                    Text = "[1] S. Haroche and J.-M. Raimond, Exploring the Quantum: Atoms, Cavities, and Photons, Oxford Univ. Press, 2006.\n[2] A. Blais, S. M. Girvin, and R. J. Schoelkopf, \"Circuit quantum electrodynamics,\" Rev. Mod. Phys., vol. 93, 025005, 2021.\n[3] M. H. Devoret and R. J. Schoelkopf, \"Superconducting circuits for quantum information,\" Science, vol. 339, pp. 1169–1174, 2013.\n[4] R. H. Dicke, \"Coherence in spontaneous radiation processes,\" Phys. Rev., vol. 93, pp. 99–110, 1954.\n[5] P. Krantz et al., \"A quantum engineer's guide to superconducting qubits,\" Appl. Phys. Rev., vol. 6, 021318, 2019.\n[6] D. Gottesman, A. Kitaev, and J. Preskill, \"Encoding a qubit in an oscillator,\" Phys. Rev. A, vol. 64, 012310, 2001.\n[7] M. Mirrahimi et al., \"Dynamically protected cat-qubits,\" New J. Phys., vol. 16, 045014, 2014.\n[8] J. M. Fink et al., \"Climbing the Jaynes-Cummings ladder and observing its nonlinearity,\" Nature, vol. 454, pp. 315–318, 2008.",
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
