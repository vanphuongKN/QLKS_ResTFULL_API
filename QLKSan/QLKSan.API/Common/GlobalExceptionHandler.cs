using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace QLKSan.Common
{
    // Bắt mọi exception chưa xử lý và trả về ProblemDetails (RFC 7807) với status code phù hợp
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IProblemDetailsService _problemDetailsService;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetailsService)
        {
            _logger = logger;
            _problemDetailsService = problemDetailsService;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (status, title, detail) = exception switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Không tìm thấy", exception.Message),
                BusinessException => (StatusCodes.Status400BadRequest, "Yêu cầu không hợp lệ", exception.Message),
                ArgumentException => (StatusCodes.Status400BadRequest, "Yêu cầu không hợp lệ", exception.Message),
                ConflictException => (StatusCodes.Status409Conflict, "Xung đột dữ liệu", exception.Message),
                DbUpdateException => (StatusCodes.Status409Conflict, "Xung đột dữ liệu",
                    "Không thể lưu vì dữ liệu bị trùng hoặc đang được sử dụng ở nơi khác."),
                _ => (StatusCodes.Status500InternalServerError, "Lỗi hệ thống", "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.")
            };

            if (status == StatusCodes.Status500InternalServerError || exception is DbUpdateException)
                _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

            httpContext.Response.StatusCode = status;
            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    Detail = detail
                }
            });
        }
    }
}
