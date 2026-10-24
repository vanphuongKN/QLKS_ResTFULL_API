using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QLKSan.Enums;
using QLKSan.Validation;

namespace QLKSan.Models.Entities
{
    public class DichVu
    {
        [Key]
        [StringLength(10)]
        public string MaDichVu { get; set; }
        
        [StringLength(100)]
        public string TenDichVu { get; set; }
        
        [Column(TypeName = "decimal(18,0)")]
        public decimal GiaDichVu { get; set; }
        
        [StringLength(20)]
        public string DonViTinh { get; set; }

        public ServiceStatus TrangThai { get; set; } = ServiceStatus.Available;

        public bool LaTraiNghiem { get; set; }

        [StringLength(500)]
        public string? MoTa { get; set; }

        public ICollection<SuDungDichVu> SuDungDichVus { get; set; }
    }

    public class KhuyenMai
    {
        [Key]
        [StringLength(10)]
        public string MaKhuyenMai { get; set; }
        
        [StringLength(100)]
        public string TenKhuyenMai { get; set; }
        
        [StringLength(20)]
        public string LoaiGiam { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá trị giảm không được âm")]
        public decimal GiaTriGiam { get; set; }
        
        public DateTime NgayBatDau { get; set; }
        
        [DateGreaterThan("NgayBatDau", ErrorMessage = "Ngày kết thúc phải sau ngày bắt đầu")]
        public DateTime NgayKetThuc { get; set; }
        
        public PromotionStatus TrangThai { get; set; }

        public ICollection<HoaDon> HoaDons { get; set; }
    }

    public class PhieuDatPhong
    {
        [Key]
        [StringLength(10)]
        public string MaDatPhong { get; set; }
        
        [StringLength(10)]
        [ForeignKey("KhachHang")]
        public string MaKhachHang { get; set; }
        
        [StringLength(10)]
        [ForeignKey("NhanVien")]
        public string? MaNhanVien { get; set; }
        
        public DateTime NgayDat { get; set; } = DateTime.Now;
        
        [FutureDate(ErrorMessage = "Ngày check-in dự kiến không được ở trong quá khứ")]
        public DateTime NgayCheckInDuKien { get; set; }
        
        [DateGreaterThan("NgayCheckInDuKien", ErrorMessage = "Ngày check-out dự kiến phải sau ngày check-in")]
        public DateTime NgayCheckOutDuKien { get; set; }
        
        public DateTime? NgayCheckInThucTe { get; set; }
        public DateTime? NgayCheckOutThucTe { get; set; }
        
        [Range(1, 20, ErrorMessage = "Số lượng khách tối đa là 20 và phải lớn hơn 0")]
        public int SoLuongKhach { get; set; }
        
        public BookingStatus TrangThai { get; set; } = BookingStatus.Pending;
        
        [Column(TypeName = "decimal(18,0)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tiền cọc không được âm")]
        public decimal TienCoc { get; set; } = 0;

        public string? GhiChu { get; set; }

        // Mã khuyến mãi khách chọn khi đặt online (được áp dụng khi lập hoá đơn)
        [StringLength(10)]
        public string? MaKhuyenMai { get; set; }

        // Yêu cầu riêng khi đặt online và khung giờ khách dự kiến đến
        [StringLength(500)]
        public string? YeuCauDacBiet { get; set; }

        [StringLength(30)]
        public string? GioDenDuKien { get; set; }

        public KhachHang KhachHang { get; set; }
        public NhanVien NhanVien { get; set; }
        public HoaDon HoaDon { get; set; }
        public ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; }
        public ICollection<SuDungDichVu> SuDungDichVus { get; set; }
    }

    public class ChiTietDatPhong
    {
        [Key]
        [StringLength(10)]
        public string MaCTDP { get; set; }
        
        [StringLength(10)]
        [ForeignKey("PhieuDatPhong")]
        public string MaDatPhong { get; set; }
        
        [StringLength(10)]
        [ForeignKey("Phong")]
        public string MaPhong { get; set; }
        
        public RentalType HinhThucThue { get; set; }
        
        [Column(TypeName = "decimal(18,0)")]
        public decimal GiaThueThucTe { get; set; }

        public PhieuDatPhong PhieuDatPhong { get; set; }
        public Phong Phong { get; set; }
    }

    public class SuDungDichVu
    {
        [Key]
        [StringLength(10)]
        public string MaSDDV { get; set; }
        
        [StringLength(10)]
        [ForeignKey("PhieuDatPhong")]
        public string MaDatPhong { get; set; }
        
        [StringLength(10)]
        [ForeignKey("DichVu")]
        public string MaDichVu { get; set; }
        
        [Range(1, 100, ErrorMessage = "Số lượng dịch vụ không hợp lệ")]
        public int SoLuong { get; set; }
        
        public DateTime NgaySuDung { get; set; } = DateTime.Now;
        
        [Column(TypeName = "decimal(18,0)")]
        public decimal GiaThucTe { get; set; }
        
        [Column(TypeName = "decimal(18,0)")]
        public decimal ThanhTien { get; set; }

        public PhieuDatPhong PhieuDatPhong { get; set; }
        public DichVu DichVu { get; set; }
    }

    public class HoaDon
    {
        [Key]
        [StringLength(10)]
        public string MaHoaDon { get; set; }
        
        [StringLength(10)]
        [ForeignKey("PhieuDatPhong")]
        public string MaDatPhong { get; set; }
        
        [StringLength(10)]
        [ForeignKey("NhanVienLap")]
        public string MaNhanVienLap { get; set; }
        
        public DateTime NgayLapHoaDon { get; set; } = DateTime.Now;
        
        [Column(TypeName = "decimal(18,0)")]
        public decimal TongTienPhong { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,0)")]
        public decimal TongTienDichVu { get; set; } = 0;
        
        [StringLength(10)]
        [ForeignKey("KhuyenMai")]
        public string? MaKhuyenMai { get; set; }
        
        [Column(TypeName = "decimal(18,0)")]
        public decimal GiamGiaKhuyenMai { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,0)")]
        public decimal TienPhat { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,0)")]
        public decimal ThueVAT { get; set; }
        
        [Column(TypeName = "decimal(18,0)")]
        public decimal TongTienCanThanhToan { get; set; }

        // Số đêm tính tiền thực tế (giá có thể khác nhau từng đêm nên không suy ra được từ TongTienPhong)
        public int SoDem { get; set; }

        // Tiền cọc của phiếu đặt, trừ vào số tiền phải thu khi thanh toán
        [Column(TypeName = "decimal(18,0)")]
        public decimal TienCoc { get; set; } = 0;

        // Cột tính toán trong SQL: TongTienCanThanhToan - TienCoc (âm = khách cọc thừa)
        public decimal SoTienConLai { get; set; }
        
        public InvoiceStatus TrangThai { get; set; } = InvoiceStatus.Unpaid;

        public PhieuDatPhong PhieuDatPhong { get; set; }
        public NhanVien NhanVienLap { get; set; }
        public KhuyenMai KhuyenMai { get; set; }
        public ICollection<ThanhToan> ThanhToans { get; set; }
    }

    public class ThanhToan
    {
        [Key]
        [StringLength(10)]
        public string MaThanhToan { get; set; }
        
        [StringLength(10)]
        [ForeignKey("HoaDon")]
        public string? MaHoaDon { get; set; }
        
        [StringLength(50)]
        public string PhuongThuc { get; set; }
        
        public DateTime NgayThanhToan { get; set; } = DateTime.Now;
        
        [Column(TypeName = "decimal(18,0)")]
        public decimal SoTienDaTra { get; set; }
        
        [StringLength(255)]
        public string GhiChu { get; set; }

        // Thu tiền khách hay hoàn tiền cho khách (số tiền luôn dương)
        public PaymentType LoaiGiaoDich { get; set; } = PaymentType.Payment;

        // Nhân viên thực hiện giao dịch (dùng để đối soát/chốt ca)
        [StringLength(10)]
        [ForeignKey("NhanVien")]
        public string? MaNhanVien { get; set; }

        public HoaDon? HoaDon { get; set; }
        public NhanVien? NhanVien { get; set; }
        public ICollection<LichSuGiaoDich> LichSuGiaoDichs { get; set; }
    }

    // Công việc dọn phòng giao cho nhân viên buồng phòng
    public class PhanCongDonPhong
    {
        [Key]
        [StringLength(10)]
        public string MaPhanCong { get; set; }

        [StringLength(10)]
        [ForeignKey("Phong")]
        public string MaPhong { get; set; }

        // NULL = chưa giao cho ai
        [StringLength(10)]
        [ForeignKey("NhanVien")]
        public string? MaNhanVien { get; set; }

        // Phiếu đặt vừa trả phòng tạo ra công việc này (nếu có)
        [StringLength(10)]
        public string? MaDatPhong { get; set; }

        [StringLength(50)]
        public string LoaiCongViec { get; set; }

        public HousekeepingTaskStatus TrangThai { get; set; } = HousekeepingTaskStatus.Unassigned;
        public DateTime ThoiGianTao { get; set; } = DateTime.Now;
        public DateTime? ThoiGianGiao { get; set; }
        public DateTime? ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianHoanThanh { get; set; }

        [StringLength(500)]
        public string? GhiChu { get; set; }

        public Phong Phong { get; set; }
        public NhanVien? NhanVien { get; set; }
    }

    public class LichSuGiaoDich
    {
        [Key]
        [StringLength(50)]
        public string MaGiaoDich { get; set; }
        
        [StringLength(10)]
        [ForeignKey("ThanhToan")]
        public string MaThanhToan { get; set; }
        
        [StringLength(50)]
        public string CongThanhToan { get; set; }
        
        [Column(TypeName = "decimal(18,0)")]
        public decimal SoTien { get; set; }
        
        public TransactionStatus TrangThaiGiaoDich { get; set; }
        
        public DateTime ThoiGianGiaoDich { get; set; } = DateTime.Now;
        
        [StringLength(255)]
        public string NoiDungChuyenKhoan { get; set; }

        public ThanhToan ThanhToan { get; set; }
    }

    public class DanhGia
    {
        [Key]
        [StringLength(10)]
        public string MaDanhGia { get; set; }
        
        [StringLength(10)]
        [ForeignKey("KhachHang")]
        public string MaKhachHang { get; set; }
        
        [StringLength(10)]
        [ForeignKey("Phong")]
        public string MaPhong { get; set; }
        
        [Range(1, 5, ErrorMessage = "Đánh giá từ 1 đến 5 sao")]
        public int SoSao { get; set; }
        
        [StringLength(1000)]
        public string NoiDung { get; set; }
        
        public DateTime NgayDanhGia { get; set; } = DateTime.Now;
        
        public bool AnDanh { get; set; } = false;

        // Lần lưu trú được đánh giá (mỗi phiếu đặt 1 đánh giá)
        [StringLength(10)]
        [ForeignKey("PhieuDatPhong")]
        public string? MaDatPhong { get; set; }

        public PhieuDatPhong PhieuDatPhong { get; set; }

        public KhachHang KhachHang { get; set; }
        public Phong Phong { get; set; }
    }
}
