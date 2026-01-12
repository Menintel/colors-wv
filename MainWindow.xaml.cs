using colors.Services;
using colors.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace colors;

/// <summary>
/// The main application window containing a navigation frame for managing page views.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();

        // Set fixed window size for floating UI
        var appWindow = this.AppWindow;
        appWindow.Resize(new Windows.Graphics.SizeInt32(400, 800));

        // Navigate to Projects page by default
        ContentFrame.Navigate(typeof(ProjectsPage));
    }

    private void ProjectsButton_Click(object sender, RoutedEventArgs e)
    {
        if (ContentFrame != null)
        {
            ContentFrame.Navigate(typeof(ProjectsPage));
        }
    }

    private void ScreenPickerButton_Click(object sender, RoutedEventArgs e)
    {
        if (ContentFrame != null)
        {
            ContentFrame.Navigate(typeof(ColorPickerPage));
        }
    }

    private void ImagePaletteButton_Click(object sender, RoutedEventArgs e)
    {
        if (ContentFrame != null)
        {
            ContentFrame.Navigate(typeof(ImagePalettePage));
        }
    }
}
