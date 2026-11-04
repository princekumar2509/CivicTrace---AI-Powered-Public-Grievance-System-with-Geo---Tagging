using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PublicGrievanceSystem.Services
{
    public class PendingComplaintUploadStore
    {
        private readonly string _storagePath;

        private const int ExpirationMinutes = 30;

        public PendingComplaintUploadStore(
            IWebHostEnvironment environment)
        {
            _storagePath = Path.Combine(
                environment.ContentRootPath,
                "App_Data",
                "PendingComplaintUploads");
        }

        private class UploadManifest
        {
            public DateTime CreatedAtUtc { get; set; }

            public List<UploadMetadata> Images { get; set; } = new();
        }

        private class UploadMetadata
        {
            public string StoredFileName { get; set; } = "";

            public string OriginalFileName { get; set; } = "";

            public string ContentType { get; set; } = "";
        }

        public class PendingImage
        {
            public byte[] Content { get; set; } = Array.Empty<byte>();

            public string FileName { get; set; } = "";

            public string ContentType { get; set; } = "";
        }

        private string GetUserFolder(string userId)
        {
            string userHash = Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(userId)));

            return Path.Combine(_storagePath, userHash);
        }

        private string GetUploadFolder(string token, string userId)
        {
            return Path.Combine(
                GetUserFolder(userId),
                Guid.ParseExact(token, "N").ToString("N"));
        }

        public async Task<string> SaveAsync(
            IEnumerable<IFormFile> files,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var selectedFiles = files
                .Where(f => f != null && f.Length > 0)
                .ToList();

            if (selectedFiles.Count == 0)
                throw new InvalidOperationException(
                    "No images were provided.");

            string token = Guid.NewGuid().ToString("N");

            string folder = GetUploadFolder(token, userId);

            Directory.CreateDirectory(folder);

            try
            {
                var manifest = new UploadManifest
                {
                    CreatedAtUtc = DateTime.UtcNow
                };

                foreach (var file in selectedFiles)
                {
                    string extension =
                        Path.GetExtension(file.FileName);

                    string storedName =
                        Guid.NewGuid().ToString("N") + extension;

                    string filePath =
                        Path.Combine(folder, storedName);

                    await using (var stream = new FileStream(
                        filePath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None))
                    {
                        await file.CopyToAsync(
                            stream,
                            cancellationToken);
                    }

                    manifest.Images.Add(new UploadMetadata
                    {
                        StoredFileName = storedName,
                        OriginalFileName =
                            Path.GetFileName(file.FileName),
                        ContentType = file.ContentType
                    });
                }

                string json = JsonSerializer.Serialize(manifest);

                await File.WriteAllTextAsync(
                    Path.Combine(folder, "metadata.json"),
                    json,
                    cancellationToken);

                return token;
            }
            catch
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);

                throw;
            }
        }

        public async Task<IReadOnlyList<PendingImage>> RestoreAsync(
            string token,
            string userId,
            CancellationToken cancellationToken = default)
        {
            var restoredImages = new List<PendingImage>();

            if (string.IsNullOrWhiteSpace(userId) ||
                !Guid.TryParseExact(token, "N", out _))
            {
                return restoredImages;
            }

            string folder = GetUploadFolder(token, userId);

            string manifestPath =
                Path.Combine(folder, "metadata.json");

            if (!File.Exists(manifestPath))
                return restoredImages;

            var manifest = JsonSerializer.Deserialize<UploadManifest>(
                await File.ReadAllTextAsync(
                    manifestPath,
                    cancellationToken));

            if (manifest == null ||
                DateTime.UtcNow - manifest.CreatedAtUtc >
                TimeSpan.FromMinutes(ExpirationMinutes))
            {
                await DeleteAsync(token, userId);
                return restoredImages;
            }

            foreach (var image in manifest.Images)
            {
                if (Path.GetFileName(image.StoredFileName)
                    != image.StoredFileName)
                {
                    continue;
                }

                string path = Path.Combine(
                    folder,
                    image.StoredFileName);

                if (!File.Exists(path))
                    continue;

                restoredImages.Add(new PendingImage
                {
                    Content = await File.ReadAllBytesAsync(
                        path,
                        cancellationToken),

                    FileName = image.OriginalFileName,

                    ContentType = image.ContentType
                });
            }

            return restoredImages;
        }

        public Task DeleteAsync(string token, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId) ||
                !Guid.TryParseExact(token, "N", out _))
            {
                return Task.CompletedTask;
            }

            string folder = GetUploadFolder(token, userId);

            if (Directory.Exists(folder))
                Directory.Delete(folder, true);

            return Task.CompletedTask;
        }
    }
}
