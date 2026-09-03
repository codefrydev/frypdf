using System;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using Xunit;

namespace PdfEditorApp.Tests;

public class SignatureAndFormsServiceTests
{
    [Fact]
    public void SignatureService_ComputeDocumentSha256_ProducesDeterministicHash()
    {
        // Arrange
        var service = new SignatureService();
        var doc1 = new PdfDocumentModel
        {
            Title = "Contract_Final.pdf",
            Author = "Enterprise Signer",
            Subject = "Service Agreement"
        };
        var page1 = new PdfPageModel { PageNumber = 1 };
        page1.Elements.Add(new PdfTextElement { Text = "Terms & Conditions", X = 50, Y = 50 });
        doc1.Pages.Add(page1);

        var doc2 = new PdfDocumentModel
        {
            Title = "Contract_Final.pdf",
            Author = "Enterprise Signer",
            Subject = "Service Agreement"
        };
        var page2 = new PdfPageModel { PageNumber = 1 };
        page2.Elements.Add(new PdfTextElement { Text = "Terms & Conditions", X = 50, Y = 50 });
        doc2.Pages.Add(page2);

        // Act
        string hash1 = service.ComputeDocumentSha256(doc1);
        string hash2 = service.ComputeDocumentSha256(doc2);

        // Assert
        Assert.Equal(64, hash1.Length);
        Assert.Equal(hash1, hash2);

        // Modify doc2
        doc2.Title = "Contract_Tampered.pdf";
        string hash3 = service.ComputeDocumentSha256(doc2);
        Assert.NotEqual(hash1, hash3);
    }

    [Fact]
    public void SignatureService_CreateCryptographicSignatureSeal_PopulatesMetadataAndDigest()
    {
        // Arrange
        var service = new SignatureService();
        string dummyDigest = "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855";

        // Act
        var seal = service.CreateCryptographicSignatureSeal("Dr. Evelyn Reed", "Global Medical Corp", "Clinical Approval", dummyDigest, 120, 250);

        // Assert
        Assert.NotNull(seal);
        Assert.Equal(120, seal.X);
        Assert.Equal(250, seal.Y);
        Assert.Equal(320, seal.Width);
        Assert.Equal(100, seal.Height);
        Assert.Contains("Dr. Evelyn Reed", seal.Label);
        Assert.Contains("Global Medical Corp", seal.Label);
        Assert.Contains("Clinical Approval", seal.Label);
        Assert.Contains("SHA256:", seal.Label);
        Assert.Equal("#16A34A", seal.StrokeColorHex); // Emerald verification border
    }

    [Fact]
    public void SignatureService_CreateCursiveSignatureElement_AppliesFontAndStyleCorrectly()
    {
        // Arrange
        var service = new SignatureService();

        // Act
        var sigExecutive = service.CreateCursiveSignatureElement("Alexander Hamilton", SignatureStyle.CursiveElegance, 50, 100);
        var sigScript = service.CreateCursiveSignatureElement("Alexander Hamilton", SignatureStyle.ClassicScript, 50, 100);

        // Assert
        Assert.Equal("Georgia", sigExecutive.FontFamily);
        Assert.True(sigExecutive.IsItalic);
        Assert.Equal("Alexander Hamilton", sigExecutive.Text);

        Assert.Equal("Times New Roman", sigScript.FontFamily);
        Assert.Equal("#0F6CBD", sigScript.TextColorHex);
    }

    [Fact]
    public void SignatureService_CreateFormFieldElement_GeneratesProperFieldTypes()
    {
        // Arrange
        var service = new SignatureService();

        // Act
        var txtField = service.CreateFormFieldElement(FormFieldType.Text, "txt_customer_name", 50, 100, 200, 30);
        var chkField = service.CreateFormFieldElement(FormFieldType.Checkbox, "chk_terms_agreed", 50, 150, 24, 24);

        // Assert
        Assert.Equal(FormFieldType.Text, txtField.FieldType);
        Assert.Equal("txt_customer_name", txtField.FieldName);
        Assert.Equal(200, txtField.Width);

        Assert.Equal(FormFieldType.Checkbox, chkField.FieldType);
        Assert.Equal("chk_terms_agreed", chkField.FieldName);
        Assert.Equal("false", chkField.DefaultValue);
    }
}
