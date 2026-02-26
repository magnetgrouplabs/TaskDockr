using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace TaskDockr.Services
{
    public interface IErrorHandlingService
    {
        Task HandleErrorAsync(Exception exception, string? userFriendlyMessage = null);
        Task<bool> ShowConfirmationAsync(string title, string message);
        Task ShowErrorMessageAsync(string title, string message);
        Task ShowSuccessMessageAsync(string title, string message);
        Task<bool> ValidateGroupNameAsync(string name);
        Task<bool> ValidateFilePathAsync(string path);
        Task<bool> ValidateUrlAsync(string url);
        Task<bool> ValidateIconPathAsync(string path);
    }

    public class ErrorHandlingService : IErrorHandlingService
    {
        // Configuration service will be resolved lazily when needed, after DI container is built
        private IConfigurationService GetConfigService() => App.GetService<IConfigurationService>();

        public async Task HandleErrorAsync(Exception exception, string? userFriendlyMessage = null)
        {
            var message = userFriendlyMessage ?? GetUserFriendlyErrorMessage(exception);
            System.Diagnostics.Debug.WriteLine($"Error occurred: {exception.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {exception.StackTrace}");
            await ShowErrorMessageAsync("Error", message);
            if (IsConfigurationError(exception))
                await GetConfigService().BackupConfigAsync();
        }

        public Task<bool> ShowConfirmationAsync(string title, string message)
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return Task.FromResult(result == MessageBoxResult.Yes);
        }

        public Task ShowErrorMessageAsync(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            return Task.CompletedTask;
        }

        public Task ShowSuccessMessageAsync(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            return Task.CompletedTask;
        }

        public async Task<bool> ValidateGroupNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                await ShowErrorMessageAsync("Invalid Group Name", "Group name cannot be empty.");
                return false;
            }
            if (name.Length > 50)
            {
                await ShowErrorMessageAsync("Invalid Group Name", "Group name cannot exceed 50 characters.");
                return false;
            }
            if (System.Text.RegularExpressions.Regex.IsMatch(name, @"[\\/:*?""<>|]"))
            {
                await ShowErrorMessageAsync("Invalid Group Name", "Group name cannot contain \\ / : * ? \" < > |");
                return false;
            }
            return true;
        }

        public async Task<bool> ValidateFilePathAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                await ShowErrorMessageAsync("Invalid File Path", "File path cannot be empty.");
                return false;
            }
            try
            {
                var fullPath = Path.GetFullPath(path);
                if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
                {
                    await ShowErrorMessageAsync("File Not Found", "The specified file or directory does not exist.");
                    return false;
                }
                var invalidChars = Path.GetInvalidPathChars();
                if (path.IndexOfAny(invalidChars) >= 0)
                {
                    await ShowErrorMessageAsync("Invalid File Path", "File path contains invalid characters.");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync("Invalid File Path", $"Invalid file path: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ValidateUrlAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                await ShowErrorMessageAsync("Invalid URL", "URL cannot be empty.");
                return false;
            }
            try
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var uriResult))
                {
                    var validSchemes = new[] { Uri.UriSchemeHttp, Uri.UriSchemeHttps, Uri.UriSchemeFtp, Uri.UriSchemeMailto, Uri.UriSchemeFile };
                    if (validSchemes.Contains(uriResult.Scheme))
                        return true;
                }
                await ShowErrorMessageAsync("Invalid URL", "Please enter a valid URL (http://, https://, ftp://, mailto:, or file://).");
                return false;
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync("Invalid URL", $"Invalid URL format: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ValidateIconPathAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return true; // Empty is allowed
            var validExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".svg" };
            try
            {
                var fullPath = Path.GetFullPath(path);
                var extension = Path.GetExtension(fullPath).ToLowerInvariant();
                if (!validExtensions.Contains(extension))
                {
                    await ShowErrorMessageAsync("Invalid Icon", $"Supported formats: {string.Join(", ", validExtensions)}");
                    return false;
                }
                if (!File.Exists(fullPath))
                {
                    await ShowErrorMessageAsync("Icon Not Found", "The specified icon file does not exist.");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                await ShowErrorMessageAsync("Invalid Icon Path", $"Invalid icon path: {ex.Message}");
                return false;
            }
        }

        private string GetUserFriendlyErrorMessage(Exception exception) => exception switch
        {
            FileNotFoundException => "The required file could not be found.",
            DirectoryNotFoundException => "The required directory could not be found.",
            UnauthorizedAccessException => "Access denied. Please check file permissions.",
            IOException => "An I/O error occurred. Please try again.",
            ArgumentException => "Invalid input provided.",
            InvalidOperationException => "Operation could not be completed.",
            _ => $"An unexpected error occurred: {exception.Message}"
        };

        private bool IsConfigurationError(Exception exception)
            => exception is IOException || exception is UnauthorizedAccessException ||
               exception is System.Text.Json.JsonException;
    }
}
