using Microsoft.Extensions.Caching.Memory;
using QLKSan.ApiClients;
using QLKSan.Models.DTOs;

namespace QLKSan.Infrastructure
{
    // Thông tin khách sạn (GET /api/hotel) dùng chung cho footer và trang chủ, lưu đệm 5 phút
    public class HotelInfoProvider
    {
        private const string CacheKey = "hotel-info";
        private readonly IHotelApiClient _hotelApi;
        private readonly IMemoryCache _cache;

        public HotelInfoProvider(IHotelApiClient hotelApi, IMemoryCache cache)
        {
            _hotelApi = hotelApi;
            _cache = cache;
        }

        // Gọi sau khi quản lý sửa cấu hình để footer / trang khách hiện ngay thông tin mới
        public void Invalidate() => _cache.Remove(CacheKey);

        // API chưa chạy: trả về thông tin tối thiểu để trang vẫn hiển thị được (không lưu đệm để lần sau thử lại)
        public async Task<HotelInfoDto> GetAsync()
        {
            if (_cache.TryGetValue(CacheKey, out HotelInfoDto info)) return info;
            try
            {
                info = await _hotelApi.GetAsync();
                _cache.Set(CacheKey, info, TimeSpan.FromMinutes(5));
                return info;
            }
            catch (Exception ex) when (ex is ApiException or HttpRequestException)
            {
                return new HotelInfoDto { Ten = "LUXE RESERVE" };
            }
        }
    }
}
