using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace RenderVN.CoreApi.Infrastructure;

public sealed class CloudinaryImageStore
    : IImageStore
{
    private readonly Cloudinary? _cloudinary;
    private readonly ILogger<CloudinaryImageStore> _logger;

    public CloudinaryImageStore(
        IConfiguration configuration,
        ILogger<CloudinaryImageStore> logger)
    {
        _logger = logger;
        var cloudinaryUrl = configuration["CLOUDINARY_URL"];
        if (string.IsNullOrWhiteSpace(cloudinaryUrl))
        {
            logger.LogError("Cloudinary image storage is not configured.");
            return;
        }

        try
        {
            _cloudinary = new Cloudinary(cloudinaryUrl);
            _cloudinary.Api.Secure = true;
        }
        catch (Exception)
        {
            logger.LogError("Cloudinary image storage configuration is invalid.");
            _cloudinary = null;
        }
    }

    public async Task<StoredImage> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_cloudinary is null)
            {
                throw new ImageStoreException("The image provider is not configured.");
            }

            var storageKey = $"users/{ownerId:N}/uploads/{Guid.NewGuid():N}";
            var parameters = new ImageUploadParams
            {
                File = new FileDescription(fileName, content),
                PublicId = storageKey,
                Overwrite = false,
                UniqueFilename = false,
                UseFilename = false,
                Type = "private"
            };

            var result = await _cloudinary.UploadAsync(parameters, cancellationToken);
            if (result.Error is not null || string.IsNullOrWhiteSpace(result.PublicId))
            {
                _logger.LogError("Cloudinary rejected an image upload.");
                throw new ImageStoreException("The image provider rejected the upload.");
            }

            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
            var signedUrl = _cloudinary.DownloadPrivate(
                result.PublicId,
                attachment: false,
                format: result.Format,
                type: "private",
                expiresAt: expiresAt,
                resourceType: "image",
                transformation: null,
                targetFilename: null);

            return new StoredImage(
                signedUrl,
                result.Width,
                result.Height,
                result.PublicId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ImageStoreException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Cloudinary image upload failed.");
            throw new ImageStoreException("The image provider upload failed.", exception);
        }
    }
}
