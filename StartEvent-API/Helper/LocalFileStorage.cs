using System.IO;

namespace StartEvent_API.Helper
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string _basePath;

        public LocalFileStorage(IWebHostEnvironment environment)
        {
            _environment = environment;
            
            // Handle case where WebRootPath might be null
            var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            _basePath = Path.Combine(webRootPath, "qrcodes");
            
            // Ensure directory exists
            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        public async Task<string> SaveFileAsync(byte[] fileData, string fileName, string folder = "qrcodes")
        {
            var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var folderPath = Path.Combine(webRootPath, folder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = Path.Combine(folderPath, fileName);
            await File.WriteAllBytesAsync(filePath, fileData);
            
            return Path.Combine(folder, fileName).Replace("\\", "/");
        }

        public async Task<byte[]?> GetFileAsync(string filePath)
        {
            var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
            var fullPath = Path.Combine(webRootPath, filePath);
            if (!File.Exists(fullPath))
                return null;

            return await File.ReadAllBytesAsync(fullPath);
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                var webRootPath = _environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
                var fullPath = Path.Combine(webRootPath, filePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public Task<string> GetFileUrlAsync(string filePath)
        {
            // In a real application, this might return a CDN URL or full URL
            return Task.FromResult($"/{filePath}");
        }
    }
}
