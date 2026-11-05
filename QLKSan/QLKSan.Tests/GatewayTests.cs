using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;

namespace QLKSan.Tests
{
    // Gateway (YARP): định tuyến tới đúng service, giới hạn tần suất theo người dùng / IP, 429 dạng ProblemDetails tiếng Việt.
    // API lõi và ChatBot được thay bằng hai server giả trả về tên service + đường dẫn nhận được.
    public class GatewayTests : IAsyncLifetime
    {
        private const string Khoa = "khoa-kiem-thu-gateway-dai-hon-32-ky-tu-0123";
        private WebApplication _core, _chatbot;

        public async Task InitializeAsync()
        {
            _core = await StubAsync("core");
            _chatbot = await StubAsync("chatbot");
        }

        public async Task DisposeAsync()
        {
            await _core.DisposeAsync();
            await _chatbot.DisposeAsync();
        }

        private static async Task<WebApplication> StubAsync(string ten)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            var app = builder.Build();
            app.Run(ctx => ctx.Response.WriteAsJsonAsync(new { service = ten, path = ctx.Request.Path.Value + ctx.Request.QueryString }));
            await app.StartAsync();
            return app;
        }

        private WebApplicationFactory<QLKSan.Gateway.Program> Gateway(int moiPhut = 1000, int dangNhap = 1000, int chat = 1000)
            => new WebApplicationFactory<QLKSan.Gateway.Program>().WithWebHostBuilder(b => b
                .UseSetting("Jwt:Key", Khoa)
                .UseSetting("RateLimit:MoiPhut", moiPhut.ToString())
                .UseSetting("RateLimit:DangNhapMoiPhut", dangNhap.ToString())
                .UseSetting("RateLimit:ChatMoiPhut", chat.ToString())
                .UseSetting("ReverseProxy:Clusters:core:Destinations:api:Address", _core.Urls.First() + "/")
                .UseSetting("ReverseProxy:Clusters:chatbot:Destinations:chatbot:Address", _chatbot.Urls.First() + "/"));

        private static string Token(string maNguoiDung) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: "QLKSan.API", audience: "QLKSan.Web",
            claims: new[] { new Claim(ClaimTypes.NameIdentifier, maNguoiDung) },
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Khoa)), SecurityAlgorithms.HmacSha256)));

        private static async Task<(HttpStatusCode, JsonNode)> GetAsync(HttpClient http, string url, string token = null)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            if (token != null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var res = await http.SendAsync(req);
            var text = await res.Content.ReadAsStringAsync();
            return (res.StatusCode, string.IsNullOrEmpty(text) ? null : JsonNode.Parse(text));
        }

        [Theory]
        [InlineData("/api/room-types/available?soNguoi=2", "core")]
        [InlineData("/api/chat/conversations", "chatbot")]
        [InlineData("/uploads/rooms/a.jpg", "core")]
        [InlineData("/api/auth/login", "core")]
        public async Task DinhTuyen_TheoDuongDan(string url, string service)
        {
            using var gw = Gateway();
            var (status, body) = await GetAsync(gw.CreateClient(), url);

            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Equal(service, body!["service"]!.GetValue<string>());
            Assert.Equal(url, body["path"]!.GetValue<string>());
        }

        [Fact]
        public async Task Health_CuaGateway_KhongChuyenTiep()
        {
            using var gw = Gateway();
            var (status, body) = await GetAsync(gw.CreateClient(), "/health");

            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Equal("QLKSan.Gateway", body!["service"]!.GetValue<string>());
        }

        [Fact]
        public async Task DangNhap_QuaGioiHan_Tra429ProblemDetailsTiengViet_CacApiKhacVanChay()
        {
            using var gw = Gateway(dangNhap: 2);
            var http = gw.CreateClient();
            var ketQua = new List<HttpStatusCode>();
            for (var i = 0; i < 3; i++)
                ketQua.Add((await http.PostAsJsonAsync("/api/auth/login", new { TenDangNhap = "a", MatKhau = "b" })).StatusCode);

            Assert.Equal(new[] { HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests }, ketQua);
            var bi = await http.PostAsJsonAsync("/api/auth/register", new { });
            Assert.Equal(HttpStatusCode.TooManyRequests, bi.StatusCode);
            Assert.Equal("application/problem+json", bi.Content.Headers.ContentType!.MediaType);
            var problem = JsonNode.Parse(await bi.Content.ReadAsStringAsync())!;
            Assert.Equal(429, problem["status"]!.GetValue<int>());
            Assert.Equal("Bạn thao tác quá nhanh, vui lòng thử lại sau ít phút.", problem["detail"]!.GetValue<string>());
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(http, "/api/hotel")).Item1);
        }

        [Fact]
        public async Task GioiHan_TheoTungNguoiDung_KhongDonChung()
        {
            using var gw = Gateway(moiPhut: 2);
            var http = gw.CreateClient();
            string a = Token("TK0001"), b = Token("TK0002");

            Assert.Equal(HttpStatusCode.OK, (await GetAsync(http, "/api/customers/me", a)).Item1);
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(http, "/api/customers/me", a)).Item1);
            Assert.Equal(HttpStatusCode.TooManyRequests, (await GetAsync(http, "/api/customers/me", a)).Item1);
            // Người khác và khách chưa đăng nhập có hạn mức riêng
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(http, "/api/customers/me", b)).Item1);
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(http, "/api/hotel")).Item1);
            // Token giả (sai khoá) không được tính là người dùng khác: rơi về hạn mức theo IP
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(http, "/api/hotel", "token-gia")).Item1);
            Assert.Equal(HttpStatusCode.TooManyRequests, (await GetAsync(http, "/api/hotel", "token-gia-khac")).Item1);
        }

        [Fact]
        public async Task TroLyAo_CoHanMucRieng()
        {
            using var gw = Gateway(chat: 1);
            var http = gw.CreateClient();

            Assert.Equal(HttpStatusCode.OK, (await GetAsync(http, "/api/chat/conversations")).Item1);
            Assert.Equal(HttpStatusCode.TooManyRequests, (await GetAsync(http, "/api/chat/conversations")).Item1);
            Assert.Equal(HttpStatusCode.OK, (await GetAsync(http, "/api/room-types")).Item1);
        }

        [Fact]
        public void ThieuKhoaJwt_KhongKhoiDong()
        {
            using var gw = new WebApplicationFactory<QLKSan.Gateway.Program>().WithWebHostBuilder(b => b.UseSetting("Jwt:Key", "ngan"));
            var ex = Assert.ThrowsAny<Exception>(() => gw.CreateClient());
            Assert.Contains("Jwt:Key", ex.ToString());
        }
    }
}
