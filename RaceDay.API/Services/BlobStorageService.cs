
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.API.Services;

public class BlobStorageService
{
    private readonly BlobContainerClient _container;

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration[
            "AzureBlobStorage:ConnectionString"];

        var containerName = configuration[
            "AzureBlobStorage:ContainerName"];

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Azure Blob Storage connection string is missing.");

        if (string.IsNullOrWhiteSpace(containerName))
            throw new InvalidOperationException(
                "Azure Blob Storage container name is missing.");

        _container = new BlobContainerClient(
            connectionString, containerName);
    }

    public async Task<string> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        var blobName = $"{Guid.NewGuid():N}{extension}";

        var blobClient = _container.GetBlobClient(blobName);

        await blobClient.UploadAsync(
            fileStream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            });

        return blobName;
    }

    public async Task<Stream> DownloadAsync(string blobName)
    {
        var blobClient = _container.GetBlobClient(blobName);

        var response = await blobClient.DownloadStreamingAsync();

        return response.Value.Content;
    }

    public async Task DeleteAsync(string blobName)
    {
        var blobClient = _container.GetBlobClient(blobName);

        await blobClient.DeleteIfExistsAsync();
    }
}
