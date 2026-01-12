using colors.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Generic;
using Windows.ApplicationModel.DataTransfer;
using Newtonsoft.Json;
using Microsoft.UI.Xaml.Media;

namespace colors.Views;

public sealed partial class PalettesPage : Page
{
    private ObservableCollection<PaletteViewModel> _palettes = new();

    public PalettesPage()
    {
        this.InitializeComponent();
        this.Loaded += PalettesPage_Loaded;
    }

    private async void PalettesPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadDataAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadDataAsync();
    }

    private async System.Threading.Tasks.Task LoadDataAsync()
    {
        try
        {
            if (App.FirebaseService == null) return;

            LoadingRing.IsActive = true;
            LoadingRing.Visibility = Visibility.Visible;
            EmptyState.Visibility = Visibility.Collapsed;
            PalettesList.Visibility = Visibility.Collapsed;

            // 1. Get all projects
            var projects = await App.FirebaseService.GetProjectsAsync();
            
            // 2. Get palettes for all projects
            // Note: In a real app, this might be inefficient. Ideally backend supports GetAllPalettes
            var allPalettes = new List<ColorPalette>();
            foreach (var proj in projects)
            {
                var projPalettes = await App.FirebaseService.GetPalettesAsync(proj.Id);
                allPalettes.AddRange(projPalettes);
            }

            _palettes.Clear();
            // Sort by CreatedAt desc
            foreach (var pal in allPalettes.OrderByDescending(p => p.CreatedAt))
            {
                _palettes.Add(new PaletteViewModel(pal));
            }

            PalettesList.ItemsSource = _palettes;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading palettes: {ex.Message}");
        }
        finally
        {
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
            
            if (_palettes.Count == 0)
            {
                EmptyState.Visibility = Visibility.Visible;
            }
            else
            {
                PalettesList.Visibility = Visibility.Visible;
            }
        }
    }

    private async void DeletePalette_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is PaletteViewModel vm)
        {
             var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "Delete Palette",
                Content = $"Delete '{vm.Name}'?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                if (App.FirebaseService != null)
                {
                    await App.FirebaseService.DeletePaletteAsync(vm.Id);
                    _palettes.Remove(vm);
                    if (_palettes.Count == 0) EmptyState.Visibility = Visibility.Visible;
                }
            }
        }
    }

    private void CopyJson_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is PaletteViewModel vm)
        {
            var json = JsonConvert.SerializeObject(vm.SourcePalette, Formatting.Indented);
            var dp = new DataPackage();
            dp.SetText(json);
            Clipboard.SetContent(dp);
        }
    }
}

public class PaletteViewModel
{
    public ColorPalette SourcePalette { get; }

    public PaletteViewModel(ColorPalette palette)
    {
        SourcePalette = palette;
    }

    public string Id => SourcePalette.Id;
    public string Name => SourcePalette.Name;
    public string CreatedAt => SourcePalette.CreatedAt.ToString("d");

    public List<ColorPreviewItem> ColorsPreview => SourcePalette.Colors.Select(c => new ColorPreviewItem(c)).ToList();
}

public class ColorPreviewItem
{
    private ColorItem _item;
    public ColorPreviewItem(ColorItem item) { _item = item; }
    public string HexColor => _item.HexColor;
    public Windows.UI.Color WindowsColor => _item.ToWindowsColor();
}
