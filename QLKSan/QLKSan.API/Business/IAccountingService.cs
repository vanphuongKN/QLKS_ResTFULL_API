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
        Task<List<InvoiceListItemDto>> GetInvoicesAsync(DateTime? tuNgay, DateTime? denNgay, InvoiceStatus? trangThai, string search);
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
