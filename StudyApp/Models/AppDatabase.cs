using System.Collections.Generic;

namespace StudyApp.Models;

// Simple in-memory database for the app.
// Used to store users, notes, and current login state.
public sealed class AppDatabase
{
    public string? CurrentUserId { get; set; }

    public List<UserRecord> Users { get; set; } = [];

    public List<NoteRecord> Notes { get; set; } = [];
}
