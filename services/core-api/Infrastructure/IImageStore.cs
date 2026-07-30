namespace RenderVN.CoreApi.Infrastructure;

public interface IImageStore
{
    Task<StoredImage> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        Guid ownerId,
        CancellationToken cancellationToken);
}

public sealed record StoredImage(
    string Url,
    int Width,
    int Height,
    string StorageKey);

public sealed class ImageStoreException(string message, Exception? innerException = null)
    : Exception(message, innerException);
