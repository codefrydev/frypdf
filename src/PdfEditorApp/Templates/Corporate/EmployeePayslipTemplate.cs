using System.Collections.Generic;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;

namespace PdfEditorApp.Templates.Corporate;

public class EmployeePayslipTemplate : ITemplateDefinition
{
    public string Id => "payslip";
    public string Name => "Employee Monthly Payslip & Salary Statement";
    public string Description => "Modern corporate employee salary statement with dynamic earnings & deductions tables, verification QR code, barcode, and merge placeholders.";
    public string Category => "Corporate";
    public string IconKind => "CashMultiple";
    public string AccentColorHex => "#0F6CBD";

    public PdfDocumentModel Create()
    {
        var doc = new PdfDocumentModel
        {
            Title = "Payslip_{{EmployeeId}}_{{EmployeeName}}_{{PayPeriod}}.pdf",
            Author = "{{CompanyName ?? CodeFryDev Inc.}}",
            Subject = "Salary Payslip for {{EmployeeName}} - {{PayPeriod}}"
        };

        var page = new PdfPageModel
        {
            PageNumber = 1,
            Format = PageFormat.A4,
            Orientation = PageOrientation.Portrait,
            Width = 800,
            Height = 1131,
            BackgroundColorHex = "#FFFFFF",
            FooterLeft = "CodeFryDev Inc. • Confidential & Proprietary Document",
            FooterCenter = "Computer Generated • No Physical Signature Required",
            FooterRight = "Generated for {{EmployeeName}} (ID: {{EmployeeId}})",
            Elements = new List<PdfElementBase>
            {
                // Top Brand Accent Header Bar
                new PdfShapeElement
                {
                    X = 0,
                    Y = 0,
                    Width = 800,
                    Height = 8,
                    FillColorHex = "#0F6CBD",
                    StrokeColorHex = "#00000000",
                    StrokeThickness = 0
                },

                // 1. Company Logo Badge
                new PdfShapeElement
                {
                    X = 50,
                    Y = 35,
                    Width = 48,
                    Height = 48,
                    CornerRadius = 10,
                    FillColorHex = "#0F6CBD",
                    StrokeColorHex = "#0C599B",
                    StrokeThickness = 0,
                    Label = "CF",
                    LabelColorHex = "#FFFFFF",
                    LabelFontSize = 18
                },

                // 2. Company Details
                new PdfTextElement
                {
                    X = 110,
                    Y = 35,
                    Width = 380,
                    Height = 24,
                    Text = "{{CompanyName ?? CODEFRYDEV INC.}}",
                    FontSize = 14,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F172A"
                },
                new PdfTextElement
                {
                    X = 110,
                    Y = 60,
                    Width = 380,
                    Height = 35,
                    Text = "100 Innovation Way, Suite 500 • Silicon Valley, CA 94025\nTax ID: US-EIN 94-8291042 • payroll@codefrydev.in",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.3,
                    TextColorHex = "#64748B"
                },

                // 3. Document Title & Pay Period Card
                new PdfShapeElement
                {
                    X = 520,
                    Y = 35,
                    Width = 230,
                    Height = 60,
                    CornerRadius = 8,
                    FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 530,
                    Y = 42,
                    Width = 210,
                    Height = 20,
                    Text = "MONTHLY SALARY SLIP",
                    FontSize = 12,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    Alignment = TextAlignmentMode.Center,
                    TextColorHex = "#0F6CBD"
                },
                new PdfTextElement
                {
                    X = 530,
                    Y = 66,
                    Width = 210,
                    Height = 20,
                    Text = "Pay Period: {{PayPeriod ?? August 2026}}",
                    FontSize = 10.5,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    Alignment = TextAlignmentMode.Center,
                    TextColorHex = "#334155"
                },

                // Divider Line
                new PdfDividerElement
                {
                    X = 50,
                    Y = 108,
                    Width = 700,
                    Height = 1,
                    ColorHex = "#E2E8F0",
                    Thickness = 1
                },

                // 4. Employee Information Summary Card (Container)
                new PdfShapeElement
                {
                    X = 50,
                    Y = 120,
                    Width = 700,
                    Height = 115,
                    CornerRadius = 8,
                    FillColorHex = "#F8FAFC",
                    StrokeColorHex = "#CBD5E1",
                    StrokeThickness = 1
                },

                // Section Header
                new PdfTextElement
                {
                    X = 65,
                    Y = 128,
                    Width = 300,
                    Height = 18,
                    Text = "EMPLOYEE & BANKING DETAILS",
                    FontSize = 10,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#0F6CBD"
                },

                // Row 1: Name, ID, Designation
                new PdfTextElement
                {
                    X = 65,
                    Y = 150,
                    Width = 220,
                    Height = 36,
                    Text = "Employee Name\n{{EmployeeName ?? John Doe}}",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#1E293B"
                },
                new PdfTextElement
                {
                    X = 300,
                    Y = 150,
                    Width = 200,
                    Height = 36,
                    Text = "Employee ID\n{{EmployeeId ?? EMP-2026-0842}}",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#1E293B"
                },
                new PdfTextElement
                {
                    X = 520,
                    Y = 150,
                    Width = 210,
                    Height = 36,
                    Text = "Designation\n{{Designation ?? Senior Software Architect}}",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#1E293B"
                },

                // Row 2: Department, Bank Details, PAN/Tax ID
                new PdfTextElement
                {
                    X = 65,
                    Y = 190,
                    Width = 220,
                    Height = 36,
                    Text = "Department\n{{Department ?? Cloud Engineering}}",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#1E293B"
                },
                new PdfTextElement
                {
                    X = 300,
                    Y = 190,
                    Width = 200,
                    Height = 36,
                    Text = "Bank & Account No.\n{{BankName ?? First National Bank}} • {{AccountNumber ?? ****4892}}",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#1E293B"
                },
                new PdfTextElement
                {
                    X = 520,
                    Y = 190,
                    Width = 210,
                    Height = 36,
                    Text = "Date of Joining / Pay Days\n{{JoiningDate ?? 2022-04-15}} ({{WorkingDays ?? 30}} Days)",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#1E293B"
                },

                // 5. Earnings & Deductions Tables (Two Column Layout)

                // Left: Earnings Table
                new PdfTableElement
                {
                    X = 50,
                    Y = 250,
                    Width = 342,
                    Height = 220,
                    HeaderBackgroundHex = "#0F6CBD",
                    HeaderTextHex = "#FFFFFF",
                    AlternateRowBackgroundHex = "#F8FAFC",
                    BorderColorHex = "#CBD5E1",
                    Headers = new List<string> { "EARNINGS BREAKDOWN", "AMOUNT" },
                    Rows = new List<List<string>>
                    {
                        new() { "Basic Salary", "{{BasicSalary:C}}" },
                        new() { "House Rent Allowance (HRA)", "{{HRA:C}}" },
                        new() { "Special Allowance", "{{SpecialAllowance:C}}" },
                        new() { "Performance Incentive / Bonus", "{{Bonus:C}}" },
                        new() { "Medical & Conveyance Allowance", "{{MedicalAllowance:C}}" },
                        new() { "GROSS EARNINGS TOTAL", "{{GrossEarnings:C}}" }
                    }
                },

                // Right: Deductions Table
                new PdfTableElement
                {
                    X = 408,
                    Y = 250,
                    Width = 342,
                    Height = 220,
                    HeaderBackgroundHex = "#475569",
                    HeaderTextHex = "#FFFFFF",
                    AlternateRowBackgroundHex = "#F8FAFC",
                    BorderColorHex = "#CBD5E1",
                    Headers = new List<string> { "DEDUCTIONS BREAKDOWN", "AMOUNT" },
                    Rows = new List<List<string>>
                    {
                        new() { "Provident Fund (PF) / 401(k)", "{{ProvidentFund:C}}" },
                        new() { "Income Tax / TDS / PAYE", "{{IncomeTax:C}}" },
                        new() { "Professional Tax", "{{ProfessionalTax:C}}" },
                        new() { "Health & Life Insurance", "{{Insurance:C}}" },
                        new() { "Other Deductions", "{{OtherDeductions:C}}" },
                        new() { "TOTAL DEDUCTIONS", "{{TotalDeductions:C}}" }
                    }
                },

                // 6. Large Net Salary Highlight Card
                new PdfShapeElement
                {
                    X = 50,
                    Y = 490,
                    Width = 700,
                    Height = 82,
                    CornerRadius = 10,
                    FillColorHex = "#0F6CBD",
                    StrokeColorHex = "#0C599B",
                    StrokeThickness = 1
                },
                new PdfTextElement
                {
                    X = 75,
                    Y = 502,
                    Width = 350,
                    Height = 24,
                    Text = "NET SALARY PAYABLE (TAKE HOME)",
                    FontSize = 11,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#E0F2FE"
                },
                new PdfTextElement
                {
                    X = 75,
                    Y = 528,
                    Width = 350,
                    Height = 32,
                    Text = "{{NetSalary:C}}",
                    FontSize = 22,
                    FontFamily = "Segoe UI",
                    IsBold = true,
                    TextColorHex = "#FFFFFF"
                },
                new PdfTextElement
                {
                    X = 440,
                    Y = 512,
                    Width = 290,
                    Height = 44,
                    Text = "Amount in words:\n{{NetSalaryInWords ?? US Dollars Only}}",
                    FontSize = 9.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.3,
                    TextColorHex = "#E0F2FE"
                },

                // 7. Security Verification & Barcode Section
                new PdfShapeElement
                {
                    X = 50,
                    Y = 590,
                    Width = 700,
                    Height = 145,
                    CornerRadius = 8,
                    FillColorHex = "#FAFAFA",
                    StrokeColorHex = "#E2E8F0",
                    StrokeThickness = 1
                },

                // QR Code
                new PdfQrCodeElement
                {
                    X = 70,
                    Y = 605,
                    Width = 100,
                    Height = 100,
                    Content = "https://payroll.codefrydev.in/verify?emp={{EmployeeId}}&period={{PayPeriod}}&hash={{AuthHash ?? 8f92a10c}}",
                    Label = "SCAN TO VERIFY",
                    DarkColorHex = "#0F172A",
                    LightColorHex = "#FFFFFF"
                },

                // Verification details text
                new PdfTextElement
                {
                    X = 185,
                    Y = 608,
                    Width = 270,
                    Height = 110,
                    Text = "OFFICIAL DIGITAL RECORD\n\nThis payroll document is digitally signed and cryptographically registered in the CodeFryDev Enterprise Payroll Ledger.\n\nVerification Token: {{AuthHash ?? 8F92-A10C-5542}}\nDisbursement Date: {{PaymentDate ?? 2026-08-31}}",
                    FontSize = 8.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#475569"
                },

                // Barcode
                new PdfBarcodeElement
                {
                    X = 480,
                    Y = 612,
                    Width = 250,
                    Height = 55,
                    CodeValue = "{{EmployeeId ?? EMP-2026-0842}}",
                    BarcodeFormat = "Code128",
                    BarColorHex = "#0F172A",
                    BackgroundColorHex = "#FFFFFF",
                    ShowText = true
                },

                // Authorized Signatory Stamp Box
                new PdfTextElement
                {
                    X = 480,
                    Y = 680,
                    Width = 250,
                    Height = 40,
                    Text = "Authorized Payroll Officer\nCodeFryDev Inc.",
                    FontSize = 9,
                    FontFamily = "Segoe UI",
                    Alignment = TextAlignmentMode.Center,
                    TextColorHex = "#64748B"
                },

                // Confidentiality Legal Notice
                new PdfTextElement
                {
                    X = 50,
                    Y = 750,
                    Width = 700,
                    Height = 40,
                    Text = "CONFIDENTIALITY NOTICE: This document contains strictly confidential compensation information intended solely for the named employee. Unauthorized copying, distribution, or disclosure is strictly prohibited under corporate policy and applicable privacy laws.",
                    FontSize = 7.5,
                    FontFamily = "Segoe UI",
                    LineHeight = 1.35,
                    TextColorHex = "#94A3B8"
                }
            }
        };

        doc.Pages.Add(page);
        return doc;
    }
}
