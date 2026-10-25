using System;
using System.Collections.Generic;
using QLKSan.Enums;

namespace QLKSan.Models.DTOs
{
    // GET /api/front-desk/today: màn hình đầu ca của lễ tân — khách đến / đi / đang ở hôm nay, phiếu sắp đến, tình trạng phòng
    public class FrontDeskTodayDto
    {
        public DateTime Ngay { get; set; }
        public DateTime NgayCapNhat { get; set; }

        // Tình trạng phòng (trạng thái vật lý hiện tại); PhongTrong = trống tối nay (trừ phòng đã giữ cho khách đến hôm nay)
        public int TongSoPhong { get; set; }
        public int PhongTrong { get; set; }
        public int PhongCoKhach { get; set; }
        public int PhongDangDon { get; set; }
        public int PhongBaoTri { get; set; }

        public int SoKhachDen { get; set; }
        public int SoKhachDenDaNhan { get; set; }
        public int SoKhachDi { get; set; }
        public int SoKhachDiDaTra { get; set; }
        public int PhieuChoDuyet { get; set; }
        public int ViecBuongPhongChuaXong { get; set; }
        public int SuCoBuongPhong { get; set; }

        // Khách đến: phiếu nhận phòng hôm nay (chưa nhận trước, đã nhận sau)
        public List<FrontDeskStayDto> KhachDen { get; set; } = new List<FrontDeskStayDto>();

        // Khách đi: đang ở có ngày trả dự kiến hôm nay hoặc đã quá hạn, và phiếu đã trả phòng trong hôm nay
        public List<FrontDeskStayDto> KhachDi { get; set; } = new List<FrontDeskStayDto>();

        // Mọi phiếu đang lưu trú, theo số phòng
        public List<FrontDeskStayDto> DangO { get; set; } = new List<FrontDeskStayDto>();

        // Phiếu chờ duyệt / đã duyệt nhận phòng từ ngày mai trở đi, theo ngày đến
        public List<FrontDeskStayDto> SapDen { get; set; } = new List<FrontDeskStayDto>();

        // Yêu cầu khách chưa xử lý xong (giờ hẹn gần nhất trước) và ghi chú bàn giao của ca gần nhất
        public int YeuCauDangMo { get; set; }
        public List<GuestRequestDto> YeuCauSapToi { get; set; } = new List<GuestRequestDto>();
        public ShiftHandoverDto CaTruoc { get; set; }
    }

    public class GuestRequestDto
    {
        public int MaYeuCau { get; set; }
        public string MaDatPhong { get; set; }
        public string MaPhong { get; set; }
        public string SoPhong { get; set; }
        public string HoTenKhachHang { get; set; }
        public string Loai { get; set; }
        public string NoiDung { get; set; }
        public DateTime? ThoiGianHen { get; set; }
        public GuestRequestStatus TrangThai { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public string TenNhanVienTao { get; set; }
        public DateTime? ThoiGianXuLy { get; set; }
        public string TenNhanVienXuLy { get; set; }
        public string KetQua { get; set; }

        // Chưa xong mà đã quá giờ hẹn
        public bool QuaHen { get; set; }
    }

    // GET /api/front-desk/shift: tiền nhân viên đang đăng nhập đã thu / chi trong ca (thanh toán hoá đơn, hoàn tiền, thu / hoàn cọc)
    public class ShiftSummaryDto
    {
        public string MaNhanVien { get; set; }
        public string TenNhanVien { get; set; }
        public DateTime TuLuc { get; set; }
        public DateTime DenLuc { get; set; }
        public decimal TongThu { get; set; }
        public decimal TongChi { get; set; }
        public decimal TongThuRong { get; set; }
        public decimal TienMatHeThong { get; set; }
        public List<ShiftMethodDto> TheoPhuongThuc { get; set; } = new List<ShiftMethodDto>();
        public List<ShiftTransactionDto> GiaoDich { get; set; } = new List<ShiftTransactionDto>();
    }

    public class ShiftMethodDto
    {
        public string PhuongThuc { get; set; }
        public int SoGiaoDich { get; set; }
        public decimal Thu { get; set; }
        public decimal Chi { get; set; }
        public decimal Rong { get; set; }
    }

    public class ShiftTransactionDto
    {
        public DateTime ThoiGian { get; set; }
        // Thanh toán hoá đơn | Hoàn tiền hoá đơn | Thu cọc | Hoàn cọc
        public string Loai { get; set; }
        public string MaThamChieu { get; set; }
        public string PhuongThuc { get; set; }
        // Dương = thu vào, âm = trả ra
        public decimal SoTien { get; set; }
        public string GhiChu { get; set; }
    }

    // Một lần chốt ca / bàn giao
    public class ShiftHandoverDto
    {
        public int MaGiaoCa { get; set; }
        public string MaNhanVien { get; set; }
        public string TenNhanVien { get; set; }
        public DateTime BatDau { get; set; }
        public DateTime KetThuc { get; set; }
        public decimal TongThuRong { get; set; }
        public decimal TienMatHeThong { get; set; }
        public decimal TienMatThucTe { get; set; }
        public decimal ChenhLech { get; set; }
        public string GhiChuBanGiao { get; set; }
        public List<ShiftMethodDto> TheoPhuongThuc { get; set; } = new List<ShiftMethodDto>();
    }

    // Một phiếu đặt trên màn hình lễ tân, kèm thông tin chăm sóc khách
    public class FrontDeskStayDto
    {
        public string MaDatPhong { get; set; }
        public BookingStatus TrangThai { get; set; }
        public string MaKhachHang { get; set; }
        public string HoTenKhachHang { get; set; }
        public string SDT { get; set; }
        public bool LaVIP { get; set; }
        public string SoThich { get; set; }

        // Khách bị hạn chế (A3): lễ tân cần xem lý do trước khi phục vụ
        public bool HanChe { get; set; }
        public string LyDoHanChe { get; set; }

        public string MaPhong { get; set; }
        public string SoPhong { get; set; }
        public string TenLoaiPhong { get; set; }
        public RoomStatus? TrangThaiPhong { get; set; }

        public DateTime NgayNhan { get; set; }
        public DateTime NgayTra { get; set; }
        public DateTime? NgayCheckInThucTe { get; set; }
        public DateTime? NgayCheckOutThucTe { get; set; }
        public int SoDem { get; set; }
        public int SoKhach { get; set; }
        public string GioDenDuKien { get; set; }
        public string YeuCauDacBiet { get; set; }
        public decimal TienCoc { get; set; }
        public string MaKhuyenMai { get; set; }
        public string NguonDat { get; set; }

        // Đã nhận phòng (danh sách đến) / đã trả phòng (danh sách đi)
        public bool DaXuLy { get; set; }

        // Đang ở nhưng đã qua ngày trả dự kiến
        public bool QuaHanTra { get; set; }

        public List<CompanionDto> KhachDiCung { get; set; } = new List<CompanionDto>();
    }

    // GET /api/bookings/{id}/stay-dates/quote: báo giá khi gia hạn / rút ngắn / đổi ngày (giá từng đêm theo quy tắc giá)
    public class StayChangeQuoteDto
    {
        public string MaDatPhong { get; set; }
        public DateTime NgayNhanCu { get; set; }
        public DateTime NgayTraCu { get; set; }
        public DateTime NgayNhanMoi { get; set; }
        public DateTime NgayTraMoi { get; set; }
        public int SoDemCu { get; set; }
        public int SoDemMoi { get; set; }
        public decimal TienPhongCu { get; set; }
        public decimal TienPhongMoi { get; set; }
        public decimal ChenhLech { get; set; }
        public List<NightPriceDto> CacDem { get; set; } = new List<NightPriceDto>();

        // Phòng đang xếp còn trống cho khoảng ngày mới (false: LyDo cho biết vướng phiếu nào)
        public bool ConPhong { get; set; }
        public string LyDo { get; set; }
    }
}
