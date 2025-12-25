using System;
using System.IO;
using System.Linq;

namespace environment_manager.Models;

// Shared validation logic
public static class PathValidator {
    // Normalize a path-like string for comparisons: expand env vars, trim, remove trailing separators,
    // attempt to get full path when possible, and fold case for comparison.
    public static string NormalizeForComparison(string rawPath) {
        if (string.IsNullOrWhiteSpace(rawPath)) return "";

        try {
            // Expand env vars first
            var expanded = Environment.ExpandEnvironmentVariables(rawPath).Trim().Trim('"');

            // Remove enclosing semicolons/spaces
            expanded = expanded.Trim().Trim(';');

            // Remove trailing directory separators
            expanded = expanded.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

            // Attempt to get full path for normalization when it looks like a path
            bool looksLikePath = expanded.Contains(System.IO.Path.DirectorySeparatorChar) || expanded.Contains(System.IO.Path.AltDirectorySeparatorChar) || expanded.Contains(':');

            if (looksLikePath) {
                try {
                    expanded = System.IO.Path.GetFullPath(expanded);
                } catch { /* leave expanded as-is */ }
            }

            return expanded.ToLowerInvariant();
        } catch {
            return rawPath.Trim().ToLowerInvariant();
        }
    }
    public static ValidationStatus Check(string rawPath) {
        if (string.IsNullOrWhiteSpace(rawPath)) return ValidationStatus.Warning;

        // 1. Heuristic: Is it a path?
        // Must contain separators, colon, or start with %
        bool looksLikePath = rawPath.Contains(System.IO.Path.DirectorySeparatorChar)
                          || rawPath.Contains(System.IO.Path.AltDirectorySeparatorChar)
                          || rawPath.Contains(':')
                          || rawPath.StartsWith("%");

        if (!looksLikePath) return ValidationStatus.NotAPath;

        try {
            string expanded = Environment.ExpandEnvironmentVariables(rawPath);

            // 2. File Check
            if (File.Exists(expanded)) return ValidationStatus.Valid;

            // 3. Directory Check
            if (Directory.Exists(expanded)) {
                // Check if empty
                try {
                    // EnumerateFileSystemEntries is faster than GetFiles/GetDirectories
                    if (!Directory.EnumerateFileSystemEntries(expanded).Any()) {
                        return ValidationStatus.Warning; // "Yellow for a path leading to an empty directory"
                    }
                } catch {
                    // If we can't read it (Access Denied), assume valid to be safe
                }
                return ValidationStatus.Valid;
            }

            // 4. Exists neither as file nor folder
            return ValidationStatus.Invalid;

        } catch {
            // "Broken being a value that looks like a path but is malformed"
            return ValidationStatus.Warning;
        }
    }
}
