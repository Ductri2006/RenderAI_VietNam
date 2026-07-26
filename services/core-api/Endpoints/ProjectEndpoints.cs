using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RenderVN.CoreApi.Data;
using RenderVN.CoreApi.Domain;

namespace RenderVN.CoreApi.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/projects")
            .RequireAuthorization();

        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapDelete("/{id:guid}", DeleteAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateProjectRequest request,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        if (!TryParseRoomType(request.RoomType, out var roomType))
        {
            return Results.BadRequest(new ApiError(
                "invalid_room_type",
                "Room type must be living-room, bedroom, or kitchen."));
        }

        var projectName = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(projectName) || projectName.Length > 200)
        {
            return Results.BadRequest(new ApiError(
                "invalid_name",
                "Project name is required and must not exceed 200 characters."));
        }

        var project = new Project
        {
            UserId = GetUserId(principal),
            Name = projectName,
            RoomType = roomType
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);

        return Results.Created(
            $"/api/projects/{project.Id}",
            ToResponse(project));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var project = await db.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == id && item.UserId == userId,
                cancellationToken);
        return project is null
            ? Results.NotFound(new ApiError("project_not_found", "Project was not found."))
            : Results.Ok(ToResponse(project));
    }

    private static async Task<IResult> ListAsync(
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var projects = await db.Projects
            .AsNoTracking()
            .Where(project => project.UserId == userId)
            .ToListAsync(cancellationToken);
        return Results.Ok(projects
            .OrderByDescending(project => project.CreatedAt)
            .Select(ToResponse)
            .ToList());
    }

    private static async Task<IResult> DeleteAsync(
        Guid id,
        ClaimsPrincipal principal,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var project = await db.Projects
            .Include(item => item.SourceImages)
            .Include(item => item.RenderJobs)
                .ThenInclude(job => job.Result)
            .SingleOrDefaultAsync(
                item => item.Id == id && item.UserId == userId,
                cancellationToken);
        if (project is null)
        {
            return Results.NotFound(new ApiError("project_not_found", "Project was not found."));
        }

        db.RenderResults.RemoveRange(project.RenderJobs
            .Where(job => job.Result is not null)
            .Select(job => job.Result!));
        db.RenderJobs.RemoveRange(project.RenderJobs);
        db.SourceImages.RemoveRange(project.SourceImages);
        db.Projects.Remove(project);
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static Guid GetUserId(ClaimsPrincipal principal)
    {
        return Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    }

    private static bool TryParseRoomType(string value, out RoomType roomType)
    {
        roomType = value switch
        {
            "living-room" => RoomType.LivingRoom,
            "bedroom" => RoomType.Bedroom,
            "kitchen" => RoomType.Kitchen,
            _ => default
        };
        return roomType != default;
    }

    private static ProjectResponse ToResponse(Project project)
    {
        var roomType = project.RoomType switch
        {
            RoomType.LivingRoom => "living-room",
            RoomType.Bedroom => "bedroom",
            RoomType.Kitchen => "kitchen",
            _ => throw new InvalidOperationException("Unsupported room type.")
        };
        return new ProjectResponse(project.Id, project.Name, roomType, project.CreatedAt);
    }

    private sealed record CreateProjectRequest(string Name, string RoomType);
    private sealed record ProjectResponse(
        Guid Id,
        string Name,
        string RoomType,
        DateTimeOffset CreatedAt);
    private sealed record ApiError(string Code, string Message);
}
