using colors.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace colors.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class ColorPickerPage : Page
{
    private DispatcherTimer _pickingTimer;
    private ColorItem? _currentColor;
    private ObservableCollection<ColorHistoryViewModel> _colorHistory = new();
    private bool _isPicking = false;
    public ColorPickerPage()
    {
        this.InitializeComponent();
        this.Loaded += ColorPickerPage_Loaded;

        // Setup timer for live color picking
        _pickingTimer = new DispatcherTimer();
        _pickingTimer.Interval = TimeSpan.FromMilliseconds(50); // Update 20 times per second
        _pickingTimer.Tick += PickingTimer_Tick;
    }

    private async void ColorPickerPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadColorHistoryAsync();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        StopPicking();
    }

    private async System.Threading.Tasks.Task LoadColorHistoryAsync()
    {
        try
        {
            if (App.FirebaseService == null) return;
            var colors = await App.FirebaseService.GetPickedColorsAsync(50);
            _colorHistory.Clear();

            foreach (var color in colors)
            {
                _colorHistory.Add(new ColorHistoryViewModel(color));
            }

            ColorHistoryList.ItemsSource = _colorHistory;
            UpdateEmptyState();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading color history: {ex.Message}");
        }
    }

    private void UpdateEmptyState()
    {
        EmptyHistoryState.Visibility = _colorHistory.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void StartPickerButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isPicking)
        {
            StopPicking();
        }
        else
        {
            StartPicking();
        }
    }

    private void StartPicking()
    {
        _isPicking = true;
        StartPickerButton.Content = "? Stop Picking (Press ESC)";
        InstructionText.Text = "Move your mouse around the screen. Click to capture a color. Press ESC to stop.";

        // Start the timer to continuously update the color
        _pickingTimer.Start();

        // Register keyboard hook for ESC key
        this.KeyDown += OnKeyDown;
        
        // Ensure focus to capture keys
        this.Focus(FocusState.Programmatic);
    }

    private void StopPicking()
    {
        _isPicking = false;
        StartPickerButton.Content = "?? Start Picking";
        InstructionText.Text = "Click the button above to start picking colors from your screen. Press ESC to stop.";

        // Stop the timer
        _pickingTimer.Stop();

        // Unregister keyboard hook
        // Unregister keyboard hook
        this.KeyDown -= OnKeyDown;
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            StopPicking();
        }
    }

    private void PickingTimer_Tick(object? sender, object e)
    {
        try
        {
            if (App.ScreenColorPickerService == null) return;
            // Get color at cursor position
            _currentColor = App.ScreenColorPickerService.GetColorAtCursor();

            if (_currentColor != null)
            {
                UpdateColorPreview(_currentColor);
            }

            // Get cursor position
            var (x, y) = App.ScreenColorPickerService.GetCursorPosition();
            PositionValue.Text = $"Position: ({x}, {y})";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error picking color: {ex.Message}");
        }
    }

    private void UpdateColorPreview(ColorItem color)
    {
        // Update swatch
        var colorBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, (byte)color.R, (byte)color.G, (byte)color.B));
        LiveColorSwatch.Background = colorBrush;

        // Update text values
        HexValue.Text = $"HEX: {color.HexColor}";
        RgbValue.Text = $"RGB: ({color.R}, {color.G}, {color.B})";

        // Enable action buttons
        CopyHexButton.IsEnabled = true;
        CopyRgbButton.IsEnabled = true;
        SaveColorButton.IsEnabled = true;
    }

    private void CopyHexButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentColor != null)
        {
            CopyToClipboard(_currentColor.HexColor);
            ShowCopiedNotification("HEX copied!");
        }
    }

    private void CopyRgbButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentColor != null)
        {
            var rgbText = $"rgb({_currentColor.R}, {_currentColor.G}, {_currentColor.B})";
            CopyToClipboard(rgbText);
            ShowCopiedNotification("RGB copied!");
        }
    }

    private async void SaveColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentColor != null)
        {
            try
            {
                var colorToSave = new ColorItem(_currentColor.R, _currentColor.G, _currentColor.B);
                if (App.FirebaseService == null) return;
                await App.FirebaseService.SavePickedColorAsync(colorToSave);

                // Add to history list
                _colorHistory.Insert(0, new ColorHistoryViewModel(colorToSave));
                UpdateEmptyState();

                ShowCopiedNotification("Color saved to history!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving color: {ex.Message}");
            }
        }
    }

    private void CopyToClipboard(string text)
    {
        var dataPackage = new DataPackage();
        dataPackage.SetText(text);
        Clipboard.SetContent(dataPackage);
    }

    private void ShowCopiedNotification(string message)
    {
        InstructionText.Text = $"? {message}";

        // Reset message after 2 seconds
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        timer.Tick += (s, args) =>
        {
            if (_isPicking)
            {
                InstructionText.Text = "Move your mouse around the screen. Click to capture a color. Press ESC to stop.";
            }
            else
            {
                InstructionText.Text = "Click the button above to start picking colors from your screen. Press ESC to stop.";
            }
            timer.Stop();
        };
        timer.Start();
    }

    private async void RefreshHistory_Click(object sender, RoutedEventArgs e)
    {
        await LoadColorHistoryAsync();
    }

    private async void ClearHistory_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            XamlRoot = this.XamlRoot,
            Title = "Clear History",
            Content = "Are you sure you want to clear all color history?",
            PrimaryButtonText = "Clear All",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            try
            {
                if (App.FirebaseService == null) return;

                // Delete all colors from Firebase
                foreach (var color in _colorHistory)
                {
                    await App.FirebaseService.DeletePickedColorAsync(color.Id);
                }

                _colorHistory.Clear();
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing history: {ex.Message}");
            }
        }
    }

    private void CopyHistoryColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ColorHistoryViewModel color)
        {
            CopyToClipboard(color.HexColor);
            ShowCopiedNotification("Color copied!");
        }
    }

    private async void DeleteHistoryColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ColorHistoryViewModel color)
        {
            try
            {
                if (App.FirebaseService == null) return;
                await App.FirebaseService.DeletePickedColorAsync(color.Id);
                _colorHistory.Remove(color);
                UpdateEmptyState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting color: {ex.Message}");
            }
        }
    }
}

public class ColorHistoryViewModel
{
    private ColorItem _colorItem;

    public ColorHistoryViewModel(ColorItem colorItem)
    {
        _colorItem = colorItem;
    }

    public string Id => _colorItem.Id;
    public string HexColor => _colorItem.HexColor;
    public int R => _colorItem.R;
    public int G => _colorItem.G;
    public int B => _colorItem.B;
    public string CreatedAt => _colorItem.CreatedAt.ToLocalTime().ToString("MMM dd, yyyy h:mm tt");
    public Windows.UI.Color WindowsColor => _colorItem.ToWindowsColor();
}

