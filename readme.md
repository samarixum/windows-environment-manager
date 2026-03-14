# Environment Manager

Environment Manager is a desktop app for viewing and editing Windows environment variables with safer PATH editing and quick visual validation.

It is built with Avalonia UI and targets Windows.

## What It Does

- Shows User and System environment variables in separate panels.
- Supports creating, editing, and deleting variables.
- Opens a smart multi-entry editor for semicolon-separated values (especially PATH-like variables).
- Highlights potential issues with color indicators (invalid paths, duplicates, warnings, etc.).
- Detects cross-scope overlap (for example, user entries that duplicate system entries).
- Copies the previous value to clipboard before delete as a safety fallback.

## Key Features

### 1) User vs System Scope

- User variables are editable in normal mode.
- System variables require Administrator privileges to create/edit/delete.
- In non-admin mode, system values are visible but protected (read-only behavior enforced by app actions).

### 2) Smart Editor For PATH-Like Values

When editing a variable value, the Smart Editor splits entries by semicolon and allows:

- Add folder from picker.
- Move entries up/down.
- Edit a selected entry inline.
- Remove selected entry.
- Remove all dead entries (entries flagged invalid).
- Save back to a semicolon-joined value.

### 3) Validation + Color Status

The app computes status for variables and individual path entries:

- Green: valid path/file or healthy value.
- Yellow: warning (empty value, empty directory, malformed/uncertain path case).
- Orange: duplicate or shadowed value.
- Red: path-like value that does not exist.
- Blue: non-path value (informational).

### 4) Shadowing / Duplicate Detection

- Detects duplicate entries within the same variable value.
- Detects overlap against system/user values when comparing scopes.
- Marks user variables that shadow system variables with the same name.

## Tech Stack

- .NET 10 (target framework: net10.0-windows)
- C#
- Avalonia UI 11

## Requirements

- Windows
- .NET 10 SDK (or compatible preview SDK for net10.0-windows)

## Build And Run

From the repository root:

```powershell
dotnet restore
dotnet build
dotnet run --project "environment manager.csproj"
```

To manage System variables, launch the app as Administrator.

## Project Structure

- source/Program.cs: app entry point.
- source/App.axaml + source/App.axaml.cs: app bootstrap and theme setup.
- source/Windows/MainWindow.axaml(.cs): main UI and variable CRUD workflow.
- source/Windows/SmartEditor.axaml(.cs): structured editor for semicolon-separated entries.
- source/Models/PathValidator.cs: path heuristics and validation rules.
- source/Models/EnvVarItem.cs + source/Models/PathEntry.cs: UI-bound models and status logic.

## Notes

- The app uses Windows-specific APIs (registry + machine/user environment targets), so this build is Windows-focused.
- Changes are written through standard environment variable APIs and refreshed in the app after each operation.

## Future Improvements

- Add export/import of selected variables.
- Add search/filter and sort options.
- Add undo/history for safer bulk edits.
- Add installer/package for easier distribution.

## Notice
This project was generated entirely with AI.
