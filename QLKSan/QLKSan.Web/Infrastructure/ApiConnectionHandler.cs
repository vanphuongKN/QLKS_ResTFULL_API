using System.Net;

namespace QLKSan.Infrastructure
{
    // Không kết nối được API (Gateway chưa chạy, sai ApiSettings:BaseUrl, quá thời gian chờ): đổi thành ApiException 503
    // để các trang vốn đã bắt ApiException hiện thông báo cho người dùng thay vì lỗi 500, và ghi log chỉ rõ địa chỉ cần kiểm tra
    public class ApiConnectionHandler : DelegatingHandler
    {
        public const string ThongBao = "Hệ thống tạm thời không kết nối được máy chủ. Vui lòng thử lại sau ít phút.";

        private readonly ILogger<ApiConnectionHandler> _logger;

        public ApiConnectionHandler(ILogger<ApiConnectionHandler> logger)
        {
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
            {
                _logger.LogError(ex, "Không gọi được API tại {Url}. Kiểm tra QLKSan.Gateway và QLKSan.API đang chạy (run.bat, hoặc Visual Studio: Multiple startup projects gồm API + Gateway + ChatBot + Web) và ApiSettings:BaseUrl.",
                    request.RequestUri?.GetLeftPart(UriPartial.Authority));
                throw new ApiException(HttpStatusCode.ServiceUnavailable, ThongBao);
            }
        }
    }
}
