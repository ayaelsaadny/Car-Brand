using Limousine.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Limousine.API.Services
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;

        public FileService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string folderName, CancellationToken cancellationToken = default)
        {
            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            var uploadsFolder = Path.Combine(webRootPath, "uploads", folderName);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileDestination = new FileStream(fullPath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileDestination, cancellationToken);
            }

            return $"/uploads/{folderName}/{uniqueFileName}";
        }

        public bool DeleteFile(string relativeFilePath)
        {
            if (string.IsNullOrWhiteSpace(relativeFilePath)) return false;

            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            var cleanPath = relativeFilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(webRootPath, cleanPath);

            if (File.Exists(fullPath))
            {
                try
                {
                    File.Delete(fullPath);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
    }
}
