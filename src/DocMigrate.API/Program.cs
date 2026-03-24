using DocMigrate.API.Extensions;
using DocMigrate.API.Middleware;
using DocMigrate.Application.Interfaces;
using DocMigrate.Application.Validators;
using DocMigrate.Infrastructure.Configuration;
using DocMigrate.Infrastructure.Data;
using DocMigrate.Infrastructure.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Minio;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5433;Database=docmigrate;Username=docmigrate;Password=docmigrate_dev";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// MinIO
builder.Services.Configure<MinioSettings>(builder.Configuration.GetSection("Minio"));

var minioSettings = builder.Configuration.GetSection("Minio").Get<MinioSettings>() ?? new MinioSettings();

builder.Services.AddSingleton<IMinioClient>(_ =>
    new MinioClient()
        .WithEndpoint(minioSettings.Endpoint)
        .WithCredentials(minioSettings.AccessKey, minioSettings.SecretKey)
        .WithSSL(minioSettings.UseSsl)
        .Build());

// Services
builder.Services.AddScoped<ISpaceService, SpaceService>();
builder.Services.AddScoped<IPageService, PageService>();
builder.Services.AddScoped<IFileService, MinioFileService>();
builder.Services.AddScoped<IUserPreferenceService, UserPreferenceService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<ITemplateService, TemplateService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IReferenceService, ReferenceService>();
builder.Services.AddScoped<IPageVersionService, PageVersionService>();
builder.Services.AddScoped<IUserResolverService, UserResolverService>();
builder.Services.AddScoped<IUserActivityService, UserActivityService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IPageTranslationService, PageTranslationService>();
var geminiApiKey = builder.Configuration["GeminiTranslation:ApiKey"];
if (!string.IsNullOrEmpty(geminiApiKey))
{
    builder.Services.AddHttpClient("GeminiTranslation");
    builder.Services.AddSingleton<ITranslationProvider, GeminiTranslationProvider>();
}
else
{
    builder.Services.AddSingleton<ITranslationProvider, NoOpTranslationProvider>();
}
builder.Services.AddSingleton<TiptapTranslationHelper>();
builder.Services.AddSingleton<IPlainTextExtractor, TiptapPlainTextExtractor>();

// Validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateSpaceRequestValidator>();

// Controllers
builder.Services.AddControllers();

// OpenAPI / Swagger
builder.Services.AddOpenApi();

// CORS (environment-driven)
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AppCors", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// Response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

// Keycloak Authentication & Authorization
builder.Services.AddKeycloakAuth(builder.Configuration);

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("EditorOnly", policy =>
        policy.RequireRole("docmigrate-editor"));
});

var app = builder.Build();

// Global exception handling (BEFORE everything else)
app.UseMiddleware<GlobalExceptionMiddleware>();

// Response compression (before CORS and routing)
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// CORS (all environments)
app.UseCors("AppCors");

// Auth pipeline
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/api/health", [AllowAnonymous] () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck");


app.Run();
