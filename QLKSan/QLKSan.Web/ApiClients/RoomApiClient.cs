using QLKSan.Enums;
using QLKSan.Infrastructure;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.ApiClients
{
    public interface IRoomApiClient
    {
        Task<List<RoomDto>> GetAllAsync(RoomStatus? trangThai = null);
        // Phòng trống trong khoảng ngày [checkIn, checkOut) — mặc định hôm nay → ngày mai
        Task<List<RoomDto>> GetAvailableAsync(DateTime? checkIn = null, DateTime? checkOut = null, int? soNguoi = null);
        // Báo giá từng đêm (áp dụng giá mùa / cuối tuần / lễ)
        Task<PriceQuoteDto> QuoteAsync(string maPhong, DateTime checkIn, DateTime checkOut);
        Task<RoomDetailDto> GetByIdAsync(string maPhong);
        Task<BookingDto> GetCurrentStayAsync(string maPhong);
        Task CreateAsync(RoomRequest request);
        Task UpdateAsync(string maPhong, RoomRequest request);
        Task DeleteAsync(string maPhong);
        Task UpdateStatusAsync(string maPhong, RoomStatus trangThai);
        Task SetAmenitiesAsync(string maPhong, RoomAmenitiesRequest request);
    }

    // /api/rooms
    public class RoomApiClient : ApiClientBase, IRoomApiClient
    {
        public RoomApiClient(HttpClient http) : base(http) { }

        public Task<List<RoomDto>> GetAllAsync(RoomStatus? trangThai = null)
            => HttpGetAsync<List<RoomDto>>(trangThai == null ? "api/rooms" : $"api/rooms?trangThai={trangThai}");

        public Task<List<RoomDto>> GetAvailableAsync(DateTime? checkIn = null, DateTime? checkOut = null, int? soNguoi = null)
        {
            var query = new List<string>();
            if (checkIn != null) query.Add($"checkIn={checkIn:yyyy-MM-dd}");
            if (checkOut != null) query.Add($"checkOut={checkOut:yyyy-MM-dd}");
            if (soNguoi != null) query.Add($"soNguoi={soNguoi}");
            return HttpGetAsync<List<RoomDto>>("api/rooms/available" + (query.Count > 0 ? "?" + string.Join("&", query) : ""));
        }

        public Task<PriceQuoteDto> QuoteAsync(string maPhong, DateTime checkIn, DateTime checkOut)
            => HttpGetAsync<PriceQuoteDto>($"api/rooms/{Uri.EscapeDataString(maPhong)}/quote?checkIn={checkIn:yyyy-MM-dd}&checkOut={checkOut:yyyy-MM-dd}");

        public Task<RoomDetailDto> GetByIdAsync(string maPhong)
            => HttpGetOrDefaultAsync<RoomDetailDto>($"api/rooms/{Uri.EscapeDataString(maPhong)}");

        public Task<BookingDto> GetCurrentStayAsync(string maPhong)
            => HttpGetOrDefaultAsync<BookingDto>($"api/rooms/{Uri.EscapeDataString(maPhong)}/current-stay");

        public Task CreateAsync(RoomRequest request)
            => HttpPostAsync("api/rooms", request);

        public Task UpdateAsync(string maPhong, RoomRequest request)
            => HttpPutAsync($"api/rooms/{Uri.EscapeDataString(maPhong)}", request);

        public Task DeleteAsync(string maPhong)
            => HttpDeleteAsync($"api/rooms/{Uri.EscapeDataString(maPhong)}");

        public Task UpdateStatusAsync(string maPhong, RoomStatus trangThai)
            => HttpPatchAsync($"api/rooms/{Uri.EscapeDataString(maPhong)}", new UpdateRoomStatusRequest { TrangThai = trangThai });
        public Task SetAmenitiesAsync(string maPhong, RoomAmenitiesRequest request)
            => HttpPutAsync($"api/rooms/{Uri.EscapeDataString(maPhong)}/amenities", request);
    }
}
