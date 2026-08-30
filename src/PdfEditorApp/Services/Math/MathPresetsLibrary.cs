using System.Collections.Generic;
using System.Linq;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services.MathEngine;

public record MathPresetItem(
    string Id,
    string Name,
    string Formula,
    MathCategory Category,
    string Description,
    string DefaultEquationNumber,
    double DefaultWidth = 340,
    double DefaultHeight = 55
);

public static class MathPresetsLibrary
{
    private static readonly List<MathPresetItem> _presets = new()
    {
        // ==========================================
        // 1. CALCULUS & ANALYSIS
        // ==========================================
        new(
            "gaussian_integral",
            "Gaussian Integral",
            @"\int_{-\infty}^{\infty} e^{-x^2} \, dx = \sqrt{\pi}",
            MathCategory.Calculus,
            "Euler-Poisson definite integral over the entire real line",
            "(1)",
            260, 48
        ),
        new(
            "fundamental_calculus",
            "Fundamental Theorem of Calculus",
            @"\int_{a}^{b} f(x) \, dx = F(b) - F(a) = \left[ F(x) \right]_{a}^{b}",
            MathCategory.Calculus,
            "Connecting derivatives and definite integrals",
            "(2)",
            320, 50
        ),
        new(
            "taylor_series",
            "Taylor Series Expansion",
            @"f(x) = \sum_{n=0}^{\infty} \frac{f^{(n)}(a)}{n!} (x - a)^n",
            MathCategory.Calculus,
            "Infinite power series representation of a smooth function",
            "(3)",
            300, 52
        ),
        new(
            "fourier_transform",
            "Continuous Fourier Transform",
            @"\hat{f}(\xi) = \int_{-\infty}^{\infty} f(x) \, e^{-2\pi i x \xi} \, dx",
            MathCategory.Calculus,
            "Frequency spectrum decomposition of a time-domain signal",
            "(4)",
            300, 50
        ),
        new(
            "laplace_transform",
            "Laplace Transform",
            @"\mathcal{L}\{f(t)\} = F(s) = \int_{0}^{\infty} f(t) \, e^{-st} \, dt",
            MathCategory.Calculus,
            "Integral transform mapping time functions to complex frequency s-plane",
            "(5)",
            320, 50
        ),
        new(
            "cauchy_residue",
            "Cauchy's Residue Theorem",
            @"\oint_{\gamma} f(z) \, dz = 2\pi i \sum_{k=1}^n \operatorname{Res}(f, a_k)",
            MathCategory.Calculus,
            "Contour integral evaluation via isolated singularities",
            "(6)",
            310, 52
        ),
        new(
            "greens_theorem",
            "Green's Theorem in the Plane",
            @"\oint_{C} (L \, dx + M \, dy) = \iint_{D} \left( \frac{\partial M}{\partial x} - \frac{\partial L}{\partial y} \right) dx \, dy",
            MathCategory.Calculus,
            "Relating line integrals around closed curves to 2D double integrals",
            "(7)",
            380, 55
        ),

        // ==========================================
        // 2. PHYSICS & RELATIVITY
        // ==========================================
        new(
            "mass_energy",
            "Mass-Energy Equivalence",
            @"E = m c^2",
            MathCategory.Physics,
            "Einstein's rest-frame mass-energy equivalence relation",
            "(1)",
            160, 42
        ),
        new(
            "relativistic_energy_momentum",
            "Relativistic Energy-Momentum",
            @"E^2 = (p c)^2 + (m_0 c^2)^2",
            MathCategory.Physics,
            "Energy of a particle with relativistic momentum p and rest mass m0",
            "(2)",
            240, 45
        ),
        new(
            "maxwell_diff",
            "Maxwell's Equations (Differential)",
            @"\nabla \cdot \mathbf{E} = \frac{\rho}{\varepsilon_0}, \quad \nabla \times \mathbf{B} = \mu_0 \mathbf{J} + \mu_0 \varepsilon_0 \frac{\partial \mathbf{E}}{\partial t}",
            MathCategory.Physics,
            "Fundamental laws of classical electrodynamics",
            "(3)",
            420, 52
        ),
        new(
            "schrodinger_time_dependent",
            "Time-Dependent Schrödinger Equation",
            @"i \hbar \frac{\partial}{\partial t} \Psi(\mathbf{r}, t) = \hat{H} \Psi(\mathbf{r}, t)",
            MathCategory.Physics,
            "Quantum state time evolution governed by the Hamiltonian operator",
            "(4)",
            300, 50
        ),
        new(
            "heisenberg_uncertainty",
            "Heisenberg Uncertainty Principle",
            @"\sigma_x \sigma_p \ge \frac{\hbar}{2}",
            MathCategory.Physics,
            "Fundamental lower bound on position-momentum measurement precision",
            "(5)",
            190, 48
        ),
        new(
            "dirac_equation",
            "Dirac Relativistic Wave Equation",
            @"(i \gamma^\mu \partial_\mu - m) \psi = 0",
            MathCategory.Physics,
            "Relativistic quantum mechanics for spin-1/2 fermions predicting antimatter",
            "(6)",
            230, 46
        ),
        new(
            "navier_stokes",
            "Navier-Stokes Fluid Dynamics",
            @"\rho \left( \frac{\partial \mathbf{u}}{\partial t} + \mathbf{u} \cdot \nabla \mathbf{u} \right) = -\nabla p + \mu \nabla^2 \mathbf{u} + \mathbf{f}",
            MathCategory.Physics,
            "Momentum balance equation for incompressible viscous fluid flows",
            "(7)",
            420, 55
        ),

        // ==========================================
        // 3. QUANTUM MECHANICS & CQED
        // ==========================================
        new(
            "dicke_hamiltonian",
            "Dicke Cavity QED Hamiltonian",
            @"\hat{H} = \hbar \omega_c \hat{a}^\dagger \hat{a} + \frac{1}{2} \hbar \omega_q \sum_{j=1}^N \hat{\sigma}_z^{(j)} + \frac{\hbar g}{\sqrt{N}} \sum_{j=1}^N (\hat{a}^\dagger \hat{\sigma}_-^{(j)} + \hat{a} \hat{\sigma}_+^{(j)})",
            MathCategory.QuantumMechanics,
            "Collective interaction between an array of N transmons and a cavity mode",
            "(1)",
            460, 56
        ),
        new(
            "lindblad_master_equation",
            "Lindblad Open-System Master Equation",
            @"\frac{\partial \hat{\rho}}{\partial t} = -\frac{i}{\hbar} [\hat{H}, \hat{\rho}] + \sum_k \gamma_k \left( \hat{L}_k \hat{\rho} \hat{L}_k^\dagger - \frac{1}{2} \{ \hat{L}_k^\dagger \hat{L}_k, \hat{\rho} \} \right)",
            MathCategory.QuantumMechanics,
            "Markovian density matrix evolution with dissipation and dephasing channels",
            "(2)",
            440, 58
        ),
        new(
            "wigner_distribution",
            "Wigner Quasi-Probability Function",
            @"W(\alpha) = \frac{2}{\pi} \operatorname{Tr} \left[ \hat{\rho} \, \hat{D}(\alpha) \, (-1)^{\hat{a}^\dagger \hat{a}} \, \hat{D}^\dagger(\alpha) \right]",
            MathCategory.QuantumMechanics,
            "Phase-space representation for quantum state tomography and squeezing",
            "(3)",
            360, 52
        ),
        new(
            "pauli_matrices",
            "Pauli Spin-1/2 Matrices",
            @"\hat{\sigma}_x = \begin{pmatrix} 0 & 1 \\ 1 & 0 \end{pmatrix}, \quad \hat{\sigma}_y = \begin{pmatrix} 0 & -i \\ i & 0 \end{pmatrix}, \quad \hat{\sigma}_z = \begin{pmatrix} 1 & 0 \\ 0 & -1 \end{pmatrix}",
            MathCategory.QuantumMechanics,
            "Standard Pauli matrix representations for 2-level qubit states",
            "(4)",
            420, 54
        ),

        // ==========================================
        // 4. QUANTITATIVE FINANCE & ECONOMETRICS
        // ==========================================
        new(
            "bates_jump_diffusion",
            "Bates Jump-Diffusion SDE System",
            @"dS_t = (\mu - \lambda \bar{k}) S_t dt + \sqrt{V_t} S_t dW_t^S + (e^J - 1) S_t dN_t",
            MathCategory.Finance,
            "Continuous-time asset pricing with stochastic volatility and Poisson jump arrivals",
            "(1)",
            400, 50
        ),
        new(
            "heston_variance_sde",
            "Heston CIR Stochastic Variance Process",
            @"dV_t = \kappa (\theta - V_t) dt + \sigma_v \sqrt{V_t} dW_t^V, \quad d\langle W^S, W^V \rangle_t = \rho dt",
            MathCategory.Finance,
            "Mean-reverting square-root variance process with leverage correlation",
            "(2)",
            410, 52
        ),
        new(
            "black_scholes_pde",
            "Black-Scholes-Merton PDE",
            @"\frac{\partial V}{\partial t} + \frac{1}{2} \sigma^2 S^2 \frac{\partial^2 V}{\partial S^2} + r S \frac{\partial V}{\partial S} - r V = 0",
            MathCategory.Finance,
            "Linear parabolic PDE for European contingent claim derivative pricing",
            "(3)",
            380, 52
        ),
        new(
            "sharpe_ratio",
            "Annualized Sharpe Ratio",
            @"SR = \frac{\mathbb{E}[R_p - R_f]}{\sigma(R_p - R_f)} \cdot \sqrt{252}",
            MathCategory.Finance,
            "Risk-adjusted excess return per unit of annualized total portfolio volatility",
            "(4)",
            260, 50
        ),
        new(
            "capm_pricing",
            "Capital Asset Pricing Model (CAPM)",
            @"\mathbb{E}[R_i] = R_f + \beta_i (\mathbb{E}[R_m] - R_f), \quad \beta_i = \frac{\operatorname{Cov}(R_i, R_m)}{\operatorname{Var}(R_m)}",
            MathCategory.Finance,
            "Equilibrium asset pricing model relating expected return to systematic market beta",
            "(5)",
            400, 52
        ),

        // ==========================================
        // 5. ALGEBRA & DISCRETE MATHEMATICS
        // ==========================================
        new(
            "quadratic_formula",
            "Quadratic Formula",
            @"x = \frac{-b \pm \sqrt{b^2 - 4ac}}{2a}",
            MathCategory.Algebra,
            "Exact analytic roots of the general quadratic equation ax² + bx + c = 0",
            "(1)",
            240, 48
        ),
        new(
            "eulers_identity",
            "Euler's Beautiful Identity",
            @"e^{i\pi} + 1 = 0",
            MathCategory.Algebra,
            "Profound identity linking the fundamental constants e, i, π, 1, and 0",
            "(2)",
            160, 42
        ),
        new(
            "binomial_theorem",
            "Binomial Theorem",
            @"(x + y)^n = \sum_{k=0}^{n} \binom{n}{k} x^{n-k} y^k",
            MathCategory.Algebra,
            "Algebraic expansion of powers of a binomial sum using combinations",
            "(3)",
            280, 48
        ),
        new(
            "matrix_2x2_inverse",
            "2x2 Matrix Inversion",
            @"A^{-1} = \frac{1}{ad - bc} \begin{pmatrix} d & -b \\ -c & a \end{pmatrix}",
            MathCategory.Algebra,
            "Analytic inverse of a 2×2 non-singular matrix with determinant ad - bc",
            "(4)",
            300, 54
        ),
        new(
            "euler_polyhedron",
            "Euler's Polyhedral Formula",
            @"V - E + F = 2",
            MathCategory.DiscreteMath,
            "Topological invariant relating vertices, edges, and faces of convex polyhedra",
            "(5)",
            170, 42
        ),

        // ==========================================
        // 6. STATISTICS & MACHINE LEARNING
        // ==========================================
        new(
            "normal_distribution",
            "Normal (Gaussian) Distribution PDF",
            @"f(x) = \frac{1}{\sigma \sqrt{2\pi}} \exp\left( -\frac{1}{2}\left(\frac{x - \mu}{\sigma}\right)^2 \right)",
            MathCategory.Statistics,
            "Probability density function of the Gaussian distribution with mean μ and variance σ²",
            "(1)",
            350, 54
        ),
        new(
            "bayes_theorem",
            "Bayes' Conditional Probability Theorem",
            @"P(A \mid B) = \frac{P(B \mid A) \, P(A)}{P(B)}",
            MathCategory.Statistics,
            "Computing posterior probability from likelihood, prior, and marginal evidence",
            "(2)",
            250, 48
        ),
        new(
            "softmax_function",
            "Softmax Activation Function",
            @"\sigma(\mathbf{z})_i = \frac{e^{z_i}}{\sum_{j=1}^K e^{z_j}} \quad \text{for } i = 1, \dots, K",
            MathCategory.Statistics,
            "Probability distribution normalizer across K multi-class logit logits",
            "(3)",
            320, 50
        ),
        new(
            "cross_entropy_loss",
            "Categorical Cross-Entropy Loss",
            @"\mathcal{L}_{\text{CE}} = -\sum_{i=1}^C y_i \log(\hat{y}_i)",
            MathCategory.Statistics,
            "Standard loss metric comparing true one-hot distribution y and predicted probabilities ŷ",
            "(4)",
            260, 48
        ),
        new(
            "sample_variance",
            "Sample Standard Deviation",
            @"s = \sqrt{\frac{1}{N - 1} \sum_{i=1}^{N} (x_i - \bar{x})^2}",
            MathCategory.Statistics,
            "Unbiased sample variance and standard deviation with Bessel's correction N - 1",
            "(5)",
            280, 50
        ),

        // ==========================================
        // 7. CHEMISTRY & THERMODYNAMICS
        // ==========================================
        new(
            "arrhenius_equation",
            "Arrhenius Rate Equation",
            @"k = A \, e^{-\frac{E_a}{R T}}",
            MathCategory.Chemistry,
            "Temperature dependence of chemical reaction rate constants and activation energy",
            "(1)",
            200, 48
        ),
        new(
            "nernst_equation",
            "Nernst Electrochemical Potential",
            @"E = E^\circ - \frac{R T}{z F} \ln Q",
            MathCategory.Chemistry,
            "Reduction potential of an electrochemical cell as a function of standard potential and quotient Q",
            "(2)",
            250, 50
        ),
        new(
            "ideal_gas_law",
            "Ideal Gas State Equation",
            @"P V = n R T = N k_B T",
            MathCategory.Chemistry,
            "Thermodynamic equation of state for hypothetical ideal gases",
            "(3)",
            220, 42
        )
    };

    public static IReadOnlyList<MathPresetItem> AllPresets => _presets;
    public static IReadOnlyList<MathPresetItem> GetAllPresets() => _presets;

    public static IEnumerable<MathPresetItem> GetByCategory(MathCategory category) =>
        _presets.Where(p => p.Category == category);

    public static MathPresetItem? FindById(string id) =>
        _presets.FirstOrDefault(p => p.Id.Equals(id, System.StringComparison.OrdinalIgnoreCase));

    public static MathPresetItem? FindByName(string name) =>
        _presets.FirstOrDefault(p => p.Name.Equals(name, System.StringComparison.OrdinalIgnoreCase));

    private static readonly Dictionary<string, string> _snippets = new(System.StringComparer.OrdinalIgnoreCase)
    {
        ["frac"] = @"\frac{a}{b}",
        ["sqrt"] = @"\sqrt{x}",
        ["rootn"] = @"\sqrt[n]{x}",
        ["sum"] = @"\sum_{i=1}^{n}",
        ["prod"] = @"\prod_{i=1}^{n}",
        ["int"] = @"\int_{a}^{b}",
        ["iint"] = @"\iint",
        ["iiint"] = @"\iiint",
        ["oint"] = @"\oint",
        ["lim"] = @"\lim_{x \to 0}",
        ["dydx"] = @"\frac{\partial y}{\partial x}",
        ["partial"] = @"\partial",
        ["nabla"] = @"\nabla",
        ["hbar"] = @"\hbar",
        ["infty"] = @"\infty",
        ["hat"] = @"\hat{H}",
        ["vec"] = @"\vec{F}",
        ["dot"] = @"\dot{x}",
        ["ddot"] = @"\ddot{x}",
        ["x2"] = @"x^{2}",
        ["xi"] = @"x_{i}",
        ["xi2"] = @"x_{i}^{2}",
        ["binom"] = @"\binom{n}{k}",
        ["pmatrix"] = @"\begin{pmatrix} a & b \\ c & d \end{pmatrix}",
        ["bmatrix"] = @"\begin{bmatrix} a & b \\ c & d \end{bmatrix}",
        ["cases"] = @"\begin{cases} x & x \ge 0 \\ -x & x < 0 \end{cases}",
        ["parens"] = @"\left(  \right)",
        ["brackets"] = @"\left[  \right]",
        ["set"] = @"\left\{  \right\}",
        ["bra"] = @"\langle \psi |",
        ["ket"] = @"| \psi \rangle"
    };

    public static string ResolveSnippet(string snippet)
    {
        if (string.IsNullOrWhiteSpace(snippet)) return "";
        if (_snippets.TryGetValue(snippet.Trim(), out var expansion))
        {
            return expansion;
        }
        return snippet;
    }
}
