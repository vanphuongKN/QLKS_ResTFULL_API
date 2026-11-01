using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using QLKSan.Models.DTOs;

namespace QLKSan.ChatBot.Tools
{
    // Đọc dữ liệu công khai của API lõi qua Gateway (không đăng nhập, không truy cập CSDL).
    // Dữ liệu ít thay đổi (thông tin khách sạn, hạng phòng, dịch vụ, đánh giá) lưu đệm 2 phút; phòng trống luôn hỏi mới.
    public class HotelDataClient
    {
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private static readonly TimeSpan ThoiGianDem = TimeSpan.FromMinutes(2);

        private readonly HttpClient _http;
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HotelDataClient(HttpClient http, IMemoryCache cache, IHttpContextAccessor httpContextAccessor)
        {
            _http = http;
            _cache = cache;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task<HotelInfoDto> GetHotelAsync(CancellationToken ct)
            => CachedAsync("hotel", () => GetAsync<HotelInfoDto>("api/hotel", ct));

        public Task<List<RoomTypeDto>> GetRoomTypesAsync(CancellationToken ct)
            => CachedAsync("room-types", () => GetAsync<List<RoomTypeDto>>("api/room-types", ct));

        public async Task<List<ServiceDto>> GetServicesAsync(CancellationToken ct)
            => (await CachedAsync("services", () => GetAsync<List<ServiceDto>>("api/services", ct)))
                .Where(s => s.TrangThai == Enums.ServiceStatus.Available && !s.LaMinibar).ToList();

        public Task<List<ReviewDto>> GetFeaturedReviewsAsync(CancellationToken ct)
            => CachedAsync("reviews", () => GetAsync<List<ReviewDto>>("api/reviews/featured?soLuong=5", ct));

        public Task<List<RoomTypeAvailabilityDto>> FindAvailableAsync(DateTime checkIn, DateTime checkOut, int soNguoi, CancellationToken ct)
            => GetAsync<List<RoomTypeAvailabilityDto>>($"api/room-types/available?checkIn={checkIn:yyyy-MM-dd}&checkOut={checkOut:yyyy-MM-dd}&soNguoi={soNguoi}", ct);

        private async Task<T> CachedAsync<T>(string key, Func<Task<T>> load)
        {
            if (_cache.TryGetValue(key, out T value)) return value;
            value = await load();
            _cache.Set(key, value, ThoiGianDem);
            return value;
        }

        private async Task<T> GetAsync<T>(string url, CancellationToken ct)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            // Gửi kèm IP của khách để Gateway giới hạn tần suất theo từng khách, không dồn chung cho ChatBot
            var ip = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;
            if (ip != null) request.Headers.Add("X-Forwarded-For", (ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip).ToString());
            using var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        }
    }
}
