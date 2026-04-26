using System.Runtime.CompilerServices;
using StudyApp.Models;
using StudyApp.Services;

namespace StudyApp.Pages;

public partial class NewNotePage : ContentPage
{
    private readonly UserSessionService _sessionService;
    private string? _editingNoteId;
    private string _headerText = "Create a new note";
    private string _hintText = "Your note is stored locally in database.json and can be edited any time.";
    private string _noteContent = string.Empty;
    private string _noteTitle = string.Empty;
    private string _saveButtonText = "Publish note";

    public NewNotePage(UserSessionService sessionService)
    {
        InitializeComponent();
        _sessionService = sessionService;
        BindingContext = this;

        // Default to create mode
        ConfigureForCreate();
    }

    public string HeaderText
    {
        get => _headerText;
        set => SetProperty(ref _headerText, value);
    }

    public string HintText
    {
        get => _hintText;
        set => SetProperty(ref _hintText, value);
    }

    public string SaveButtonText
    {
        get => _saveButtonText;
        set => SetProperty(ref _saveButtonText, value);
    }

    public string NoteTitle
    {
        get => _noteTitle;
        set => SetProperty(ref _noteTitle, value);
    }

    public string NoteContent
    {
        get => _noteContent;
        set => SetProperty(ref _noteContent, value);
    }

    /// <summary>
    /// Switch to create mode and reset fields.
    /// </summary>
    public void ConfigureForCreate()
    {
        _editingNoteId = null;
        Title = "New Note";
        HeaderText = "Create a new note";
        HintText = "Your note is stored locally in database.json and can be edited any time.";
        SaveButtonText = "Publish note";
        NoteTitle = string.Empty;
        NoteContent = string.Empty;
    }

    /// <summary>
    /// Switch to edit mode and populate existing note.
    /// </summary>
    public void ConfigureForEdit(NoteRecord note)
    {
        ArgumentNullException.ThrowIfNull(note);

        _editingNoteId = note.Id;
        Title = "Edit Note";
        HeaderText = "Edit note";
        HintText = "Update the title or content and save to overwrite this note.";
        SaveButtonText = "Save changes";
        NoteTitle = note.Title;
        NoteContent = note.Content;
    }

    /// <summary>
    /// Save note (create or update based on mode).
    /// </summary>
    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var result = string.IsNullOrWhiteSpace(_editingNoteId)
            ? await _sessionService.AddNoteAsync(NoteTitle, NoteContent)
            : await _sessionService.UpdateNoteAsync(_editingNoteId, NoteTitle, NoteContent);

        if (!result.Success)
        {
            await DisplayAlert(
                string.IsNullOrWhiteSpace(_editingNoteId) ? "Publish failed" : "Save failed",
                result.Message,
                "OK");
            return;
        }


        // Close page after success
        await Navigation.PopModalAsync();
    }


    /// <summary>
    /// Cancel and close page.
    /// </summary>
    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }


    /// <summary>
    /// Property helper (INotifyPropertyChanged).
    /// </summary>
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
