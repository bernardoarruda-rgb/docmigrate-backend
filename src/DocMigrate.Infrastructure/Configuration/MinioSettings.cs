namespace DocMigrate.Infrastructure.Configuration;

public class MinioSettings
{
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "docmigrate";
    public string SecretKey { get; set; } = "docmigrate_dev";
    public string BucketName { get; set; } = "docmigrate";
    public bool UseSsl { get; set; } = false;
}
