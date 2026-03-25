using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using EBookNepal.Services.Interfaces;

namespace EBookNepal.Services
{
    public class ImageServices : IImageServices
    {
        private readonly Cloudinary _cloudinary;
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

        public ImageServices(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task<ImageUploadResult> UploadPhotoAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null.", nameof(file));

            if (file.Length > MaxFileSizeBytes)
                throw new ArgumentException($"File size exceeds the {MaxFileSizeBytes / (1024 * 1024)} MB limit.", nameof(file));

            if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
                throw new ArgumentException($"Unsupported file type '{file.ContentType}'. Allowed: jpeg, png, webp.", nameof(file));

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "ebooks",
                // Auto-crop to a square and resize to 400x400 for consistent profile images
                Transformation = new Transformation()
                    .Width(400).Height(400)
                    .Crop("fill")
                    .Gravity("face")
                    .Quality("auto")
                    .FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new InvalidOperationException($"Cloudinary upload failed: {uploadResult.Error.Message}");

            return uploadResult;
        }

        public async Task<DeletionResult> DeletePhotoAsync(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                throw new ArgumentException("Public ID is required.", nameof(publicId));

            var deleteParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Image
            };

            var result = await _cloudinary.DestroyAsync(deleteParams);

            if (result.Error != null)
                throw new InvalidOperationException($"Cloudinary deletion failed: {result.Error.Message}");

            return result;
        }
    }
}