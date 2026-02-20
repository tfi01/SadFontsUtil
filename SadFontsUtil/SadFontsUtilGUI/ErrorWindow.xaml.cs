using System.Windows;

namespace SadFontsUtilGUI;

/// <summary>
/// Interaction logic for ErrorWindow.xaml
/// </summary>
public partial class ErrorWindow : Window
{
    public ErrorWindow(string title, string message)
    {
        InitializeComponent();
        txtErrorTitle.Text = title;
        txtErrorDetails.Text = message;
    }

    private void BtnCopy_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Clipboard.SetText(txtErrorDetails.Text);
        btnCopy.Content = "Copied!";
        btnCopy.IsEnabled = false;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
