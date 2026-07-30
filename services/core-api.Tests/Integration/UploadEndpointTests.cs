using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using RenderVN.CoreApi.Infrastructure;

namespace RenderVN.CoreApi.Tests.Integration;

public sealed class UploadEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly byte[] PngBytes = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    [Fact]
    public async Task AnonymousUploadIsRejected()
    {
        using var client = factory.CreateAuthenticatedClient();
        using var response = await client.PostAsync("/api/uploads", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ValidPngReturnsDimensionsAndPassesBytesToStore()
    {
        var store = new RecordingImageStore();
        using var host = CreateHost(store);
        using var client = await RegisterAsync(host);
        using var form = CreateForm(PngBytes, "image/png", "room.png");

        using var response = await client.PostAsync("/api/uploads", form);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<UploadResponse>();
        Assert.NotNull(payload);
        Assert.Equal("https://images.example/uploaded.png", payload.Url);
        Assert.Equal(1, payload.Width);
        Assert.Equal(1, payload.Height);
        Assert.Equal("upload", payload.SourceType);
        Assert.Equal(1, store.Calls);
        Assert.Equal(PngBytes, store.Content);
        Assert.Equal("image/png", store.ContentType);
        Assert.Equal("room.png", store.FileName);
        Assert.NotEqual(Guid.Empty, store.OwnerId);
    }

    [Fact]
    public async Task MissingFileReturnsBadRequestWithoutCallingStore()
    {
        var store = new RecordingImageStore();
        using var host = CreateHost(store);
        using var client = await RegisterAsync(host);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("upload"), "mode");

        using var response = await client.PostAsync("/api/uploads", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("missing_file", (await response.Content.ReadFromJsonAsync<ApiError>())!.Code);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task MalformedMultipartReturnsBadRequestWithoutCallingStore()
    {
        var store = new RecordingImageStore();
        using var host = CreateHost(store);
        using var client = await RegisterAsync(host);
        using var content = new ByteArrayContent("not-a-valid-multipart-body"u8.ToArray());
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(
            "multipart/form-data; boundary=render-vn-boundary");

        using var response = await client.PostAsync("/api/uploads", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_multipart", (await response.Content.ReadFromJsonAsync<ApiError>())!.Code);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task MultipartWithoutBoundaryReturnsBadRequest()
    {
        var store = new RecordingImageStore();
        using var host = CreateHost(store);
        using var client = await RegisterAsync(host);
        using var content = new ByteArrayContent([]);
        content.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");

        using var response = await client.PostAsync("/api/uploads", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_multipart", (await response.Content.ReadFromJsonAsync<ApiError>())!.Code);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task MultipartWithOverlongBoundaryReturnsBadRequest()
    {
        var store = new RecordingImageStore();
        using var host = CreateHost(store);
        using var client = await RegisterAsync(host);
        using var content = new ByteArrayContent([]);
        content.Headers.TryAddWithoutValidation(
            "Content-Type",
            $"multipart/form-data; boundary={new string('x', 200)}");

        using var response = await client.PostAsync("/api/uploads", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_multipart", (await response.Content.ReadFromJsonAsync<ApiError>())!.Code);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task MultipleFilesReturnBadRequestWithoutCallingStore()
    {
        var store = new RecordingImageStore();
        using var host = CreateHost(store);
        using var client = await RegisterAsync(host);
        using var form = CreateForm(PngBytes, "image/png", "one.png");
        form.Add(new ByteArrayContent(PngBytes), "file", "two.png");

        using var response = await client.PostAsync("/api/uploads", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("exactly_one_file", (await response.Content.ReadFromJsonAsync<ApiError>())!.Code);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task UnsupportedMimeReturnsBadRequestWithoutCallingStore()
    {
        var store = new RecordingImageStore();
        using var host = CreateHost(store);
        using var client = await RegisterAsync(host);
        using var form = CreateForm(PngBytes, "image/gif", "room.gif");

        using var response = await client.PostAsync("/api/uploads", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("unsupported_file_type", (await response.Content.ReadFromJsonAsync<ApiError>())!.Code);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task InvalidImageBytesReturnBadRequestWithoutCallingStore()
    {
        var store = new RecordingImageStore();
        using var host = CreateHost(store);
        using var client = await RegisterAsync(host);
        using var form = CreateForm("not-an-image"u8.ToArray(), "image/png", "room.png");

        using var response = await client.PostAsync("/api/uploads", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_image", (await response.Content.ReadFromJsonAsync<ApiError>())!.Code);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task DeclaredMimeMustMatchDetectedImageFormat()
    {
        var store = new RecordingImageStore();
        using var host = CreateHost(store);
        using var client = await RegisterAsync(host);
        using var form = CreateForm(PngBytes, "image/jpeg", "room.jpg");

        using var response = await client.PostAsync("/api/uploads", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_image", (await response.Content.ReadFromJsonAsync<ApiError>())!.Code);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task FilePartMustUseTheFileFieldName()
    {
        var store = new RecordingImageStore();
        using var host = CreateHost(store);
        using var client = await RegisterAsync(host);
        using var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(PngBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(content, "other", "room.png");

        using var response = await client.PostAsync("/api/uploads", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_file_field", (await response.Content.ReadFromJsonAsync<ApiError>())!.Code);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task TruncatedKnownFormatReturnsBadRequestWithoutCallingStore()
    {
        var store = new RecordingImageStore();
        using var host = CreateHost(store);
        using var client = await RegisterAsync(host);
        using var form = CreateForm(PngBytes[..24], "image/png", "room.png");

        using var response = await client.PostAsync("/api/uploads", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("invalid_image", (await response.Content.ReadFromJsonAsync<ApiError>())!.Code);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task FileOverTenMegabytesReturnsPayloadTooLargeWithoutCallingStore()
    {
        var store = new RecordingImageStore();
        using var host = CreateHost(store);
        using var client = await RegisterAsync(host);
        using var form = CreateForm(new byte[10_485_761], "image/png", "large.png");

        using var response = await client.PostAsync("/api/uploads", form);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("file_too_large", (await response.Content.ReadFromJsonAsync<ApiError>())!.Code);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task ChunkedFileOverTenMegabytesReturnsPayloadTooLarge()
    {
        var store = new RecordingImageStore();
        using var host = CreateHost(store);
        using var client = await RegisterAsync(host);
        using var form = new ChunkedMultipartContent(10_485_761);

        using var response = await client.PostAsync("/api/uploads", form);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal("file_too_large", (await response.Content.ReadFromJsonAsync<ApiError>())!.Code);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task StorageFailureReturnsBadGateway()
    {
        var store = new RecordingImageStore
        {
            Failure = new ImageStoreException("provider unavailable")
        };
        using var host = CreateHost(store);
        using var client = await RegisterAsync(host);
        using var form = CreateForm(PngBytes, "image/png", "room.png");

        using var response = await client.PostAsync("/api/uploads", form);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("image_store_failed", (await response.Content.ReadFromJsonAsync<ApiError>())!.Code);
        Assert.Equal(1, store.Calls);
    }

    [Fact]
    public async Task UnexpectedStorageFailureReturnsBadGateway()
    {
        var store = new RecordingImageStore
        {
            Failure = new HttpRequestException("provider unavailable")
        };
        using var host = CreateHost(store);
        using var client = await RegisterAsync(host);
        using var form = CreateForm(PngBytes, "image/png", "room.png");

        using var response = await client.PostAsync("/api/uploads", form);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal("image_store_failed", (await response.Content.ReadFromJsonAsync<ApiError>())!.Code);
        Assert.Equal(1, store.Calls);
    }

    [Fact]
    public async Task MissingCloudinaryConfigurationFailsThroughTypedProviderBoundary()
    {
        var store = new CloudinaryImageStore(
            new ConfigurationBuilder().Build(),
            NullLogger<CloudinaryImageStore>.Instance);

        await Assert.ThrowsAsync<ImageStoreException>(() => store.UploadAsync(
            new MemoryStream(PngBytes),
            "room.png",
            "image/png",
            Guid.NewGuid(),
            CancellationToken.None));
    }

    [Fact]
    public async Task MalformedCloudinaryConfigurationFailsThroughTypedProviderBoundary()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CLOUDINARY_URL"] = "not-a-cloudinary-url"
            })
            .Build();
        var store = new CloudinaryImageStore(
            configuration,
            NullLogger<CloudinaryImageStore>.Instance);

        await Assert.ThrowsAsync<ImageStoreException>(() => store.UploadAsync(
            new MemoryStream(PngBytes),
            "room.png",
            "image/png",
            Guid.NewGuid(),
            CancellationToken.None));
    }

    private WebApplicationFactory<Program> CreateHost(IImageStore store)
    {
        return factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IImageStore>();
            services.AddSingleton(store);
        }));
    }

    private static async Task<HttpClient> RegisterAsync(WebApplicationFactory<Program> host)
    {
        var client = host.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        using var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"upload-{Guid.NewGuid():N}@example.com",
            password = "StrongPass123!"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return client;
    }

    private static MultipartFormDataContent CreateForm(
        byte[] bytes,
        string contentType,
        string fileName)
    {
        var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(content, "file", fileName);
        return form;
    }

    private sealed class RecordingImageStore : IImageStore
    {
        public int Calls { get; private set; }
        public byte[]? Content { get; private set; }
        public string? FileName { get; private set; }
        public string? ContentType { get; private set; }
        public Guid OwnerId { get; private set; }
        public Exception? Failure { get; init; }

        public async Task<StoredImage> UploadAsync(
            Stream content,
            string fileName,
            string contentType,
            Guid ownerId,
            CancellationToken cancellationToken)
        {
            Calls++;
            if (Failure is not null)
            {
                throw Failure;
            }

            using var copy = new MemoryStream();
            await content.CopyToAsync(copy, cancellationToken);
            Content = copy.ToArray();
            FileName = fileName;
            ContentType = contentType;
            OwnerId = ownerId;
            return new StoredImage(
                "https://images.example/uploaded.png",
                1,
                1,
                "users/test/uploaded");
        }
    }

    private sealed class ChunkedMultipartContent : HttpContent
    {
        private const string Boundary = "render-vn-boundary";
        private readonly int _fileBytes;

        public ChunkedMultipartContent(int fileBytes)
        {
            _fileBytes = fileBytes;
            Headers.ContentType = MediaTypeHeaderValue.Parse(
                $"multipart/form-data; boundary={Boundary}");
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }

        protected override async Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context)
        {
            var prefix = $"--{Boundary}\r\n"
                + "Content-Disposition: form-data; name=\"file\"; filename=\"large.png\"\r\n"
                + "Content-Type: image/png\r\n\r\n";
            await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(prefix));
            await stream.WriteAsync(new byte[_fileBytes]);
            var suffix = $"\r\n--{Boundary}--\r\n";
            await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(suffix));
        }
    }

    private sealed record ApiError(string Code, string Message);
    private sealed record UploadResponse(string Url, int Width, int Height, string SourceType);
}
