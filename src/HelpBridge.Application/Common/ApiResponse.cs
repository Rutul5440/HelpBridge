namespace HelpBridge.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = [];
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string TraceId { get; set; } = Guid.NewGuid().ToString("N");

    public static ApiResponse<T> Ok(T data, string message = "Operation completed successfully.")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static ApiResponse<T> Fail(string error, string message = "Operation failed.")
    {
        return new ApiResponse<T>
        {
            Success = false,
            Errors = [error],
            Message = message
        };
    }

    public static ApiResponse<T> Fail(IEnumerable<string> errors, string message = "Validation or processing errors occurred.")
    {
        return new ApiResponse<T>
        {
            Success = false,
            Errors = errors.ToList(),
            Message = message
        };
    }
}

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string Error { get; }

    protected Result(bool isSuccess, T? value, string error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, string.Empty);
    public static Result<T> Failure(string error) => new(false, default, error);
}

