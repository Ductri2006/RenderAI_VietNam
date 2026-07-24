using System.Net;
using System.Net.Http.Json;

namespace RenderVN.CoreApi.Tests.Integration;

public sealed class ProjectEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task OwnerCanCreateAndReadProject()
    {
        var client = await CreateRegisteredClientAsync();

        using var created = await client.PostAsJsonAsync("/api/projects", new
        {
            name = "Warm Living Room",
            roomType = "living-room"
        });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdProject = await created.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.NotNull(createdProject);
        Assert.Equal("Warm Living Room", createdProject.Name);
        Assert.Equal("living-room", createdProject.RoomType);

        using var fetched = await client.GetAsync($"/api/projects/{createdProject.Id}");

        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        var fetchedProject = await fetched.Content.ReadFromJsonAsync<ProjectResponse>();
        Assert.Equal(createdProject, fetchedProject);
    }

    [Fact]
    public async Task UnsupportedRoomTypeReturnsStableValidationError()
    {
        var client = await CreateRegisteredClientAsync();

        using var response = await client.PostAsJsonAsync("/api/projects", new
        {
            name = "Office",
            roomType = "office"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Equal("invalid_room_type", error.Code);
    }

    [Fact]
    public async Task OwnerCanListOnlyTheirProjects()
    {
        var owner = await CreateRegisteredClientAsync();
        var otherOwner = await CreateRegisteredClientAsync();
        await CreateProjectAsync(owner, "Owner Bedroom", "bedroom");
        await CreateProjectAsync(owner, "Owner Kitchen", "kitchen");
        await CreateProjectAsync(otherOwner, "Other Kitchen", "kitchen");

        using var response = await owner.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var projects = await response.Content.ReadFromJsonAsync<List<ProjectResponse>>();
        Assert.NotNull(projects);
        Assert.Equal(2, projects.Count);
        Assert.Equal(
            ["Owner Bedroom", "Owner Kitchen"],
            projects.Select(project => project.Name).Order().ToArray());
    }

    [Fact]
    public async Task OwnerCanDeleteProject()
    {
        var owner = await CreateRegisteredClientAsync();
        var project = await CreateProjectAsync(owner, "Delete Me", "bedroom");

        using var deleted = await owner.DeleteAsync($"/api/projects/{project.Id}");
        using var fetched = await owner.GetAsync($"/api/projects/{project.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, fetched.StatusCode);
    }

    [Fact]
    public async Task SecondUserCannotReadAnotherUsersProject()
    {
        var owner = await CreateRegisteredClientAsync();
        var otherUser = await CreateRegisteredClientAsync();
        var project = await CreateProjectAsync(owner, "Private Project", "kitchen");

        using var response = await otherUser.GetAsync($"/api/projects/{project.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiError>();
        Assert.NotNull(error);
        Assert.Equal("project_not_found", error.Code);
    }

    [Fact]
    public async Task SecondUserCannotDeleteAnotherUsersProject()
    {
        var owner = await CreateRegisteredClientAsync();
        var otherUser = await CreateRegisteredClientAsync();
        var project = await CreateProjectAsync(owner, "Private Delete", "bedroom");

        using var deletion = await otherUser.DeleteAsync($"/api/projects/{project.Id}");
        using var ownerRead = await owner.GetAsync($"/api/projects/{project.Id}");

        Assert.Equal(HttpStatusCode.NotFound, deletion.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ownerRead.StatusCode);
    }

    private async Task<HttpClient> CreateRegisteredClientAsync()
    {
        var client = factory.CreateAuthenticatedClient();
        using var registration = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"project-{Guid.NewGuid():N}@example.com",
            password = "StrongPass123!"
        });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        return client;
    }

    private static async Task<ProjectResponse> CreateProjectAsync(
        HttpClient client,
        string name,
        string roomType)
    {
        using var response = await client.PostAsJsonAsync("/api/projects", new
        {
            name,
            roomType
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProjectResponse>())!;
    }

    private sealed record ProjectResponse(
        Guid Id,
        string Name,
        string RoomType,
        DateTimeOffset CreatedAt);

    private sealed record ApiError(string Code, string Message);
}
