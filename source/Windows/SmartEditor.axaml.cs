using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;

namespace environment_manager;

public partial class SmartEditor : Window {
    private ObservableCollection<Models.PathEntry> _entries = new();
    private Dictionary<string,string> _compareMap = new(StringComparer.OrdinalIgnoreCase);

    public SmartEditor() { InitializeComponent(); }

    // Updated constructor to accept an optional compare map (path -> source variable)
    public SmartEditor(string name, string value, Dictionary<string,string>? compareMap = null) : this() {
        Title = $"Editing: {name}";

        if (compareMap != null) {
            _compareMap = new Dictionary<string,string>(compareMap, StringComparer.OrdinalIgnoreCase);
        }

        var parts = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts) AddEntry(p, false); // don't refresh duplicates per-item during load
        RefreshDuplicates();
        PathList.ItemsSource = _entries;
    }

    private void AddEntry(string path, bool refresh = true) {
        var entry = new Models.PathEntry { Path = path };

        // Re-evaluate duplicates when the path text changes
        entry.PropertyChanged += (s, e) => {
            if (e.PropertyName == nameof(Models.PathEntry.Path)) RefreshDuplicates();
        };

        _entries.Add(entry);

        if (refresh) RefreshDuplicates();
    }

    private void OnListDoubleTapped(object? sender, RoutedEventArgs e) {
        // Focus the edit textbox when user double-clicks an entry
        try {
            TxtEdit.Focus();
        } catch { }
    }

    private async void OnAddClick(object? sender, RoutedEventArgs e) {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Select Folder" });
        if (folders.Count > 0) {
            AddEntry(folders[0].Path.LocalPath);
        }
    }

    private void OnUpClick(object? sender, RoutedEventArgs e) {
        int idx = PathList.SelectedIndex;
        if (idx > 0) _entries.Move(idx, idx - 1);
    }

    private void OnDownClick(object? sender, RoutedEventArgs e) {
        int idx = PathList.SelectedIndex;
        if (idx >= 0 && idx < _entries.Count - 1) _entries.Move(idx, idx + 1);
    }

    private void OnRemoveClick(object? sender, RoutedEventArgs e) {
        if (PathList.SelectedItem is Models.PathEntry entry) {
            _entries.Remove(entry);
            RefreshDuplicates();
        }
    }

    private void OnCleanClick(object? sender, RoutedEventArgs e) {
        var toRemove = _entries.Where(x => !x.IsValid).ToList();
        foreach (var item in toRemove) _entries.Remove(item);
        RefreshDuplicates();
    }

    private void RefreshDuplicates() {
        var groups = _entries.GroupBy(x => {
            var k = x.NormalizedPath;
            return string.IsNullOrEmpty(k) ? x.Path.Trim().ToLowerInvariant() : k;
        });
        foreach (var g in groups) {
            bool isDup = g.Count() > 1;
            foreach (var entry in g) entry.IsDuplicate = isDup;
        }
        // Cross-scope (shadowed) detection using compare set
        if (_compareMap.Count > 0) {
            foreach (var entry in _entries) {
                var key = entry.NormalizedPath;
                if (string.IsNullOrEmpty(key)) key = entry.Path.Trim().ToLowerInvariant();

                if (_compareMap.TryGetValue(key, out var src)) {
                    // src string is formatted as "Scope Name" (e.g. "User BLENDER")
                    // We extract the name to check for explicit self-references
                    string srcVarName = "";
                    int sp = src.IndexOf(' ');
                    if (sp >= 0) srcVarName = src.Substring(sp + 1);

                    // If the path explicitly contains the variable name (e.g. %BLENDER%),
                    // treat it as an intentional reference, not a shadow/duplicate.
                    if (!string.IsNullOrEmpty(srcVarName) &&
                        entry.Path.IndexOf($"%{srcVarName}%", StringComparison.OrdinalIgnoreCase) >= 0) {
                        entry.IsShadowed = false;
                        entry.ShadowedBy = "";
                    } else {
                        entry.IsShadowed = true;
                        entry.ShadowedBy = src;
                    }
                } else {
                    entry.IsShadowed = false;
                    entry.ShadowedBy = "";
                }
            }
        } else {
            foreach (var entry in _entries) { entry.IsShadowed = false; entry.ShadowedBy = ""; }
        }
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e) {
        var result = string.Join(";", _entries.Select(x => x.Path));
        Close(result);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(null);
}
