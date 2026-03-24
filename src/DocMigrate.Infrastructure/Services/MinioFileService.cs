using DocMigrate.Application.Interfaces;
using DocMigrate.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace DocMigrate.Infrastructure.Services;

public class MinioFileService(IMinioClient minioClient, IOptions<MinioSettings> options) : IFileService
{
    private readonly MinioSettings _settings = options.Value;

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType)
    {
        var bucketExists = await minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_settings.BucketName));

        if (!bucketExists)
        {
            await minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_settings.BucketName));
        }

        var extension = Path.GetExtension(fileName);
        var objectName = $"icons/{Guid.NewGuid()}{extension}";

        await minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_settings.BucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(contentType));

        var protocol = _settings.UseSsl ? "https" : "http";
        return $"{protocol}://{_settings.Endpoint}/{_settings.BucketName}/{objectName}";
    }

    public async Task<string> UploadImageAsync(Stream stream, string fileName, string contentType)
    {
        var bucketExists = await minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_settings.BucketName));

        if (!bucketExists)
        {
            await minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_settings.BucketName));
        }

        var extension = Path.GetExtension(fileName);
        var objectName = $"images/{Guid.NewGuid()}{extension}";

        await minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_settings.BucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(contentType));

        var protocol = _settings.UseSsl ? "https" : "http";
        return $"{protocol}://{_settings.Endpoint}/{_settings.BucketName}/{objectName}";
    }

    public async Task<string> UploadVideoAsync(Stream stream, string fileName, string contentType)
    {
        var bucketExists = await minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(_settings.BucketName));

        if (!bucketExists)
        {
            await minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(_settings.BucketName));
        }

        var extension = Path.GetExtension(fileName);
        var objectName = $"videos/{Guid.NewGuid()}{extension}";

        await minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_settings.BucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType(contentType));

        var protocol = _settings.UseSsl ? "https" : "http";
        return $"{protocol}://{_settings.Endpoint}/{_settings.BucketName}/{objectName}";
    }

    public async Task DeleteAsync(string fileUrl)
    {
        var uri = new Uri(fileUrl);
        // Path format: /{bucket}/icons/{filename}
        var objectName = uri.AbsolutePath
            .TrimStart('/')
            .Substring(_settings.BucketName.Length + 1)
            .TrimStart('/');

        await minioClient.RemoveObjectAsync(new RemoveObjectArgs()
            .WithBucket(_settings.BucketName)
            .WithObject(objectName));
    }
}
