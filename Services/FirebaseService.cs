using colors.Models;
using colors.Config;
using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using System.Threading.Tasks;

namespace colors.Services;

public class FirebaseService : IDisposable
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
                .Child("users")
                .Child(_userId)
                .Child("projects")
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
            bool allPalettesDeleted = true;

            foreach (var palette in palettes)
            {
                bool success = await DeletePaletteAsync(palette.Id);
                if (!success)
                {
                    allPalettesDeleted = false;
                    // Continue trying to delete others, or break? 
                    // It's usually better to clean up as much as possible, 
                    // but strictly speaking we just need to know if we can proceed.
                }
            }

            if (!allPalettesDeleted)
            {
                System.Diagnostics.Debug.WriteLine($"Cannot delete project {projectId}: One or more palettes failed to delete.");
                return false;
            }

            // Delete the project only if all palettes were deleted
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
            // Fetch all palettes and filter client-side
            // This avoids the need for defining Indexes in Firebase Console rules
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
                bool success = await UpdateProjectAsync(project);
                
                if (!success)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to link palette {palette.Id} to project. Rolling back.");
                    
                    // Rollback: Delete the newly created palette
                    await _firebaseClient
                        .Child("users")
                        .Child(_userId)
                        .Child("palettes")
                        .Child(palette.Id)
                        .DeleteAsync();
                        
                    return null; 
                }
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

            // Delete the palette first
            await _firebaseClient
                .Child("users")
                .Child(_userId)
                .Child("palettes")
                .Child(paletteId)
                .DeleteAsync();

            // If successful (no exception thrown), remove from project's palette list
            if (palette != null)
            {
                var project = await GetProjectByIdAsync(palette.ProjectId);
                if (project != null)
                {
                    project.PaletteIds.Remove(paletteId);
                    await UpdateProjectAsync(project);
                }
            }

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

    public void Dispose()
    {
        _firebaseClient?.Dispose();

    }
}
