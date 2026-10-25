using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QLKSan.Enums;
using QLKSan.Models.DTOs;

namespace QLKSan.Business
{
    // Nghiệp vụ kế toán: tra cứu hoá đơn, hoàn tiền, báo cáo doanh thu / công suất phòng
    public interface IAccountingService
    {
        // congTy = true: chỉ hoá đơn xuất cho công ty (có mã số thuế)
        Task<List<InvoiceListItemDto>> GetInvoicesAsync(DateTime? tuNgay, DateTime? denNgay, InvoiceStatus? trangThai, string search, bool congTy = false);

        // Số hoá đơn điện tử đã xuất cho hoá đơn công ty đã thanh toán
        Task<InvoiceDto> SetEInvoiceAsync(string maHoaDon, string soHoaDonDienTu);

        // Doanh thu thuần, ADR, RevPAR, công suất của một kỳ (dùng chung cho bảng điều khiển và tổng quan kế toán)
        Task<DashboardPeriodDto> GetPeriodAsync(DateTime tuNgay, DateTime denNgay);
        Task<AccountingOverviewDto> GetOverviewAsync(DateTime tuNgay, DateTime denNgay);
        Task<DepositReportDto> GetDepositReportAsync(DateTime tuNgay, DateTime denNgay);
        Task<InvoiceDetailDto> GetInvoiceDetailAsync(string maHoaDon);

        // Hoàn tiền cho hoá đơn đã thanh toán; ném BusinessException nếu vượt quá số tiền còn có thể hoàn
        Task<InvoiceDetailDto> RefundAsync(string maHoaDon, decimal soTien, string phuongThuc, string lyDo, string maNhanVien);

        Task<RevenueReportDto> GetRevenueReportAsync(DateTime tuNgay, DateTime denNgay);
        Task<OccupancyReportDto> GetOccupancyReportAsync(DateTime tuNgay, DateTime denNgay);

        // Sổ khách lưu trú: khách có thời gian ở giao với khoảng ngày (phục vụ thông báo lưu trú với công an)
        Task<List<GuestRegisterDto>> GetGuestRegisterAsync(DateTime tuNgay, DateTime denNgay);
        Task<byte[]> ExportGuestRegisterExcelAsync(DateTime tuNgay, DateTime denNgay);

        // File Excel (.xlsx) gồm tổng quan, doanh thu theo ngày/loại phòng/dịch vụ/phương thức và danh sách hoá đơn
        Task<byte[]> ExportRevenueExcelAsync(DateTime tuNgay, DateTime denNgay);
    }
}
