using Avalonia.Controls;
using Avalonia.Interactivity;

namespace environment_manager;

public partial class TextDialog : Window {
    public TextDialog() { InitializeComponent(); }

    public TextDialog(string title, string msg, bool isConfirm = false) : this() {
        Title = title;
        MsgText.Text = msg;
        if (isConfirm) {
            BtnCancel.IsVisible = true;
            BtnOk.Content = "Yes";
        }
    }

    private void OnOk(object? sender, RoutedEventArgs e) => Close(true);
    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
