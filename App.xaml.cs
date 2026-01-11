using Microsoft.UI.Xaml;
using colors.Services;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace colors;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    public static Window? MainWindow { get; private set; }
    public static FirebaseService? FirebaseService { get; private set; }
    public static ColorExtractionService? ColorExtractionService { get; private set; }
    public static ScreenColorPickerService? ScreenColorPickerService { get; private set; }

    public App()
    {
        this.InitializeComponent();
        // Initialize Services
        FirebaseService = new FirebaseService();
        ColorExtractionService = new ColorExtractionService();
        ScreenColorPickerService = new ScreenColorPickerService();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        if (MainWindow == null)
        {
            MainWindow = new MainWindow();
            MainWindow.Closed += (sender, args) => MainWindow = null;
        }
        MainWindow.Activate();
    }
}
