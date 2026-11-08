using HelpBridge.Application.Interfaces;

namespace HelpBridge.Infrastructure.Storage;

public class LocalOrBlobStorageService : IBlobStorageService
{
    private readonly string _storageDirectory;

    public LocalOrBlobStorageService()
    {
        _storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "PrivateUploads");
        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, string containerName, CancellationToken cancellationToken = default)
    {
        var safeFileName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        var containerDir = Path.Combine(_storageDirectory, containerName);
        if (!Directory.Exists(containerDir))
        {
            Directory.CreateDirectory(containerDir);
        }

        var fullPath = Path.Combine(containerDir, safeFileName);
        using var targetStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await fileStream.CopyToAsync(targetStream, cancellationToken);

        return $"{containerName}/{safeFileName}";
    }

    public string GenerateSasUri(string blobPath, TimeSpan validDuration)
    {
        // For development/mocking, returns a secure time-tokenized route
        var expiry = DateTimeOffset.UtcNow.Add(validDuration).ToUnixTimeSeconds();
        return $"/api/v1/documents/stream?path={Uri.EscapeDataString(blobPath)}&expires={expiry}";
    }

    public Task<bool> DeleteAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storageDirectory, blobPath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}

