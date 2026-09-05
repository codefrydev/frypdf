/**
 * Sample Interactive Executive Presentation Deck (.frypdf)
 * QuantumScale_Global_Executive_Strategic_Brief_2026.frypdf
 *
 * Demonstrates capabilities unique to FryPDF's living format on the web:
 * - Searchable, sortable living data tables with instant CSV export
 * - Dynamic animated charts (Pillars, Progress meters, Donut KPI rings)
 * - Interactive compliance checklist form fields
 * - Cryptographic digital signature badges
 * - Vector QR codes, barcodes, and FOIA redactions
 */

const SAMPLE_FRYPDF_DECK = {
  id: "qs_deck_2026",
  title: "QuantumScale_Global_Executive_Strategic_Brief_2026.frypdf",
  author: "QuantumScale Cloud Enterprise Systems",
  subject: "Interactive Board of Directors Presentation Deck & Telemetry Ledger",
  creator: "FryPDF Studio Desktop v0.0.1",
  createdDate: "2026-03-01T09:00:00Z",
  pages: [
    // =========================================================================
    // SLIDE 1: EXECUTIVE INTELLIGENCE & TELEMETRY DASHBOARD
    // =========================================================================
    {
      pageNumber: 1,
      width: 1131,
      height: 800,
      backgroundColorHex: "#FFFFFF",
      showHeaderFooter: true,
      headerLeft: "QUANTUMSCALE ENTERPRISE • GLOBAL TELEMETRY BRIEF",
      headerCenter: "CONFIDENTIAL • BOARD OF DIRECTORS PRESENTATION",
      headerRight: "FY 2026-2027",
      footerLeft: "QUANTUMSCALE SYSTEMS • INTERACTIVE .FRYPDF DECK",
      footerCenter: "CONFIDENTIAL & PROPRIETARY",
      footerRight: "Slide 1 of 3",
      elements: [
        // Brand Top Accent Strip
        {
          $type: "shape",
          x: 0,
          y: 0,
          width: 1131,
          height: 6,
          fillColorHex: "#4F46E5"
        },
        // Logo Shape Badge
        {
          $type: "shape",
          x: 50,
          y: 28,
          width: 48,
          height: 48,
          cornerRadius: 12,
          fillColorHex: "#4F46E5",
          label: "QS",
          labelColorHex: "#FFFFFF",
          labelFontSize: 20
        },
        // Header Titles
        {
          $type: "text",
          x: 110,
          y: 30,
          width: 600,
          height: 18,
          text: "QUANTUMSCALE CLOUD ENTERPRISE • EXECUTIVE STRATEGIC BRIEFING",
          fontSize: 9.5,
          fontFamily: "Inter, Segoe UI, sans-serif",
          isBold: true,
          textColorHex: "#6366F1",
          characterSpacing: 1.0
        },
        {
          $type: "text",
          x: 110,
          y: 48,
          width: 650,
          height: 34,
          text: "Global Enterprise Telemetry & Growth Outlook",
          fontSize: 22,
          fontFamily: "Inter, Segoe UI, sans-serif",
          isBold: true,
          textColorHex: "#0F172A"
        },
        // Right Status Pill
        {
          $type: "shape",
          x: 930,
          y: 34,
          width: 151,
          height: 34,
          cornerRadius: 17,
          fillColorHex: "#EEF2FF",
          strokeColorHex: "#C7D2FE",
          strokeThickness: 1,
          label: "● LIVE INTERACTIVE",
          labelColorHex: "#4F46E5",
          labelFontSize: 10
        },
        {
          $type: "text",
          x: 850,
          y: 74,
          width: 231,
          height: 18,
          text: "Fiscal Year 2026-2027 • Board Review",
          fontSize: 9.5,
          fontFamily: "Inter, sans-serif",
          textColorHex: "#64748B",
          alignment: "Right"
        },
        // Divider
        {
          $type: "divider",
          x: 50,
          y: 96,
          width: 1031,
          height: 2,
          colorHex: "#E2E8F0"
        },

        // --- ROW 1: FOUR EXECUTIVE KPI CARDS ---
        // KPI 1: ARR
        {
          $type: "shape",
          x: 50,
          y: 112,
          width: 242,
          height: 84,
          cornerRadius: 12,
          fillColorHex: "#F8FAFC",
          strokeColorHex: "#E2E8F0",
          strokeThickness: 1
        },
        {
          $type: "shape",
          x: 50,
          y: 112,
          width: 4,
          height: 84,
          cornerRadius: 2,
          fillColorHex: "#4F46E5"
        },
        {
          $type: "text",
          x: 64,
          y: 124,
          width: 210,
          height: 16,
          text: "ANNUAL RECURRING REVENUE (ARR)",
          fontSize: 8.5,
          isBold: true,
          textColorHex: "#64748B"
        },
        {
          $type: "text",
          x: 64,
          y: 142,
          width: 120,
          height: 34,
          text: "$148.6M",
          fontSize: 24,
          isBold: true,
          textColorHex: "#0F172A"
        },
        {
          $type: "shape",
          x: 186,
          y: 148,
          width: 96,
          height: 22,
          cornerRadius: 11,
          fillColorHex: "#ECFDF5",
          strokeColorHex: "#A7F3D0",
          strokeThickness: 1,
          label: "▲ +38.2% YoY",
          labelColorHex: "#059669",
          labelFontSize: 9
        },

        // KPI 2: NDR
        {
          $type: "shape",
          x: 313,
          y: 112,
          width: 242,
          height: 84,
          cornerRadius: 12,
          fillColorHex: "#F8FAFC",
          strokeColorHex: "#E2E8F0",
          strokeThickness: 1
        },
        {
          $type: "shape",
          x: 313,
          y: 112,
          width: 4,
          height: 84,
          cornerRadius: 2,
          fillColorHex: "#0284C7"
        },
        {
          $type: "text",
          x: 327,
          y: 124,
          width: 210,
          height: 16,
          text: "NET DOLLAR RETENTION (NDR)",
          fontSize: 8.5,
          isBold: true,
          textColorHex: "#64748B"
        },
        {
          $type: "text",
          x: 327,
          y: 142,
          width: 120,
          height: 34,
          text: "142.4%",
          fontSize: 24,
          isBold: true,
          textColorHex: "#0F172A"
        },
        {
          $type: "shape",
          x: 448,
          y: 148,
          width: 96,
          height: 22,
          cornerRadius: 11,
          fillColorHex: "#F0F9FF",
          strokeColorHex: "#BAE6FD",
          strokeThickness: 1,
          label: "Top 5% SaaS",
          labelColorHex: "#0284C7",
          labelFontSize: 9
        },

        // KPI 3: SLA
        {
          $type: "shape",
          x: 576,
          y: 112,
          width: 242,
          height: 84,
          cornerRadius: 12,
          fillColorHex: "#F8FAFC",
          strokeColorHex: "#E2E8F0",
          strokeThickness: 1
        },
        {
          $type: "shape",
          x: 576,
          y: 112,
          width: 4,
          height: 84,
          cornerRadius: 2,
          fillColorHex: "#059669"
        },
        {
          $type: "text",
          x: 590,
          y: 124,
          width: 210,
          height: 16,
          text: "GLOBAL MESH AVAILABILITY",
          fontSize: 8.5,
          isBold: true,
          textColorHex: "#64748B"
        },
        {
          $type: "text",
          x: 590,
          y: 142,
          width: 120,
          height: 34,
          text: "99.998%",
          fontSize: 24,
          isBold: true,
          textColorHex: "#0F172A"
        },
        {
          $type: "shape",
          x: 712,
          y: 148,
          width: 96,
          height: 22,
          cornerRadius: 11,
          fillColorHex: "#ECFDF5",
          strokeColorHex: "#A7F3D0",
          strokeThickness: 1,
          label: "Tier 4 Uptime",
          labelColorHex: "#059669",
          labelFontSize: 9
        },

        // KPI 4: FCF
        {
          $type: "shape",
          x: 839,
          y: 112,
          width: 242,
          height: 84,
          cornerRadius: 12,
          fillColorHex: "#F8FAFC",
          strokeColorHex: "#E2E8F0",
          strokeThickness: 1
        },
        {
          $type: "shape",
          x: 839,
          y: 112,
          width: 4,
          height: 84,
          cornerRadius: 2,
          fillColorHex: "#D97706"
        },
        {
          $type: "text",
          x: 853,
          y: 124,
          width: 210,
          height: 16,
          text: "FREE CASH FLOW (FCF)",
          fontSize: 8.5,
          isBold: true,
          textColorHex: "#64748B"
        },
        {
          $type: "text",
          x: 853,
          y: 142,
          width: 120,
          height: 34,
          text: "$42.8M",
          fontSize: 24,
          isBold: true,
          textColorHex: "#0F172A"
        },
        {
          $type: "shape",
          x: 975,
          y: 148,
          width: 96,
          height: 22,
          cornerRadius: 11,
          fillColorHex: "#FFFBEB",
          strokeColorHex: "#FDE68A",
          strokeThickness: 1,
          label: "28.8% Margin",
          labelColorHex: "#D97706",
          labelFontSize: 9
        },

        // --- ROW 2: LIVING DATA TABLE (Searchable, Sortable, Instant CSV) ---
        {
          $type: "table",
          x: 50,
          y: 216,
          width: 660,
          height: 320,
          headerBackgroundHex: "#4F46E5",
          borderColorHex: "#E2E8F0",
          headers: [
            "Regional Cluster",
            "Active Nodes",
            "Compute Load",
            "Availability SLA",
            "Gross Margin",
            "Cluster Health"
          ],
          rows: [
            ["US-East (Virginia)", "4,280", "18.4 PFlops", "99.998%", "78.4%", "OPTIMAL"],
            ["EU-West (Frankfurt)", "3,150", "14.2 PFlops", "99.995%", "76.1%", "OPTIMAL"],
            ["AP-South (Mumbai)", "2,840", "12.8 PFlops", "99.992%", "74.8%", "EXPANDING"],
            ["AP-East (Tokyo)", "2,120", "9.6 PFlops", "99.997%", "77.2%", "OPTIMAL"],
            ["SA-East (São Paulo)", "1,450", "6.2 PFlops", "99.989%", "71.5%", "BALANCED"]
          ]
        },

        // --- ROW 2 (RIGHT): ANIMATED PROGRESS METERS ---
        {
          $type: "chart",
          x: 730,
          y: 216,
          width: 351,
          height: 320,
          chartType: "HorizontalBar",
          title: "Multi-Cloud Workload Migration",
          borderColorHex: "#E2E8F0",
          items: [
            { category: "AWS Hybrid Mesh", value: 88, colorHex: "#4F46E5" },
            { category: "Azure Core Enterprise", value: 74, colorHex: "#0284C7" },
            { category: "GCP Telemetry Analytics", value: 65, colorHex: "#059669" },
            { category: "On-Premises Bare Metal", value: 48, colorHex: "#D97706" }
          ]
        },

        // --- ROW 3: STICKY NOTE & VERIFICATION INFO ---
        {
          $type: "stickynote",
          x: 50,
          y: 560,
          width: 480,
          height: 120,
          colorHex: "#FEF3C7",
          borderColorHex: "#F59E0B",
          author: "Board Audit Committee",
          timestamp: "Verified Feb 28, 2026",
          status: "APPROVED FOR PRESENTATION",
          noteText: "All telemetric figures and margin reconciliations in this .frypdf deck have been verified against ERP data streams. Table data is directly exportable to CSV."
        },
        {
          $type: "shape",
          x: 550,
          y: 560,
          width: 531,
          height: 120,
          cornerRadius: 12,
          fillColorHex: "#F8FAFC",
          strokeColorHex: "#E2E8F0",
          strokeThickness: 1
        },
        {
          $type: "text",
          x: 570,
          y: 574,
          width: 500,
          height: 18,
          text: "KEY TAKEAWAY FOR DIRECTORS",
          fontSize: 9.5,
          isBold: true,
          textColorHex: "#4F46E5"
        },
        {
          $type: "text",
          x: 570,
          y: 596,
          width: 490,
          height: 70,
          text: "Our transition to interactive .frypdf presentations empowers our board, stakeholders, and partners to engage directly with living data rather than static snapshots. All numbers here are verifiable directly against our cloud warehouse.",
          fontSize: 11,
          textColorHex: "#334155",
          lineHeight: 1.45
        }
      ]
    },

    // =========================================================================
    // SLIDE 2: FINANCIAL MODEL & ARCHITECTURE TELEMETRY
    // =========================================================================
    {
      pageNumber: 2,
      width: 1131,
      height: 800,
      backgroundColorHex: "#FFFFFF",
      showHeaderFooter: true,
      headerLeft: "QUANTUMSCALE ENTERPRISE • FINANCIAL MODEL",
      headerCenter: "MULTI-YEAR REVENUE ACCELERATION",
      headerRight: "FY 2026-2027",
      footerLeft: "QUANTUMSCALE SYSTEMS • REVENUE & RESOURCE TELEMETRY",
      footerCenter: "CONFIDENTIAL & PROPRIETARY",
      footerRight: "Slide 2 of 3",
      elements: [
        // Brand Top Accent Strip
        {
          $type: "shape",
          x: 0,
          y: 0,
          width: 1131,
          height: 6,
          fillColorHex: "#0284C7"
        },
        // Header
        {
          $type: "text",
          x: 50,
          y: 30,
          width: 600,
          height: 18,
          text: "SEGMENT TELEMETRY & REVENUE TRAJECTORY",
          fontSize: 9.5,
          fontFamily: "Inter, sans-serif",
          isBold: true,
          textColorHex: "#0284C7",
          characterSpacing: 1.0
        },
        {
          $type: "text",
          x: 50,
          y: 48,
          width: 700,
          height: 34,
          text: "Quarterly Revenue Acceleration & Resource Distribution",
          fontSize: 22,
          fontFamily: "Inter, sans-serif",
          isBold: true,
          textColorHex: "#0F172A"
        },
        // Divider
        {
          $type: "divider",
          x: 50,
          y: 96,
          width: 1031,
          height: 2,
          colorHex: "#E2E8F0"
        },

        // --- ROW 1: ANIMATED BAR CHART & DONUT KPI ---
        // Bar Chart (Quarterly Revenue)
        {
          $type: "chart",
          x: 50,
          y: 116,
          width: 500,
          height: 320,
          chartType: "Bar",
          title: "Quarterly Revenue Acceleration ($M)",
          borderColorHex: "#E2E8F0",
          items: [
            { category: "Q1-25", value: 28.4, colorHex: "#6366F1" },
            { category: "Q2-25", value: 34.1, colorHex: "#4F46E5" },
            { category: "Q3-25", value: 41.5, colorHex: "#0284C7" },
            { category: "Q4-25", value: 44.6, colorHex: "#059669" }
          ]
        },

        // Donut KPI Chart (Resource Distribution)
        {
          $type: "chart",
          x: 570,
          y: 116,
          width: 511,
          height: 320,
          chartType: "Donut",
          title: "Global Cloud Resource Allocation",
          borderColorHex: "#E2E8F0",
          centerSummaryValue: "99.99%",
          centerSummaryLabel: "UPTIME",
          items: [
            { category: "Compute Mesh (PFlops)", value: 45, colorHex: "#4F46E5" },
            { category: "Distributed NVMe Storage", value: 25, colorHex: "#0284C7" },
            { category: "Global Optical Backbone", value: 18, colorHex: "#059669" },
            { category: "Edge Security & Vaults", value: 12, colorHex: "#D97706" }
          ]
        },

        // --- ROW 2: CAPEX ALLOCATION TABLE ---
        {
          $type: "table",
          x: 50,
          y: 456,
          width: 1031,
          height: 260,
          headerBackgroundHex: "#0284C7",
          borderColorHex: "#E2E8F0",
          headers: [
            "Infrastructure Segment",
            "FY25 Actual",
            "FY26 Budget",
            "Variance YoY",
            "Portfolio Share",
            "Strategic Priority"
          ],
          rows: [
            ["AI Model Training Clusters (H200 Mesh)", "$38.2M", "$55.0M", "+43.9%", "36.9%", "TIER 1 EXPANSION"],
            ["High-Throughput NVMe Storage Fabric", "$42.1M", "$48.5M", "+15.2%", "32.5%", "CORE RESILIENCE"],
            ["Ultra-Low Latency Optical CDN Mesh", "$18.4M", "$24.0M", "+30.4%", "16.1%", "LATENCY CRITICAL"],
            ["Zero-Trust Cryptographic Hardware Vaults", "$15.8M", "$21.5M", "+36.1%", "14.5%", "MANDATORY DEFENSE"]
          ]
        }
      ]
    },

    // =========================================================================
    // SLIDE 3: GOVERNANCE, COMPLIANCE & VERIFIED DIGITAL SIGNATURE
    // =========================================================================
    {
      pageNumber: 3,
      width: 1131,
      height: 800,
      backgroundColorHex: "#FFFFFF",
      showHeaderFooter: true,
      headerLeft: "QUANTUMSCALE ENTERPRISE • COMPLIANCE AUDIT",
      headerCenter: "CRYPTOGRAPHIC PROOF OF AUTHENTICITY",
      headerRight: "FY 2026-2027",
      footerLeft: "DIGITALLY SEALED DOCUMENT • VERIFIABLE AUDIT TRAIL",
      footerCenter: "CONFIDENTIAL & PROPRIETARY",
      footerRight: "Slide 3 of 3",
      elements: [
        // Brand Top Accent Strip
        {
          $type: "shape",
          x: 0,
          y: 0,
          width: 1131,
          height: 6,
          fillColorHex: "#059669"
        },
        // Header
        {
          $type: "text",
          x: 50,
          y: 30,
          width: 600,
          height: 18,
          text: "GOVERNANCE & CRYPTOGRAPHIC VERIFICATION",
          fontSize: 9.5,
          fontFamily: "Inter, sans-serif",
          isBold: true,
          textColorHex: "#059669",
          characterSpacing: 1.0
        },
        {
          $type: "text",
          x: 50,
          y: 48,
          width: 700,
          height: 34,
          text: "Compliance Sign-off, Audit Controls & Executive Seal",
          fontSize: 22,
          fontFamily: "Inter, sans-serif",
          isBold: true,
          textColorHex: "#0F172A"
        },
        // Divider
        {
          $type: "divider",
          x: 50,
          y: 96,
          width: 1031,
          height: 2,
          colorHex: "#E2E8F0"
        },

        // --- ROW 1 (LEFT): INTERACTIVE FORM CHECKLIST ---
        {
          $type: "shape",
          x: 50,
          y: 116,
          width: 500,
          height: 300,
          cornerRadius: 12,
          fillColorHex: "#F8FAFC",
          strokeColorHex: "#E2E8F0",
          strokeThickness: 1
        },
        {
          $type: "text",
          x: 74,
          y: 136,
          width: 450,
          height: 20,
          text: "BOARD COMPLIANCE & SECURITY CHECKLIST (INTERACTIVE)",
          fontSize: 10,
          isBold: true,
          textColorHex: "#0F172A"
        },
        {
          $type: "formfield",
          x: 74,
          y: 170,
          width: 450,
          height: 32,
          fieldType: "Checkbox",
          label: "SOC 2 Type II Annual Recertification Completed & Filed",
          isChecked: true
        },
        {
          $type: "formfield",
          x: 74,
          y: 210,
          width: 450,
          height: 32,
          fieldType: "Checkbox",
          label: "ISO 27001 / 27701 Global Privacy Controls Enforced",
          isChecked: true
        },
        {
          $type: "formfield",
          x: 74,
          y: 250,
          width: 450,
          height: 32,
          fieldType: "Checkbox",
          label: "Zero-Trust Mesh & HSM Cryptographic Keys Rotated",
          isChecked: true
        },
        {
          $type: "formfield",
          x: 74,
          y: 290,
          width: 450,
          height: 32,
          fieldType: "Checkbox",
          label: "Quarterly Board Financial Audit Sign-Off Verified",
          isChecked: true
        },
        {
          $type: "formfield",
          x: 74,
          y: 330,
          width: 450,
          height: 32,
          fieldType: "Checkbox",
          label: "Disaster Recovery & Multi-Region Failover Tested",
          isChecked: true
        },

        // --- ROW 1 (RIGHT): CRYPTOGRAPHIC SIGNATURE & QR ---
        // Verified Digital Signature Block
        {
          $type: "formfield",
          x: 580,
          y: 116,
          width: 501,
          height: 80,
          fieldType: "Signature"
        },
        {
          $type: "text",
          x: 580,
          y: 206,
          width: 320,
          height: 18,
          text: "Signatory: Dr. Elena Rostova, Chief Executive Officer",
          fontSize: 11,
          isBold: true,
          textColorHex: "#0F172A"
        },
        {
          $type: "text",
          x: 580,
          y: 226,
          width: 320,
          height: 16,
          text: "Signature Hash: SHA-256 / 8f9a2b1c4e7d3a01 • Verified",
          fontSize: 9.5,
          textColorHex: "#64748B"
        },

        // QR Code
        {
          $type: "qrcode",
          x: 930,
          y: 206,
          width: 151,
          height: 151
        },
        {
          $type: "text",
          x: 930,
          y: 364,
          width: 151,
          height: 16,
          text: "SCAN TO AUDIT LEDGER",
          fontSize: 8.5,
          isBold: true,
          alignment: "Center",
          textColorHex: "#64748B"
        },

        // Barcode
        {
          $type: "barcode",
          x: 580,
          y: 260,
          width: 320,
          height: 60,
          codeValue: "QS-2026-BOARD-8842",
          showText: true
        },

        // --- ROW 2: REDACTION DEMO & REGULATORY NOTE ---
        {
          $type: "text",
          x: 50,
          y: 440,
          width: 450,
          height: 18,
          text: "CONFIDENTIAL PROPRIETARY DISCLOSURE (REDACTION TEST)",
          fontSize: 9.5,
          isBold: true,
          textColorHex: "#64748B"
        },
        {
          $type: "redaction",
          x: 50,
          y: 464,
          width: 420,
          height: 38,
          fillColorHex: "#000000",
          exemptionCode: "FOIA (b)(4) PROPRIETARY QUANTUM HARDWARE SPEC"
        },
        {
          $type: "text",
          x: 50,
          y: 512,
          width: 500,
          height: 48,
          text: "Above section is permanently redacted using standard FOIA exemption (b)(4) for commercial and financial trade secret protection.",
          fontSize: 10,
          textColorHex: "#64748B"
        },

        // LaTeX Formula
        {
          $type: "math",
          x: 580,
          y: 440,
          width: 501,
          height: 70,
          formula: "\\text{Efficiency Ratio } \\eta = \\frac{\\sum_{i=1}^{n} P_i \\cdot \\Delta t}{\\text{CapEx} + \\text{OpEx}} \\cdot (1 - \\delta)",
          showEquationNumber: true,
          equationNumber: "Eq. 4.1",
          fontSize: 15,
          textColorHex: "#0F172A"
        }
      ]
    }
  ]
};

if (typeof window !== 'undefined') {
  window.SAMPLE_FRYPDF_DECK = SAMPLE_FRYPDF_DECK;
}
if (typeof module !== 'undefined' && module.exports) {
  module.exports = SAMPLE_FRYPDF_DECK;
}
