using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Principal;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Input;
using Microsoft.Win32; // Required for Registry access

namespace environment_manager;

public partial class MainWindow : Window {
    private ObservableCollection<Models.EnvVarItem> _userVars = new();
    private ObservableCollection<Models.EnvVarItem> _sysVars = new();
    private bool _isAdmin;
    private Dictionary<string,string> _sysMap = new(StringComparer.OrdinalIgnoreCase);

    public MainWindow() {
        InitializeComponent();

        if (OperatingSystem.IsWindows()) {
            _isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        } else {
            _isAdmin = false;
        }

        if (_isAdmin) Title += " (Administrator)";
        else Title += " (User Mode - System Read-Only)";

        ListUser.ItemsSource = _userVars;
        ListSystem.ItemsSource = _sysVars;

        LoadData();
    }

    private void LoadData() {
        _userVars.Clear();
        _sysVars.Clear();

        if (!OperatingSystem.IsWindows()) return;

        // First load SYSTEM variables so we can detect shadowing
        var sysNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _sysMap.Clear();
        var sysMap = _sysMap;
        try {
            using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment");
            if (key != null) {
                var list = key.GetValueNames()
                    .Select(n => new Models.EnvVarItem {
                        Scope = "Machine",
                        Name = n,
                        Value = key.GetValue(n, "", RegistryValueOptions.DoNotExpandEnvironmentNames)?.ToString() ?? ""
                    })
                    .OrderBy(x => x.Name)
                    .ToList();
                foreach (var item in list) {
                    _sysVars.Add(item);
                    sysNames.Add(item.Name);
                    sysMap[item.Name] = item.Value;
                }
            }
        } catch { /* Handle perms */ }

        // Then load USER variables and mark shadowed names
        try {
            using var key = Registry.CurrentUser.OpenSubKey("Environment");
            if (key != null) {
                var list = key.GetValueNames()
                    .Select(n => {
                        string val = key.GetValue(n, "", RegistryValueOptions.DoNotExpandEnvironmentNames)?.ToString() ?? "";
                        bool hasSys = sysMap.ContainsKey(n);
                        return new Models.EnvVarItem {
                            Scope = "User",
                            Name = n,
                            Value = val,
                            IsShadowed = hasSys,
                            CompareValue = hasSys ? sysMap[n] : ""
                        };
                    })
                    .OrderBy(x => x.Name)
                    .ToList();
                foreach (var item in list) _userVars.Add(item);
            }
        } catch { /* Handle perms */ }
    }

    // --- USER ACTIONS ---

    private async void OnNewUserClick(object? sender, RoutedEventArgs e) {
        var dialog = new NewVarDialog(false); // Force User scope
        var result = await dialog.ShowDialog<Models.EnvVarItem?>(this);
        if (result != null) SaveVar("User", result.Name, result.Value);
    }

    private void OnEditUserClick(object? sender, RoutedEventArgs e) {
        if (ListUser.SelectedItem is Models.EnvVarItem item) EditItem(item);
    }

    private void OnDeleteUserClick(object? sender, RoutedEventArgs e) {
        if (ListUser.SelectedItem is Models.EnvVarItem item) DeleteItem(item);
    }

    // --- SYSTEM ACTIONS ---

    private async void OnNewSystemClick(object? sender, RoutedEventArgs e) {
        if (!_isAdmin) {
            await new TextDialog("Access Denied", "Run as Administrator to create system variables.").ShowDialog(this);
            return;
        }
        var dialog = new NewVarDialog(true); // Force Machine scope
        var result = await dialog.ShowDialog<Models.EnvVarItem?>(this);
        if (result != null) SaveVar("Machine", result.Name, result.Value);
    }

    private void OnEditSystemClick(object? sender, RoutedEventArgs e) {
        if (ListSystem.SelectedItem is Models.EnvVarItem item) EditItem(item);
    }

    private void OnDeleteSystemClick(object? sender, RoutedEventArgs e) {
        if (ListSystem.SelectedItem is Models.EnvVarItem item) DeleteItem(item);
    }

    // --- SHARED LOGIC ---

    private async void EditItem(Models.EnvVarItem item) {
        if (item.Scope == "Machine" && !_isAdmin) {
            await new TextDialog("Access Denied", "Run as Administrator to edit system variables.").ShowDialog(this);
            return;
        }

        // Build compare map: normalized path -> source variable name (e.g. "Machine PATH" or "User BLENDER")
        var compareMap = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);

        // Add system variables (all paths from system scope)
        foreach (var kv in _sysMap) {
            var sysName = kv.Key;
            var parts = (kv.Value ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts) {
                var k = Models.PathValidator.NormalizeForComparison(p);
                if (string.IsNullOrEmpty(k)) continue;
                if (!compareMap.ContainsKey(k)) compareMap[k] = $"Machine {sysName}";
            }
        }

        // Add other user variables (so a PATH entry can detect matches in USER vars like BLENDER)
        foreach (var uv in _userVars) {
            if (uv.Name.Equals(item.Name, StringComparison.OrdinalIgnoreCase)) continue; // skip current
            var parts = (uv.Value ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts) {
                var k = Models.PathValidator.NormalizeForComparison(p);
                if (string.IsNullOrEmpty(k)) continue;
                if (!compareMap.ContainsKey(k)) compareMap[k] = $"User {uv.Name}";
            }
        }

        var editor = new SmartEditor(item.Name, item.Value, compareMap);
        var result = await editor.ShowDialog<string?>(this);

        if (result != null) {
            SaveVar(item.Scope, item.Name, result);
        }
    }

    private async void DeleteItem(Models.EnvVarItem item) {
        if (item.Scope == "Machine" && !_isAdmin) {
            await new TextDialog("Access Denied", "Run as Administrator to delete system variables.").ShowDialog(this);
            return;
        }

        var confirm = new TextDialog("Confirm Delete", $"Delete '{item.Name}'?\nValue backup will be copied to clipboard.", true);
        if (await confirm.ShowDialog<bool>(this)) {
            var old = Environment.GetEnvironmentVariable(item.Name, GetTarget(item.Scope));
            if (!string.IsNullOrEmpty(old)) {
                TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(old);
            }
            Environment.SetEnvironmentVariable(item.Name, null, GetTarget(item.Scope));
            LoadData();
        }
    }

    private void SaveVar(string scope, string name, string value) {
        try {
            var target = GetTarget(scope);
            Environment.SetEnvironmentVariable(name, value, target);
            LoadData();
        } catch (Exception ex) {
            new TextDialog("Error", ex.Message).ShowDialog(this);
        }
    }

    private EnvironmentVariableTarget GetTarget(string scope) {
        return scope == "Machine" ? EnvironmentVariableTarget.Machine : EnvironmentVariableTarget.User;
    }
}