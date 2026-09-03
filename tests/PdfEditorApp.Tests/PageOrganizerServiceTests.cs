using System;
using System.Collections.Generic;
using System.Linq;
using PdfEditorApp.Core.Models;
using PdfEditorApp.Core.Models.Elements;
using PdfEditorApp.Models;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels;
using Xunit;

namespace PdfEditorApp.Tests;

public class PageOrganizerServiceTests
{
    private static PdfDocumentModel CreateTestDoc(int pageCount)
    {
        var doc = new PdfDocumentModel
        {
            Title = "Master_Contract.pdf",
            Author = "Legal Department",
            Subject = "Multi-party Agreement"
        };

        for (int i = 1; i <= pageCount; i++)
        {
            var page = new PdfPageModel
            {
                PageNumber = i,
                Width = 800,
                Height = 1131,
                RotationAngle = 0
            };
            page.Elements.Add(new PdfTextElement
            {
                Text = $"Clause {i}.0 Content",
                X = 50,
                Y = 50,
                Width = 200,
                Height = 30
            });
            doc.Pages.Add(page);
        }

        return doc;
    }

    [Fact]
    public void PageOrganizerService_SplitEveryNPages_SplitsCorrectly()
    {
        var service = new PageOrganizerService();
        var doc = CreateTestDoc(7);

        // Act: Split every 3 pages
        var parts = service.SplitEveryNPages(doc, 3);

        // Assert: 3 parts (3 + 3 + 1)
        Assert.Equal(3, parts.Count);
        Assert.Equal(3, parts[0].Pages.Count);
        Assert.Equal(3, parts[1].Pages.Count);
        Assert.Single(parts[2].Pages);

        // Verify page renumbering in each split
        Assert.Equal(1, parts[0].Pages[0].PageNumber);
        Assert.Equal(2, parts[0].Pages[1].PageNumber);
        Assert.Equal(3, parts[0].Pages[2].PageNumber);

        Assert.Equal(1, parts[1].Pages[0].PageNumber);
        Assert.Equal(2, parts[1].Pages[1].PageNumber);
        Assert.Equal(3, parts[1].Pages[2].PageNumber);

        Assert.Equal(1, parts[2].Pages[0].PageNumber);

        // Verify content preserved
        Assert.Equal("Clause 1.0 Content", ((PdfTextElement)parts[0].Pages[0].Elements[0]).Text);
        Assert.Equal("Clause 7.0 Content", ((PdfTextElement)parts[2].Pages[0].Elements[0]).Text);
    }

    [Fact]
    public void PageOrganizerService_SplitByRanges_PartitionsRanges()
    {
        var service = new PageOrganizerService();
        var doc = CreateTestDoc(6);

        // Act: Split by "1-2, 3-4, 5-6"
        var parts = service.SplitByRanges(doc, "1-2, 3-4, 5-6");

        // Assert
        Assert.Equal(3, parts.Count);
        Assert.All(parts, p => Assert.Equal(2, p.Pages.Count));
        Assert.Equal("Clause 3.0 Content", ((PdfTextElement)parts[1].Pages[0].Elements[0]).Text);
        Assert.Equal("Clause 4.0 Content", ((PdfTextElement)parts[1].Pages[1].Elements[0]).Text);
    }

    [Fact]
    public void PageOrganizerService_ExtractPages_ExtractsSubset()
    {
        var service = new PageOrganizerService();
        var doc = CreateTestDoc(5);

        // Act: Extract page indexes 0, 2, 4 (Pages 1, 3, 5)
        var extracted = service.ExtractPages(doc, new[] { 0, 2, 4 });

        // Assert
        Assert.Equal(3, extracted.Pages.Count);
        Assert.Equal(1, extracted.Pages[0].PageNumber);
        Assert.Equal(2, extracted.Pages[1].PageNumber);
        Assert.Equal(3, extracted.Pages[2].PageNumber);
        Assert.Equal("Clause 1.0 Content", ((PdfTextElement)extracted.Pages[0].Elements[0]).Text);
        Assert.Equal("Clause 3.0 Content", ((PdfTextElement)extracted.Pages[1].Elements[0]).Text);
        Assert.Equal("Clause 5.0 Content", ((PdfTextElement)extracted.Pages[2].Elements[0]).Text);
    }

    [Fact]
    public void PageOrganizerService_MergeDocuments_CombinesPagesAndRenumbers()
    {
        var service = new PageOrganizerService();
        var doc1 = CreateTestDoc(2);
        var doc2 = CreateTestDoc(3);

        // Act
        var merged = service.MergeDocuments(new[] { doc1, doc2 }, "Merged_Master.pdf");

        // Assert
        Assert.Equal(5, merged.Pages.Count);
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(i + 1, merged.Pages[i].PageNumber);
        }
        Assert.Equal("Clause 1.0 Content", ((PdfTextElement)merged.Pages[0].Elements[0]).Text);
        Assert.Equal("Clause 1.0 Content", ((PdfTextElement)merged.Pages[2].Elements[0]).Text); // from doc2
    }

    [Fact]
    public void PageOrganizerService_BatchRotatePages_RotatesTargetPages()
    {
        var service = new PageOrganizerService();
        var doc = CreateTestDoc(4);

        // Act: Rotate even pages by 90
        int count = service.BatchRotatePages(doc, PageFilterTarget.EvenPages, 90);

        // Assert
        Assert.Equal(2, count);
        Assert.Equal(0, doc.Pages[0].RotationAngle); // Page 1 (odd)
        Assert.Equal(90, doc.Pages[1].RotationAngle); // Page 2 (even)
        Assert.Equal(0, doc.Pages[2].RotationAngle); // Page 3 (odd)
        Assert.Equal(90, doc.Pages[3].RotationAngle); // Page 4 (even)
    }

    [Fact]
    public void PageOrganizerService_ParsePageRanges_ParsesComplexStrings()
    {
        var service = new PageOrganizerService();

        var indexes = service.ParsePageRanges("1-3, 5, 7-8", 10);
        Assert.Equal(new[] { 0, 1, 2, 4, 6, 7 }, indexes);

        var clamped = service.ParsePageRanges("1-20", 5);
        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, clamped);
    }

    [Fact]
    public void MainViewModel_ReorderPage_MovesAndMaintainsUndo()
    {
        // Arrange
        var vm = new MainViewModel();
        while (vm.Pages.Count < 3) vm.AddPage();

        var firstPage = vm.Pages[0];
        var secondPage = vm.Pages[1];
        var thirdPage = vm.Pages[2];

        // Act - Move Page 1 (index 0) to Page 3 (index 2)
        vm.ReorderPage(0, 2);

        // Assert - Order is now [secondPage, thirdPage, firstPage]
        Assert.Same(secondPage, vm.Pages[0]);
        Assert.Same(thirdPage, vm.Pages[1]);
        Assert.Same(firstPage, vm.Pages[2]);
        Assert.Equal(1, vm.Pages[0].PageNumber);
        Assert.Equal(2, vm.Pages[1].PageNumber);
        Assert.Equal(3, vm.Pages[2].PageNumber);

        // Act - Undo reorder
        vm.UndoRedo.Undo();
        Assert.Same(firstPage, vm.Pages[0]);
        Assert.Same(secondPage, vm.Pages[1]);
        Assert.Same(thirdPage, vm.Pages[2]);

        // Act - Redo reorder
        vm.UndoRedo.Redo();
        Assert.Same(secondPage, vm.Pages[0]);
        Assert.Same(thirdPage, vm.Pages[1]);
        Assert.Same(firstPage, vm.Pages[2]);
    }
}
