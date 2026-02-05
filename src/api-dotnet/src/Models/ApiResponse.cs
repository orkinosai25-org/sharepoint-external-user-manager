namespace SharePointExternalUserManager.Functions.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public ApiError? Error { get; set; }
    public PaginationMeta? Pagination { get; set; }

    public static ApiResponse<T> SuccessResponse(T data, PaginationMeta? pagination = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Pagination = pagination
        };
    }

    public static ApiResponse<T> ErrorResponse(string code, string message, string? details = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = new ApiError
            {
                Code = code,
                Message = message,
                Details = details
            }
        };
    }
}

public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
}

public class PaginationMeta
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public bool HasNext { get; set; }
}
