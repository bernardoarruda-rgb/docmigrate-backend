namespace DocMigrate.Application.Interfaces;

public interface IFileService
{
    Task<string> UploadAsync(Stream stream, string fileName, string contentType);
    Task<string> UploadImageAsync(Stream stream, string fileName, string contentType);
    Task<string> UploadVideoAsync(Stream stream, string fileName, string contentType);
    Task DeleteAsync(string fileUrl);
}
