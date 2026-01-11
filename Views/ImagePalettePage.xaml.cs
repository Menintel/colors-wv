using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Media.Imaging;
using colors.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace colors.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ImagePalettePage : Page
{
    private Project? _project;
    private StorageFile? _selectedImageFile;
    private ObservableCollection<ColorItemViewModel> _extractedColors = new ObservableCollection<ColorItemViewModel>();

    public ImagePalettePage()
    {
        this.InitializeComponent();
    }

    public ImagePalettePage(Project project)
    {
        this.InitializeComponent();
        _project = project;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is Project project)
        {
            _project = project;
            ProjectNameText.Text = $"Project: {_project.Name}";
        }
    }

    private async void UploadImageButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".bmp");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                _selectedImageFile = file;
                await LoadImagePreview(file);
                
                // Show step 2
                Step2_Extract.Visibility = Visibility.Visible;
                ImagePreviewContainer.Visibility = Visibility.Visible;
                UploadArea.Visibility = Visibility.Collapsed;
                ImageFileName.Text = file.Name;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error selecting image: {ex.Message}");
            await ShowErrorDialog("Failed to load image. Please try again.");
        }
    }

    private async System.Threading.Tasks.Task LoadImagePreview(StorageFile file)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.DecodePixelWidth = 1000;
            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
            {
                await bitmap.SetSourceAsync(stream);
            }
            SelectedImage.Source = bitmap;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading image preview: {ex.Message}");
        }
    }

     private async void ExtractColorsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedImageFile == null) return;

        try
        {
            // Show progress
            ExtractColorsButton.IsEnabled = false;
            ExtractionProgress.IsActive = true;
            ExtractionProgress.Visibility = Visibility.Visible;

            // Get selected count
            int colorCount = 4;
            if (Radio6.IsChecked == true) colorCount = 6;
            else if (Radio8.IsChecked == true) colorCount = 8;
            else if (Radio10.IsChecked == true) colorCount = 10;
            else if (Radio16.IsChecked == true) colorCount = 16;

            // Extract colors
            if (App.ColorExtractionService == null)
            {
                await ShowErrorDialog("Color service is not initialized.");
                return;
            }
            var colors = await App.ColorExtractionService.ExtractDominantColorsAsync(_selectedImageFile, colorCount);

            // Convert to view models
            _extractedColors.Clear();
            foreach (var color in colors)
            {
                _extractedColors.Add(new ColorItemViewModel(color));
            }

            ExtractedColorsList.ItemsSource = _extractedColors;

            // Show step 3 & 4
            Step3_Preview.Visibility = Visibility.Visible;
            Step4_Save.Visibility = Visibility.Visible;

            // Suggest palette name
            PaletteNameBox.Text = $"{_selectedImageFile.DisplayName} Palette";

            // Hide progress
            ExtractColorsButton.IsEnabled = true;
            ExtractionProgress.IsActive = false;
            ExtractionProgress.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error extracting colors: {ex.Message}");
            await ShowErrorDialog("Failed to extract colors. Please try again.");
            
            ExtractColorsButton.IsEnabled = true;
            ExtractionProgress.IsActive = false;
            ExtractionProgress.Visibility = Visibility.Collapsed;
        }
    }

    private async void SelectedImage_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (_selectedImageFile == null) return;

        try
        {
            var point = e.GetCurrentPoint(SelectedImage);
            var position = point.Position;

            // Get image actual dimensions
            var imageSource = SelectedImage.Source as BitmapImage;
            if (imageSource == null) return;

            // Calculate actual pixel position
            double scaleX = imageSource.PixelWidth / SelectedImage.ActualWidth;
            double scaleY = imageSource.PixelHeight / SelectedImage.ActualHeight;

            int x = (int)(position.X * scaleX);
            int y = (int)(position.Y * scaleY);

            // Get color at position
            if (App.ColorExtractionService == null) return;
            var color = await App.ColorExtractionService.GetColorAtPositionAsync(_selectedImageFile, x, y);

            if (color != null)
            {
                // Add to extracted colors if not already there
                if (!_extractedColors.Any(c => c.HexColor == color.HexColor))
                {
                    _extractedColors.Add(new ColorItemViewModel(color));
                    
                    // Show feedback
                    PickedColorPreview.Text = $"✓ Picked color: {color.HexColor}";
                    PickedColorPreview.Visibility = Visibility.Visible;

                    // Auto-hide feedback after 2 seconds
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                    timer.Tick += (s, args) =>
                    {
                        PickedColorPreview.Visibility = Visibility.Collapsed;
                        timer.Stop();
                    };
                    timer.Start();

                    // Show preview steps if hidden
                    if (Step3_Preview.Visibility == Visibility.Collapsed)
                    {
                        Step3_Preview.Visibility = Visibility.Visible;
                        Step4_Save.Visibility = Visibility.Visible;
                        ExtractedColorsList.ItemsSource = _extractedColors;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error picking color: {ex.Message}");
        }
    }   

    private void SelectedImage_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        // Optional: Show color preview while hovering
        // Can be implemented later for better UX
    }

    private async void AddColorManually_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "Add Color Manually",
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary
        };

        var stack = new StackPanel { Spacing = 12 };
        
        var hexBox = new TextBox 
        { 
            Header = "HEX Color Code", 
            PlaceholderText = "#FF5733 or FF5733",
            MaxLength = 7
        };
        
        var descBox = new TextBox 
        { 
            Header = "Description", 
            PlaceholderText = "e.g., 'Primary button'"
        };
        
        stack.Children.Add(hexBox);
        stack.Children.Add(descBox);
        dialog.Content = stack;

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(hexBox.Text))
        {
            var color = ColorItem.FromHex(hexBox.Text.Trim(), descBox.Text.Trim());
            if (color != null)
            {
                _extractedColors.Add(new ColorItemViewModel(color));
            }
            else
            {
                await ShowErrorDialog("Invalid HEX color code. Use format: #FF5733");
            }
        }
    }

    private void RemoveColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ColorItemViewModel color)
        {
            _extractedColors.Remove(color);
        }
    }

    private async void SavePaletteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_project == null)
        {
            await ShowErrorDialog("No project context found.");
            return;
        }

        if (string.IsNullOrWhiteSpace(PaletteNameBox.Text))
        {
            await ShowErrorDialog("Please enter a palette name.");
            return;
        }

        if (_extractedColors.Count == 0)
        {
            await ShowErrorDialog("Please extract or add at least one color.");
            return;
        }

        try
        {
            SavePaletteButton.IsEnabled = false;
            SavingProgress.IsActive = true;
            SavingProgress.Visibility = Visibility.Visible;

            if (App.FirebaseService == null)
            {
                await ShowErrorDialog("Firebase service is not initialized.");
                return;
            }

            // Create palette
            var palette = new ColorPalette(PaletteNameBox.Text.Trim(), _project.Id);
            
            // Add colors
            foreach (var colorVM in _extractedColors)
            {
                palette.Colors.Add(colorVM.ToColorItem());
            }

            // Save image to local app data and upload to Firebase
            if (_selectedImageFile != null)
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var imagesFolder = await localFolder.CreateFolderAsync("images", CreationCollisionOption.OpenIfExists);
                var fileName = $"{palette.Id}_{_selectedImageFile.Name}";
                var localFile = await _selectedImageFile.CopyAsync(imagesFolder, fileName, NameCollisionOption.ReplaceExisting);
                
                palette.ReferenceImagePath = localFile.Path;

                // Upload to Firebase Storage
                using (var stream = await localFile.OpenStreamForReadAsync())
                {
                    var imageUrl = await App.FirebaseService.UploadImageAsync(stream, fileName);
                    palette.ReferenceImageUrl = imageUrl ?? string.Empty;
                }
            }

            // Save to Firebase
            await App.FirebaseService.CreatePaletteAsync(palette);

            // Show success and navigate back
            var successDialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "Success!",
                Content = $"Palette '{palette.Name}' has been saved with {palette.Colors.Count} colors.",
                CloseButtonText = "OK"
            };
            await successDialog.ShowAsync();

            // Navigate back to projects
            Frame.Navigate(typeof(ProjectsPage));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving palette: {ex.Message}");
            await ShowErrorDialog("Failed to save palette. Please try again.");
        }
        finally
        {
            SavePaletteButton.IsEnabled = true;
            SavingProgress.IsActive = false;
            SavingProgress.Visibility = Visibility.Collapsed;
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(ProjectsPage));
    }

    private async System.Threading.Tasks.Task ShowErrorDialog(string message)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "Error",
            Content = message,
            CloseButtonText = "OK"
        };
        await dialog.ShowAsync();
    }
}

// ViewModel for color items to support data binding
public class ColorItemViewModel(ColorItem colorItem)
{
    private ColorItem _colorItem = colorItem;

    public string HexColor 
    { 
        get => _colorItem.HexColor; 
        set => _colorItem.HexColor = value; 
    }

    public int R 
    { 
        get => _colorItem.R; 
        set => _colorItem.R = value; 
    }

    public int G 
    { 
        get => _colorItem.G; 
        set => _colorItem.G = value; 
    }

    public int B 
    { 
        get => _colorItem.B; 
        set => _colorItem.B = value; 
    }

    public string Description 
    { 
        get => _colorItem.Description; 
        set => _colorItem.Description = value; 
    }

    public Windows.UI.Color WindowsColor => _colorItem.ToWindowsColor();

    public ColorItem ToColorItem() => _colorItem;
}
