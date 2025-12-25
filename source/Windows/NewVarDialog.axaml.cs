using Avalonia.Controls;
using Avalonia.Interactivity;

namespace environment_manager;

public partial class NewVarDialog : Window {
    public NewVarDialog() { InitializeComponent(); }

    // Overloaded constructor to preset scope (forceMachine == true => Machine)
    public NewVarDialog(bool forceMachine) : this() {
        if (forceMachine) {
            CmbScope.SelectedIndex = 1; // Machine
            CmbScope.IsEnabled = false; // Lock selection
        } else {
            CmbScope.SelectedIndex = 0; // User
            CmbScope.IsEnabled = false; // Lock selection
        }
    }

    private void OnCreateClick(object? sender, RoutedEventArgs e) {
        string scope = ((ComboBoxItem?)CmbScope.SelectedItem)?.Content?.ToString() ?? "User";
        if (string.IsNullOrWhiteSpace(TxtName.Text)) return;

        Close(new Models.EnvVarItem {
            Scope = scope,
            Name = TxtName.Text,
            Value = TxtValue.Text ?? ""
        });
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(null);
}
