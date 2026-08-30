using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PdfEditorApp.Models;

namespace PdfEditorApp.Services.MathEngine;

public record MathRenderOptions(
    double FontSize = 16,
    string TextColorHex = "#0F172A",
    string BackgroundColorHex = "#00000000",
    string BorderColorHex = "#00000000",
    double BorderThickness = 0,
    double CornerRadius = 4,
    double Padding = 8,
    bool ShowBackground = false,
    bool ShowBorder = false,
    bool ShowEquationNumber = false,
    string EquationNumber = "(1)",
    TextAlignmentMode Alignment = TextAlignmentMode.Center,
    MathDisplayStyle DisplayStyle = MathDisplayStyle.DisplayBlock,
    double TargetWidth = 0,
    double TargetHeight = 0
);

public class MathRenderResult
{
    public string SvgXml { get; set; } = "";
    public string SvgMarkup => SvgXml;
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public string PathGeometryData { get; set; } = "";
}

public static class MathLayoutEngine
{
    // =========================================================================
    // SYMBOL & COMMAND DICTIONARIES
    // =========================================================================
    private static readonly Dictionary<string, (string unicode, bool isOperator)> _greekSymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        // Lowercase Greek
        ["alpha"] = ("α", false), ["beta"] = ("β", false), ["gamma"] = ("γ", false), ["delta"] = ("δ", false),
        ["epsilon"] = ("ϵ", false), ["varepsilon"] = ("ε", false), ["zeta"] = ("ζ", false), ["eta"] = ("η", false),
        ["theta"] = ("θ", false), ["vartheta"] = ("ϑ", false), ["iota"] = ("ι", false), ["kappa"] = ("κ", false),
        ["lambda"] = ("λ", false), ["mu"] = ("μ", false), ["nu"] = ("ν", false), ["xi"] = ("ξ", false),
        ["pi"] = ("π", false), ["varpi"] = ("ϖ", false), ["rho"] = ("ρ", false), ["varrho"] = ("ϱ", false),
        ["sigma"] = ("σ", false), ["varsigma"] = ("ς", false), ["tau"] = ("τ", false), ["upsilon"] = ("υ", false),
        ["phi"] = ("ϕ", false), ["varphi"] = ("φ", false), ["chi"] = ("χ", false), ["psi"] = ("ψ", false),
        ["omega"] = ("ω", false),

        // Uppercase Greek
        ["Gamma"] = ("Γ", false), ["Delta"] = ("Δ", false), ["Theta"] = ("Θ", false), ["Lambda"] = ("Λ", false),
        ["Xi"] = ("Ξ", false), ["Pi"] = ("Π", false), ["Sigma"] = ("Σ", false), ["Upsilon"] = ("Υ", false),
        ["Phi"] = ("Φ", false), ["Psi"] = ("Ψ", false), ["Omega"] = ("Ω", false)
    };

    private static readonly Dictionary<string, string> _mathSymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        // Binary Operators & Signs
        ["pm"] = "±", ["mp"] = "∓", ["times"] = "×", ["cdot"] = "·", ["div"] = "÷",
        ["ast"] = "∗", ["star"] = "★", ["circ"] = "∘", ["bullet"] = "•",
        ["oplus"] = "⊕", ["otimes"] = "⊗", ["odot"] = "⊙", ["wedge"] = "∧", ["vee"] = "∨",
        ["cap"] = "∩", ["cup"] = "∪",

        // Relations & Sets
        ["le"] = "≤", ["leq"] = "≤", ["ge"] = "≥", ["geq"] = "≥", ["neq"] = "≠", ["ne"] = "≠",
        ["approx"] = "≈", ["equiv"] = "≡", ["sim"] = "∼", ["simeq"] = "≃", ["cong"] = "≅",
        ["propto"] = "∝", ["subset"] = "⊂", ["subseteq"] = "⊆", ["supset"] = "⊃", ["supseteq"] = "⊇",
        ["in"] = "∈", ["notin"] = "∉", ["ni"] = "∋", ["mid"] = "∣", ["parallel"] = "∥", ["perp"] = "⊥",

        // Arrows
        ["to"] = "→", ["rightarrow"] = "→", ["leftarrow"] = "←", ["Rightarrow"] = "⇒",
        ["Leftarrow"] = "⇐", ["leftrightarrow"] = "↔", ["Leftrightarrow"] = "⇔", ["iff"] = "⟺",
        ["mapsto"] = "↦", ["uparrow"] = "↑", ["downarrow"] = "↓",

        // Calculus, Physics & Analysis
        ["partial"] = "∂", ["nabla"] = "∇", ["hbar"] = "ℏ", ["infty"] = "∞", ["ell"] = "ℓ",
        ["dagger"] = "†", ["ddagger"] = "‡", ["prime"] = "′", ["forall"] = "∀", ["exists"] = "∃",
        ["nexists"] = "∄", ["empty"] = "∅", ["emptyset"] = "∅", ["neg"] = "¬", ["angle"] = "∠",
        ["Re"] = "ℜ", ["Im"] = "ℑ", ["aleph"] = "ℵ", ["wp"] = "℘"
    };

    private static readonly Dictionary<string, string> _blackboardBold = new(StringComparer.OrdinalIgnoreCase)
    {
        ["R"] = "ℝ", ["C"] = "ℂ", ["N"] = "ℕ", ["Z"] = "ℤ", ["Q"] = "ℚ",
        ["P"] = "ℙ", ["E"] = "𝔼", ["H"] = "ℍ", ["F"] = "𝔽", ["K"] = "𝕂"
    };

    private static readonly Dictionary<string, string> _calligraphic = new(StringComparer.OrdinalIgnoreCase)
    {
        ["A"] = "𝒜", ["B"] = "ℬ", ["C"] = "𝒞", ["D"] = "𝒟", ["E"] = "ℰ",
        ["F"] = "ℱ", ["G"] = "𝒢", ["H"] = "ℋ", ["I"] = "ℐ", ["J"] = "𝒥",
        ["K"] = "𝒦", ["L"] = "ℒ", ["M"] = "ℳ", ["N"] = "𝒩", ["O"] = "𝒪",
        ["P"] = "𝒫", ["Q"] = "𝒬", ["R"] = "ℛ", ["S"] = "𝒮", ["T"] = "𝒯",
        ["U"] = "𝒰", ["V"] = "𝒱", ["W"] = "𝒲", ["X"] = "𝒳", ["Y"] = "𝒴",
        ["Z"] = "𝒵"
    };

    private static readonly HashSet<string> _standardFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "sin", "cos", "tan", "cot", "sec", "csc",
        "arcsin", "arccos", "arctan", "sinh", "cosh", "tanh",
        "ln", "log", "exp", "det", "dim", "ker", "deg", "gcd",
        "max", "min", "sup", "inf", "lim", "limsup", "liminf",
        "Pr", "arg", "Tr", "diag", "cov", "var", "Res", "rank"
    };

    // =========================================================================
    // PUBLIC MAIN RENDER API
    // =========================================================================
    public static MathRenderResult RenderToSvg(string formula, MathRenderOptions? options = null)
    {
        options ??= new MathRenderOptions();
        var result = new MathRenderResult();

        try
        {
            if (string.IsNullOrWhiteSpace(formula))
            {
                formula = @"f(x) = y";
            }

            // 1. Tokenize & Parse AST
            var tokens = Tokenize(formula);
            var parser = new MathParser(tokens);
            var rootNode = parser.ParseExpression();

            // 2. Measure AST Nodes recursively
            double baseFontSize = Math.Max(8, options.FontSize);
            var measureCtx = new LayoutContext(baseFontSize, options.TextColorHex, options.DisplayStyle);
            rootNode.Measure(measureCtx);

            double contentWidth = rootNode.Width;
            double contentHeight = rootNode.Height;
            double contentAscent = rootNode.Ascent;

            // 3. Compute SVG Dimensions and Margins
            double pad = Math.Max(0, options.Padding);
            double eqNumWidth = 0;
            if (options.ShowEquationNumber && !string.IsNullOrWhiteSpace(options.EquationNumber))
            {
                eqNumWidth = (options.EquationNumber.Length * baseFontSize * 0.55) + 20;
            }

            double totalWidth = Math.Max(options.TargetWidth, contentWidth + (pad * 2) + eqNumWidth);
            double totalHeight = Math.Max(options.TargetHeight, contentHeight + (pad * 2));

            // Align content X inside bounding box
            double startX = pad;
            if (options.Alignment == TextAlignmentMode.Center)
            {
                startX = pad + Math.Max(0, (totalWidth - (pad * 2) - eqNumWidth - contentWidth) / 2);
            }
            else if (options.Alignment == TextAlignmentMode.Right)
            {
                startX = totalWidth - pad - eqNumWidth - contentWidth;
            }

            double baselineY = pad + contentAscent + Math.Max(0, (totalHeight - (pad * 2) - contentHeight) / 2);

            // 4. Generate SVG Output
            var svgBuilder = new StringBuilder();
            string strW = totalWidth.ToString("F1", CultureInfo.InvariantCulture);
            string strH = totalHeight.ToString("F1", CultureInfo.InvariantCulture);

            svgBuilder.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {strW} {strH}\" width=\"{strW}\" height=\"{strH}\">");
            svgBuilder.AppendLine("  <defs>");
            svgBuilder.AppendLine("    <style>");
            svgBuilder.AppendLine("      .m-text { font-family: 'Times New Roman', 'Cambria Math', 'Latin Modern Math', serif; }");
            svgBuilder.AppendLine("      .m-var { font-family: 'Times New Roman', 'Cambria Math', serif; font-style: italic; }");
            svgBuilder.AppendLine("      .m-upright { font-family: 'Times New Roman', 'Cambria Math', serif; font-style: normal; }");
            svgBuilder.AppendLine("      .m-sans { font-family: 'Arial', sans-serif; }");
            svgBuilder.AppendLine("    </style>");
            svgBuilder.AppendLine("  </defs>");

            // Background & Border
            if (options.ShowBackground || options.ShowBorder)
            {
                string fill = options.ShowBackground && !string.IsNullOrEmpty(options.BackgroundColorHex) ? options.BackgroundColorHex : "none";
                string stroke = options.ShowBorder && !string.IsNullOrEmpty(options.BorderColorHex) ? options.BorderColorHex : "none";
                string strokeW = options.BorderThickness > 0 ? options.BorderThickness.ToString("F1", CultureInfo.InvariantCulture) : "0";
                string rx = options.CornerRadius.ToString("F1", CultureInfo.InvariantCulture);

                svgBuilder.AppendLine($"  <rect x=\"0.5\" y=\"0.5\" width=\"{(totalWidth - 1).ToString("F1", CultureInfo.InvariantCulture)}\" height=\"{(totalHeight - 1).ToString("F1", CultureInfo.InvariantCulture)}\" rx=\"{rx}\" ry=\"{rx}\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"{strokeW}\" />");
            }

            // Render Math AST Content
            var renderCtx = new RenderContext(baseFontSize, options.TextColorHex, svgBuilder, options.DisplayStyle);
            rootNode.Render(startX, baselineY, renderCtx);

            // Render Right-Aligned Equation Number (e.g. "(1)")
            if (options.ShowEquationNumber && !string.IsNullOrWhiteSpace(options.EquationNumber))
            {
                double eqNumX = totalWidth - pad;
                double eqNumFontSize = baseFontSize * 0.9;
                svgBuilder.AppendLine($"  <text x=\"{eqNumX.ToString("F1", CultureInfo.InvariantCulture)}\" y=\"{baselineY.ToString("F1", CultureInfo.InvariantCulture)}\" text-anchor=\"end\" fill=\"{options.TextColorHex}\" font-size=\"{eqNumFontSize.ToString("F1", CultureInfo.InvariantCulture)}\" font-weight=\"bold\" class=\"m-text m-upright\">{EscapeXml(options.EquationNumber)}</text>");
            }

            svgBuilder.AppendLine("</svg>");

            result.SvgXml = svgBuilder.ToString();
            result.Width = totalWidth;
            result.Height = totalHeight;
            result.IsSuccess = true;
            result.PathGeometryData = GeneratePathPreviewData(totalWidth, totalHeight);
        }
        catch (Exception ex)
        {
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
            result.SvgXml = GenerateFallbackErrorSvg(formula, options, ex.Message);
            result.Width = Math.Max(200, options.TargetWidth);
            result.Height = Math.Max(40, options.TargetHeight);
        }

        return result;
    }

    private static string GeneratePathPreviewData(double width, double height)
    {
        // Simple geometry representation for Avalonia vector path binding fallback
        return $"M 0,0 L {width.ToString("F0", CultureInfo.InvariantCulture)},0 L {width.ToString("F0", CultureInfo.InvariantCulture)},{height.ToString("F0", CultureInfo.InvariantCulture)} L 0,{height.ToString("F0", CultureInfo.InvariantCulture)} Z";
    }

    private static string GenerateFallbackErrorSvg(string rawFormula, MathRenderOptions options, string error)
    {
        double w = Math.Max(240, options.TargetWidth > 0 ? options.TargetWidth : 240);
        double h = Math.Max(50, options.TargetHeight > 0 ? options.TargetHeight : 50);

        return $@"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 {w} {h}"" width=""{w}"" height=""{h}"">
  <rect width=""{w}"" height=""{h}"" rx=""4"" fill=""#FEF2F2"" stroke=""#F87171"" stroke-width=""1"" />
  <text x=""12"" y=""24"" fill=""#B91C1C"" font-size=""12"" font-family=""monospace"" font-weight=""bold"">{EscapeXml(rawFormula)}</text>
  <text x=""12"" y=""40"" fill=""#DC2626"" font-size=""9"" font-family=""sans-serif"">Syntax Error: {EscapeXml(error)}</text>
</svg>";
    }

    public static string EscapeXml(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    // =========================================================================
    // TOKENIZER
    // =========================================================================
    public enum TokenType
    {
        Command,
        Identifier,
        Number,
        Operator,
        OpenBrace,
        CloseBrace,
        OpenBracket,
        CloseBracket,
        OpenParen,
        CloseParen,
        Superscript,
        Subscript,
        Ampersand,
        Newline,
        StringLiteral,
        EOF
    }

    public record Token(TokenType Type, string Value, int Position);

    public static List<Token> Tokenize(string formula)
    {
        var tokens = new List<Token>();
        int i = 0;
        int len = formula.Length;

        while (i < len)
        {
            char c = formula[i];

            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c == '\\')
            {
                int start = i;
                i++; // Skip backslash
                if (i < len && (formula[i] == '\\' || formula[i] == ',' || formula[i] == ':' || formula[i] == ';' || formula[i] == '!' || formula[i] == '{' || formula[i] == '}' || formula[i] == '|' || formula[i] == ' ' || formula[i] == '%'))
                {
                    char sym = formula[i++];
                    if (sym == '\\')
                    {
                        tokens.Add(new Token(TokenType.Newline, "\\\\", start));
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Command, "\\" + sym, start));
                    }
                }
                else
                {
                    int cmdStart = i;
                    while (i < len && char.IsLetter(formula[i]))
                    {
                        i++;
                    }
                    string cmd = formula[cmdStart..i];
                    tokens.Add(new Token(TokenType.Command, cmd, start));
                }
                continue;
            }

            if (c == '{')
            {
                tokens.Add(new Token(TokenType.OpenBrace, "{", i++));
                continue;
            }

            if (c == '}')
            {
                tokens.Add(new Token(TokenType.CloseBrace, "}", i++));
                continue;
            }

            if (c == '[')
            {
                tokens.Add(new Token(TokenType.OpenBracket, "[", i++));
                continue;
            }

            if (c == ']')
            {
                tokens.Add(new Token(TokenType.CloseBracket, "]", i++));
                continue;
            }

            if (c == '(')
            {
                tokens.Add(new Token(TokenType.OpenParen, "(", i++));
                continue;
            }

            if (c == ')')
            {
                tokens.Add(new Token(TokenType.CloseParen, ")", i++));
                continue;
            }

            if (c == '^')
            {
                tokens.Add(new Token(TokenType.Superscript, "^", i++));
                continue;
            }

            if (c == '_')
            {
                tokens.Add(new Token(TokenType.Subscript, "_", i++));
                continue;
            }

            if (c == '&')
            {
                tokens.Add(new Token(TokenType.Ampersand, "&", i++));
                continue;
            }

            if (char.IsDigit(c) || (c == '.' && i + 1 < len && char.IsDigit(formula[i + 1])))
            {
                int numStart = i;
                while (i < len && (char.IsDigit(formula[i]) || formula[i] == '.'))
                {
                    i++;
                }
                tokens.Add(new Token(TokenType.Number, formula[numStart..i], numStart));
                continue;
            }

            if (char.IsLetter(c))
            {
                tokens.Add(new Token(TokenType.Identifier, c.ToString(), i++));
                continue;
            }

            // Operators & Punctuation: +, -, =, <, >, *, /, !, ,, ;, :, |, ', ~
            tokens.Add(new Token(TokenType.Operator, c.ToString(), i++));
        }

        tokens.Add(new Token(TokenType.EOF, "", i));
        return tokens;
    }

    // =========================================================================
    // AST NODES DEFINITION
    // =========================================================================
    public record LayoutContext(double FontSize, string ColorHex, MathDisplayStyle DisplayStyle);
    public record RenderContext(double BaseFontSize, string TextColorHex, StringBuilder Svg, MathDisplayStyle DisplayStyle);

    public abstract class MathAstNode
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double Ascent { get; set; }
        public double Descent { get; set; }

        public abstract void Measure(LayoutContext ctx);
        public abstract void Render(double x, double y, RenderContext ctx);
    }

    public class MathSequenceNode : MathAstNode
    {
        public List<MathAstNode> Children { get; } = new();

        public override void Measure(LayoutContext ctx)
        {
            Width = 0;
            Ascent = 0;
            Descent = 0;

            foreach (var child in Children)
            {
                child.Measure(ctx);
                Width += child.Width;
                Ascent = Math.Max(Ascent, child.Ascent);
                Descent = Math.Max(Descent, child.Descent);
            }

            if (Children.Count == 0)
            {
                Ascent = ctx.FontSize * 0.75;
                Descent = ctx.FontSize * 0.25;
            }

            Height = Ascent + Descent;
        }

        public override void Render(double x, double y, RenderContext ctx)
        {
            double curX = x;
            foreach (var child in Children)
            {
                child.Render(curX, y, ctx);
                curX += child.Width;
            }
        }
    }

    public class MathTextNode : MathAstNode
    {
        public string Text { get; set; }
        public bool IsVariable { get; set; }
        public bool IsUpright { get; set; }
        public bool IsBold { get; set; }
        public string? ColorOverride { get; set; }

        public MathTextNode(string text, bool isVariable = false, bool isUpright = false, bool isBold = false)
        {
            Text = text;
            IsVariable = isVariable;
            IsUpright = isUpright;
            IsBold = isBold;
        }

        public override void Measure(LayoutContext ctx)
        {
            Ascent = ctx.FontSize * 0.78;
            Descent = ctx.FontSize * 0.22;
            Height = Ascent + Descent;

            // Character width calculation
            double charW = ctx.FontSize * 0.56;
            if (Text.Length == 1)
            {
                char c = Text[0];
                if (c is 'i' or 'l' or 'j' or 't' or '!' or ',' or ';' or '.') charW = ctx.FontSize * 0.32;
                else if (c is 'm' or 'w' or 'W' or 'M') charW = ctx.FontSize * 0.82;
                else if (char.IsUpper(c)) charW = ctx.FontSize * 0.68;
                else if (char.IsDigit(c)) charW = ctx.FontSize * 0.52;
                Width = charW;
            }
            else
            {
                Width = Text.Length * charW;
            }
        }

        public override void Render(double x, double y, RenderContext ctx)
        {
            string color = ColorOverride ?? ctx.TextColorHex;
            string cls = IsVariable ? "m-text m-var" : "m-text m-upright";
            string weight = IsBold ? "font-weight=\"bold\" " : "";
            string fontStyle = IsVariable ? "font-style=\"italic\" " : "";
            string fontSizeStr = ctx.BaseFontSize.ToString("F1", CultureInfo.InvariantCulture);

            ctx.Svg.AppendLine($"  <text x=\"{x.ToString("F1", CultureInfo.InvariantCulture)}\" y=\"{y.ToString("F1", CultureInfo.InvariantCulture)}\" fill=\"{color}\" font-size=\"{fontSizeStr}\" {weight}{fontStyle}class=\"{cls}\">{EscapeXml(Text)}</text>");
        }
    }

    public class MathSymbolNode : MathAstNode
    {
        public string Symbol { get; set; }
        public bool IsOperator { get; set; }
        public double ExtraSpacing { get; set; }

        public MathSymbolNode(string symbol, bool isOperator = true, double extraSpacing = 0)
        {
            Symbol = symbol;
            IsOperator = isOperator;
            ExtraSpacing = extraSpacing;
        }

        public override void Measure(LayoutContext ctx)
        {
            Ascent = ctx.FontSize * 0.8;
            Descent = ctx.FontSize * 0.2;
            Height = Ascent + Descent;

            double symW = ctx.FontSize * 0.6;
            if (Symbol is "+" or "-" or "=" or "<" or ">" or "≤" or "≥" or "≠" or "≈" or "≡" or "→" or "⇒" or "⟺" or "±")
            {
                symW = ctx.FontSize * 0.72;
                ExtraSpacing = ctx.FontSize * 0.18;
            }
            else if (Symbol is "·" or "∗" or "∘")
            {
                symW = ctx.FontSize * 0.45;
                ExtraSpacing = ctx.FontSize * 0.12;
            }

            Width = symW + (ExtraSpacing * 2);
        }

        public override void Render(double x, double y, RenderContext ctx)
        {
            double posX = x + ExtraSpacing;
            string fontSizeStr = ctx.BaseFontSize.ToString("F1", CultureInfo.InvariantCulture);
            ctx.Svg.AppendLine($"  <text x=\"{posX.ToString("F1", CultureInfo.InvariantCulture)}\" y=\"{y.ToString("F1", CultureInfo.InvariantCulture)}\" fill=\"{ctx.TextColorHex}\" font-size=\"{fontSizeStr}\" class=\"m-text m-upright\">{EscapeXml(Symbol)}</text>");
        }
    }

    public class MathFractionNode : MathAstNode
    {
        public MathAstNode Numerator { get; }
        public MathAstNode Denominator { get; }

        public MathFractionNode(MathAstNode numerator, MathAstNode denominator)
        {
            Numerator = numerator;
            Denominator = denominator;
        }

        public override void Measure(LayoutContext ctx)
        {
            double subScale = 0.82;
            var subCtx = new LayoutContext(ctx.FontSize * subScale, ctx.ColorHex, MathDisplayStyle.Inline);

            Numerator.Measure(subCtx);
            Denominator.Measure(subCtx);

            double maxContentW = Math.Max(Numerator.Width, Denominator.Width);
            Width = maxContentW + (ctx.FontSize * 0.35); // Margin around fraction bar

            double barGap = ctx.FontSize * 0.22;
            Ascent = Numerator.Height + barGap;
            Descent = Denominator.Height + barGap;
            Height = Ascent + Descent;
        }

        public override void Render(double x, double y, RenderContext ctx)
        {
            double subScale = 0.82;
            double childFontSize = ctx.BaseFontSize * subScale;
            var childCtx = new RenderContext(childFontSize, ctx.TextColorHex, ctx.Svg, MathDisplayStyle.Inline);

            double barY = y - (ctx.BaseFontSize * 0.28);
            double barGap = ctx.BaseFontSize * 0.22;

            // Render Numerator (centered horizontally above bar)
            double numX = x + ((Width - Numerator.Width) / 2);
            double numY = barY - barGap - Numerator.Descent;
            Numerator.Render(numX, numY, childCtx);

            // Draw Fraction Horizontal Bar Line
            double barThickness = Math.Max(1.0, ctx.BaseFontSize * 0.07);
            string x1 = x.ToString("F1", CultureInfo.InvariantCulture);
            string y1 = barY.ToString("F1", CultureInfo.InvariantCulture);
            string x2 = (x + Width).ToString("F1", CultureInfo.InvariantCulture);
            string thickStr = barThickness.ToString("F1", CultureInfo.InvariantCulture);

            ctx.Svg.AppendLine($"  <line x1=\"{x1}\" y1=\"{y1}\" x2=\"{x2}\" y2=\"{y1}\" stroke=\"{ctx.TextColorHex}\" stroke-width=\"{thickStr}\" stroke-linecap=\"round\" />");

            // Render Denominator (centered horizontally below bar)
            double denX = x + ((Width - Denominator.Width) / 2);
            double denY = barY + barGap + Denominator.Ascent;
            Denominator.Render(denX, denY, childCtx);
        }
    }

    public class MathRadicalNode : MathAstNode
    {
        public MathAstNode Radicand { get; }
        public MathAstNode? Degree { get; }

        public MathRadicalNode(MathAstNode radicand, MathAstNode? degree = null)
        {
            Radicand = radicand;
            Degree = degree;
        }

        public override void Measure(LayoutContext ctx)
        {
            Radicand.Measure(ctx);
            if (Degree != null)
            {
                var degCtx = new LayoutContext(ctx.FontSize * 0.6, ctx.ColorHex, MathDisplayStyle.Inline);
                Degree.Measure(degCtx);
            }

            double radicalSymbolW = ctx.FontSize * 0.65;
            double degW = Degree?.Width ?? 0;

            Width = radicalSymbolW + Radicand.Width + (ctx.FontSize * 0.15) + (degW > 0 ? degW * 0.4 : 0);
            Ascent = Radicand.Ascent + (ctx.FontSize * 0.25);
            Descent = Radicand.Descent + (ctx.FontSize * 0.08);
            Height = Ascent + Descent;
        }

        public override void Render(double x, double y, RenderContext ctx)
        {
            double degOffset = 0;
            if (Degree != null)
            {
                double degFontSize = ctx.BaseFontSize * 0.58;
                var degCtx = new RenderContext(degFontSize, ctx.TextColorHex, ctx.Svg, MathDisplayStyle.Inline);
                Degree.Render(x, y - (ctx.BaseFontSize * 0.5), degCtx);
                degOffset = Degree.Width * 0.4;
            }

            double radX = x + degOffset;
            double topY = y - Ascent + (ctx.BaseFontSize * 0.05);
            double bottomY = y + Descent;
            double radicandStartX = radX + (ctx.BaseFontSize * 0.6);
            double radicandEndX = x + Width;

            // Draw Radical √ Symbol Path + Overbar
            double lineThick = Math.Max(1.0, ctx.BaseFontSize * 0.065);
            double hookX = radX + (ctx.BaseFontSize * 0.12);
            double hookY = y - (ctx.BaseFontSize * 0.2);
            double cornerX = radX + (ctx.BaseFontSize * 0.28);

            string d = $"M {radX.ToString("F1", CultureInfo.InvariantCulture)},{hookY.ToString("F1", CultureInfo.InvariantCulture)} " +
                       $"L {cornerX.ToString("F1", CultureInfo.InvariantCulture)},{bottomY.ToString("F1", CultureInfo.InvariantCulture)} " +
                       $"L {radicandStartX.ToString("F1", CultureInfo.InvariantCulture)},{topY.ToString("F1", CultureInfo.InvariantCulture)} " +
                       $"L {radicandEndX.ToString("F1", CultureInfo.InvariantCulture)},{topY.ToString("F1", CultureInfo.InvariantCulture)}";

            ctx.Svg.AppendLine($"  <path d=\"{d}\" fill=\"none\" stroke=\"{ctx.TextColorHex}\" stroke-width=\"{lineThick.ToString("F1", CultureInfo.InvariantCulture)}\" stroke-linejoin=\"miter\" stroke-linecap=\"square\" />");

            // Render Radicand
            Radicand.Render(radicandStartX + (ctx.BaseFontSize * 0.08), y, ctx);
        }
    }

    public class MathSubSuperNode : MathAstNode
    {
        public MathAstNode BaseNode { get; }
        public MathAstNode? Subscript { get; }
        public MathAstNode? Superscript { get; }

        public MathSubSuperNode(MathAstNode baseNode, MathAstNode? sub = null, MathAstNode? sup = null)
        {
            BaseNode = baseNode;
            Subscript = sub;
            Superscript = sup;
        }

        public override void Measure(LayoutContext ctx)
        {
            BaseNode.Measure(ctx);

            double scriptScale = 0.72;
            var scriptCtx = new LayoutContext(ctx.FontSize * scriptScale, ctx.ColorHex, MathDisplayStyle.Inline);

            double scriptWidth = 0;
            if (Superscript != null)
            {
                Superscript.Measure(scriptCtx);
                scriptWidth = Math.Max(scriptWidth, Superscript.Width);
            }
            if (Subscript != null)
            {
                Subscript.Measure(scriptCtx);
                scriptWidth = Math.Max(scriptWidth, Subscript.Width);
            }

            Width = BaseNode.Width + scriptWidth + (ctx.FontSize * 0.06);

            double supAscent = Superscript != null ? (ctx.FontSize * 0.45) + Superscript.Ascent : 0;
            double subDescent = Subscript != null ? (ctx.FontSize * 0.35) + Subscript.Descent : 0;

            Ascent = Math.Max(BaseNode.Ascent, supAscent);
            Descent = Math.Max(BaseNode.Descent, subDescent);
            Height = Ascent + Descent;
        }

        public override void Render(double x, double y, RenderContext ctx)
        {
            BaseNode.Render(x, y, ctx);

            double scriptX = x + BaseNode.Width + (ctx.BaseFontSize * 0.04);
            double scriptFontSize = ctx.BaseFontSize * 0.72;
            var scriptCtx = new RenderContext(scriptFontSize, ctx.TextColorHex, ctx.Svg, MathDisplayStyle.Inline);

            if (Superscript != null)
            {
                double supY = y - (ctx.BaseFontSize * 0.42);
                Superscript.Render(scriptX, supY, scriptCtx);
            }

            if (Subscript != null)
            {
                double subY = y + (ctx.BaseFontSize * 0.28);
                Subscript.Render(scriptX, subY, scriptCtx);
            }
        }
    }

    public class MathLargeOperatorNode : MathAstNode
    {
        public string OperatorName { get; }
        public MathAstNode? LowerLimit { get; }
        public MathAstNode? UpperLimit { get; }
        public bool IsDisplayMode { get; }

        public MathLargeOperatorNode(string opName, MathAstNode? lower = null, MathAstNode? upper = null, bool isDisplay = true)
        {
            OperatorName = opName;
            LowerLimit = lower;
            UpperLimit = upper;
            IsDisplayMode = isDisplay;
        }

        public override void Measure(LayoutContext ctx)
        {
            double opScale = IsDisplayMode ? 1.4 : 1.1;
            double opW = ctx.FontSize * (OperatorName.Contains("int") ? 0.75 : 1.1) * opScale;

            double scriptScale = 0.68;
            var scriptCtx = new LayoutContext(ctx.FontSize * scriptScale, ctx.ColorHex, MathDisplayStyle.Inline);

            LowerLimit?.Measure(scriptCtx);
            UpperLimit?.Measure(scriptCtx);

            bool isIntegral = OperatorName.Contains("int");

            if (isIntegral || !IsDisplayMode)
            {
                // Side limits
                double limitsW = Math.Max(LowerLimit?.Width ?? 0, UpperLimit?.Width ?? 0);
                Width = opW + limitsW + (ctx.FontSize * 0.1);
                Ascent = (ctx.FontSize * opScale * 0.8) + (UpperLimit != null ? UpperLimit.Height * 0.4 : 0);
                Descent = (ctx.FontSize * opScale * 0.35) + (LowerLimit != null ? LowerLimit.Height * 0.4 : 0);
            }
            else
            {
                // Top/bottom centered limits (Sum, Prod, Lim)
                double maxW = Math.Max(opW, Math.Max(LowerLimit?.Width ?? 0, UpperLimit?.Width ?? 0));
                Width = maxW + (ctx.FontSize * 0.15);
                Ascent = (ctx.FontSize * opScale * 0.75) + (UpperLimit?.Height ?? 0) + (ctx.FontSize * 0.12);
                Descent = (ctx.FontSize * opScale * 0.3) + (LowerLimit?.Height ?? 0) + (ctx.FontSize * 0.12);
            }

            Height = Ascent + Descent;
        }

        public override void Render(double x, double y, RenderContext ctx)
        {
            double opScale = IsDisplayMode ? 1.4 : 1.1;
            double opFontSize = ctx.BaseFontSize * opScale;
            double scriptFontSize = ctx.BaseFontSize * 0.68;
            var scriptCtx = new RenderContext(scriptFontSize, ctx.TextColorHex, ctx.Svg, MathDisplayStyle.Inline);

            bool isIntegral = OperatorName.Contains("int");
            string sym = OperatorName switch
            {
                "sum" => "∑",
                "prod" => "∏",
                "coprod" => "∐",
                "int" => "∫",
                "iint" => "∬",
                "iiint" => "∭",
                "oint" => "∮",
                "lim" => "lim",
                _ => "∑"
            };

            if (isIntegral || !IsDisplayMode)
            {
                double opW = ctx.BaseFontSize * 0.75 * opScale;
                // Render Integral / Operator symbol
                ctx.Svg.AppendLine($"  <text x=\"{x.ToString("F1", CultureInfo.InvariantCulture)}\" y=\"{y.ToString("F1", CultureInfo.InvariantCulture)}\" fill=\"{ctx.TextColorHex}\" font-size=\"{opFontSize.ToString("F1", CultureInfo.InvariantCulture)}\" class=\"m-text m-upright\">{sym}</text>");

                double limitX = x + opW + (ctx.BaseFontSize * 0.05);
                if (UpperLimit != null)
                {
                    double upY = y - (ctx.BaseFontSize * opScale * 0.55);
                    UpperLimit.Render(limitX, upY, scriptCtx);
                }
                if (LowerLimit != null)
                {
                    double lowY = y + (ctx.BaseFontSize * opScale * 0.35);
                    LowerLimit.Render(limitX, lowY, scriptCtx);
                }
            }
            else
            {
                // Centered stacked limits (for \sum, \prod, \lim)
                double opW = sym == "lim" ? ctx.BaseFontSize * 1.3 : ctx.BaseFontSize * 1.0 * opScale;
                double opX = x + ((Width - opW) / 2);

                if (UpperLimit != null)
                {
                    double upX = x + ((Width - UpperLimit.Width) / 2);
                    double upY = y - (ctx.BaseFontSize * opScale * 0.65) - UpperLimit.Descent;
                    UpperLimit.Render(upX, upY, scriptCtx);
                }

                ctx.Svg.AppendLine($"  <text x=\"{opX.ToString("F1", CultureInfo.InvariantCulture)}\" y=\"{y.ToString("F1", CultureInfo.InvariantCulture)}\" fill=\"{ctx.TextColorHex}\" font-size=\"{opFontSize.ToString("F1", CultureInfo.InvariantCulture)}\" class=\"m-text m-upright\">{sym}</text>");

                if (LowerLimit != null)
                {
                    double lowX = x + ((Width - LowerLimit.Width) / 2);
                    double lowY = y + (ctx.BaseFontSize * opScale * 0.3) + LowerLimit.Ascent;
                    LowerLimit.Render(lowX, lowY, scriptCtx);
                }
            }
        }
    }

    public class MathDelimiterNode : MathAstNode
    {
        public string LeftDelim { get; }
        public string RightDelim { get; }
        public MathAstNode Content { get; }

        public MathDelimiterNode(string left, string right, MathAstNode content)
        {
            LeftDelim = left;
            RightDelim = right;
            Content = content;
        }

        public override void Measure(LayoutContext ctx)
        {
            Content.Measure(ctx);

            double delimW = ctx.FontSize * 0.45;
            if (LeftDelim == "." || string.IsNullOrEmpty(LeftDelim)) delimW = 0;
            double rightDelimW = ctx.FontSize * 0.45;
            if (RightDelim == "." || string.IsNullOrEmpty(RightDelim)) rightDelimW = 0;

            Width = delimW + Content.Width + rightDelimW;
            Ascent = Content.Ascent;
            Descent = Content.Descent;
            Height = Content.Height;
        }

        public override void Render(double x, double y, RenderContext ctx)
        {
            double curX = x;
            double delimScale = Math.Max(1.0, Content.Height / (ctx.BaseFontSize * 1.1));

            if (LeftDelim != "." && !string.IsNullOrEmpty(LeftDelim))
            {
                RenderSingleDelimiter(LeftDelim, curX, y, Content.Ascent, Content.Descent, delimScale, ctx);
                curX += ctx.BaseFontSize * 0.45;
            }

            Content.Render(curX, y, ctx);
            curX += Content.Width;

            if (RightDelim != "." && !string.IsNullOrEmpty(RightDelim))
            {
                RenderSingleDelimiter(RightDelim, curX, y, Content.Ascent, Content.Descent, delimScale, ctx);
            }
        }

        private static void RenderSingleDelimiter(string delim, double x, double y, double ascent, double descent, double scale, RenderContext ctx)
        {
            double fontSize = ctx.BaseFontSize * scale;
            string str = delim switch
            {
                "(" => "(",
                ")" => ")",
                "[" => "[",
                "]" => "]",
                "{" or "\\{" => "{",
                "}" or "\\}" => "}",
                "|" or "\\|" => "|",
                "\\langle" or "langle" => "⟨",
                "\\rangle" or "rangle" => "⟩",
                "\\lceil" or "lceil" => "⌈",
                "\\rceil" or "rceil" => "⌉",
                "\\lfloor" or "lfloor" => "⌊",
                "\\rfloor" or "rfloor" => "⌋",
                _ => delim
            };

            double adjustY = y + ((descent - ascent) * 0.1);
            ctx.Svg.AppendLine($"  <text x=\"{x.ToString("F1", CultureInfo.InvariantCulture)}\" y=\"{adjustY.ToString("F1", CultureInfo.InvariantCulture)}\" fill=\"{ctx.TextColorHex}\" font-size=\"{fontSize.ToString("F1", CultureInfo.InvariantCulture)}\" class=\"m-text m-upright\">{EscapeXml(str)}</text>");
        }
    }

    public class MathMatrixNode : MathAstNode
    {
        public List<List<MathAstNode>> Rows { get; } = new();
        public string MatrixType { get; }

        public MathMatrixNode(string matrixType)
        {
            MatrixType = matrixType;
        }

        private List<double> _colWidths = new();
        private List<double> _rowHeights = new();
        private List<double> _rowAscents = new();

        public override void Measure(LayoutContext ctx)
        {
            _colWidths.Clear();
            _rowHeights.Clear();
            _rowAscents.Clear();

            int numCols = 0;
            foreach (var row in Rows)
            {
                numCols = Math.Max(numCols, row.Count);
            }

            for (int c = 0; c < numCols; c++) _colWidths.Add(0);

            foreach (var row in Rows)
            {
                double rowAsc = 0;
                double rowDesc = 0;

                for (int c = 0; c < row.Count; c++)
                {
                    row[c].Measure(ctx);
                    _colWidths[c] = Math.Max(_colWidths[c], row[c].Width);
                    rowAsc = Math.Max(rowAsc, row[c].Ascent);
                    rowDesc = Math.Max(rowDesc, row[c].Descent);
                }

                _rowAscents.Add(rowAsc);
                _rowHeights.Add(rowAsc + rowDesc + (ctx.FontSize * 0.3));
            }

            double totalW = _colWidths.Sum() + (Math.Max(0, numCols - 1) * ctx.FontSize * 0.5);
            double delimW = MatrixType switch
            {
                "matrix" => 0,
                "cases" => ctx.FontSize * 0.6,
                _ => ctx.FontSize * 0.8
            };

            Width = totalW + delimW;
            double totalH = _rowHeights.Sum();
            Ascent = totalH / 2;
            Descent = totalH / 2;
            Height = totalH;
        }

        public override void Render(double x, double y, RenderContext ctx)
        {
            double startX = x + (MatrixType == "cases" ? ctx.BaseFontSize * 0.6 : (MatrixType != "matrix" ? ctx.BaseFontSize * 0.4 : 0));
            double curY = y - Ascent + (_rowAscents.FirstOrDefault());

            // Draw Left Bracket/Delim
            if (MatrixType == "pmatrix")
            {
                ctx.Svg.AppendLine($"  <text x=\"{x.ToString("F1", CultureInfo.InvariantCulture)}\" y=\"{y.ToString("F1", CultureInfo.InvariantCulture)}\" fill=\"{ctx.TextColorHex}\" font-size=\"{(Height * 0.95).ToString("F1", CultureInfo.InvariantCulture)}\" class=\"m-text m-upright\">(</text>");
                ctx.Svg.AppendLine($"  <text x=\"{(x + Width - (ctx.BaseFontSize * 0.35)).ToString("F1", CultureInfo.InvariantCulture)}\" y=\"{y.ToString("F1", CultureInfo.InvariantCulture)}\" fill=\"{ctx.TextColorHex}\" font-size=\"{(Height * 0.95).ToString("F1", CultureInfo.InvariantCulture)}\" class=\"m-text m-upright\">)</text>");
            }
            else if (MatrixType == "bmatrix")
            {
                ctx.Svg.AppendLine($"  <text x=\"{x.ToString("F1", CultureInfo.InvariantCulture)}\" y=\"{y.ToString("F1", CultureInfo.InvariantCulture)}\" fill=\"{ctx.TextColorHex}\" font-size=\"{(Height * 0.95).ToString("F1", CultureInfo.InvariantCulture)}\" class=\"m-text m-upright\">[</text>");
                ctx.Svg.AppendLine($"  <text x=\"{(x + Width - (ctx.BaseFontSize * 0.35)).ToString("F1", CultureInfo.InvariantCulture)}\" y=\"{y.ToString("F1", CultureInfo.InvariantCulture)}\" fill=\"{ctx.TextColorHex}\" font-size=\"{(Height * 0.95).ToString("F1", CultureInfo.InvariantCulture)}\" class=\"m-text m-upright\">]</text>");
            }
            else if (MatrixType == "vmatrix")
            {
                double lineThick = Math.Max(1.0, ctx.BaseFontSize * 0.08);
                double y1 = (y - Ascent).ToString("F1", CultureInfo.InvariantCulture) == "0.0" ? 0 : y - Ascent;
                double y2 = y + Descent;
                ctx.Svg.AppendLine($"  <line x1=\"{x.ToString("F1", CultureInfo.InvariantCulture)}\" y1=\"{y1.ToString("F1", CultureInfo.InvariantCulture)}\" x2=\"{x.ToString("F1", CultureInfo.InvariantCulture)}\" y2=\"{y2.ToString("F1", CultureInfo.InvariantCulture)}\" stroke=\"{ctx.TextColorHex}\" stroke-width=\"{lineThick.ToString("F1", CultureInfo.InvariantCulture)}\" />");
                ctx.Svg.AppendLine($"  <line x1=\"{(x + Width).ToString("F1", CultureInfo.InvariantCulture)}\" y1=\"{y1.ToString("F1", CultureInfo.InvariantCulture)}\" x2=\"{(x + Width).ToString("F1", CultureInfo.InvariantCulture)}\" y2=\"{y2.ToString("F1", CultureInfo.InvariantCulture)}\" stroke=\"{ctx.TextColorHex}\" stroke-width=\"{lineThick.ToString("F1", CultureInfo.InvariantCulture)}\" />");
            }
            else if (MatrixType == "cases")
            {
                ctx.Svg.AppendLine($"  <text x=\"{x.ToString("F1", CultureInfo.InvariantCulture)}\" y=\"{y.ToString("F1", CultureInfo.InvariantCulture)}\" fill=\"{ctx.TextColorHex}\" font-size=\"{(Height * 0.95).ToString("F1", CultureInfo.InvariantCulture)}\" class=\"m-text m-upright\">{{</text>");
            }

            for (int r = 0; r < Rows.Count; r++)
            {
                var row = Rows[r];
                double colX = startX;

                for (int c = 0; c < row.Count; c++)
                {
                    double cellW = _colWidths[c];
                    double alignX = colX + ((cellW - row[c].Width) / 2); // Center cell
                    row[c].Render(alignX, curY, ctx);
                    colX += cellW + (ctx.BaseFontSize * 0.5);
                }

                if (r + 1 < Rows.Count)
                {
                    curY += _rowHeights[r];
                }
            }
        }
    }

    public class MathAccentNode : MathAstNode
    {
        public MathAstNode BaseNode { get; }
        public string AccentType { get; }

        public MathAccentNode(MathAstNode baseNode, string accentType)
        {
            BaseNode = baseNode;
            AccentType = accentType;
        }

        public override void Measure(LayoutContext ctx)
        {
            BaseNode.Measure(ctx);
            Width = BaseNode.Width;
            Ascent = BaseNode.Ascent + (ctx.FontSize * 0.28);
            Descent = BaseNode.Descent;
            Height = Ascent + Descent;
        }

        public override void Render(double x, double y, RenderContext ctx)
        {
            BaseNode.Render(x, y, ctx);

            double accentX = x + (BaseNode.Width / 2);
            double accentY = y - BaseNode.Ascent - (ctx.BaseFontSize * 0.05);

            string accentSym = AccentType switch
            {
                "hat" => "^",
                "bar" or "overline" => "¯",
                "vec" => "→",
                "dot" => "˙",
                "ddot" => "¨",
                "tilde" => "~",
                _ => "^"
            };

            double accentSize = ctx.BaseFontSize * (AccentType is "dot" or "ddot" ? 1.1 : 0.85);
            ctx.Svg.AppendLine($"  <text x=\"{accentX.ToString("F1", CultureInfo.InvariantCulture)}\" y=\"{accentY.ToString("F1", CultureInfo.InvariantCulture)}\" text-anchor=\"middle\" fill=\"{ctx.TextColorHex}\" font-size=\"{accentSize.ToString("F1", CultureInfo.InvariantCulture)}\" class=\"m-text m-upright\">{accentSym}</text>");
        }
    }

    public class MathSpaceNode : MathAstNode
    {
        public double SpaceFactor { get; }

        public MathSpaceNode(double spaceFactor = 0.3)
        {
            SpaceFactor = spaceFactor;
        }

        public override void Measure(LayoutContext ctx)
        {
            Width = ctx.FontSize * SpaceFactor;
            Ascent = ctx.FontSize * 0.5;
            Descent = 0;
            Height = Ascent;
        }

        public override void Render(double x, double y, RenderContext ctx)
        {
            // Empty space, no rendering required
        }
    }

    // =========================================================================
    // PARSER
    // =========================================================================
    public class MathParser
    {
        private readonly List<Token> _tokens;
        private int _pos;

        public MathParser(List<Token> tokens)
        {
            _tokens = tokens;
            _pos = 0;
        }

        private Token Current => _pos < _tokens.Count ? _tokens[_pos] : _tokens[^1];
        private Token Peek(int offset = 1) => _pos + offset < _tokens.Count ? _tokens[_pos + offset] : _tokens[^1];

        private Token Consume()
        {
            var t = Current;
            _pos++;
            return t;
        }

        private bool Match(TokenType type)
        {
            if (Current.Type == type)
            {
                _pos++;
                return true;
            }
            return false;
        }

        private bool MatchCommand(string cmd)
        {
            if (Current.Type == TokenType.Command && Current.Value.Equals(cmd, StringComparison.OrdinalIgnoreCase))
            {
                _pos++;
                return true;
            }
            return false;
        }

        public MathAstNode ParseExpression()
        {
            var seq = new MathSequenceNode();

            while (Current.Type != TokenType.EOF &&
                   Current.Type != TokenType.CloseBrace &&
                   Current.Type != TokenType.CloseBracket &&
                   Current.Type != TokenType.Ampersand &&
                   Current.Type != TokenType.Newline &&
                   !(Current.Type == TokenType.Command && Current.Value.Equals("end", StringComparison.OrdinalIgnoreCase)))
            {
                int beforePos = _pos;
                var atom = ParseAtom();
                if (atom != null)
                {
                    // Check for trailing subscript / superscript (e.g. x_1^2 or x^2_1)
                    atom = ParseSubSuper(atom);
                    seq.Children.Add(atom);
                }
                else if (_pos == beforePos)
                {
                    // Skip unrecognized token to avoid infinite loop
                    Consume();
                }
            }

            if (seq.Children.Count == 1)
            {
                return seq.Children[0];
            }

            return seq;
        }

        private MathAstNode? ParseAtom()
        {
            var token = Current;

            // 1. Grouped Expression { ... }
            if (Match(TokenType.OpenBrace))
            {
                var group = ParseExpression();
                Match(TokenType.CloseBrace);
                return group;
            }

            // 2. Parenthesized ( ... )
            if (Match(TokenType.OpenParen))
            {
                return new MathSymbolNode("(", false);
            }
            if (Match(TokenType.CloseParen))
            {
                return new MathSymbolNode(")", false);
            }

            // 3. Brackets [ ... ]
            if (Match(TokenType.OpenBracket))
            {
                return new MathSymbolNode("[", false);
            }
            if (Match(TokenType.CloseBracket))
            {
                return new MathSymbolNode("]", false);
            }

            // 4. Number
            if (Match(TokenType.Number))
            {
                return new MathTextNode(token.Value, isVariable: false, isUpright: true);
            }

            // 5. Single Variable Letter
            if (Match(TokenType.Identifier))
            {
                return new MathTextNode(token.Value, isVariable: true);
            }

            // 6. Operator
            if (Match(TokenType.Operator))
            {
                return new MathSymbolNode(token.Value, isOperator: true);
            }

            // 7. LaTeX Commands
            if (Current.Type == TokenType.Command)
            {
                return ParseCommand();
            }

            return null;
        }

        private MathAstNode ParseCommand()
        {
            var cmdToken = Consume();
            string cmd = cmdToken.Value;

            // Fractions: \frac{a}{b}, \dfrac, \tfrac
            if (cmd is "frac" or "dfrac" or "tfrac")
            {
                var num = ParseRequiredGroup();
                var den = ParseRequiredGroup();
                return new MathFractionNode(num, den);
            }

            // Binomial: \binom{n}{k}
            if (cmd is "binom" or "dbinom" or "tbinom")
            {
                var top = ParseRequiredGroup();
                var bot = ParseRequiredGroup();
                var frac = new MathFractionNode(top, bot);
                return new MathDelimiterNode("(", ")", frac);
            }

            // Radicals: \sqrt{x}, \sqrt[n]{x}
            if (cmd is "sqrt")
            {
                MathAstNode? degree = null;
                if (Match(TokenType.OpenBracket))
                {
                    degree = ParseExpression();
                    Match(TokenType.CloseBracket);
                }
                var radicand = ParseRequiredGroup();
                return new MathRadicalNode(radicand, degree);
            }

            // Large Operators: \sum, \prod, \int, \iint, \iiint, \oint, \coprod, \lim
            if (cmd is "sum" or "prod" or "int" or "iint" or "iiint" or "oint" or "coprod" or "lim")
            {
                MathAstNode? lower = null;
                MathAstNode? upper = null;

                while (Current.Type is TokenType.Subscript or TokenType.Superscript)
                {
                    if (Match(TokenType.Subscript))
                    {
                        lower = ParseRequiredGroup();
                    }
                    else if (Match(TokenType.Superscript))
                    {
                        upper = ParseRequiredGroup();
                    }
                }

                return new MathLargeOperatorNode(cmd, lower, upper, isDisplay: true);
            }

            // Dynamic Delimiters: \left( ... \right), \left[ ... \right]
            if (cmd is "left")
            {
                string leftDelim = Current.Value;
                Consume();

                var content = ParseExpression();

                string rightDelim = ")";
                if (MatchCommand("right"))
                {
                    rightDelim = Current.Value;
                    Consume();
                }

                return new MathDelimiterNode(leftDelim, rightDelim, content);
            }

            // Matrices & Environments: \begin{matrix}, \begin{pmatrix}, \begin{cases}
            if (cmd is "begin")
            {
                string envType = "matrix";
                if (Match(TokenType.OpenBrace))
                {
                    envType = Current.Value;
                    Consume();
                    Match(TokenType.CloseBrace);
                }

                return ParseMatrixEnvironment(envType);
            }

            // Accents: \hat{x}, \bar{x}, \vec{x}, \dot{x}, \ddot{x}, \tilde{x}
            if (cmd is "hat" or "bar" or "vec" or "dot" or "ddot" or "tilde" or "overline")
            {
                var baseNode = ParseRequiredGroup();
                return new MathAccentNode(baseNode, cmd);
            }

            // Text & Font Styles: \text{...}, \mathrm{...}, \mathbf{...}, \mathbb{...}, \mathcal{...}
            if (cmd is "text" or "mathrm" or "mathbf" or "mathit" or "mathsf" or "mathtt")
            {
                var inner = ParseRequiredGroup();
                return inner;
            }

            if (cmd is "mathbb")
            {
                var inner = ParseRequiredGroup();
                if (inner is MathTextNode textNode && _blackboardBold.TryGetValue(textNode.Text, out var bbSym))
                {
                    return new MathSymbolNode(bbSym, isOperator: false);
                }
                return inner;
            }

            if (cmd is "mathcal")
            {
                var inner = ParseRequiredGroup();
                if (inner is MathTextNode textNode && _calligraphic.TryGetValue(textNode.Text, out var calSym))
                {
                    return new MathSymbolNode(calSym, isOperator: false);
                }
                return inner;
            }

            // Standard Functions: \sin, \cos, \tan, \ln, \log, \exp, \det, \dim, \ker
            if (_standardFunctions.Contains(cmd))
            {
                return new MathTextNode(cmd, isVariable: false, isUpright: true);
            }

            // Greek Alphabet
            if (_greekSymbols.TryGetValue(cmd, out var greek))
            {
                return new MathSymbolNode(greek.unicode, greek.isOperator);
            }

            // Mathematical Symbols & Operators
            if (_mathSymbols.TryGetValue(cmd, out var mathSym))
            {
                return new MathSymbolNode(mathSym, isOperator: true);
            }

            // Spacing: \quad, \qquad, \,, \:, \;, \!
            if (cmd is "quad") return new MathSpaceNode(1.0);
            if (cmd is "qquad") return new MathSpaceNode(2.0);
            if (cmd is "\\," or ",") return new MathSpaceNode(0.2);
            if (cmd is "\\:" or ":") return new MathSpaceNode(0.3);
            if (cmd is "\\;" or ";") return new MathSpaceNode(0.4);
            if (cmd is "\\!" or "!") return new MathSpaceNode(-0.15);

            // Fallback for custom or unrecognized commands: render text
            return new MathTextNode(cmd, isVariable: false, isUpright: true);
        }

        private MathAstNode ParseMatrixEnvironment(string envType)
        {
            var matrixNode = new MathMatrixNode(envType);
            var currentRow = new List<MathAstNode>();

            while (Current.Type != TokenType.EOF)
            {
                if (Current.Type == TokenType.Command && Current.Value.Equals("end", StringComparison.OrdinalIgnoreCase))
                {
                    Consume();
                    if (Match(TokenType.OpenBrace))
                    {
                        Consume(); // Consume env name
                        Match(TokenType.CloseBrace);
                    }
                    break;
                }

                if (Match(TokenType.Ampersand))
                {
                    // Next column
                    continue;
                }

                if (Match(TokenType.Newline))
                {
                    // Next row
                    if (currentRow.Count > 0)
                    {
                        matrixNode.Rows.Add(currentRow);
                        currentRow = new List<MathAstNode>();
                    }
                    continue;
                }

                int beforePos = _pos;
                var cellExpr = ParseExpression();
                if (cellExpr != null)
                {
                    currentRow.Add(cellExpr);
                }
                if (_pos == beforePos && Current.Type != TokenType.EOF)
                {
                    Consume();
                }
            }

            if (currentRow.Count > 0)
            {
                matrixNode.Rows.Add(currentRow);
            }

            return matrixNode;
        }

        private MathAstNode ParseSubSuper(MathAstNode baseNode)
        {
            MathAstNode? sub = null;
            MathAstNode? sup = null;

            while (Current.Type is TokenType.Subscript or TokenType.Superscript)
            {
                if (Match(TokenType.Subscript))
                {
                    sub = ParseRequiredGroup();
                }
                else if (Match(TokenType.Superscript))
                {
                    sup = ParseRequiredGroup();
                }
            }

            if (sub != null || sup != null)
            {
                return new MathSubSuperNode(baseNode, sub, sup);
            }

            return baseNode;
        }

        private MathAstNode ParseRequiredGroup()
        {
            if (Match(TokenType.OpenBrace))
            {
                var group = ParseExpression();
                Match(TokenType.CloseBrace);
                return group;
            }

            var atom = ParseAtom();
            return atom ?? new MathTextNode("", false);
        }
    }
}
