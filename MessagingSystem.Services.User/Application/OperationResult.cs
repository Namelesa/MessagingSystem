namespace MessagingSystem.Services.User.Application;

public class OperationResult<T>
{
    public bool Success { get; }
    public string? Message { get; }
    public T? Data { get; }

    private OperationResult(bool success, T? data, string? message = null)
    {
        Success = success;
        Message = message;
        Data = data;
    }

    public static OperationResult<T> Ok(T data) => new(true, data);
    public static OperationResult<T> Fail(string message) => new(false, default, message);
}