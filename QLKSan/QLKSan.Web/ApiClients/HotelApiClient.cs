using QLKSan.Infrastructure;
using QLKSan.Models.DTOs;

namespace QLKSan.ApiClients
{
    // ===== /api/hotel: liên hệ + chính sách lưu trú (công khai) =====
    public interface IHotelApiClient
    {
        Task<HotelInfoDto> GetAsync();
    }

    public class HotelApiClient : ApiClientBase, IHotelApiClient
    {
        public HotelApiClient(HttpClient http) : base(http) { }

        public Task<HotelInfoDto> GetAsync() => HttpGetAsync<HotelInfoDto>("api/hotel");
    }
}
