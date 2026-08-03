namespace TwinParticles.CheckEngine.Application.Common.Results;

public sealed class CommandResult
{
    private CommandResult(bool success, string? errorCode)
    {
        Success = success;
        ErrorCode = errorCode;
    }

    public bool Success { get; }

    public string? ErrorCode { get; }

    public static CommandResult Ok() => new(true, null);

    public static CommandResult Fail(string errorCode) => new(false, errorCode);
}
