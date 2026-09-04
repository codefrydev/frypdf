using System;
using System.Collections.Generic;

namespace PdfEditorApp.Services;

/// <summary>
/// Built-in vector ornaments, ceremonial emblems, wedding garlands, and luxury invitation assets.
/// </summary>
public static class SvgOrnamentLibrary
{
    public static readonly Dictionary<string, string> Presets = new(StringComparer.OrdinalIgnoreCase)
    {
        { "GaneshaCrest", GetGaneshaCrestSvg() },
        { "MarigoldToran", GetMarigoldToranSvg() },
        { "TraditionalDeepam", GetTraditionalDeepamSvg() },
        { "HangingDiyas", GetHangingDiyasSvg() },
        { "DottedFloralDivider", GetDottedFloralDividerSvg() },
        { "MandapArch", GetMandapArchSvg() },
        { "PlantainTrees", GetPlantainTreesSvg() },
        { "AuspiciousKalash", GetAuspiciousKalashSvg() },
        { "BotanicalWreath", GetBotanicalWreathSvg() },
        { "ArtDecoFrame", GetArtDecoFrameSvg() },
        { "CalligraphicFlourish", GetCalligraphicFlourishSvg() },
        { "OmCrest", GetOmCrestSvg() },
        { "TriangularPhaseCycle", GetTriangularPhaseCycleSvg() },
        { "Triangular Phase Cycle", GetTriangularPhaseCycleSvg() },
        { "ParticleArrangementGrid", GetParticleArrangementGridSvg() },
        { "ParticleArrangement", GetParticleArrangementGridSvg() },
        { "Particle Arrangement", GetParticleArrangementGridSvg() },
        { "QuadrilateralSetDiagram", GetQuadrilateralSetDiagramSvg() },
        { "Quadrilaterals Geometry Diagram", GetQuadrilateralSetDiagramSvg() },
        { "QuadrilateralsGeometryDiagram", GetQuadrilateralSetDiagramSvg() }
    };

    public static string GetSvg(string? presetName, string? tintHex = null)
    {
        if (string.IsNullOrWhiteSpace(presetName) ||
            (!Presets.TryGetValue(presetName, out var svg) &&
             !Presets.TryGetValue(presetName.Replace(" ", "").Replace("-", "").Replace("_", ""), out svg)))
        {
            svg = GetGaneshaCrestSvg();
        }

        if (!string.IsNullOrWhiteSpace(tintHex))
        {
            // Apply tint to main strokes and fills if requested
            svg = svg.Replace("currentColor", tintHex);
        }

        return svg;
    }

    /// <summary>
    /// Sacred Lord Ganesha auspicious line-art crest for wedding invitations.
    /// </summary>
    public static string GetGaneshaCrestSvg(string colorHex = "#8B0000", string goldHex = "#D97706")
    {
        return $@"<svg viewBox=""0 0 200 220"" xmlns=""http://www.w3.org/2000/svg"">
  <!-- Ganesha Aura / Tilak Halo -->
  <circle cx=""100"" cy=""105"" r=""75"" fill=""none"" stroke=""{goldHex}"" stroke-width=""1.5"" stroke-dasharray=""3,3"" opacity=""0.6"" />
  
  <!-- Crown / Mukut -->
  <path d=""M 85,25 L 100,5 L 115,25 L 108,40 L 92,40 Z"" fill=""{goldHex}"" stroke=""{colorHex}"" stroke-width=""1.5"" />
  <circle cx=""100"" cy=""18"" r=""3"" fill=""{colorHex}"" />
  <path d=""M 90,40 Q 100,45 110,40"" fill=""none"" stroke=""{colorHex}"" stroke-width=""2"" />

  <!-- Sacred Tilak / Trishul on Forehead -->
  <path d=""M 100,42 L 100,68"" stroke=""{colorHex}"" stroke-width=""3"" stroke-linecap=""round"" />
  <path d=""M 93,48 Q 93,64 100,66 Q 107,64 107,48"" fill=""none"" stroke=""{goldHex}"" stroke-width=""2"" />
  <circle cx=""100"" cy=""55"" r=""2.5"" fill=""{colorHex}"" />

  <!-- Ears (Left & Right) -->
  <!-- Left Ear (Karn) -->
  <path d=""M 85,55 C 50,45 40,75 55,100 C 65,115 80,120 90,125"" fill=""none"" stroke=""{colorHex}"" stroke-width=""3"" stroke-linecap=""round"" />
  <path d=""M 65,70 C 58,80 58,95 68,105"" fill=""none"" stroke=""{goldHex}"" stroke-width=""1.5"" />
  
  <!-- Right Ear -->
  <path d=""M 115,55 C 150,45 160,75 145,100 C 135,115 120,120 110,125"" fill=""none"" stroke=""{colorHex}"" stroke-width=""3"" stroke-linecap=""round"" />
  <path d=""M 135,70 C 142,80 142,95 132,105"" fill=""none"" stroke=""{goldHex}"" stroke-width=""1.5"" />

  <!-- Head Outline & Eyes -->
  <ellipse cx=""88"" cy=""82"" rx=""2.5"" ry=""4"" fill=""{colorHex}"" transform=""rotate(-10 88 82)"" />
  <ellipse cx=""112"" cy=""82"" rx=""2.5"" ry=""4"" fill=""{colorHex}"" transform=""rotate(10 112 82)"" />
  <path d=""M 80,75 Q 88,72 94,76"" fill=""none"" stroke=""{colorHex}"" stroke-width=""1.5"" />
  <path d=""M 120,75 Q 112,72 106,76"" fill=""none"" stroke=""{colorHex}"" stroke-width=""1.5"" />

  <!-- Trunk (Vakratunda) with Modak holding tip -->
  <path d=""M 96,85 C 92,110 85,135 105,155 C 120,170 145,160 140,140 C 136,125 118,130 115,142"" fill=""none"" stroke=""{colorHex}"" stroke-width=""3.5"" stroke-linecap=""round"" />
  <path d=""M 98,100 Q 106,120 116,132"" fill=""none"" stroke=""{goldHex}"" stroke-width=""1.5"" />
  
  <!-- Single Tusk (Ekdanta) -->
  <path d=""M 86,105 L 75,112 L 86,114 Z"" fill=""{goldHex}"" stroke=""{colorHex}"" stroke-width=""1"" />

  <!-- Sweet Modak / Ladoo in Hand -->
  <circle cx=""138"" cy=""138"" r=""6"" fill=""{goldHex}"" stroke=""{colorHex}"" stroke-width=""1"" />
  <path d=""M 134,136 Q 138,130 142,136"" fill=""none"" stroke=""{colorHex}"" stroke-width=""1"" />

  <!-- Bottom Lotus Petal Base -->
  <path d=""M 50,185 Q 100,205 150,185 Q 100,195 50,185 Z"" fill=""{goldHex}"" stroke=""{colorHex}"" stroke-width=""1.5"" />
  <path d=""M 65,180 Q 100,198 135,180"" fill=""none"" stroke=""{colorHex}"" stroke-width=""1.5"" />
</svg>";
    }

    /// <summary>
    /// Festive Marigold Toran Garland with mango leaves and hanging brass lamps.
    /// </summary>
    public static string GetMarigoldToranSvg(string orange = "#EA580C", string yellow = "#FBBF24", string green = "#15803D", string gold = "#D97706")
    {
        return $@"<svg viewBox=""0 0 1000 120"" xmlns=""http://www.w3.org/2000/svg"">
  <!-- Top Rope / String -->
  <path d=""M 0,15 Q 250,22 500,15 Q 750,22 1000,15"" fill=""none"" stroke=""{gold}"" stroke-width=""3"" />

  <!-- Mango Leaves (Pachai Maavilai) Array -->
  <g fill=""{green}"" stroke=""#14532D"" stroke-width=""1"">
    <path d=""M 50,15 Q 55,45 50,60 Q 45,45 50,15 Z"" />
    <path d=""M 150,15 Q 155,45 150,60 Q 145,45 150,15 Z"" />
    <path d=""M 250,15 Q 255,45 250,60 Q 245,45 250,15 Z"" />
    <path d=""M 350,15 Q 355,45 350,60 Q 345,45 350,15 Z"" />
    <path d=""M 450,15 Q 455,45 450,60 Q 445,45 450,15 Z"" />
    <path d=""M 550,15 Q 555,45 550,60 Q 545,45 550,15 Z"" />
    <path d=""M 650,15 Q 655,45 650,60 Q 645,45 650,15 Z"" />
    <path d=""M 750,15 Q 755,45 750,60 Q 745,45 750,15 Z"" />
    <path d=""M 850,15 Q 855,45 850,60 Q 845,45 850,15 Z"" />
    <path d=""M 950,15 Q 955,45 950,60 Q 945,45 950,15 Z"" />
  </g>

  <!-- Marigold Swag Arc 1 (0 to 500) -->
  <path d=""M 0,15 Q 250,75 500,15"" fill=""none"" stroke=""{yellow}"" stroke-width=""14"" stroke-linecap=""round"" stroke-dasharray=""16,4"" />
  <path d=""M 0,15 Q 250,75 500,15"" fill=""none"" stroke=""{orange}"" stroke-width=""10"" stroke-linecap=""round"" stroke-dasharray=""10,10"" />

  <!-- Marigold Swag Arc 2 (500 to 1000) -->
  <path d=""M 500,15 Q 750,75 1000,15"" fill=""none"" stroke=""{yellow}"" stroke-width=""14"" stroke-linecap=""round"" stroke-dasharray=""16,4"" />
  <path d=""M 500,15 Q 750,75 1000,15"" fill=""none"" stroke=""{orange}"" stroke-width=""10"" stroke-linecap=""round"" stroke-dasharray=""10,10"" />

  <!-- Hanging Golden Diyas / Brass Bells -->
  <!-- Diya 1 (Left) -->
  <g transform=""translate(100, 0)"">
    <line x1=""0"" y1=""15"" x2=""0"" y2=""80"" stroke=""{orange}"" stroke-width=""2"" stroke-dasharray=""4,3"" />
    <ellipse cx=""0"" cy=""85"" rx=""16"" ry=""7"" fill=""{gold}"" stroke=""#92400E"" stroke-width=""1.5"" />
    <path d=""M -6,85 Q 0,70 6,85 Z"" fill=""#DC2626"" />
    <circle cx=""0"" cy=""75"" r=""4"" fill=""{yellow}"" />
  </g>

  <!-- Diya 2 (Center-Left) -->
  <g transform=""translate(300, 0)"">
    <line x1=""0"" y1=""15"" x2=""0"" y2=""90"" stroke=""{orange}"" stroke-width=""2"" stroke-dasharray=""4,3"" />
    <ellipse cx=""0"" cy=""95"" rx=""16"" ry=""7"" fill=""{gold}"" stroke=""#92400E"" stroke-width=""1.5"" />
    <path d=""M -6,95 Q 0,80 6,95 Z"" fill=""#DC2626"" />
    <circle cx=""0"" cy=""85"" r=""4"" fill=""{yellow}"" />
  </g>

  <!-- Diya 3 (Center) -->
  <g transform=""translate(500, 0)"">
    <line x1=""0"" y1=""15"" x2=""0"" y2=""100"" stroke=""{orange}"" stroke-width=""2.5"" stroke-dasharray=""4,3"" />
    <ellipse cx=""0"" cy=""105"" rx=""20"" ry=""8"" fill=""{gold}"" stroke=""#92400E"" stroke-width=""1.5"" />
    <path d=""M -8,105 Q 0,88 8,105 Z"" fill=""#DC2626"" />
    <circle cx=""0"" cy=""93"" r=""5"" fill=""{yellow}"" />
  </g>

  <!-- Diya 4 (Center-Right) -->
  <g transform=""translate(700, 0)"">
    <line x1=""0"" y1=""15"" x2=""0"" y2=""90"" stroke=""{orange}"" stroke-width=""2"" stroke-dasharray=""4,3"" />
    <ellipse cx=""0"" cy=""95"" rx=""16"" ry=""7"" fill=""{gold}"" stroke=""#92400E"" stroke-width=""1.5"" />
    <path d=""M -6,95 Q 0,80 6,95 Z"" fill=""#DC2626"" />
    <circle cx=""0"" cy=""85"" r=""4"" fill=""{yellow}"" />
  </g>

  <!-- Diya 5 (Right) -->
  <g transform=""translate(900, 0)"">
    <line x1=""0"" y1=""15"" x2=""0"" y2=""80"" stroke=""{orange}"" stroke-width=""2"" stroke-dasharray=""4,3"" />
    <ellipse cx=""0"" cy=""85"" rx=""16"" ry=""7"" fill=""{gold}"" stroke=""#92400E"" stroke-width=""1.5"" />
    <path d=""M -6,85 Q 0,70 6,85 Z"" fill=""#DC2626"" />
    <circle cx=""0"" cy=""75"" r=""4"" fill=""{yellow}"" />
  </g>
</svg>";
    }

    /// <summary>
    /// Traditional brass Indian standing oil lamp (Samai / Kuthu Vilakku) with glowing flame.
    /// </summary>
    public static string GetTraditionalDeepamSvg(string gold = "#D97706", string flame = "#EA580C", string glow = "#FBBF24")
    {
        return $@"<svg viewBox=""0 0 100 240"" xmlns=""http://www.w3.org/2000/svg"">
  <!-- Glowing Flame (Jyoti) -->
  <path d=""M 50,10 C 44,22 42,32 50,42 C 58,32 56,22 50,10 Z"" fill=""{glow}"" />
  <path d=""M 50,18 C 47,26 46,32 50,38 C 54,32 53,26 50,18 Z"" fill=""{flame}"" />
  <circle cx=""50"" cy=""32"" r=""3"" fill=""#FFFFFF"" />

  <!-- Top Crown / Kalasam -->
  <path d=""M 46,42 L 54,42 L 52,50 L 48,50 Z"" fill=""{gold}"" stroke=""#92400E"" stroke-width=""1"" />

  <!-- Top Tier Oil Bowl -->
  <ellipse cx=""50"" cy=""55"" rx=""26"" ry=""9"" fill=""{gold}"" stroke=""#92400E"" stroke-width=""1.5"" />
  <ellipse cx=""50"" cy=""53"" rx=""22"" ry=""6"" fill=""#B45309"" />

  <!-- Upper Column Shaft -->
  <path d=""M 46,55 L 46,95 L 44,100 L 56,100 L 54,95 L 54,55 Z"" fill=""{gold}"" stroke=""#92400E"" stroke-width=""1.2"" />
  <!-- Middle Ring Knob -->
  <ellipse cx=""50"" cy=""102"" rx=""10"" ry=""4"" fill=""{gold}"" stroke=""#92400E"" stroke-width=""1"" />

  <!-- Middle Tier Oil Bowl -->
  <ellipse cx=""50"" cy=""115"" rx=""34"" ry=""11"" fill=""{gold}"" stroke=""#92400E"" stroke-width=""1.5"" />
  <ellipse cx=""50"" cy=""112"" rx=""28"" ry=""7"" fill=""#B45309"" />

  <!-- Lower Column Shaft -->
  <path d=""M 45,115 L 45,165 L 42,175 L 58,175 L 55,165 L 55,115 Z"" fill=""{gold}"" stroke=""#92400E"" stroke-width=""1.2"" />
  <ellipse cx=""50"" cy=""178"" rx=""14"" ry=""5"" fill=""{gold}"" stroke=""#92400E"" stroke-width=""1"" />

  <!-- Lower Tier / Large Base Bowl -->
  <ellipse cx=""50"" cy=""192"" rx=""42"" ry=""14"" fill=""{gold}"" stroke=""#92400E"" stroke-width=""1.5"" />
  <ellipse cx=""50"" cy=""188"" rx=""36"" ry=""9"" fill=""#B45309"" />

  <!-- Pedestal Base (Peedam) -->
  <path d=""M 35,195 L 20,230 L 80,230 L 65,195 Z"" fill=""{gold}"" stroke=""#92400E"" stroke-width=""1.5"" />
  <rect x=""15"" y=""230"" width=""70"" height=""8"" rx=""3"" fill=""{gold}"" stroke=""#92400E"" stroke-width=""1.5"" />
</svg>";
    }

    /// <summary>
    /// Pair of hanging decorative lamps.
    /// </summary>
    public static string GetHangingDiyasSvg(string gold = "#D97706", string flame = "#EA580C", string yellow = "#FBBF24")
    {
        return $@"<svg viewBox=""0 0 120 180"" xmlns=""http://www.w3.org/2000/svg"">
  <line x1=""60"" y1=""0"" x2=""60"" y2=""120"" stroke=""{gold}"" stroke-width=""2"" stroke-dasharray=""3,3"" />
  <circle cx=""60"" cy=""120"" r=""5"" fill=""{gold}"" />
  <!-- Flame -->
  <path d=""M 60,95 C 55,106 53,114 60,122 C 67,114 65,106 60,95 Z"" fill=""{yellow}"" />
  <path d=""M 60,102 C 57,110 56,115 60,119 C 64,115 63,110 60,102 Z"" fill=""{flame}"" />
  <!-- Diya Bowl -->
  <ellipse cx=""60"" cy=""130"" rx=""35"" ry=""12"" fill=""{gold}"" stroke=""#92400E"" stroke-width=""1.5"" />
  <ellipse cx=""60"" cy=""127"" rx=""28"" ry=""8"" fill=""#B45309"" />
  <!-- Bottom Dangling Pearl Drop -->
  <line x1=""60"" y1=""135"" x2=""60"" y2=""155"" stroke=""{gold}"" stroke-width=""1.5"" />
  <circle cx=""60"" cy=""160"" r=""4"" fill=""{yellow}"" stroke=""#92400E"" stroke-width=""1"" />
</svg>";
    }

    /// <summary>
    /// Dotted floral divider with center lotus medallion.
    /// </summary>
    public static string GetDottedFloralDividerSvg(string gold = "#D97706", string maroon = "#8B0000")
    {
        return $@"<svg viewBox=""0 0 600 40"" xmlns=""http://www.w3.org/2000/svg"">
  <!-- Left Dotted Line -->
  <line x1=""20"" y1=""20"" x2=""260"" y2=""20"" stroke=""{gold}"" stroke-width=""2"" stroke-linecap=""round"" stroke-dasharray=""1,6"" />
  
  <!-- Right Dotted Line -->
  <line x1=""340"" y1=""20"" x2=""580"" y2=""20"" stroke=""{gold}"" stroke-width=""2"" stroke-linecap=""round"" stroke-dasharray=""1,6"" />

  <!-- Center Lotus Ornament -->
  <!-- Lotus Center Petal -->
  <path d=""M 300,5 C 294,13 293,22 300,30 C 307,22 306,13 300,5 Z"" fill=""{maroon}"" stroke=""{gold}"" stroke-width=""1"" />
  <!-- Left Petal -->
  <path d=""M 300,22 C 285,14 275,18 280,27 C 286,31 294,27 300,22 Z"" fill=""{gold}"" stroke=""{maroon}"" stroke-width=""1"" />
  <!-- Right Petal -->
  <path d=""M 300,22 C 315,14 325,18 320,27 C 314,31 306,27 300,22 Z"" fill=""{gold}"" stroke=""{maroon}"" stroke-width=""1"" />

  <!-- Accents / Dots -->
  <circle cx=""270"" cy=""20"" r=""3"" fill=""{gold}"" />
  <circle cx=""330"" cy=""20"" r=""3"" fill=""{gold}"" />
  <circle cx=""300"" cy=""32"" r=""2"" fill=""{gold}"" />
</svg>";
    }

    /// <summary>
    /// Traditional Indian wedding mandap pillar arch border.
    /// </summary>
    public static string GetMandapArchSvg(string gold = "#D97706", string maroon = "#8B0000")
    {
        return $@"<svg viewBox=""0 0 800 100"" xmlns=""http://www.w3.org/2000/svg"">
  <rect x=""10"" y=""10"" width=""780"" height=""80"" fill=""none"" stroke=""{gold}"" stroke-width=""1.5"" />
  <rect x=""16"" y=""16"" width=""768"" height=""68"" fill=""none"" stroke=""{maroon}"" stroke-width=""1"" stroke-dasharray=""4,3"" />
  
  <!-- Corner Lotus Motifs -->
  <circle cx=""25"" cy=""25"" r=""6"" fill=""{gold}"" />
  <circle cx=""775"" cy=""25"" r=""6"" fill=""{gold}"" />
  <circle cx=""25"" cy=""75"" r=""6"" fill=""{gold}"" />
  <circle cx=""775"" cy=""75"" r=""6"" fill=""{gold}"" />

  <!-- Center Medallion -->
  <circle cx=""400"" cy=""50"" r=""12"" fill=""none"" stroke=""{gold}"" stroke-width=""1.5"" />
  <circle cx=""400"" cy=""50"" r=""6"" fill=""{maroon}"" />
</svg>";
    }

    /// <summary>
    /// Ceremonial banana trees / plantain wedding stems.
    /// </summary>
    public static string GetPlantainTreesSvg(string green = "#16A34A", string darkGreen = "#15803D", string yellow = "#FBBF24")
    {
        return $@"<svg viewBox=""0 0 140 280"" xmlns=""http://www.w3.org/2000/svg"">
  <!-- Central Trunk (Maram) -->
  <path d=""M 65,110 L 60,260 L 80,260 L 75,110 Z"" fill=""{darkGreen}"" stroke=""#14532D"" stroke-width=""1.5"" />

  <!-- Banana Leaves Spreading Out -->
  <!-- Top Center Leaf -->
  <path d=""M 70,110 C 65,60 70,20 70,10 C 75,20 80,60 70,110 Z"" fill=""{green}"" stroke=""#14532D"" stroke-width=""1.5"" />
  <!-- Top Left Leaf -->
  <path d=""M 70,110 C 45,70 15,45 5,40 C 15,65 40,95 70,110 Z"" fill=""{green}"" stroke=""#14532D"" stroke-width=""1.5"" />
  <!-- Top Right Leaf -->
  <path d=""M 70,110 C 95,70 125,45 135,40 C 125,65 100,95 70,110 Z"" fill=""{green}"" stroke=""#14532D"" stroke-width=""1.5"" />
  <!-- Mid Left Drooping Leaf -->
  <path d=""M 68,130 C 35,110 5,120 0,135 C 15,145 45,135 68,130 Z"" fill=""{darkGreen}"" stroke=""#14532D"" stroke-width=""1.5"" />
  <!-- Mid Right Drooping Leaf -->
  <path d=""M 72,130 C 105,110 135,120 140,135 C 125,145 95,135 72,130 Z"" fill=""{darkGreen}"" stroke=""#14532D"" stroke-width=""1.5"" />

  <!-- Banana Fruit Bunch (Thaaru) -->
  <g fill=""{yellow}"" stroke=""#B45309"" stroke-width=""1"">
    <ellipse cx=""65"" cy=""145"" rx=""5"" ry=""9"" transform=""rotate(-20 65 145)"" />
    <ellipse cx=""75"" cy=""145"" rx=""5"" ry=""9"" transform=""rotate(20 75 145)"" />
    <ellipse cx=""70"" cy=""152"" rx=""5"" ry=""9"" />
    <ellipse cx=""63"" cy=""158"" rx=""4"" ry=""8"" transform=""rotate(-15 63 158)"" />
    <ellipse cx=""77"" cy=""158"" rx=""4"" ry=""8"" transform=""rotate(15 77 158)"" />
    <!-- Red Flower Blossom (Vazhaipoo) -->
    <path d=""M 65,170 C 65,185 70,195 70,195 C 70,195 75,185 75,170 Z"" fill=""#990000"" stroke=""#7F1D1D"" stroke-width=""1"" />
  </g>
</svg>";
    }

    /// <summary>
    /// Auspicious Vedic Kalash with coconut and mango leaves.
    /// </summary>
    public static string GetAuspiciousKalashSvg(string copper = "#B45309", string gold = "#F59E0B", string green = "#15803D")
    {
        return $@"<svg viewBox=""0 0 140 180"" xmlns=""http://www.w3.org/2000/svg"">
  <!-- Sacred Coconut (Nariyal) -->
  <ellipse cx=""70"" cy=""40"" rx=""20"" ry=""25"" fill=""#78350F"" stroke=""#451A03"" stroke-width=""1.5"" />
  <path d=""M 70,15 L 70,30"" stroke=""#451A03"" stroke-width=""2"" />

  <!-- Mango Leaves framing Coconut -->
  <path d=""M 70,60 C 45,35 30,15 25,10 C 35,30 55,55 70,60 Z"" fill=""{green}"" stroke=""#14532D"" stroke-width=""1"" />
  <path d=""M 70,60 C 95,35 110,15 115,10 C 105,30 85,55 70,60 Z"" fill=""{green}"" stroke=""#14532D"" stroke-width=""1"" />
  <path d=""M 70,60 C 50,45 40,30 35,25 C 48,45 60,55 70,60 Z"" fill=""#16A34A"" />
  <path d=""M 70,60 C 90,45 100,30 105,25 C 92,45 80,55 70,60 Z"" fill=""#16A34A"" />

  <!-- Pot Neck (Kumbha Neck) -->
  <ellipse cx=""70"" cy=""65"" rx=""32"" ry=""8"" fill=""{gold}"" stroke=""#92400E"" stroke-width=""1.5"" />
  <rect x=""44"" y=""65"" width=""52"" height=""12"" fill=""{copper}"" stroke=""#92400E"" stroke-width=""1.5"" />

  <!-- Pot Body (Kumbha Vessel) -->
  <ellipse cx=""70"" cy=""115"" rx=""48"" ry=""42"" fill=""{copper}"" stroke=""#92400E"" stroke-width=""2"" />
  <!-- Sacred Thread / Swastika on Kalash -->
  <circle cx=""70"" cy=""115"" r=""22"" fill=""none"" stroke=""{gold}"" stroke-width=""2"" stroke-dasharray=""4,3"" />
  <path d=""M 62,115 L 78,115 M 70,107 L 70,123"" stroke=""{gold}"" stroke-width=""3"" stroke-linecap=""round"" />

  <!-- Kalash Base Ring -->
  <ellipse cx=""70"" cy=""155"" rx=""26"" ry=""8"" fill=""{gold}"" stroke=""#92400E"" stroke-width=""1.5"" />
</svg>";
    }

    /// <summary>
    /// Royal Botanical Floral Wreath for Luxury Gala and Wedding Invitations.
    /// </summary>
    public static string GetBotanicalWreathSvg(string gold = "#D97706", string leaf = "#059669")
    {
        return $@"<svg viewBox=""0 0 200 200"" xmlns=""http://www.w3.org/2000/svg"">
  <circle cx=""100"" cy=""100"" r=""75"" fill=""none"" stroke=""{gold}"" stroke-width=""1.5"" stroke-dasharray=""4,4"" />
  <!-- Left Botanical Branch -->
  <path d=""M 100,180 C 45,175 25,125 25,100 C 25,65 55,25 100,20"" fill=""none"" stroke=""{gold}"" stroke-width=""2"" stroke-linecap=""round"" />
  <!-- Right Botanical Branch -->
  <path d=""M 100,180 C 155,175 175,125 175,100 C 175,65 145,25 100,20"" fill=""none"" stroke=""{gold}"" stroke-width=""2"" stroke-linecap=""round"" />
  
  <!-- Left Leaves -->
  <path d=""M 40,140 Q 25,130 35,120 Q 45,130 40,140 Z"" fill=""{leaf}"" stroke=""{gold}"" stroke-width=""0.8"" />
  <path d=""M 25,100 Q 10,95 20,85 Q 30,95 25,100 Z"" fill=""{leaf}"" stroke=""{gold}"" stroke-width=""0.8"" />
  <path d=""M 40,60 Q 30,45 45,45 Q 50,55 40,60 Z"" fill=""{leaf}"" stroke=""{gold}"" stroke-width=""0.8"" />
  <path d=""M 70,30 Q 65,15 80,20 Q 80,30 70,30 Z"" fill=""{leaf}"" stroke=""{gold}"" stroke-width=""0.8"" />

  <!-- Right Leaves -->
  <path d=""M 160,140 Q 175,130 165,120 Q 155,130 160,140 Z"" fill=""{leaf}"" stroke=""{gold}"" stroke-width=""0.8"" />
  <path d=""M 175,100 Q 190,95 180,85 Q 170,95 175,100 Z"" fill=""{leaf}"" stroke=""{gold}"" stroke-width=""0.8"" />
  <path d=""M 160,60 Q 170,45 155,45 Q 150,55 160,60 Z"" fill=""{leaf}"" stroke=""{gold}"" stroke-width=""0.8"" />
  <path d=""M 130,30 Q 135,15 120,20 Q 120,30 130,30 Z"" fill=""{leaf}"" stroke=""{gold}"" stroke-width=""0.8"" />

  <!-- Ribbon Bow at Base -->
  <circle cx=""100"" cy=""180"" r=""5"" fill=""{gold}"" />
  <path d=""M 95,180 Q 75,195 85,205 Q 100,190 95,180 Z"" fill=""{gold}"" />
  <path d=""M 105,180 Q 125,195 115,205 Q 100,190 105,180 Z"" fill=""{gold}"" />
</svg>";
    }

    /// <summary>
    /// Art Deco Gold Luxury Geometric Frame.
    /// </summary>
    public static string GetArtDecoFrameSvg(string gold = "#D97706")
    {
        return $@"<svg viewBox=""0 0 400 400"" xmlns=""http://www.w3.org/2000/svg"">
  <rect x=""15"" y=""15"" width=""370"" height=""370"" fill=""none"" stroke=""{gold}"" stroke-width=""2"" />
  <rect x=""25"" y=""25"" width=""350"" height=""350"" fill=""none"" stroke=""{gold}"" stroke-width=""1"" stroke-dasharray=""6,4"" />
  
  <!-- Corner Stepped Chevrons -->
  <polygon points=""15,15 65,15 65,25 25,25 25,65 15,65"" fill=""{gold}"" />
  <polygon points=""385,15 335,15 335,25 375,25 375,65 385,65"" fill=""{gold}"" />
  <polygon points=""15,385 65,385 65,375 25,375 25,335 15,335"" fill=""{gold}"" />
  <polygon points=""385,385 335,385 335,375 375,375 375,335 385,335"" fill=""{gold}"" />

  <!-- Center Corner Diamonds -->
  <polygon points=""40,40 46,46 40,52 34,46"" fill=""{gold}"" />
  <polygon points=""360,40 366,46 360,52 354,46"" fill=""{gold}"" />
  <polygon points=""40,360 46,366 40,372 34,366"" fill=""{gold}"" />
  <polygon points=""360,360 366,366 360,372 354,366"" fill=""{gold}"" />
</svg>";
    }

    /// <summary>
    /// Calligraphic Swirl & Filigree Flourish.
    /// </summary>
    public static string GetCalligraphicFlourishSvg(string colorHex = "#78350F")
    {
        return $@"<svg viewBox=""0 0 300 60"" xmlns=""http://www.w3.org/2000/svg"">
  <path d=""M 20,30 C 50,5 90,55 130,30 C 145,20 155,20 170,30 C 210,55 250,5 280,30"" fill=""none"" stroke=""{colorHex}"" stroke-width=""2"" stroke-linecap=""round"" />
  <circle cx=""150"" cy=""30"" r=""4"" fill=""{colorHex}"" />
  <circle cx=""138"" cy=""30"" r=""2.5"" fill=""{colorHex}"" />
  <circle cx=""162"" cy=""30"" r=""2.5"" fill=""{colorHex}"" />
</svg>";
    }

    /// <summary>
    /// Sacred Om symbol crest.
    /// </summary>
    public static string GetOmCrestSvg(string gold = "#D97706", string maroon = "#8B0000")
    {
        return $@"<svg viewBox=""0 0 160 160"" xmlns=""http://www.w3.org/2000/svg"">
  <circle cx=""80"" cy=""80"" r=""72"" fill=""none"" stroke=""{gold}"" stroke-width=""2"" stroke-dasharray=""4,3"" />
  <circle cx=""80"" cy=""80"" r=""65"" fill=""none"" stroke=""{maroon}"" stroke-width=""1"" />
  <!-- Om Glyph -->
  <path d=""M 55,50 C 70,35 90,45 80,65 C 95,65 105,85 85,105 C 70,115 50,105 52,90"" fill=""none"" stroke=""{maroon}"" stroke-width=""6"" stroke-linecap=""round"" />
  <path d=""M 80,65 C 105,70 120,95 125,120"" fill=""none"" stroke=""{maroon}"" stroke-width=""5"" stroke-linecap=""round"" />
  <!-- Crescent & Bindi -->
  <path d=""M 95,45 Q 115,55 125,40"" fill=""none"" stroke=""{maroon}"" stroke-width=""4"" stroke-linecap=""round"" />
  <circle cx=""110"" cy=""32"" r=""4"" fill=""{maroon}"" />
</svg>";
    }

    /// <summary>
    /// Triangular Phase Change Cycle Diagram: Solid (ठोस) &lt;-&gt; Liquid (द्रव) &lt;-&gt; Gas (गैस) with 6 transition arrows.
    /// Matches sample 'interchange_solid_liquid_gas' and 'states_of_matter_visual_notes'.
    /// </summary>
    public static string GetTriangularPhaseCycleSvg(string primary = "#0284C7", string accent = "#DC2626", string textDark = "#0F172A")
    {
        return $@"<svg viewBox=""0 0 520 400"" xmlns=""http://www.w3.org/2000/svg"">
  <defs>
    <marker id=""arrow-red"" viewBox=""0 0 10 10"" refX=""6"" refY=""5"" markerWidth=""6"" markerHeight=""6"" orient=""auto-start-reverse"">
      <path d=""M 0 1 L 10 5 L 0 9 z"" fill=""#DC2626"" />
    </marker>
    <marker id=""arrow-blue"" viewBox=""0 0 10 10"" refX=""6"" refY=""5"" markerWidth=""6"" markerHeight=""6"" orient=""auto-start-reverse"">
      <path d=""M 0 1 L 10 5 L 0 9 z"" fill=""#2563EB"" />
    </marker>
  </defs>

  <!-- Gas Node (Top) -->
  <rect x=""190"" y=""20"" width=""140"" height=""60"" rx=""12"" fill=""#EFF6FF"" stroke=""#2563EB"" stroke-width=""2"" />
  <text x=""260"" y=""45"" font-family=""sans-serif"" font-size=""14"" font-weight=""bold"" fill=""#1E3A8A"" text-anchor=""middle"">GAS (गैस)</text>
  <text x=""260"" y=""65"" font-family=""sans-serif"" font-size=""10"" fill=""#3B82F6"" text-anchor=""middle"">High Kinetic Energy</text>

  <!-- Solid Node (Bottom Left) -->
  <rect x=""30"" y=""310"" width=""140"" height=""60"" rx=""12"" fill=""#FEF2F2"" stroke=""#DC2626"" stroke-width=""2"" />
  <text x=""100"" y=""335"" font-family=""sans-serif"" font-size=""14"" font-weight=""bold"" fill=""#7F1D1D"" text-anchor=""middle"">SOLID (ठोस)</text>
  <text x=""100"" y=""355"" font-family=""sans-serif"" font-size=""10"" fill=""#EF4444"" text-anchor=""middle"">Fixed Shape &amp; Volume</text>

  <!-- Liquid Node (Bottom Right) -->
  <rect x=""350"" y=""310"" width=""140"" height=""60"" rx=""12"" fill=""#F0FDF4"" stroke=""#16A34A"" stroke-width=""2"" />
  <text x=""420"" y=""335"" font-family=""sans-serif"" font-size=""14"" font-weight=""bold"" fill=""#14532D"" text-anchor=""middle"">LIQUID (द्रव)</text>
  <text x=""420"" y=""355"" font-family=""sans-serif"" font-size=""10"" fill=""#22C55E"" text-anchor=""middle"">Fixed Volume, Fluid</text>

  <!-- Bottom Forward: Melting (Solid -> Liquid) -->
  <path d=""M 175 325 L 345 325"" fill=""none"" stroke=""#DC2626"" stroke-width=""2"" marker-end=""url(#arrow-red)"" />
  <text x=""260"" y=""318"" font-family=""sans-serif"" font-size=""11"" font-weight=""bold"" fill=""#B91C1C"" text-anchor=""middle"">Melting / गलनांक / संलयन →</text>

  <!-- Bottom Reverse: Freezing (Liquid -> Solid) -->
  <path d=""M 345 355 L 175 355"" fill=""none"" stroke=""#2563EB"" stroke-width=""2"" marker-end=""url(#arrow-blue)"" />
  <text x=""260"" y=""372"" font-family=""sans-serif"" font-size=""11"" font-weight=""bold"" fill=""#1D4ED8"" text-anchor=""middle"">← Freezing / जमना / हिमीकरण</text>

  <!-- Left Up: Sublimation (Solid -> Gas) -->
  <path d=""M 115 305 Q 110 160 200 70"" fill=""none"" stroke=""#DC2626"" stroke-width=""2"" marker-end=""url(#arrow-red)"" />
  <text x=""105"" y=""180"" font-family=""sans-serif"" font-size=""10"" font-weight=""bold"" fill=""#B91C1C"" text-anchor=""middle"" transform=""rotate(-50 105 180)"">Sublimation / ऊर्ध्वपातन ↗</text>

  <!-- Left Down: Deposition (Gas -> Solid) -->
  <path d=""M 215 80 Q 150 160 145 305"" fill=""none"" stroke=""#2563EB"" stroke-width=""2"" marker-end=""url(#arrow-blue)"" />
  <text x=""190"" y=""200"" font-family=""sans-serif"" font-size=""10"" font-weight=""bold"" fill=""#1D4ED8"" text-anchor=""middle"" transform=""rotate(50 190 200)"">↙ Deposition / निक्षेपण</text>

  <!-- Right Down: Condensation (Gas -> Liquid) -->
  <path d=""M 315 80 Q 370 160 375 305"" fill=""none"" stroke=""#2563EB"" stroke-width=""2"" marker-end=""url(#arrow-blue)"" />
  <text x=""335"" y=""200"" font-family=""sans-serif"" font-size=""10"" font-weight=""bold"" fill=""#1D4ED8"" text-anchor=""middle"" transform=""rotate(-50 335 200)"">Condensation / संघनन ↘</text>

  <!-- Right Up: Vaporization (Liquid -> Gas) -->
  <path d=""M 405 305 Q 410 160 320 70"" fill=""none"" stroke=""#DC2626"" stroke-width=""2"" marker-end=""url(#arrow-red)"" />
  <text x=""415"" y=""180"" font-family=""sans-serif"" font-size=""10"" font-weight=""bold"" fill=""#B91C1C"" text-anchor=""middle"" transform=""rotate(50 415 180)"">↖ Vaporization / वाष्पीकरण</text>
</svg>";
    }

    /// <summary>
    /// Comparative Particle Arrangement Diagram (Solid lattice, Liquid fluid, Gas dispersed).
    /// Matches sample 'states_of_matter_visual_notes' and 'states_of_matter_notes'.
    /// </summary>
    public static string GetParticleArrangementGridSvg()
    {
        return $@"<svg viewBox=""0 0 540 180"" xmlns=""http://www.w3.org/2000/svg"">
  <!-- Solid Box -->
  <g transform=""translate(10, 10)"">
    <rect x=""0"" y=""0"" width=""150"" height=""130"" rx=""8"" fill=""#FEF2F2"" stroke=""#DC2626"" stroke-width=""1.5"" />
    <text x=""75"" y=""155"" font-family=""sans-serif"" font-size=""12"" font-weight=""bold"" fill=""#991B1B"" text-anchor=""middle"">Solid (ठोस)</text>
    <!-- 4x4 Grid of packed circles -->
    <circle cx=""30"" cy=""25"" r=""11"" fill=""#EF4444"" stroke=""#B91C1C"" />
    <circle cx=""60"" cy=""25"" r=""11"" fill=""#EF4444"" stroke=""#B91C1C"" />
    <circle cx=""90"" cy=""25"" r=""11"" fill=""#EF4444"" stroke=""#B91C1C"" />
    <circle cx=""120"" cy=""25"" r=""11"" fill=""#EF4444"" stroke=""#B91C1C"" />
    <circle cx=""30"" cy=""55"" r=""11"" fill=""#EF4444"" stroke=""#B91C1C"" />
    <circle cx=""60"" cy=""55"" r=""11"" fill=""#EF4444"" stroke=""#B91C1C"" />
    <circle cx=""90"" cy=""55"" r=""11"" fill=""#EF4444"" stroke=""#B91C1C"" />
    <circle cx=""120"" cy=""55"" r=""11"" fill=""#EF4444"" stroke=""#B91C1C"" />
    <circle cx=""30"" cy=""85"" r=""11"" fill=""#EF4444"" stroke=""#B91C1C"" />
    <circle cx=""60"" cy=""85"" r=""11"" fill=""#EF4444"" stroke=""#B91C1C"" />
    <circle cx=""90"" cy=""85"" r=""11"" fill=""#EF4444"" stroke=""#B91C1C"" />
    <circle cx=""120"" cy=""85"" r=""11"" fill=""#EF4444"" stroke=""#B91C1C"" />
    <circle cx=""30"" cy=""115"" r=""11"" fill=""#EF4444"" stroke=""#B91C1C"" />
    <circle cx=""60"" cy=""115"" r=""11"" fill=""#EF4444"" stroke=""#B91C1C"" />
    <circle cx=""90"" cy=""115"" r=""11"" fill=""#EF4444"" stroke=""#B91C1C"" />
    <circle cx=""120"" cy=""115"" r=""11"" fill=""#EF4444"" stroke=""#B91C1C"" />
  </g>

  <!-- Liquid Box -->
  <g transform=""translate(195, 10)"">
    <rect x=""0"" y=""0"" width=""150"" height=""130"" rx=""8"" fill=""#F0FDF4"" stroke=""#16A34A"" stroke-width=""1.5"" />
    <text x=""75"" y=""155"" font-family=""sans-serif"" font-size=""12"" font-weight=""bold"" fill=""#166534"" text-anchor=""middle"">Liquid (द्रव)</text>
    <circle cx=""28"" cy=""40"" r=""11"" fill=""#22C55E"" stroke=""#15803D"" />
    <circle cx=""58"" cy=""32"" r=""11"" fill=""#22C55E"" stroke=""#15803D"" />
    <circle cx=""88"" cy=""42"" r=""11"" fill=""#22C55E"" stroke=""#15803D"" />
    <circle cx=""122"" cy=""35"" r=""11"" fill=""#22C55E"" stroke=""#15803D"" />
    <circle cx=""35"" cy=""75"" r=""11"" fill=""#22C55E"" stroke=""#15803D"" />
    <circle cx=""70"" cy=""70"" r=""11"" fill=""#22C55E"" stroke=""#15803D"" />
    <circle cx=""105"" cy=""72"" r=""11"" fill=""#22C55E"" stroke=""#15803D"" />
    <circle cx=""42"" cy=""110"" r=""11"" fill=""#22C55E"" stroke=""#15803D"" />
    <circle cx=""80"" cy=""105"" r=""11"" fill=""#22C55E"" stroke=""#15803D"" />
    <circle cx=""118"" cy=""112"" r=""11"" fill=""#22C55E"" stroke=""#15803D"" />
  </g>

  <!-- Gas Box -->
  <g transform=""translate(380, 10)"">
    <rect x=""0"" y=""0"" width=""150"" height=""130"" rx=""8"" fill=""#EFF6FF"" stroke=""#2563EB"" stroke-width=""1.5"" />
    <text x=""75"" y=""155"" font-family=""sans-serif"" font-size=""12"" font-weight=""bold"" fill=""#1E40AF"" text-anchor=""middle"">Gas (गैस)</text>
    <circle cx=""35"" cy=""30"" r=""9"" fill=""#3B82F6"" stroke=""#1D4ED8"" />
    <line x1=""35"" y1=""30"" x2=""50"" y2=""20"" stroke=""#93C5FD"" stroke-width=""1.5"" stroke-dasharray=""2,2"" />
    <circle cx=""115"" cy=""45"" r=""9"" fill=""#3B82F6"" stroke=""#1D4ED8"" />
    <line x1=""115"" y1=""45"" x2=""95"" y2=""55"" stroke=""#93C5FD"" stroke-width=""1.5"" stroke-dasharray=""2,2"" />
    <circle cx=""65"" cy=""85"" r=""9"" fill=""#3B82F6"" stroke=""#1D4ED8"" />
    <line x1=""65"" y1=""85"" x2=""80"" y2=""100"" stroke=""#93C5FD"" stroke-width=""1.5"" stroke-dasharray=""2,2"" />
    <circle cx=""120"" cy=""105"" r=""9"" fill=""#3B82F6"" stroke=""#1D4ED8"" />
    <line x1=""120"" y1=""105"" x2=""105"" y2=""115"" stroke=""#93C5FD"" stroke-width=""1.5"" stroke-dasharray=""2,2"" />
    <circle cx=""25"" cy=""115"" r=""9"" fill=""#3B82F6"" stroke=""#1D4ED8"" />
    <line x1=""25"" y1=""115"" x2=""40"" y2=""120"" stroke=""#93C5FD"" stroke-width=""1.5"" stroke-dasharray=""2,2"" />
  </g>
</svg>";
    }

    /// <summary>
    /// Geometric Quadrilaterals Reference Diagram: Parallelogram, Rhombus, Trapezoid, Kite.
    /// Matches sample 'Types_of_Quadrilaterals_Hindi_English'.
    /// </summary>
    public static string GetQuadrilateralSetDiagramSvg()
    {
        return $@"<svg viewBox=""0 0 520 220"" xmlns=""http://www.w3.org/2000/svg"">
  <!-- 1. Parallelogram -->
  <g transform=""translate(10, 10)"">
    <polygon points=""25,10 115,10 95,90 5,90"" fill=""#EFF6FF"" stroke=""#2563EB"" stroke-width=""2"" />
    <line x1=""25"" y1=""10"" x2=""25"" y2=""90"" stroke=""#93C5FD"" stroke-width=""1.5"" stroke-dasharray=""3,3"" />
    <text x=""22"" y=""55"" font-family=""sans-serif"" font-size=""10"" fill=""#1D4ED8"">h</text>
    <text x=""50"" y=""105"" font-family=""sans-serif"" font-size=""10"" fill=""#1D4ED8"">b</text>
    <text x=""60"" y=""130"" font-family=""sans-serif"" font-size=""11"" font-weight=""bold"" fill=""#1E3A8A"" text-anchor=""middle"">Parallelogram (समांतर)</text>
    <text x=""60"" y=""145"" font-family=""sans-serif"" font-size=""9"" fill=""#475569"" text-anchor=""middle"">Area = b × h</text>
  </g>

  <!-- 2. Rhombus -->
  <g transform=""translate(140, 10)"">
    <polygon points=""60,10 115,50 60,90 5,50"" fill=""#FEF2F2"" stroke=""#DC2626"" stroke-width=""2"" />
    <line x1=""60"" y1=""10"" x2=""60"" y2=""90"" stroke=""#FCA5A5"" stroke-width=""1.5"" stroke-dasharray=""3,3"" />
    <line x1=""5"" y1=""50"" x2=""115"" y2=""50"" stroke=""#FCA5A5"" stroke-width=""1.5"" stroke-dasharray=""3,3"" />
    <text x=""60"" y=""130"" font-family=""sans-serif"" font-size=""11"" font-weight=""bold"" fill=""#7F1D1D"" text-anchor=""middle"">Rhombus (समचतुर्भुज)</text>
    <text x=""60"" y=""145"" font-family=""sans-serif"" font-size=""9"" fill=""#475569"" text-anchor=""middle"">Area = ½ d₁ d₂</text>
  </g>

  <!-- 3. Trapezoid -->
  <g transform=""translate(270, 10)"">
    <polygon points=""30,15 90,15 115,90 5,90"" fill=""#F0FDF4"" stroke=""#16A34A"" stroke-width=""2"" />
    <line x1=""30"" y1=""15"" x2=""30"" y2=""90"" stroke=""#86EFAC"" stroke-width=""1.5"" stroke-dasharray=""3,3"" />
    <text x=""26"" y=""55"" font-family=""sans-serif"" font-size=""10"" fill=""#15803D"">h</text>
    <text x=""60"" y=""10"" font-family=""sans-serif"" font-size=""9"" fill=""#15803D"" text-anchor=""middle"">a</text>
    <text x=""60"" y=""105"" font-family=""sans-serif"" font-size=""9"" fill=""#15803D"" text-anchor=""middle"">b</text>
    <text x=""60"" y=""130"" font-family=""sans-serif"" font-size=""11"" font-weight=""bold"" fill=""#14532D"" text-anchor=""middle"">Trapezoid (समलंब)</text>
    <text x=""60"" y=""145"" font-family=""sans-serif"" font-size=""9"" fill=""#475569"" text-anchor=""middle"">Area = ½(a+b)h</text>
  </g>

  <!-- 4. Kite -->
  <g transform=""translate(400, 10)"">
    <polygon points=""55,10 100,45 55,95 10,45"" fill=""#FFFBEB"" stroke=""#D97706"" stroke-width=""2"" />
    <line x1=""55"" y1=""10"" x2=""55"" y2=""95"" stroke=""#FCD34D"" stroke-width=""1.5"" stroke-dasharray=""3,3"" />
    <line x1=""10"" y1=""45"" x2=""100"" y2=""45"" stroke=""#FCD34D"" stroke-width=""1.5"" stroke-dasharray=""3,3"" />
    <text x=""55"" y=""130"" font-family=""sans-serif"" font-size=""11"" font-weight=""bold"" fill=""#78350F"" text-anchor=""middle"">Kite (पतंग)</text>
    <text x=""55"" y=""145"" font-family=""sans-serif"" font-size=""9"" fill=""#475569"" text-anchor=""middle"">Diagonals ⊥</text>
  </g>
</svg>";
    }
}

