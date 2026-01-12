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
using Windows.ApplicationModel.DataTransfer;
using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace colors.Views;

public sealed partial class ProjectsPage : Page
{
    private ObservableCollection<Project> _projects = new();
    private ObservableCollection<PaletteViewModel> _currentProjectPalettes = new();
    private Project? _selectedProject;

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
                var projectsList = await App.FirebaseService.GetProjectsAsync();
                _projects.Clear();
                foreach(var p in projectsList) _projects.Add(p);
                ProjectsList.ItemsSource = _projects;
                EmptyProjectsState.Visibility = _projects.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading projects: {ex.Message}");
        }
    }

    private async void ProjectsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ProjectsList.SelectedItem is Project project)
        {
            _selectedProject = project;
            
            // Switch to details
            ProjectsListContainer.Visibility = Visibility.Collapsed;
            ProjectDetailsContainer.Visibility = Visibility.Visible;
            ProjectNameTitle.Text = project.Name;

            await LoadPalettesForProject(project);
            
            // Clear selection so we can re-select same item if needed later (though we switch view)
            ProjectsList.SelectedItem = null;
        }
    }

    private async System.Threading.Tasks.Task LoadPalettesForProject(Project project)
    {
        try
        {
            if (App.FirebaseService != null)
            {
                var palettes = await App.FirebaseService.GetPalettesAsync(project.Id);
                _currentProjectPalettes.Clear();
                foreach (var pal in palettes)
                {
                    _currentProjectPalettes.Add(new PaletteViewModel(pal));
                }
                ProjectPalettesList.ItemsSource = _currentProjectPalettes;
                EmptyPalettesState.Visibility = _currentProjectPalettes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading palettes: {ex.Message}");
        }
    }

    private void BackToProjectsButton_Click(object sender, RoutedEventArgs e)
    {
        _selectedProject = null;
        ProjectDetailsContainer.Visibility = Visibility.Collapsed;
        ProjectsListContainer.Visibility = Visibility.Visible;
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
        var descBox = new TextBox { PlaceholderText = "Description", Header = "Description" };
        stack.Children.Add(nameBox);
        stack.Children.Add(descBox);
        dialog.Content = stack;

        if (await dialog.ShowAsync() == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(nameBox.Text))
        {
            if (App.FirebaseService != null)
            {
                var newProj = new Project(nameBox.Text.Trim(), descBox.Text.Trim());
                await App.FirebaseService.CreateProjectAsync(newProj);
                await LoadProjectsAsync();
            }
        }
    }

    private void CreatePalette_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedProject != null)
        {
            Frame.Navigate(typeof(ImagePalettePage), _selectedProject);
        }
    }

    private async void DeleteInProjectPalette_Click(object sender, RoutedEventArgs e)
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
                    _currentProjectPalettes.Remove(vm);
                    if (_currentProjectPalettes.Count == 0) EmptyPalettesState.Visibility = Visibility.Visible;
                }
            }
        }
    }

    private void CopyInProjectJson_Click(object sender, RoutedEventArgs e)
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

public class StringToVisibilityConverter : Microsoft.UI.Xaml.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
}
