using QLKSan.Enums;
using QLKSan.Infrastructure;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.ApiClients
{
    // ===== /api/bookings =====
    public interface IBookingApiClient
    {
        Task<List<BookingDto>> GetAllAsync(string search = null);
        Task<List<BookingDto>> GetOnlineAsync();
        Task<List<BookingDto>> GetActiveAsync();
        // Lịch phòng: các phiếu chiếm phòng trong khoảng ngày (tối đa 62 ngày)
        Task<List<TimelineBookingDto>> GetTimelineAsync(DateTime tuNgay, DateTime denNgay);
        Task<BookingDto> GetByIdAsync(string maDatPhong);
        Task<BookingDto> CreateAsync(BookRoomRequest request);
        Task<BookingDto> WalkInAsync(CheckInRequest request);
        Task ApproveAsync(string maDatPhong);
        Task CheckInAsync(string maDatPhong, CheckInBookingRequest request = null);
        Task CancelAsync(string maDatPhong, string lyDo, bool hoanCoc = false);
        // Lễ tân đặt phòng cho ngày tới (điện thoại / email / tại quầy / đại lý)
        Task<BookingDto> CreateAtFrontDeskAsync(FrontDeskBookingRequest request);
        Task<BookingDto> AddDepositAsync(string maDatPhong, DepositRequest request);
        // Trong kỳ lưu trú: báo giá / đổi ngày, đổi phòng khi đang ở, huỷ dịch vụ, người đi cùng
        Task<StayChangeQuoteDto> QuoteStayDatesAsync(string maDatPhong, DateTime? ngayNhan, DateTime ngayTra);
        Task<BookingDto> ChangeStayDatesAsync(string maDatPhong, StayDatesRequest request);
        Task<BookingDto> MoveRoomAsync(string maDatPhong, MoveRoomRequest request);
        Task<BookingDto> VoidServiceAsync(string maDatPhong, string maSDDV, VoidServiceRequest request);
        Task<BookingDto> SetCompanionsAsync(string maDatPhong, CompanionsRequest request);
        // Bảng kê lưu trú (từng đêm, dịch vụ, ưu đãi, cọc, đã thu)
        Task<FolioDto> GetFolioAsync(string maDatPhong);
        Task ChangeRoomAsync(string maDatPhong, string maPhong);
        Task<BookingDto> OrderServicesAsync(string maDatPhong, OrderServiceRequest request);
    }

    public class BookingApiClient : ApiClientBase, IBookingApiClient
    {
        public BookingApiClient(HttpClient http) : base(http) { }

        public Task<List<BookingDto>> GetAllAsync(string search = null)
            => HttpGetAsync<List<BookingDto>>(string.IsNullOrWhiteSpace(search) ? "api/bookings" : $"api/bookings?search={Uri.EscapeDataString(search)}");

        public Task<List<BookingDto>> GetOnlineAsync() => HttpGetAsync<List<BookingDto>>("api/bookings/online");
        public Task<List<BookingDto>> GetActiveAsync() => HttpGetAsync<List<BookingDto>>("api/bookings/active");

        public Task<List<TimelineBookingDto>> GetTimelineAsync(DateTime tuNgay, DateTime denNgay)
            => HttpGetAsync<List<TimelineBookingDto>>($"api/bookings/timeline?tuNgay={tuNgay:yyyy-MM-dd}&denNgay={denNgay:yyyy-MM-dd}");
        public Task<BookingDto> GetByIdAsync(string maDatPhong) => HttpGetOrDefaultAsync<BookingDto>($"api/bookings/{Uri.EscapeDataString(maDatPhong)}");
        public Task<BookingDto> CreateAsync(BookRoomRequest request) => HttpPostAsync<BookingDto>("api/bookings", request);
        public Task<BookingDto> WalkInAsync(CheckInRequest request) => HttpPostAsync<BookingDto>("api/bookings/walk-in", request);
        public Task ApproveAsync(string maDatPhong) => HttpPostAsync($"api/bookings/{Uri.EscapeDataString(maDatPhong)}/approve");
        public Task CheckInAsync(string maDatPhong, CheckInBookingRequest request = null)
            => request == null
                ? HttpPostAsync($"api/bookings/{Uri.EscapeDataString(maDatPhong)}/check-in")
                : HttpPostAsync($"api/bookings/{Uri.EscapeDataString(maDatPhong)}/check-in", request);
        public Task CancelAsync(string maDatPhong, string lyDo, bool hoanCoc = false)
            => HttpPostAsync($"api/bookings/{Uri.EscapeDataString(maDatPhong)}/cancel", new CancelBookingRequest { LyDo = lyDo, HoanCoc = hoanCoc });
        public Task<BookingDto> CreateAtFrontDeskAsync(FrontDeskBookingRequest request) => HttpPostAsync<BookingDto>("api/bookings/front-desk", request);
        public Task<BookingDto> AddDepositAsync(string maDatPhong, DepositRequest request)
            => HttpPostAsync<BookingDto>($"api/bookings/{Uri.EscapeDataString(maDatPhong)}/deposits", request);
        public Task<StayChangeQuoteDto> QuoteStayDatesAsync(string maDatPhong, DateTime? ngayNhan, DateTime ngayTra)
            => HttpGetAsync<StayChangeQuoteDto>($"api/bookings/{Uri.EscapeDataString(maDatPhong)}/stay-dates/quote?ngayTra={ngayTra:yyyy-MM-dd}"
                + (ngayNhan != null ? $"&ngayNhan={ngayNhan:yyyy-MM-dd}" : ""));
        public Task<BookingDto> ChangeStayDatesAsync(string maDatPhong, StayDatesRequest request)
            => HttpPutAsync<BookingDto>($"api/bookings/{Uri.EscapeDataString(maDatPhong)}/stay-dates", request);
        public Task<BookingDto> MoveRoomAsync(string maDatPhong, MoveRoomRequest request)
            => HttpPostAsync<BookingDto>($"api/bookings/{Uri.EscapeDataString(maDatPhong)}/move", request);
        public Task<BookingDto> VoidServiceAsync(string maDatPhong, string maSDDV, VoidServiceRequest request)
            => HttpPostAsync<BookingDto>($"api/bookings/{Uri.EscapeDataString(maDatPhong)}/services/{Uri.EscapeDataString(maSDDV)}/void", request);
        public Task<BookingDto> SetCompanionsAsync(string maDatPhong, CompanionsRequest request)
            => HttpPutAsync<BookingDto>($"api/bookings/{Uri.EscapeDataString(maDatPhong)}/companions", request);
        public Task<FolioDto> GetFolioAsync(string maDatPhong) => HttpGetOrDefaultAsync<FolioDto>($"api/bookings/{Uri.EscapeDataString(maDatPhong)}/folio");
        public Task ChangeRoomAsync(string maDatPhong, string maPhong) => HttpPostAsync($"api/bookings/{Uri.EscapeDataString(maDatPhong)}/change-room", new ChangeRoomRequest { MaPhong = maPhong });

        public Task<BookingDto> OrderServicesAsync(string maDatPhong, OrderServiceRequest request)
            => HttpPostAsync<BookingDto>($"api/bookings/{Uri.EscapeDataString(maDatPhong)}/services", request);
    }

    // ===== /api/customers =====
    public interface ICustomerApiClient
    {
        Task<List<CustomerDto>> GetAllAsync(string search = null);
        Task<CustomerDetailDto> GetByIdAsync(string maKhachHang);
        Task<CustomerDto> GetMeAsync();
        Task UpdateMyContactAsync(UpdateContactRequest request);
        Task<List<BookingDto>> GetMyBookingsAsync();
        Task<List<ReviewDto>> GetMyReviewsAsync();
        Task UpdateProfileAsync(string maKhachHang, UpdateCustomerProfileRequest request);
        Task<List<PointHistoryDto>> GetPointHistoryAsync(string maKhachHang);
        Task AdjustPointsAsync(string maKhachHang, PointAdjustmentRequest request);
        Task<List<PointHistoryDto>> GetMyPointHistoryAsync();
        // Lễ tân tra khách cũ theo đúng số CCCD/Passport hoặc SĐT
        Task<List<CustomerDto>> LookupAsync(string q);
    }

    public class CustomerApiClient : ApiClientBase, ICustomerApiClient
    {
        public CustomerApiClient(HttpClient http) : base(http) { }

        public Task<List<CustomerDto>> GetAllAsync(string search = null)
            => HttpGetAsync<List<CustomerDto>>(string.IsNullOrWhiteSpace(search) ? "api/customers" : $"api/customers?search={Uri.EscapeDataString(search)}");

        public Task<CustomerDetailDto> GetByIdAsync(string maKhachHang)
            => HttpGetOrDefaultAsync<CustomerDetailDto>($"api/customers/{Uri.EscapeDataString(maKhachHang)}");

        public Task<CustomerDto> GetMeAsync() => HttpGetOrDefaultAsync<CustomerDto>("api/customers/me");
        public Task UpdateMyContactAsync(UpdateContactRequest request) => HttpPutAsync("api/customers/me/contact", request);
        public Task<List<BookingDto>> GetMyBookingsAsync() => HttpGetAsync<List<BookingDto>>("api/customers/me/bookings");
        public Task<List<ReviewDto>> GetMyReviewsAsync() => HttpGetAsync<List<ReviewDto>>("api/customers/me/reviews");
        public Task UpdateProfileAsync(string maKhachHang, UpdateCustomerProfileRequest request)
            => HttpPutAsync($"api/customers/{Uri.EscapeDataString(maKhachHang)}", request);
        public Task<List<PointHistoryDto>> GetPointHistoryAsync(string maKhachHang)
            => HttpGetAsync<List<PointHistoryDto>>($"api/customers/{Uri.EscapeDataString(maKhachHang)}/points");
        public Task AdjustPointsAsync(string maKhachHang, PointAdjustmentRequest request)
            => HttpPostAsync($"api/customers/{Uri.EscapeDataString(maKhachHang)}/point-adjustments", request);
        public Task<List<PointHistoryDto>> GetMyPointHistoryAsync() => HttpGetAsync<List<PointHistoryDto>>("api/customers/me/points");
        public Task<List<CustomerDto>> LookupAsync(string q) => HttpGetAsync<List<CustomerDto>>($"api/customers/lookup?q={Uri.EscapeDataString(q ?? "")}");
    }

    // ===== /api/invoices =====
    public interface IInvoiceApiClient
    {
        Task<InvoiceDto> GenerateAsync(GenerateInvoiceRequest request);
        Task<InvoiceDto> PayAsync(string maHoaDon, PaymentRequest request);
        Task<List<InvoiceListItemDto>> GetAllAsync(DateTime? tuNgay, DateTime? denNgay, InvoiceStatus? trangThai, string search);
        Task<InvoiceDetailDto> GetByIdAsync(string maHoaDon);
        Task<InvoiceDetailDto> RefundAsync(string maHoaDon, RefundRequest request);
        Task<InvoiceDto> SetCompanyAsync(string maHoaDon, InvoiceCompanyRequest request);
    }

    public class InvoiceApiClient : ApiClientBase, IInvoiceApiClient
    {
        public InvoiceApiClient(HttpClient http) : base(http) { }

        public Task<InvoiceDto> GenerateAsync(GenerateInvoiceRequest request) => HttpPostAsync<InvoiceDto>("api/invoices", request);

        public Task<InvoiceDto> PayAsync(string maHoaDon, PaymentRequest request)
            => HttpPostAsync<InvoiceDto>($"api/invoices/{Uri.EscapeDataString(maHoaDon)}/payments", request);

        public Task<List<InvoiceListItemDto>> GetAllAsync(DateTime? tuNgay, DateTime? denNgay, InvoiceStatus? trangThai, string search)
        {
            var query = new List<string>();
            if (tuNgay != null) query.Add($"tuNgay={tuNgay:yyyy-MM-dd}");
            if (denNgay != null) query.Add($"denNgay={denNgay:yyyy-MM-dd}");
            if (trangThai != null) query.Add($"trangThai={trangThai}");
            if (!string.IsNullOrWhiteSpace(search)) query.Add($"search={Uri.EscapeDataString(search)}");
            return HttpGetAsync<List<InvoiceListItemDto>>("api/invoices" + (query.Count > 0 ? "?" + string.Join("&", query) : ""));
        }

        public Task<InvoiceDetailDto> GetByIdAsync(string maHoaDon)
            => HttpGetOrDefaultAsync<InvoiceDetailDto>($"api/invoices/{Uri.EscapeDataString(maHoaDon)}");

        public Task<InvoiceDetailDto> RefundAsync(string maHoaDon, RefundRequest request)
            => HttpPostAsync<InvoiceDetailDto>($"api/invoices/{Uri.EscapeDataString(maHoaDon)}/refunds", request);

        public Task<InvoiceDto> SetCompanyAsync(string maHoaDon, InvoiceCompanyRequest request)
            => HttpPutAsync<InvoiceDto>($"api/invoices/{Uri.EscapeDataString(maHoaDon)}/company", request);
    }

    // ===== /api/reports =====
    public interface IReportApiClient
    {
        Task<RevenueReportDto> GetRevenueAsync(DateTime tuNgay, DateTime denNgay);
        Task<OccupancyReportDto> GetOccupancyAsync(DateTime tuNgay, DateTime denNgay);
        Task<(byte[] Content, string ContentType, string FileName)> ExportRevenueAsync(DateTime tuNgay, DateTime denNgay);
        Task<List<GuestRegisterDto>> GetGuestsAsync(DateTime tuNgay, DateTime denNgay);
        Task<(byte[] Content, string ContentType, string FileName)> ExportGuestsAsync(DateTime tuNgay, DateTime denNgay);
    }

    public class ReportApiClient : ApiClientBase, IReportApiClient
    {
        public ReportApiClient(HttpClient http) : base(http) { }

        private static string Range(DateTime tuNgay, DateTime denNgay) => $"tuNgay={tuNgay:yyyy-MM-dd}&denNgay={denNgay:yyyy-MM-dd}";

        public Task<RevenueReportDto> GetRevenueAsync(DateTime tuNgay, DateTime denNgay)
            => HttpGetAsync<RevenueReportDto>("api/reports/revenue?" + Range(tuNgay, denNgay));

        public Task<OccupancyReportDto> GetOccupancyAsync(DateTime tuNgay, DateTime denNgay)
            => HttpGetAsync<OccupancyReportDto>("api/reports/occupancy?" + Range(tuNgay, denNgay));

        public Task<(byte[] Content, string ContentType, string FileName)> ExportRevenueAsync(DateTime tuNgay, DateTime denNgay)
            => HttpGetFileAsync("api/reports/revenue/export?" + Range(tuNgay, denNgay));

        public Task<List<GuestRegisterDto>> GetGuestsAsync(DateTime tuNgay, DateTime denNgay)
            => HttpGetAsync<List<GuestRegisterDto>>("api/reports/guests?" + Range(tuNgay, denNgay));

        public Task<(byte[] Content, string ContentType, string FileName)> ExportGuestsAsync(DateTime tuNgay, DateTime denNgay)
            => HttpGetFileAsync("api/reports/guests/export?" + Range(tuNgay, denNgay));
    }

    // ===== /api/housekeeping =====
    public interface IHousekeepingApiClient
    {
        Task<HousekeepingDto> GetAsync();
        Task MarkCleanAsync(string maPhong);
        Task<List<HousekeepingTaskDto>> GetMyTasksAsync();
        Task AssignAsync(string maPhanCong, string maNhanVien);
        Task StartAsync(string maPhanCong);
        Task CompleteAsync(string maPhanCong);
        Task ReportIssueAsync(string maPhanCong, string ghiChu);
    }

    public class HousekeepingApiClient : ApiClientBase, IHousekeepingApiClient
    {
        public HousekeepingApiClient(HttpClient http) : base(http) { }

        public Task<HousekeepingDto> GetAsync() => HttpGetAsync<HousekeepingDto>("api/housekeeping");
        public Task MarkCleanAsync(string maPhong) => HttpPostAsync($"api/housekeeping/rooms/{Uri.EscapeDataString(maPhong)}/clean");
        public Task<List<HousekeepingTaskDto>> GetMyTasksAsync() => HttpGetAsync<List<HousekeepingTaskDto>>("api/housekeeping/tasks/mine");

        public Task AssignAsync(string maPhanCong, string maNhanVien)
            => HttpPostAsync($"api/housekeeping/tasks/{Uri.EscapeDataString(maPhanCong)}/assign", new AssignTaskRequest { MaNhanVien = maNhanVien });

        public Task StartAsync(string maPhanCong) => HttpPostAsync($"api/housekeeping/tasks/{Uri.EscapeDataString(maPhanCong)}/start");
        public Task CompleteAsync(string maPhanCong) => HttpPostAsync($"api/housekeeping/tasks/{Uri.EscapeDataString(maPhanCong)}/complete");

        public Task ReportIssueAsync(string maPhanCong, string ghiChu)
            => HttpPostAsync($"api/housekeeping/tasks/{Uri.EscapeDataString(maPhanCong)}/issue", new ReportIssueRequest { GhiChu = ghiChu });
    }
}
