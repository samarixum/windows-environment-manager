
namespace environment_manager.Models;

// Enum to define the specific state of a variable or path
public enum ValidationStatus {
    NotAPath,   // Blue: Not a path (e.g. config value)
    Valid,      // Green: Exists and has content
    Warning,    // Yellow: Empty string, Malformed path, or Empty Directory
    Duplicate,  // Yellow/Orange: Duplicate entries
    Invalid     // Red: Path looks valid but does not exist
}

