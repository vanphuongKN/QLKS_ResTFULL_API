namespace QLKSan.Infrastructure
{
    // Gửi kèm IP của trình duyệt (X-Forwarded-For) khi Web gọi API qua Gateway:
    // Gateway giới hạn tần suất đăng nhập theo người dùng thật, API ghi đúng IP vào nhật ký thao tác
    public class ForwardedClientHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ForwardedClientHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;
            if (ip != null)
            {
                request.Headers.Remove("X-Forwarded-For");
                request.Headers.Add("X-Forwarded-For", (ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip).ToString());
            }
            return base.SendAsync(request, cancellationToken);
        }
    }
}
