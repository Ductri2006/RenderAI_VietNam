namespace RenderVN.CoreApi.Domain;

public enum RenderJobStatus
{
    Pending = 1,
    Running = 2,
    Succeeded = 3,
    Failed = 4,
    Cancelled = 5
}
