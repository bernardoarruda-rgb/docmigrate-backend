using DocMigrate.Application.DTOs.File;
using DocMigrate.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocMigrate.API.Controllers;

[ApiController]
[Route("api/files")]
[Authorize]
public class FilesController(IFileService fileService) : AuthenticatedControllerBase
{
    private static readonly string[] AllowedIconContentTypes =
        ["image/png", "image/jpeg", "image/svg+xml", "image/webp"];

    private static readonly string[] AllowedImageContentTypes =
        ["image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml"];

    private static readonly string[] AllowedVideoContentTypes =
        ["video/mp4", "video/webm", "video/ogg"];

    private const long MaxIconSizeBytes = 1 * 1024 * 1024; // 1 MB
    private const long MaxImageSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const long MaxVideoSizeBytes = 100 * 1024 * 1024; // 100 MB

    [HttpPost("icons")]
    public async Task<ActionResult<FileUploadResponse>> UploadIcon(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Arquivo e obrigatorio" });

        if (file.Length > MaxIconSizeBytes)
            return BadRequest(new { message = "Tamanho maximo permitido: 1 MB" });

        if (!AllowedIconContentTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Tipo de arquivo nao suportado. Tipos aceitos: PNG, JPEG, SVG, WebP" });

        await using var stream = file.OpenReadStream();
        var url = await fileService.UploadAsync(stream, file.FileName, file.ContentType);

        return Ok(new FileUploadResponse { Url = url });
    }

    [HttpPost("images")]
    public async Task<ActionResult<FileUploadResponse>> UploadImage(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Arquivo e obrigatorio" });

        if (file.Length > MaxImageSizeBytes)
            return BadRequest(new { message = "Tamanho maximo permitido: 10 MB" });

        if (!AllowedImageContentTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Tipo de arquivo nao suportado. Tipos aceitos: JPG, PNG, GIF, WebP, SVG" });

        await using var stream = file.OpenReadStream();
        var url = await fileService.UploadImageAsync(stream, file.FileName, file.ContentType);

        return Ok(new FileUploadResponse { Url = url });
    }

    [HttpPost("videos")]
    [RequestSizeLimit(MaxVideoSizeBytes)]
    public async Task<ActionResult<FileUploadResponse>> UploadVideo(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Arquivo e obrigatorio" });

        if (file.Length > MaxVideoSizeBytes)
            return BadRequest(new { message = "Tamanho maximo permitido: 100 MB" });

        if (!AllowedVideoContentTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Tipo de arquivo nao suportado. Tipos aceitos: MP4, WebM, OGG" });

        await using var stream = file.OpenReadStream();
        var url = await fileService.UploadVideoAsync(stream, file.FileName, file.ContentType);

        return Ok(new FileUploadResponse { Url = url });
    }
}
