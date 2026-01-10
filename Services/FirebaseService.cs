using colors.Models;
using colors.Config;
using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace colors.Services
{
    public class FirebaseService
    {
        private readonly FirebaseClient _firebaseClient;
        private readonly FirebaseStorage _firebaseStorage;
        private readonly string _userId;

        public FirebaseService()
        {
            _userId = FirebaseConfig.UserId;
            _firebaseClient = new FirebaseClient(FirebaseConfig.DatabaseUrl);
            _firebaseStorage = new FirebaseStorage(FirebaseConfig.StorageBucket);
        }

        #region projects
        public async Task<List<Project>> GetProjectsAsync()
        {
            try
            {
                var projects = await _firebaseClient
                    .Child("Users")
                    .Child(_userId)
                    .Child("Projects")
                    .OnceAsync<Project>();

                return projects?.Select(p => p.Object).OrderByDescending(p => p.UpdatedAt).ToList()
                    ?? new List<Project>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting projects: {ex.Message}");
                return new List<Project>();
            }
        }

        public async Task<Project?> CreateProjectAsync(Project project)
        {
            try
            {
                await _firebaseClient
                    .Child("users")
                    .Child(_userId)
                    .Child("projects")
                    .Child(project.Id)
                    .PutAsync(project);

                return project;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error create project: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdateProjectAsync(Project project)
        {
            try
            {
                project.UpdatedAt = DateTime.UtcNow;
                await _firebaseClient
                    .Child("users")
                    .Child(_userId)
                    .Child("projects")
                    .Child(project.Id)
                    .PutAsync(project);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating project: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteProjectAsync(string projectId)
        {
            try
            {
                // Delete all palettes in this project first
                var palettes = await GetPalettesAsync(projectId);
                foreach (var palette in palettes)
                {
                    await DeletePaletteAsync(palette.Id);
                }

                // Delete the project
                await _firebaseClient
                    .Child("users")
                    .Child(_userId)
                    .Child("projects")
                    .Child(projectId)
                    .DeleteAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting project: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Palettes

        public async Task<List<ColorPalette>> GetPalettesAsync(string projectId)
        {
            try
            {
                var palettes = await _firebaseClient
                    .Child("users")
                    .Child(_userId)
                    .Child("palettes")
                    .OnceAsync<ColorPalette>();

                return palettes?
                    .Select(p => p.Object)
                    .Where(p => p.ProjectId == projectId)
                    .OrderByDescending(p => p.UpdatedAt)
                    .ToList() ?? new List<ColorPalette>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting palettes: {ex.Message}");
                return new List<ColorPalette>();
            }
        }

        public async Task<ColorPalette?> GetPaletteByIdAsync(string paletteId)
        {
            try
            {
                var palette = await _firebaseClient
                    .Child("users")
                    .Child(_userId)
                    .Child("palettes")
                    .Child(paletteId)
                    .OnceSingleAsync<ColorPalette>();

                return palette;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting palette: {ex.Message}");
                return null;
            }
        }

        public async Task<ColorPalette?> CreatePaletteAsync(ColorPalette palette)
        {
            try
            {
                await _firebaseClient
                    .Child("users")
                    .Child(_userId)
                    .Child("palettes")
                    .Child(palette.Id)
                    .PutAsync(palette);

                // Add palette ID to project
                var project = await GetProjectByIdAsync(palette.ProjectId);
                if (project != null && !project.PaletteIds.Contains(palette.Id))
                {
                    project.PaletteIds.Add(palette.Id);
                    await UpdateProjectAsync(project);
                }

                return palette;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating palette: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UpdatePaletteAsync(ColorPalette palette)
        {
            try
            {
                palette.UpdatedAt = DateTime.UtcNow;
                await _firebaseClient
                    .Child("users")
                    .Child(_userId)
                    .Child("palettes")
                    .Child(palette.Id)
                    .PutAsync(palette);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating palette: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeletePaletteAsync(string paletteId)
        {
            try
            {
                var palette = await GetPaletteByIdAsync(paletteId);
                if (palette != null)
                {
                    // Remove from project's palette list
                    var project = await GetProjectByIdAsync(palette.ProjectId);
                    if (project != null)
                    {
                        project.PaletteIds.Remove(paletteId);
                        await UpdateProjectAsync(project);
                    }
                }

                // Delete the palette
                await _firebaseClient
                    .Child("users")
                    .Child(_userId)
                    .Child("palettes")
                    .Child(paletteId)
                    .DeleteAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting palette: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Helper Methods

        private async Task<Project?> GetProjectByIdAsync(string projectId)
        {
            try
            {
                var project = await _firebaseClient
                    .Child("users")
                    .Child(_userId)
                    .Child("projects")
                    .Child(projectId)
                    .OnceSingleAsync<Project>();

                return project;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Image Upload

        public async Task<string?> UploadImageAsync(Stream imageStream, string fileName)
        {
            try
            {
                var imageUrl = await _firebaseStorage
                    .Child("users")
                    .Child(_userId)
                    .Child("images")
                    .Child(fileName)
                    .PutAsync(imageStream);

                return imageUrl;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error uploading image: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Quick Picked Colors (from screen picker)

        public async Task<bool> SavePickedColorAsync(ColorItem color)
        {
            try
            {
                await _firebaseClient
                    .Child("users")
                    .Child(_userId)
                    .Child("picked_colors")
                    .Child(color.Id)
                    .PutAsync(color);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving picked color: {ex.Message}");
                return false;
            }
        }

        public async Task<List<ColorItem>> GetPickedColorsAsync(int limit = 50)
        {
            try
            {
                var colors = await _firebaseClient
                    .Child("users")
                    .Child(_userId)
                    .Child("picked_colors")
                    .OnceAsync<ColorItem>();

                return colors?
                    .Select(c => c.Object)
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(limit)
                    .ToList() ?? new List<ColorItem>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting picked colors: {ex.Message}");
                return new List<ColorItem>();
            }
        }

        public async Task<bool> DeletePickedColorAsync(string colorId)
        {
            try
            {
                await _firebaseClient
                    .Child("users")
                    .Child(_userId)
                    .Child("picked_colors")
                    .Child(colorId)
                    .DeleteAsync();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting picked color: {ex.Message}");
                return false;
            }
        }

        #endregion

    }
}
