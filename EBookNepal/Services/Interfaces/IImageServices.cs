using CloudinaryDotNet.Actions;

namespace EBookNepal.Services.Interfaces
{
    public interface IImageServices
    {
        Task<ImageUploadResult> UploadPhotoAsync(IFormFile file);
        Task<DeletionResult> DeletePhotoAsync(string publicId);
    }
}