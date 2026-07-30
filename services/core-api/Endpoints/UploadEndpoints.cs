using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using RenderVN.CoreApi.Infrastructure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;

namespace RenderVN.CoreApi.Endpoints;

public static class UploadEndpoints
{
    public const long MaxFileBytes = 10_485_760;
    public const long MaxMultipartBodyBytes = MaxFileBytes + 65_536;
    private const long MaxImagePixels = 16_000_000;

    private static readonly HashSet<string> AllowedContentTypes = new(
        ["image/png", "image/jpeg", "image/webp"],
        StringComparer.OrdinalIgnoreCase);

    public static IEndpointRouteBuilder MapUploadEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/uploads", UploadAsync)
            .RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> UploadAsync(
        HttpRequest request,
        ClaimsPrincipal principal,
        IImageStore imageStore,
        CancellationToken cancellationToken)
    {
        if (!IsMultipart(request.ContentType))
        {
            return Results.BadRequest(new ApiError(
                "multipart_required",
                "Upload must use multipart/form-data."));
        }

        if (!HasBoundary(request.ContentType))
        {
            return InvalidMultipart();
        }

        IFormCollection form;
        try
        {
            form = await request.ReadFormAsync(cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            return IsMultipartBodyLimitException(exception)
                ? PayloadTooLarge()
                : InvalidMultipart();
        }
        catch (BadHttpRequestException exception)
            when (exception.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            return PayloadTooLarge();
        }
        catch (BadHttpRequestException)
        {
            return InvalidMultipart();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (IOException exception)
            when (exception.Message.Contains(
                "Unexpected end of Stream",
                StringComparison.OrdinalIgnoreCase))
        {
            return InvalidMultipart();
        }

        if (form.Files.Count == 0)
        {
            return Results.BadRequest(new ApiError(
                "missing_file",
                "One image file is required."));
        }

        if (form.Files.Count != 1)
        {
            return Results.BadRequest(new ApiError(
                "exactly_one_file",
                "Exactly one image file is required."));
        }

        var file = form.Files[0];
        if (!string.Equals(file.Name, "file", StringComparison.Ordinal))
        {
            return Results.BadRequest(new ApiError(
                "invalid_file_field",
                "The image must be sent in the file field."));
        }

        if (file.Length > MaxFileBytes)
        {
            return PayloadTooLarge();
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return Results.BadRequest(new ApiError(
                "unsupported_file_type",
                "Only PNG, JPEG, and WebP images are supported."));
        }

        byte[] bytes;
        await using (var content = new MemoryStream((int)file.Length))
        {
            await file.CopyToAsync(content, cancellationToken);
            if (content.Length > MaxFileBytes)
            {
                return PayloadTooLarge();
            }

            bytes = content.ToArray();
        }

        if (!TryValidateImage(bytes, file.ContentType, out var width, out var height))
        {
            return Results.BadRequest(new ApiError(
                "invalid_image",
                "The uploaded file is not a valid supported image."));
        }

        StoredImage storedImage;
        try
        {
            await using var content = new MemoryStream(bytes, writable: false);
            storedImage = await imageStore.UploadAsync(
                content,
                Path.GetFileName(file.FileName),
                file.ContentType,
                GetUserId(principal),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (ImageStoreException)
        {
            return ImageStoreFailed();
        }
        catch (HttpRequestException)
        {
            return ImageStoreFailed();
        }
        catch (TimeoutException)
        {
            return ImageStoreFailed();
        }
        catch (OperationCanceledException)
        {
            return ImageStoreFailed();
        }

        return Results.Created(
            storedImage.Url,
            new UploadResponse(
                storedImage.Url,
                width,
                height,
                "upload"));
    }

    private static IResult ImageStoreFailed()
    {
        return Results.Json(
            new ApiError(
                "image_store_failed",
                "The image could not be stored. Please try again."),
            statusCode: StatusCodes.Status502BadGateway);
    }

    private static bool TryValidateImage(
        byte[] bytes,
        string declaredContentType,
        out int width,
        out int height)
    {
        width = 0;
        height = 0;

        try
        {
            IImageFormat detectedFormat = Image.DetectFormat(bytes);
            if (!detectedFormat.MimeTypes.Contains(
                    declaredContentType,
                    StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            var info = Image.Identify(bytes);
            if (info is null
                || info.Width <= 0
                || info.Height <= 0
                || (long)info.Width * info.Height > MaxImagePixels)
            {
                return false;
            }

            var decoderOptions = new DecoderOptions
            {
                MaxFrames = 1,
                SkipMetadata = true
            };
            using var image = Image.Load(decoderOptions, bytes);
            width = info.Width;
            height = info.Height;
            return true;
        }
        catch (UnknownImageFormatException)
        {
            return false;
        }
        catch (InvalidImageContentException)
        {
            return false;
        }
        catch (ImageFormatException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        return Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    private static bool IsMultipart(string? contentType)
    {
        return contentType is not null
            && string.Equals(
                contentType.Split(';', 2)[0].Trim(),
                "multipart/form-data",
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasBoundary(string? contentType)
    {
        return contentType?
            .Split(';')
            .Skip(1)
            .Select(part => part.Trim())
            .Where(part => part.StartsWith("boundary=", StringComparison.OrdinalIgnoreCase))
            .Select(part => part["boundary=".Length..].Trim().Trim('"'))
            .Any(value => value.Length is > 0 and <= 128) == true;
    }

    private static bool IsMultipartBodyLimitException(InvalidDataException exception)
    {
        return exception.Message.Contains(
            "Multipart body length limit",
            StringComparison.OrdinalIgnoreCase);
    }

    private static IResult PayloadTooLarge()
    {
        return Results.Json(
            new ApiError(
                "file_too_large",
                "Image files must not exceed 10 MB."),
            statusCode: StatusCodes.Status413PayloadTooLarge);
    }

    private static IResult InvalidMultipart()
    {
        return Results.BadRequest(new ApiError(
            "invalid_multipart",
            "The multipart upload body is invalid."));
    }

    private sealed record ApiError(string Code, string Message);
    private sealed record UploadResponse(
        string Url,
        int Width,
        int Height,
        string SourceType);
}
