using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using colors.Models;
using Windows.Storage;
using Windows.Storage.Pickers;
using Newtonsoft.Json;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace colors.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ProjectsPage : Page
    {
        private List<Project> _projects = new List<Project>();
        private ColorPalette? _selectedPalette = null;

        public ProjectsPage()
        {
            this.InitializeComponent();
            this.Loaded += ProjectsPage_Loaded;
        }

        private async void ProjectsPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProjectsAsync();
        }

        private async System.Threading.Tasks.Task LoadProjectsAsync()
        {
            try
            {
                if (App.FirebaseService != null)
                {
                    _projects = await App.FirebaseService.GetProjectsAsync();
                }
                else
                {
                    _projects = new List<Project>();
                }
                RenderProjects();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading projects: {ex.Message}");
            }
        }

        private void RenderProjects()
        {
            ProjectsPanel.Children.Clear();

            if (_projects.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = "No projects yet.\nClick 'New Project' to get started!",
                    TextAlignment = TextAlignment.Center,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                    Margin = new Thickness(0, 40, 0, 0)
                };
                ProjectsPanel.Children.Add(emptyText);
                return;
            }

            foreach (var project in _projects)
            {
                var expander = new Expander
                {
                    Header = project.Name,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 0, 0, 4)
                };

                var palettesList = new StackPanel { Spacing = 4 };

                // Add "New Palette" button
                var newPaletteBtn = new Button
                {
                    Content = "+ New Palette from Image",
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 4, 0, 8)
                };
                newPaletteBtn.Click += async (s, e) => await CreateNewPalette(project);
                palettesList.Children.Add(newPaletteBtn);

                // Load palettes for this project
                LoadPalettesForProject(project, palettesList);

                expander.Content = palettesList;
                ProjectsPanel.Children.Add(expander);
            }

        }

        private async void LoadPalettesForProject(Project project, StackPanel container)
        {
            var palettes = new List<ColorPalette>();
            if (App.FirebaseService != null)
            {
                palettes = await App.FirebaseService.GetPalettesAsync(project.Id);
            }

            foreach (var palette in palettes)
            {
                var btn = new Button
                {
                    Content = $"?? {palette.Name}",
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left
                };
                btn.Click += (s, e) => LoadPalette(palette);
                container.Children.Add(btn);
            }

            if (palettes.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = "No palettes yet",
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                    Margin = new Thickness(8, 4, 0, 0)
                };
                container.Children.Add(emptyText);
            }
        }

        private async void NewProjectButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "New Project",
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            var stack = new StackPanel { Spacing = 8 };

            var nameBox = new TextBox { PlaceholderText = "Project name", Header = "Name" };
            var descBox = new TextBox { PlaceholderText = "Optional description", Header = "Description", AcceptsReturn = true, Height = 80 };

            stack.Children.Add(nameBox);
            stack.Children.Add(descBox);
            dialog.Content = stack;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(nameBox.Text))
            {
                var project = new Project(nameBox.Text.Trim(), descBox.Text.Trim());
                if (App.FirebaseService != null)
                {
                    await App.FirebaseService.CreateProjectAsync(project);
                }
                await LoadProjectsAsync();
            }
        }

        private async System.Threading.Tasks.Task CreateNewPalette(Project project)
        {
            // Navigate to Image Palette page with project context
            var imagePalettePage = new ImagePalettePage(project);
            Frame.Navigate(typeof(ImagePalettePage), project);
        }

        private async void LoadPalette(ColorPalette palette)
        {
            _selectedPalette = palette;

            EmptyState.Visibility = Visibility.Collapsed;
            PaletteHeader.Visibility = Visibility.Visible;

            PaletteName.Text = palette.Name;
            PaletteInfo.Text = $"{palette.Colors.Count} colors � Created {palette.CreatedAt:MMM dd, yyyy}";

            // Load reference image if exists
            if (!string.IsNullOrEmpty(palette.ReferenceImagePath))
            {
                try
                {
                    var file = await StorageFile.GetFileFromPathAsync(palette.ReferenceImagePath);
                    var bitmap = new BitmapImage();
                    using (var stream = await file.OpenAsync(FileAccessMode.Read))
                    {
                        await bitmap.SetSourceAsync(stream);
                    }
                    ReferenceImage.Source = bitmap;
                    ReferenceImage.Visibility = Visibility.Visible;
                }
                catch
                {
                    ReferenceImage.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                ReferenceImage.Visibility = Visibility.Collapsed;
            }

            ColorsList.ItemsSource = palette.Colors;
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPalette == null) return;

            try
            {
                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("JSON", new List<string>() { ".json" });
                savePicker.SuggestedFileName = $"{_selectedPalette.Name}_palette";

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                StorageFile file = await savePicker.PickSaveFileAsync();

                if (file != null)
                {
                    var exportData = new
                    {
                        name = _selectedPalette.Name,
                        createdAt = _selectedPalette.CreatedAt,
                        colors = _selectedPalette.Colors.Select(c => new
                        {
                            hex = c.HexColor,
                            rgb = new { r = c.R, g = c.G, b = c.B },
                            description = c.Description
                        })
                    };

                    string json = JsonConvert.SerializeObject(exportData, Formatting.Indented);
                    await FileIO.WriteTextAsync(file, json);

                    var successDialog = new ContentDialog
                    {
                        XamlRoot = this.XamlRoot,
                        Title = "Success",
                        Content = "Palette exported successfully!",
                        CloseButtonText = "OK"
                    };
                    await successDialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting: {ex.Message}");
            }
        }

        private async void DeletePaletteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPalette == null) return;

            var dialog = new ContentDialog
            {
                XamlRoot = this.XamlRoot,
                Title = "Delete Palette",
                Content = $"Are you sure you want to delete '{_selectedPalette.Name}'?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (App.FirebaseService != null)
                {
                    await App.FirebaseService.DeletePaletteAsync(_selectedPalette.Id);
                }
                _selectedPalette = null;
                EmptyState.Visibility = Visibility.Visible;
                PaletteHeader.Visibility = Visibility.Collapsed;
                await LoadProjectsAsync();
            }
        }

    }
}
