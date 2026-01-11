using colors.Services;
using colors.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace colors;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{

    public MainWindow()
    {
        this.InitializeComponent();

        // Set window size
        var appWindow = this.AppWindow;
        appWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));

        // Navigate to Projects page by default
        ContentFrame.Navigate(typeof(ProjectsPage));

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
    }

    private void ProjectsButton_Click(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(typeof(ProjectsPage));
    }

    private void ScreenPickerButton_Click(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(typeof(ColorPickerPage));
    }

    private void ImagePaletteButton_Click(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigate(typeof(ImagePalettePage));
    }
}
