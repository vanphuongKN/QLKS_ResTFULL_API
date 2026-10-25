using System.Net;

namespace QLKSan.Infrastructure
{
    // Lỗi trả về từ QLKSan.API (đọc từ ProblemDetails / ValidationProblemDetails)
    public class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        // Danh sách lỗi validation (nếu có), mỗi phần tử là một thông điệp
        public IReadOnlyList<string> Errors { get; }

        public ApiException(HttpStatusCode statusCode, string message, IReadOnlyList<string> errors = null)
            : base(message)
        {
            StatusCode = statusCode;
            Errors = errors ?? Array.Empty<string>();
        }

        // Gộp thông điệp chính và lỗi validation để hiển thị cho người dùng
        public string UserMessage => Errors.Count > 0 ? string.Join(" ", Errors) : Message;
    }
}
