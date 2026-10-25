using QLKSan.Enums;
using QLKSan.Infrastructure;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.ApiClients
{
    // ===== /api/front-desk: màn hình đầu ca, ca làm việc và bàn giao =====
    public interface IFrontDeskApiClient
    {
        Task<FrontDeskTodayDto> GetTodayAsync();
        Task<ShiftSummaryDto> GetShiftAsync();
        Task<ShiftHandoverDto> CloseShiftAsync(CloseShiftRequest request);
        Task<List<ShiftHandoverDto>> GetHandoversAsync(int soNgay = 7);
    }

    public class FrontDeskApiClient : ApiClientBase, IFrontDeskApiClient
    {
        public FrontDeskApiClient(HttpClient http) : base(http) { }

        public Task<FrontDeskTodayDto> GetTodayAsync() => HttpGetAsync<FrontDeskTodayDto>("api/front-desk/today");
        public Task<ShiftSummaryDto> GetShiftAsync() => HttpGetAsync<ShiftSummaryDto>("api/front-desk/shift");
        public Task<ShiftHandoverDto> CloseShiftAsync(CloseShiftRequest request) => HttpPostAsync<ShiftHandoverDto>("api/front-desk/shift/close", request);
        public Task<List<ShiftHandoverDto>> GetHandoversAsync(int soNgay = 7) => HttpGetAsync<List<ShiftHandoverDto>>($"api/front-desk/shifts?soNgay={soNgay}");
    }

    // ===== /api/guest-requests: sổ yêu cầu khách =====
    public interface IGuestRequestApiClient
    {
        Task<List<GuestRequestDto>> GetAllAsync(string loc = null, string maDatPhong = null);
        Task<GuestRequestDto> CreateAsync(GuestRequestCreateRequest request);
        Task<GuestRequestDto> UpdateStatusAsync(int maYeuCau, GuestRequestStatus trangThai, string ketQua);
    }

    public class GuestRequestApiClient : ApiClientBase, IGuestRequestApiClient
    {
        public GuestRequestApiClient(HttpClient http) : base(http) { }

        public Task<List<GuestRequestDto>> GetAllAsync(string loc = null, string maDatPhong = null)
        {
            var query = new List<string>();
            if (!string.IsNullOrWhiteSpace(loc)) query.Add($"loc={Uri.EscapeDataString(loc)}");
            if (!string.IsNullOrWhiteSpace(maDatPhong)) query.Add($"maDatPhong={Uri.EscapeDataString(maDatPhong)}");
            return HttpGetAsync<List<GuestRequestDto>>("api/guest-requests" + (query.Count > 0 ? "?" + string.Join("&", query) : ""));
        }

        public Task<GuestRequestDto> CreateAsync(GuestRequestCreateRequest request) => HttpPostAsync<GuestRequestDto>("api/guest-requests", request);

        public Task<GuestRequestDto> UpdateStatusAsync(int maYeuCau, GuestRequestStatus trangThai, string ketQua)
            => HttpPatchAsync<GuestRequestDto>($"api/guest-requests/{maYeuCau}", new GuestRequestUpdateRequest { TrangThai = trangThai, KetQua = ketQua });
    }
}
