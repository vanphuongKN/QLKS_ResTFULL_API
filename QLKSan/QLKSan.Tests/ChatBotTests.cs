using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QLKSan.ChatBot.Business;
using QLKSan.ChatBot.Common;
using QLKSan.ChatBot.Data;
using QLKSan.ChatBot.Providers;
using QLKSan.ChatBot.Tools;
using QLKSan.Models.DTOs;

namespace QLKSan.Tests
{
    // Trợ lý ảo: chỉ đọc endpoint công khai của API lõi (giả lập bằng FakeApi), không đặt phòng mà trả liên kết điền sẵn,
    // provider chính lỗi thì tự trả lời bằng RuleBased
    public class ChatBotTests : IDisposable
    {
        private const string Web = "https://web.test";
        private static readonly string[] EndpointCongKhai = { "/api/hotel", "/api/room-types", "/api/room-types/available", "/api/services", "/api/reviews/featured" };

        private readonly FakeHttp _api = new FakeHttp();
        private readonly IOptions<ChatBotOptions> _options = Options.Create(new ChatBotOptions { WebBaseUrl = Web + "/", ApiBaseUrl = "https://gateway.test/" });
        private readonly SqliteConnection _sqlite = new SqliteConnection("Data Source=:memory:");

        public ChatBotTests()
        {
            var loai = new[]
            {
                Loai("LP01", "Deluxe", 1_200_000, 2),
                Loai("LP02", "Superior", 900_000, 3),
                Loai("LP03", "Suite", 3_000_000, 4)
            };
            _api.Json("/api/hotel", new HotelInfoDto
            {
                Ten = "LUXE RESERVE", DiaChi = "1 Trần Phú, Nha Trang", Hotline = "1900 1234", GioNhanPhong = "14:00", GioTraPhong = "12:00",
                ChinhSachHuy = "Huỷ miễn phí trước 3 ngày.", PhuThuTraMuon = new List<LateCheckoutFeeDto>(),
                ThanhVien = new LoyaltyProgramDto { SoTienMotDiem = 100_000, GiaTriMotDiem = 1_000, DiemDoiToiThieu = 100 }
            });
            _api.Json("/api/room-types", loai);
            // Suite hết phòng; Superior rẻ hơn Deluxe
            _api.Json("/api/room-types/available", new[]
            {
                new RoomTypeAvailabilityDto { LoaiPhong = loai[0], SoPhongTrong = 2, BaoGia = new PriceQuoteDto { TongTien = 2_400_000 } },
                new RoomTypeAvailabilityDto { LoaiPhong = loai[1], SoPhongTrong = 5, BaoGia = new PriceQuoteDto { TongTien = 1_800_000 } },
                new RoomTypeAvailabilityDto { LoaiPhong = loai[2], SoPhongTrong = 0, BaoGia = new PriceQuoteDto { TongTien = 6_000_000 } }
            });
            _api.Json("/api/services", new List<ServiceDto>());
            _api.Json("/api/reviews/featured", new List<ReviewDto>());
            _sqlite.Open();
        }

        public void Dispose() => _sqlite.Dispose();

        private static RoomTypeDto Loai(string ma, string ten, decimal gia, int soNguoi)
            => new RoomTypeDto { MaLoai = ma, TenLoai = ten, GiaThueNgay = gia, SoNguoiToiDa = soNguoi };

        private HotelDataClient Data() => new HotelDataClient(new HttpClient(_api) { BaseAddress = new Uri("https://gateway.test/") },
            new MemoryCache(new MemoryCacheOptions()), new HttpContextAccessor());

        private RuleBasedProvider RuleBased() => new RuleBasedProvider(Data(), new BookingLinks(_options));
        private HotelToolbox Toolbox() => new HotelToolbox(Data(), new BookingLinks(_options));

        private static string D(int ngay) => DateTime.Today.AddDays(ngay).ToString("yyyy-MM-dd");

        [Fact]
        public async Task HoiPhongTrong_ChiHangConPhong_ReTruoc_LienKetDatPhongDienSan()
        {
            var r = await RuleBased().ReplyAsync(Array.Empty<ChatTurn>(), "Ngày mai còn phòng 2 đêm cho 3 người không?", default);

            var hoi = Assert.Single(_api.Requests, x => x.Path == "/api/room-types/available");
            Assert.Equal($"?checkIn={D(1)}&checkOut={D(3)}&soNguoi=3", hoi.Query);
            Assert.Equal(new[] { "Đặt Superior", "Đặt Deluxe" }, r.LienKet.Select(l => l.Nhan));
            Assert.Equal($"{Web}/Customer/BookRoom?maLoai=LP02&checkIn={D(1)}&checkOut={D(3)}&soNguoi=3", r.LienKet[0].Url);
            Assert.Contains("1.800.000 ₫", r.NoiDung);
            Assert.DoesNotContain("Suite", r.NoiDung);
        }

        [Fact]
        public async Task CauSauChiBoSungNgay_VanHieuLaHoiPhong()
        {
            var lichSu = new[] { new ChatTurn(ChatRoles.Khach, "Còn phòng trống không?"), new ChatTurn(ChatRoles.Bot, "Quý khách muốn nhận phòng ngày nào?") };
            var r = await RuleBased().ReplyAsync(lichSu, "ngày mai 1 đêm", default);

            Assert.Contains(_api.Requests, x => x.Path == "/api/room-types/available" && x.Query == $"?checkIn={D(1)}&checkOut={D(2)}&soNguoi=2");
            Assert.NotEmpty(r.LienKet);
        }

        [Fact]
        public async Task HoiPhongKhongCoNgay_HoiLai_KhongGoiApi()
        {
            var r = await RuleBased().ReplyAsync(Array.Empty<ChatTurn>(), "còn phòng không", default);

            Assert.Contains("ngày nào", r.NoiDung);
            Assert.Empty(r.LienKet);
            Assert.DoesNotContain(_api.Requests, x => x.Path == "/api/room-types/available");
        }

        [Fact]
        public async Task ChinhSach_LayTuThongTinKhachSan()
        {
            var r = await RuleBased().ReplyAsync(Array.Empty<ChatTurn>(), "Giờ nhận phòng mấy giờ?", default);

            Assert.Contains("14:00", r.NoiDung);
            Assert.Contains("Huỷ miễn phí trước 3 ngày.", r.NoiDung);
        }

        [Fact]
        public async Task MoiCauHoi_ChiGoiGetToiEndpointCongKhai()
        {
            var bot = RuleBased();
            foreach (var cau in new[] { "xin chào", "bảng giá các hạng phòng", "dịch vụ có gì", "đánh giá khách sạn", "địa chỉ ở đâu", "ngày mai 2 người", "đặt phòng cuối tuần này", "thời tiết thế nào" })
                await bot.ReplyAsync(Array.Empty<ChatTurn>(), cau, default);

            Assert.NotEmpty(_api.Requests);
            Assert.All(_api.Requests, x =>
            {
                Assert.Equal(HttpMethod.Get, x.Method);
                Assert.Contains(x.Path, EndpointCongKhai);
                Assert.Null(x.Authorization);
            });
        }

        [Theory]
        [InlineData(-1, 1, "đã qua")]
        [InlineData(2, 2, "sau ngày nhận")]
        [InlineData(1, 40, "tối đa 30 đêm")]
        public async Task CongCuTimPhong_TuChoiNgayKhongHopLe_KhongGoiApi(int nhan, int tra, string loi)
        {
            var (ketQua, lienKet) = await Toolbox().ExecuteAsync("tim_phong_trong", $"{{\"check_in\":\"{D(nhan)}\",\"check_out\":\"{D(tra)}\"}}", default);

            Assert.Contains(loi, ketQua);
            Assert.Empty(lienKet);
            Assert.Empty(_api.Requests);
        }

        [Fact]
        public async Task CongCuKhongTonTai_TraLoi_KhongNem()
        {
            var (ketQua, lienKet) = await Toolbox().ExecuteAsync("dat_phong", "{}", default);

            Assert.Contains("loi", ketQua);
            Assert.Empty(lienKet);
        }

        [Fact]
        public async Task OpenAI_GoiCongCu_RoiTraLoi_KemNutDatPhong()
        {
            var openAi = new FakeHttp();
            openAi.Sequence("/v1/chat/completions",
                new { choices = new[] { new { message = new { role = "assistant", content = (string)null, tool_calls = new[] {
                    new { id = "call_1", type = "function", function = new { name = "tim_phong_trong", arguments = $"{{\"check_in\":\"{D(1)}\",\"check_out\":\"{D(2)}\",\"so_nguoi\":2}}" } } } } } } },
                new { choices = new[] { new { message = new { role = "assistant", content = "Còn Superior và Deluxe, quý khách bấm nút bên dưới để đặt." } } } });
            var options = Options.Create(new ChatBotOptions { WebBaseUrl = Web, OpenAI = new OpenAiOptions { ApiKey = "sk-test", BaseUrl = "https://openai.test/v1/" } });
            var provider = new OpenAiProvider(new HttpClient(openAi), Toolbox(), options);

            var r = await provider.ReplyAsync(new[] { new ChatTurn(ChatRoles.Bot, "Xin chào") }, "mai còn phòng không", default);

            Assert.Equal("Còn Superior và Deluxe, quý khách bấm nút bên dưới để đặt.", r.NoiDung);
            Assert.Equal(new[] { "Đặt Superior", "Đặt Deluxe" }, r.LienKet.Select(l => l.Nhan));
            Assert.All(openAi.Requests, x => Assert.Equal("Bearer sk-test", x.Authorization));
            // Vòng 2 gửi lại kết quả công cụ cho mô hình
            var messages = JsonNode.Parse(openAi.Requests[1].Body)!["messages"]!.AsArray();
            var tool = messages.Single(m => m!["role"]!.GetValue<string>() == "tool")!;
            Assert.Equal("call_1", tool["tool_call_id"]!.GetValue<string>());
            Assert.Contains("LP02", tool["content"]!.GetValue<string>());
            Assert.Equal("assistant", messages[1]!["role"]!.GetValue<string>());
        }

        [Fact]
        public async Task ProviderChinhLoi_TuDungRuleBasedDuPhong_VaLuuHoiThoai()
        {
            var options = Options.Create(new ChatBotOptions { Provider = "OpenAI", WebBaseUrl = Web, OpenAI = new OpenAiOptions { ApiKey = "sk-test", BaseUrl = "https://openai.test/v1/" } });
            var openAi = new FakeHttp();
            openAi.Status("/v1/chat/completions", HttpStatusCode.TooManyRequests, "{\"error\":{\"message\":\"quota\"}}");
            using var db = new ChatDbContext(new DbContextOptionsBuilder<ChatDbContext>().UseSqlite(_sqlite).Options);
            db.Database.EnsureCreated();
            var service = new ChatService(db, new ILlmProvider[] { RuleBased(), new OpenAiProvider(new HttpClient(openAi), Toolbox(), options) },
                options, NullLogger<ChatService>.Instance);

            var hoiThoai = await service.CreateAsync(default);
            var tra = await service.SendAsync(hoiThoai.MaHoiThoai, "  Giờ trả phòng?  ", default);

            Assert.Equal("RuleBased (dự phòng)", tra.Provider);
            Assert.Contains("12:00", tra.NoiDung);
            var daLuu = await service.GetAsync(hoiThoai.MaHoiThoai, default);
            Assert.Equal(new[] { ChatRoles.Bot, ChatRoles.Khach, ChatRoles.Bot }, daLuu.TinNhan.Select(t => t.VaiTro));
            Assert.Equal("Giờ trả phòng?", daLuu.TinNhan[1].NoiDung);
            Assert.Null(await service.SendAsync(Guid.NewGuid(), "xin chào", default));
        }

        [Fact]
        public async Task OpenAI_ThieuKhoa_Nem_DeChatServiceChuyenDuPhong()
        {
            var provider = new OpenAiProvider(new HttpClient(new FakeHttp()), Toolbox(), Options.Create(new ChatBotOptions()));
            await Assert.ThrowsAsync<InvalidOperationException>(() => provider.ReplyAsync(Array.Empty<ChatTurn>(), "xin chào", default));
        }
    }

    // HttpMessageHandler giả: trả JSON theo đường dẫn (bỏ qua query) và ghi lại mọi yêu cầu
    public class FakeHttp : HttpMessageHandler
    {
        public record Request(HttpMethod Method, string Path, string Query, string Authorization, string Body);

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = null };
        private readonly Dictionary<string, Queue<(HttpStatusCode, string)>> _routes = new();
        public List<Request> Requests { get; } = new();

        public void Json(string path, object body) => _routes[path] = new Queue<(HttpStatusCode, string)>(new[] { (HttpStatusCode.OK, JsonSerializer.Serialize(body, JsonOptions)) });
        public void Sequence(string path, params object[] bodies) => _routes[path] = new Queue<(HttpStatusCode, string)>(bodies.Select(b => (HttpStatusCode.OK, JsonSerializer.Serialize(b, JsonOptions))));
        public void Status(string path, HttpStatusCode status, string body) => _routes[path] = new Queue<(HttpStatusCode, string)>(new[] { (status, body) });

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(ct);
            Requests.Add(new Request(request.Method, request.RequestUri!.AbsolutePath, request.RequestUri.Query, request.Headers.Authorization?.ToString(), body));
            if (!_routes.TryGetValue(request.RequestUri.AbsolutePath, out var q)) return new HttpResponseMessage(HttpStatusCode.NotFound);
            // Phần tử cuối được dùng lại cho các lần gọi sau
            var (status, text) = q.Count > 1 ? q.Dequeue() : q.Peek();
            return new HttpResponseMessage(status) { Content = new StringContent(text, Encoding.UTF8, "application/json") };
        }
    }
}
