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
    }
}
