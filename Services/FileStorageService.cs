using Microsoft.Extensions.Options;
using RetailForecast.Settings;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RetailForecast.Services
{
    public class FileStorageService
    {
        private readonly FileUploadSettings _settings;
        private readonly ILogger<FileStorageService> _logger;

        public FileStorageService(
            IOptions<FileUploadSettings> settings,
            ILogger<FileStorageService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        /// <summary>
        /// Saves a file to the user's upload folder with conflict resolution (Windows-style naming)
        /// </summary>
        /// <returns>Storage file name with resolved conflicts (e.g., "dataset (1).csv")</returns>
        public async Task<string> SaveFileAsync(IFormFile file, int userId, string? originalFileName = null)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            var fileName = originalFileName ?? file.FileName;
            var extension = Path.GetExtension(fileName);

            // Validate extension
            if (!_settings.AllowedExtensions.Contains(extension.ToLower()))
                throw new InvalidOperationException($"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", _settings.AllowedExtensions)}");

            var userFolder = GetUserUploadFolder(userId);
            EnsureDirectoryExists(userFolder);

            // Resolve filename conflicts (Windows-style)
            var resolvedFileName = ResolveFileName(userFolder, fileName);
            var filePath = Path.Combine(userFolder, resolvedFileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _logger.LogInformation($"File saved: {filePath}");
                return resolvedFileName;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving file: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Deletes a file from storage
        /// </summary>
        public bool DeleteFile(string storagePath)
        {
            try
            {
                if (File.Exists(storagePath))
                {
                    File.Delete(storagePath);
                    _logger.LogInformation($"File deleted: {storagePath}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting file: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets the full file path for storage
        /// </summary>
        public string GetStorageFilePath(int userId, string storageFileName)
        {
            return Path.Combine(GetUserUploadFolder(userId), storageFileName);
        }

        /// <summary>
        /// Returns a file stream for downloading
        /// </summary>
        public FileStream GetFileStream(string storagePath)
        {
            if (!File.Exists(storagePath))
                throw new FileNotFoundException($"File not found: {storagePath}");

            return new FileStream(storagePath, FileMode.Open, FileAccess.Read);
        }

        /// <summary>
        /// Gets the user's upload folder path
        /// </summary>
        public string GetUserUploadFolder(int userId)
        {
            return Path.Combine(_settings.RootPath, "users", userId.ToString());
        }

        /// <summary>
        /// Resolves filename conflicts using Windows-style naming (e.g., dataset (1).csv, dataset (2).csv)
        /// </summary>
        public string ResolveFileName(string folderPath, string fileName)
        {
            if (!File.Exists(Path.Combine(folderPath, fileName)))
                return fileName;

            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var escapedName = Regex.Escape(nameWithoutExtension);
            var escapedExtension = Regex.Escape(extension);
            var pattern = new Regex(
                $"^{escapedName}(?: \\((?<index>\\d+)\\))?{escapedExtension}$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var maxSuffix = Directory.EnumerateFiles(folderPath)
                .Select(Path.GetFileName)
                .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
                .Select(candidate => pattern.Match(candidate!))
                .Where(match => match.Success)
                .Select(match => match.Groups["index"].Success
                    ? int.Parse(match.Groups["index"].Value)
                    : 0)
                .DefaultIfEmpty(0)
                .Max();

            return $"{nameWithoutExtension} ({maxSuffix + 1}){extension}";
        }

        /// <summary>
        /// Ensures the directory exists, creates it if needed
        /// </summary>
        private void EnsureDirectoryExists(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                _logger.LogInformation($"Directory created: {folderPath}");
            }
        }

        /// <summary>
        /// Gets file size in bytes
        /// </summary>
        public long GetFileSizeBytes(string storagePath)
        {
            if (!File.Exists(storagePath))
                return 0;

            var fileInfo = new FileInfo(storagePath);
            return fileInfo.Length;
        }
    }
}
