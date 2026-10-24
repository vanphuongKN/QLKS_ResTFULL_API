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
        Task CheckInAsync(string maDatPhong);
        Task CancelAsync(string maDatPhong, string lyDo);
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
        public Task CheckInAsync(string maDatPhong) => HttpPostAsync($"api/bookings/{Uri.EscapeDataString(maDatPhong)}/check-in");
        public Task CancelAsync(string maDatPhong, string lyDo) => HttpPostAsync($"api/bookings/{Uri.EscapeDataString(maDatPhong)}/cancel", new CancelBookingRequest { LyDo = lyDo });

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
    }

    // ===== /api/invoices =====
    public interface IInvoiceApiClient
    {
        Task<InvoiceDto> GenerateAsync(GenerateInvoiceRequest request);
        Task<InvoiceDto> PayAsync(string maHoaDon, PaymentRequest request);
        Task<List<InvoiceListItemDto>> GetAllAsync(DateTime? tuNgay, DateTime? denNgay, InvoiceStatus? trangThai, string search);
        Task<InvoiceDetailDto> GetByIdAsync(string maHoaDon);
        Task<InvoiceDetailDto> RefundAsync(string maHoaDon, RefundRequest request);
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
