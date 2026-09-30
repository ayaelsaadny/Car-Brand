using System.Collections.Generic;

namespace Limousine.API.Errors
{
    public class ApiResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }

        public ApiResponse(int statusCode, string? message = null)
        {
            StatusCode = statusCode;
            Message = message ?? GetDefaultMessageForStatusCode(statusCode) ?? string.Empty;
        }

        private static string? GetDefaultMessageForStatusCode(int statusCode)
        {
            return statusCode switch
            {
                200 => "Success",
                201 => "Created",
                204 => "No Content",
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                409 => "Conflict",
                500 => "Internal Server Error",
                _ => null
            };
        }
    }

    public class ApiResponse<T> : ApiResponse
    {
        public T? Data { get; set; }

        public ApiResponse(int statusCode, string? message = null, T? data = default) 
            : base(statusCode, message)
        {
            Data = data;
        }

        public static ApiResponse<T> Success(T data, string? message = null)
        {
            return new ApiResponse<T>(200, message ?? "Success", data);
        }

        public static ApiResponse<T> Created(T data, string? message = null)
        {
            return new ApiResponse<T>(201, message ?? "Resource created successfully", data);
        }
    }

    public class ApiValidationErrorResponse : ApiResponse
    {
        public IEnumerable<string> Errors { get; set; } = new List<string>();

        public ApiValidationErrorResponse(IEnumerable<string>? errors = null, string? message = "Validation errors occurred") 
            : base(400, message)
        {
            if (errors != null)
            {
                Errors = errors;
            }
        }
    }

    public class ApiExceptionResponse : ApiResponse
    {
        public string? Details { get; set; }

        public ApiExceptionResponse(int statusCode, string? message = null, string? details = null) 
            : base(statusCode, message)
        {
            Details = details;
        }
    }
}
