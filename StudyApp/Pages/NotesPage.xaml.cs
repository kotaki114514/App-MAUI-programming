using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using StudyApp.Models;
using StudyApp.Services;
using StudyApp.Utilities;

namespace StudyApp.Pages;

public partial class NotesPage : ContentView
{
    private readonly IServiceProvider _serviceProvider;
    private readonly UserSessionService _sessionService;
    private List<NoteRecord> _allNotes = [];
    private string _notesCountText = "0 notes";
    private string _searchText = string.Empty;
    private string _emptyStateTitle = "No notes yet";
    private string _emptyStateMessage = "Tap \"Create note\" to save what you learned today.";

    public NotesPage(UserSessionService sessionService, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _sessionService = sessionService;
        _serviceProvider = serviceProvider;
        BindingContext = this;

        // Reload when data changes
        _sessionService.DataChanged += async (_, _) => await MainThread.InvokeOnMainThreadAsync(LoadAsync);
    }

    public ObservableCollection<NoteRecord> Notes { get; } = new();



    public string NotesCountText
    {
        get => _notesCountText;
        set => SetProperty(ref _notesCountText, value);
    }


    // Search input (triggers filtering)
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public string EmptyStateTitle
    {
        get => _emptyStateTitle;
        set => SetProperty(ref _emptyStateTitle, value);
    }

    public string EmptyStateMessage
    {
        get => _emptyStateMessage;
        set => SetProperty(ref _emptyStateMessage, value);
    }

    public Task RefreshAsync()
    {
        return LoadAsync();
    }



    // Load notes from service
    private async Task LoadAsync()
    {
        var notes = await _sessionService.GetCurrentUserNotesAsync();
        _allNotes = [.. notes];
        ApplyFilter();
    }


    // Apply search filter
    private void ApplyFilter()
    {
        IEnumerable<NoteRecord> filteredNotes = _allNotes;
        var keyword = SearchText?.Trim();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            filteredNotes = _allNotes.Where(note =>
                note.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                note.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        Notes.Clear();
        foreach (var note in filteredNotes)
        {
            Notes.Add(note);
        }

        UpdateNotesCountText();
        UpdateEmptyState();
    }


    // Update header text
    private void UpdateNotesCountText()
    {
        if (_allNotes.Count == 0)
        {
            NotesCountText = "No notes yet. Create your first one above.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            NotesCountText = $"{_allNotes.Count} note(s) available. You can edit or delete them any time.";
            return;
        }

        NotesCountText = Notes.Count == 0
            ? $"No matches found for \"{SearchText.Trim()}\"."
            : $"Showing {Notes.Count} of {_allNotes.Count} note(s) for \"{SearchText.Trim()}\".";
    }


    // Update empty view text
    private void UpdateEmptyState()
    {
        if (_allNotes.Count == 0)
        {
            EmptyStateTitle = "No notes yet";
            EmptyStateMessage = "Tap \"Create note\" to save what you learned today.";
            return;
        }

        EmptyStateTitle = "No matching notes";
        EmptyStateMessage = "Try a different keyword or clear the search to see all notes.";
    }


    // Open create page
    private async void OnPublishNoteClicked(object? sender, EventArgs e)
    {
        var page = _serviceProvider.GetRequiredService<NewNotePage>();
        page.ConfigureForCreate();
        await AppNavigator.PushModalAsync(new NavigationPage(page));
    }


    // Open edit page
    private async void OnEditNoteClicked(object? sender, EventArgs e)
    {
        if (GetNoteFromSender(sender) is not NoteRecord note)
        {
            return;
        }

        var page = _serviceProvider.GetRequiredService<NewNotePage>();
        page.ConfigureForEdit(note);
        await AppNavigator.PushModalAsync(new NavigationPage(page));
    }



    // Delete note with confirmation
    private async void OnDeleteNoteClicked(object? sender, EventArgs e)
    {
        if (GetNoteFromSender(sender) is not NoteRecord note)
        {
            return;
        }

        var confirm = await AppNavigator.DisplayAlertAsync(
            "Delete note",
            $"Delete \"{note.Title}\"? This action cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirm)
        {
            return;
        }

        var result = await _sessionService.DeleteNoteAsync(note.Id);
        if (!result.Success)
        {
            await AppNavigator.DisplayAlertAsync("Delete failed", result.Message, "OK");
        }
    }


    // Extract note from button parameter
    private static NoteRecord? GetNoteFromSender(object? sender)
    {
        return (sender as Button)?.CommandParameter as NoteRecord;
    }



    // Property helper
    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
