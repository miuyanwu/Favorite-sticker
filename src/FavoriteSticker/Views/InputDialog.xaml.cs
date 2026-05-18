using System.Windows;
using System.Windows.Input;

namespace FavoriteSticker.Views;

public partial class InputDialog : Window
{
    public string? Result { get; private set; }

    public InputDialog(string title, string prompt)
    {
        InitializeComponent();
        Title = title;
        PromptTextBlock.Text = prompt;
        InputTextBox.Focus();
    }

    private void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Ok_Click(sender, e);
        }
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Result = InputTextBox.Text;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
