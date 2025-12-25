using Avalonia.Media;

using System;
using System.ComponentModel;

namespace environment_manager.Models;

public class PathEntry : INotifyPropertyChanged {
    private string _path = "";
    private ValidationStatus _status;
    private bool _isDuplicate;
    private bool _isShadowed;

    public string Path {
        get => _path;
        set {
            if (_path == value) return;
            _path = value;
            Validate();
            OnPropertyChanged(nameof(Path));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(ResolvedPath)); // Update resolved path when raw changes
            OnPropertyChanged(nameof(IsValid)); // For "Remove Dead" logic
        }
    }

    public bool IsDuplicate {
        get => _isDuplicate;
        set {
            if (_isDuplicate == value) return;
            _isDuplicate = value;
            OnPropertyChanged(nameof(IsDuplicate));
            OnPropertyChanged(nameof(StatusColor));
        }
    }

    // Normalized form used for duplicate/shadow comparisons (expanded, trimmed, normalized case)
    public string NormalizedPath => PathValidator.NormalizeForComparison(_path);

    public bool IsShadowed {
        get => _isShadowed;
        set {
            if (_isShadowed == value) return;
            _isShadowed = value;
            OnPropertyChanged(nameof(IsShadowed));
            OnPropertyChanged(nameof(StatusColor));
        }
    }

    private string _shadowedBy = "";
    public string ShadowedBy {
        get => _shadowedBy;
        set {
            if (_shadowedBy == value) return;
            _shadowedBy = value;
            OnPropertyChanged(nameof(ShadowedBy));
        }
    }

    // Returns the expanded path (e.g. C:\Windows) if different from raw
    public string ResolvedPath {
        get {
             var expanded = Environment.ExpandEnvironmentVariables(_path);
             return expanded == _path ? "" : expanded;
        }
    }

    // Used by "Remove Dead" - we consider Red items 'Invalid' enough to delete.
    // Yellow items (Empty dirs) are kept unless user manually deletes.
    public bool IsValid => _status != ValidationStatus.Invalid;

    public IBrush StatusColor {
        get {
            if (_status == ValidationStatus.Invalid) return Brushes.IndianRed;
            if (_isDuplicate || _isShadowed) return Brushes.Orange;

            switch (_status) {
                case ValidationStatus.NotAPath: return Brushes.DodgerBlue;
                case ValidationStatus.Warning:  return Brushes.Gold;
                default:                        return Brushes.LimeGreen;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

    private void Validate() {
        _status = PathValidator.Check(_path);
    }
}

