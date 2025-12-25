using Avalonia.Media;

using System;
using System.Linq;
using System.Collections.Generic;

namespace environment_manager.Models;

public class EnvVarItem {
    public string Scope { get; set; } = "";
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    // The value of the variable with the same name in the OTHER scope (e.g. System value for a User var)
    public string CompareValue { get; set; } = "";
    public bool IsShadowed { get; set; } = false; // Exists in Machine and User (shadowing)

    public IBrush StatusColor {
        get {
            switch (GetStatus()) {
                case ValidationStatus.NotAPath: return Brushes.DodgerBlue;
                case ValidationStatus.Warning:  return Brushes.Gold;
                case ValidationStatus.Duplicate: return Brushes.Orange;
                case ValidationStatus.Invalid:  return Brushes.IndianRed;
                default:                        return Brushes.LimeGreen;
            }
        }
    }

    private ValidationStatus GetStatus() {
        if (string.IsNullOrWhiteSpace(Value)) return ValidationStatus.Warning; // "Yellow for empty"

        // Shadowing has priority as a Duplicate state for variable-level duplicates
        if (IsShadowed) return ValidationStatus.Duplicate;

        var parts = Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        var compareParts = CompareValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        var compareSet = new HashSet<string>(compareParts.Select(p => PathValidator.NormalizeForComparison(p)), StringComparer.OrdinalIgnoreCase);

        // 1. Invalid Paths (Red) - Highest Priority
        foreach (var part in parts) {
            if (PathValidator.Check(part) == ValidationStatus.Invalid) return ValidationStatus.Invalid;
        }

        // 2. Duplicates (Orange) - Internal OR Cross-Scope
        var distinctSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in parts) {
            var norm = PathValidator.NormalizeForComparison(part);
            // Check internal duplicate
            if (!distinctSet.Add(norm)) return ValidationStatus.Duplicate;
            // Check if existing in the other scope (e.g. System Path)
            if (compareSet.Contains(norm)) return ValidationStatus.Duplicate;
        }

        if (IsShadowed && parts.Length == 1) return ValidationStatus.Duplicate; // Exact match on single var

        // 3. Warnings (Yellow)
        bool hasPathLike = false;
        foreach (var part in parts) {
            var status = PathValidator.Check(part);
            if (status == ValidationStatus.Warning) return ValidationStatus.Warning;
            if (status != ValidationStatus.NotAPath) hasPathLike = true;
        }

        if (!hasPathLike) return ValidationStatus.NotAPath;
        return ValidationStatus.Valid;
    }
}

