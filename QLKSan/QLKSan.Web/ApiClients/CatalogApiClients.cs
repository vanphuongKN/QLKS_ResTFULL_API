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
        Task UpdateAsync(string maKhuyenMai, PromotionRequest request);
        Task SetActiveAsync(string maKhuyenMai, bool kichHoat);
        Task DeleteAsync(string maKhuyenMai);

        // Ném ApiException (404: không tồn tại, 400: hết hạn) nếu mã không còn hiệu lực
        Task<PromotionDto> GetActiveAsync(string maKhuyenMai);
    }

    public class PromotionApiClient : ApiClientBase, IPromotionApiClient
    {
        public PromotionApiClient(HttpClient http) : base(http) { }

        public Task<List<PromotionDto>> GetAllAsync() => HttpGetAsync<List<PromotionDto>>("api/promotions");
        public Task CreateAsync(PromotionRequest request) => HttpPostAsync("api/promotions", request);
        public Task UpdateAsync(string maKhuyenMai, PromotionRequest request) => HttpPutAsync($"api/promotions/{Uri.EscapeDataString(maKhuyenMai)}", request);
        public Task SetActiveAsync(string maKhuyenMai, bool kichHoat)
            => HttpPatchAsync($"api/promotions/{Uri.EscapeDataString(maKhuyenMai)}", new UpdatePromotionStatusRequest { KichHoat = kichHoat });
        public Task DeleteAsync(string maKhuyenMai) => HttpDeleteAsync($"api/promotions/{Uri.EscapeDataString(maKhuyenMai)}");
        public Task<PromotionDto> GetActiveAsync(string maKhuyenMai) => HttpGetAsync<PromotionDto>($"api/promotions/{Uri.EscapeDataString(maKhuyenMai)}");
    }

    // ===== /api/reviews =====
    public interface IReviewApiClient
    {
        Task<List<ReviewDto>> GetAllAsync(int? soSao = null, bool? chuaPhanHoi = null, bool? an = null);
        Task<List<ReviewDto>> GetFeaturedAsync(int soLuong = 3);
        Task CreateAsync(ReviewRequest request);
        Task ReplyAsync(string maDanhGia, string noiDung);
        Task DeleteReplyAsync(string maDanhGia);
        Task SetHiddenAsync(string maDanhGia, bool an);
    }

    public class ReviewApiClient : ApiClientBase, IReviewApiClient
    {
        public ReviewApiClient(HttpClient http) : base(http) { }

        public Task<List<ReviewDto>> GetAllAsync(int? soSao = null, bool? chuaPhanHoi = null, bool? an = null)
        {
            var q = new List<string>();
            if (soSao != null) q.Add($"soSao={soSao}");
            if (chuaPhanHoi == true) q.Add("chuaPhanHoi=true");
            if (an != null) q.Add($"an={an.Value.ToString().ToLowerInvariant()}");
            return HttpGetAsync<List<ReviewDto>>("api/reviews" + (q.Count > 0 ? "?" + string.Join("&", q) : ""));
        }
        public Task<List<ReviewDto>> GetFeaturedAsync(int soLuong = 3) => HttpGetAsync<List<ReviewDto>>($"api/reviews/featured?soLuong={soLuong}");
        public Task CreateAsync(ReviewRequest request) => HttpPostAsync("api/reviews", request);
        public Task ReplyAsync(string maDanhGia, string noiDung)
            => HttpPutAsync($"api/reviews/{Uri.EscapeDataString(maDanhGia)}/reply", new ReviewReplyRequest { NoiDung = noiDung });
        public Task DeleteReplyAsync(string maDanhGia) => HttpDeleteAsync($"api/reviews/{Uri.EscapeDataString(maDanhGia)}/reply");
        public Task SetHiddenAsync(string maDanhGia, bool an)
            => HttpPatchAsync($"api/reviews/{Uri.EscapeDataString(maDanhGia)}", new UpdateReviewVisibilityRequest { An = an });
    }

    // ===== /api/pricing-rules =====
    public interface IPricingRuleApiClient
    {
        Task<List<PricingRuleDto>> GetAllAsync();
        Task CreateAsync(PricingRuleRequest request);
        Task UpdateAsync(string maQuyTac, PricingRuleRequest request);
        Task SetActiveAsync(string maQuyTac, bool kichHoat);
        Task DeleteAsync(string maQuyTac);
    }

    public class PricingRuleApiClient : ApiClientBase, IPricingRuleApiClient
    {
        public PricingRuleApiClient(HttpClient http) : base(http) { }

        public Task<List<PricingRuleDto>> GetAllAsync() => HttpGetAsync<List<PricingRuleDto>>("api/pricing-rules");
        public Task CreateAsync(PricingRuleRequest request) => HttpPostAsync("api/pricing-rules", request);
        public Task UpdateAsync(string maQuyTac, PricingRuleRequest request) => HttpPutAsync($"api/pricing-rules/{Uri.EscapeDataString(maQuyTac)}", request);
        public Task SetActiveAsync(string maQuyTac, bool kichHoat)
            => HttpPatchAsync($"api/pricing-rules/{Uri.EscapeDataString(maQuyTac)}", new UpdatePricingRuleStatusRequest { KichHoat = kichHoat });
        public Task DeleteAsync(string maQuyTac) => HttpDeleteAsync($"api/pricing-rules/{Uri.EscapeDataString(maQuyTac)}");
    }

    // ===== /api/dashboard =====
    public interface IDashboardApiClient
    {
        Task<DashboardDto> GetAsync();
    }

    public class DashboardApiClient : ApiClientBase, IDashboardApiClient
    {
        public DashboardApiClient(HttpClient http) : base(http) { }

        public Task<DashboardDto> GetAsync() => HttpGetAsync<DashboardDto>("api/dashboard");
    }

    // ===== /api/audit-logs =====
    public interface IAuditLogApiClient
    {
        Task<AuditLogPageDto> SearchAsync(DateTime? tuNgay, DateTime? denNgay, string doiTuong, string nguoiDung, string tuKhoa, int trang);
    }

    public class AuditLogApiClient : ApiClientBase, IAuditLogApiClient
    {
        public AuditLogApiClient(HttpClient http) : base(http) { }

        public Task<AuditLogPageDto> SearchAsync(DateTime? tuNgay, DateTime? denNgay, string doiTuong, string nguoiDung, string tuKhoa, int trang)
        {
            var q = new List<string> { $"trang={trang}" };
            if (tuNgay != null) q.Add($"tuNgay={tuNgay:yyyy-MM-dd}");
            if (denNgay != null) q.Add($"denNgay={denNgay:yyyy-MM-dd}");
            if (!string.IsNullOrWhiteSpace(doiTuong)) q.Add("doiTuong=" + Uri.EscapeDataString(doiTuong));
            if (!string.IsNullOrWhiteSpace(nguoiDung)) q.Add("nguoiDung=" + Uri.EscapeDataString(nguoiDung));
            if (!string.IsNullOrWhiteSpace(tuKhoa)) q.Add("tuKhoa=" + Uri.EscapeDataString(tuKhoa));
            return HttpGetAsync<AuditLogPageDto>("api/audit-logs?" + string.Join("&", q));
        }
    }

    // ===== /api/settings =====
    public interface ISettingsApiClient
    {
        Task<HotelSettingsDto> GetAsync();
        Task SaveHotelInfoAsync(HotelInfoSettingsDto request);
        Task SavePolicyAsync(HotelPolicySettingsDto request);
        Task SaveLoyaltyAsync(LoyaltySettingsDto request);
    }

    public class SettingsApiClient : ApiClientBase, ISettingsApiClient
    {
        public SettingsApiClient(HttpClient http) : base(http) { }

        public Task<HotelSettingsDto> GetAsync() => HttpGetAsync<HotelSettingsDto>("api/settings");
        public Task SaveHotelInfoAsync(HotelInfoSettingsDto request) => HttpPutAsync("api/settings/hotel-info", request);
        public Task SavePolicyAsync(HotelPolicySettingsDto request) => HttpPutAsync("api/settings/policy", request);
        public Task SaveLoyaltyAsync(LoyaltySettingsDto request) => HttpPutAsync("api/settings/loyalty", request);
    }

    // ===== /api/amenities =====
    public interface IAmenityApiClient
    {
        Task<List<AmenityDto>> GetAllAsync();
        Task CreateAsync(AmenityRequest request);
        Task UpdateAsync(string maTienIch, AmenityRequest request);
        Task DeleteAsync(string maTienIch);
    }

    public class AmenityApiClient : ApiClientBase, IAmenityApiClient
    {
        public AmenityApiClient(HttpClient http) : base(http) { }

        public Task<List<AmenityDto>> GetAllAsync() => HttpGetAsync<List<AmenityDto>>("api/amenities");
        public Task CreateAsync(AmenityRequest request) => HttpPostAsync("api/amenities", request);
        public Task UpdateAsync(string maTienIch, AmenityRequest request) => HttpPutAsync($"api/amenities/{Uri.EscapeDataString(maTienIch)}", request);
        public Task DeleteAsync(string maTienIch) => HttpDeleteAsync($"api/amenities/{Uri.EscapeDataString(maTienIch)}");
    }
}
