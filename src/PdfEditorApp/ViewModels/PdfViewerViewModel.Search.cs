using System;
using System.Linq;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.Input;

namespace PdfEditorApp.ViewModels;

public partial class PdfViewerViewModel
{
    // --- Text Search & Find in Document ---

    [RelayCommand]
    public void ToggleSearchBar()
    {
        IsSearchBarVisible = !IsSearchBarVisible;
        if (IsSearchBarVisible)
        {
            SelectedSidebarTab = PdfViewerSidebarTab.Search;
            IsSidebarOpen = true;
        }
    }

    [RelayCommand]
    public void PerformSearch()
    {
        SearchResults.Clear();
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            TotalMatchesCount = 0;
            CurrentMatchIndex = 0;
            SearchStatusText = string.Empty;
            OnPropertyChanged(nameof(HasSearchResults));
            return;
        }

        string q = SearchQuery.Trim();
        int matchIdx = 1;

        if (SearchWholeWord)
        {
            var regexOptions = SearchMatchCase ? RegexOptions.None : RegexOptions.IgnoreCase;
            string pattern = $@"\b{Regex.Escape(q)}\b";

            foreach (var page in Pages)
            {
                if (string.IsNullOrEmpty(page.ExtractedText)) continue;

                var matches = Regex.Matches(page.ExtractedText, pattern, regexOptions);
                foreach (Match m in matches)
                {
                    int snippetStart = Math.Max(0, m.Index - 25);
                    int snippetLen = Math.Min(page.ExtractedText.Length - snippetStart, m.Length + 50);
                    string snippet = "..." + page.ExtractedText.Substring(snippetStart, snippetLen).Replace('\r', ' ').Replace('\n', ' ') + "...";

                    SearchResults.Add(new PdfViewerSearchMatch
                    {
                        PageNumber = page.PageNumber,
                        Snippet = snippet,
                        MatchIndex = matchIdx++
                    });
                }
            }
        }
        else
        {
            var comp = SearchMatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            foreach (var page in Pages)
            {
                if (string.IsNullOrEmpty(page.ExtractedText)) continue;

                int startIndex = 0;
                while ((startIndex = page.ExtractedText.IndexOf(q, startIndex, comp)) != -1)
                {
                    int snippetStart = Math.Max(0, startIndex - 25);
                    int snippetLen = Math.Min(page.ExtractedText.Length - snippetStart, q.Length + 50);
                    string snippet = "..." + page.ExtractedText.Substring(snippetStart, snippetLen).Replace('\r', ' ').Replace('\n', ' ') + "...";

                    SearchResults.Add(new PdfViewerSearchMatch
                    {
                        PageNumber = page.PageNumber,
                        Snippet = snippet,
                        MatchIndex = matchIdx++
                    });

                    startIndex += q.Length;
                }
            }
        }

        TotalMatchesCount = SearchResults.Count;
        CurrentMatchIndex = TotalMatchesCount > 0 ? 1 : 0;
        SearchStatusText = TotalMatchesCount > 0 ? $"{CurrentMatchIndex} of {TotalMatchesCount} matches" : "No matches found";
        OnPropertyChanged(nameof(HasSearchResults));

        if (TotalMatchesCount > 0)
        {
            JumpToMatch(SearchResults[0]);
        }
    }

    [RelayCommand]
    public void NextMatch()
    {
        if (TotalMatchesCount == 0) return;
        CurrentMatchIndex = (CurrentMatchIndex % TotalMatchesCount) + 1;
        SearchStatusText = $"{CurrentMatchIndex} of {TotalMatchesCount} matches";
        JumpToMatch(SearchResults[CurrentMatchIndex - 1]);
    }

    [RelayCommand]
    public void PreviousMatch()
    {
        if (TotalMatchesCount == 0) return;
        CurrentMatchIndex = (CurrentMatchIndex - 2 + TotalMatchesCount) % TotalMatchesCount + 1;
        SearchStatusText = $"{CurrentMatchIndex} of {TotalMatchesCount} matches";
        JumpToMatch(SearchResults[CurrentMatchIndex - 1]);
    }

    [RelayCommand]
    public void JumpToMatch(PdfViewerSearchMatch? match)
    {
        if (match == null) return;
        int pageIdx = match.PageNumber - 1;
        if (pageIdx >= 0 && pageIdx < Pages.Count)
        {
            SelectedPage = Pages[pageIdx];
            CurrentPageNumber = match.PageNumber;
            RequestScrollToPage(match.PageNumber);
        }
    }

    [RelayCommand]
    public void ClearSearch()
    {
        SearchQuery = string.Empty;
        SearchResults.Clear();
        TotalMatchesCount = 0;
        CurrentMatchIndex = 0;
        SearchStatusText = string.Empty;
        OnPropertyChanged(nameof(HasSearchResults));
    }

}
