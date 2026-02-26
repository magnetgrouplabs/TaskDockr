using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace TaskDockr.Services
{
    public class AppWindowSettings
    {
        public double Width  { get; set; } = 1200;
        public double Height { get; set; } = 800;
        public double X { get; set; } = -1;
        public double Y { get; set; } = -1;
        public bool IsMaximized { get; set; }
    }

    public interface IWindowService
    {
        Task<AppWindowSettings> LoadWindowSettingsAsync();
        Task SaveWindowSettingsAsync(AppWindowSettings settings);
    }

    public class WindowService : IWindowService
    {
        private readonly string _settingsFilePath;

        public WindowService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, "TaskDockr");
            Directory.CreateDirectory(appFolder);
            _settingsFilePath = Path.Combine(appFolder, "window_settings.json");
        }

        public async Task<AppWindowSettings> LoadWindowSettingsAsync()
        {
            if (!File.Exists(_settingsFilePath))
                return new AppWindowSettings();
            try
            {
                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                return JsonSerializer.Deserialize<AppWindowSettings>(json, options) ?? new AppWindowSettings();
            }
            catch
            {
                return new AppWindowSettings();
            }
        }

        public async Task SaveWindowSettingsAsync(AppWindowSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var json = JsonSerializer.Serialize(settings, options);
                await File.WriteAllTextAsync(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save window settings: {ex.Message}");
            }
        }
    }
}
