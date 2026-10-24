using QLKSan.Enums;
using QLKSan.Infrastructure;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.ApiClients
{
    // ===== /api/room-types =====
    public interface IRoomTypeApiClient
    {
        Task<List<RoomTypeDto>> GetAllAsync();
        Task<RoomTypeDetailDto> GetDetailAsync(string maLoai);
        Task<List<RoomTypeAvailabilityDto>> GetAvailableAsync(DateTime checkIn, DateTime checkOut, int? soNguoi);
        Task CreateAsync(RoomTypeRequest request);
        Task UpdateAsync(string maLoai, RoomTypeRequest request);
        Task DeleteAsync(string maLoai);
        Task<RoomTypeImageDto> UploadImageAsync(string maLoai, Stream content, string fileName, string contentType);
        Task DeleteImageAsync(string maLoai, string maHinhAnh);
    }

    public class RoomTypeApiClient : ApiClientBase, IRoomTypeApiClient
    {
        public RoomTypeApiClient(HttpClient http) : base(http) { }

        public Task<List<RoomTypeDto>> GetAllAsync() => HttpGetAsync<List<RoomTypeDto>>("api/room-types");
        public Task<RoomTypeDetailDto> GetDetailAsync(string maLoai) => HttpGetOrDefaultAsync<RoomTypeDetailDto>($"api/room-types/{Uri.EscapeDataString(maLoai)}");

        public Task<List<RoomTypeAvailabilityDto>> GetAvailableAsync(DateTime checkIn, DateTime checkOut, int? soNguoi)
            => HttpGetAsync<List<RoomTypeAvailabilityDto>>($"api/room-types/available?checkIn={checkIn:yyyy-MM-dd}&checkOut={checkOut:yyyy-MM-dd}" + (soNguoi > 0 ? $"&soNguoi={soNguoi}" : ""));
        public Task CreateAsync(RoomTypeRequest request) => HttpPostAsync("api/room-types", request);
        public Task UpdateAsync(string maLoai, RoomTypeRequest request) => HttpPutAsync($"api/room-types/{Uri.EscapeDataString(maLoai)}", request);
        public Task DeleteAsync(string maLoai) => HttpDeleteAsync($"api/room-types/{Uri.EscapeDataString(maLoai)}");
        public Task<RoomTypeImageDto> UploadImageAsync(string maLoai, Stream content, string fileName, string contentType)
            => HttpPostFileAsync<RoomTypeImageDto>($"api/room-types/{Uri.EscapeDataString(maLoai)}/images", "file", content, fileName, contentType);
        public Task DeleteImageAsync(string maLoai, string maHinhAnh)
            => HttpDeleteAsync($"api/room-types/{Uri.EscapeDataString(maLoai)}/images/{Uri.EscapeDataString(maHinhAnh)}");
    }

    // ===== /api/services =====
    public interface IServiceApiClient
    {
        Task<List<ServiceDto>> GetAllAsync();
        Task CreateAsync(ServiceRequest request);
        Task UpdateAsync(string maDichVu, ServiceRequest request);
        Task DeleteAsync(string maDichVu);
        Task<ServiceDto> SetStatusAsync(string maDichVu, ServiceStatus trangThai);
    }

    public class ServiceApiClient : ApiClientBase, IServiceApiClient
    {
        public ServiceApiClient(HttpClient http) : base(http) { }

        public Task<List<ServiceDto>> GetAllAsync() => HttpGetAsync<List<ServiceDto>>("api/services");
        public Task CreateAsync(ServiceRequest request) => HttpPostAsync("api/services", request);
        public Task UpdateAsync(string maDichVu, ServiceRequest request) => HttpPutAsync($"api/services/{Uri.EscapeDataString(maDichVu)}", request);
        public Task DeleteAsync(string maDichVu) => HttpDeleteAsync($"api/services/{Uri.EscapeDataString(maDichVu)}");
        public Task<ServiceDto> SetStatusAsync(string maDichVu, ServiceStatus trangThai)
            => HttpPatchAsync<ServiceDto>($"api/services/{Uri.EscapeDataString(maDichVu)}", new UpdateServiceStatusRequest { TrangThai = trangThai });
    }

    // ===== /api/staff =====
    public interface IStaffApiClient
    {
        Task<List<StaffDto>> GetAllAsync();
        Task CreateAsync(CreateStaffRequest request);
        Task UpdateAsync(string maNhanVien, UpdateStaffRequest request);
        Task DeleteAsync(string maNhanVien);
        Task ResetPasswordAsync(string maNhanVien, string matKhauMoi);
    }

    public class StaffApiClient : ApiClientBase, IStaffApiClient
    {
        public StaffApiClient(HttpClient http) : base(http) { }

        public Task<List<StaffDto>> GetAllAsync() => HttpGetAsync<List<StaffDto>>("api/staff");
        public Task CreateAsync(CreateStaffRequest request) => HttpPostAsync("api/staff", request);
        public Task UpdateAsync(string maNhanVien, UpdateStaffRequest request) => HttpPutAsync($"api/staff/{Uri.EscapeDataString(maNhanVien)}", request);
        public Task DeleteAsync(string maNhanVien) => HttpDeleteAsync($"api/staff/{Uri.EscapeDataString(maNhanVien)}");

        public Task ResetPasswordAsync(string maNhanVien, string matKhauMoi)
            => HttpPostAsync($"api/staff/{Uri.EscapeDataString(maNhanVien)}/reset-password", new ResetPasswordRequest { MatKhauMoi = matKhauMoi });
    }

    // ===== /api/promotions =====
    public interface IPromotionApiClient
    {
        Task<List<PromotionDto>> GetAllAsync();
        Task CreateAsync(PromotionRequest request);

        // Ném ApiException (404: không tồn tại, 400: hết hạn) nếu mã không còn hiệu lực
        Task<PromotionDto> GetActiveAsync(string maKhuyenMai);
    }

    public class PromotionApiClient : ApiClientBase, IPromotionApiClient
    {
        public PromotionApiClient(HttpClient http) : base(http) { }

        public Task<List<PromotionDto>> GetAllAsync() => HttpGetAsync<List<PromotionDto>>("api/promotions");
        public Task CreateAsync(PromotionRequest request) => HttpPostAsync("api/promotions", request);
        public Task<PromotionDto> GetActiveAsync(string maKhuyenMai) => HttpGetAsync<PromotionDto>($"api/promotions/{Uri.EscapeDataString(maKhuyenMai)}");
    }

    // ===== /api/reviews =====
    public interface IReviewApiClient
    {
        Task<List<ReviewDto>> GetAllAsync();
        Task<List<ReviewDto>> GetFeaturedAsync(int soLuong = 3);
        Task CreateAsync(ReviewRequest request);
    }

    public class ReviewApiClient : ApiClientBase, IReviewApiClient
    {
        public ReviewApiClient(HttpClient http) : base(http) { }

        public Task<List<ReviewDto>> GetAllAsync() => HttpGetAsync<List<ReviewDto>>("api/reviews");
        public Task<List<ReviewDto>> GetFeaturedAsync(int soLuong = 3) => HttpGetAsync<List<ReviewDto>>($"api/reviews/featured?soLuong={soLuong}");
        public Task CreateAsync(ReviewRequest request) => HttpPostAsync("api/reviews", request);
    }

    // ===== /api/pricing-rules =====
    public interface IPricingRuleApiClient
    {
        Task<List<PricingRuleDto>> GetAllAsync();
        Task CreateAsync(PricingRuleRequest request);
        Task SetActiveAsync(string maQuyTac, bool kichHoat);
        Task DeleteAsync(string maQuyTac);
    }

    public class PricingRuleApiClient : ApiClientBase, IPricingRuleApiClient
    {
        public PricingRuleApiClient(HttpClient http) : base(http) { }

        public Task<List<PricingRuleDto>> GetAllAsync() => HttpGetAsync<List<PricingRuleDto>>("api/pricing-rules");
        public Task CreateAsync(PricingRuleRequest request) => HttpPostAsync("api/pricing-rules", request);
        public Task SetActiveAsync(string maQuyTac, bool kichHoat)
            => HttpPatchAsync($"api/pricing-rules/{Uri.EscapeDataString(maQuyTac)}", new UpdatePricingRuleStatusRequest { KichHoat = kichHoat });
        public Task DeleteAsync(string maQuyTac) => HttpDeleteAsync($"api/pricing-rules/{Uri.EscapeDataString(maQuyTac)}");
    }

    // ===== /api/dashboard =====
    public interface IDashboardApiClient
    {
        Task<DashboardDto> GetAsync(int? year = null);
    }

    public class DashboardApiClient : ApiClientBase, IDashboardApiClient
    {
        public DashboardApiClient(HttpClient http) : base(http) { }

        public Task<DashboardDto> GetAsync(int? year = null)
            => HttpGetAsync<DashboardDto>(year == null ? "api/dashboard" : $"api/dashboard?year={year}");
    }
}
