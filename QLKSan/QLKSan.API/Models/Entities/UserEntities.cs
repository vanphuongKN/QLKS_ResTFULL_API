using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QLKSan.Enums;

namespace QLKSan.Models.Entities
{
    public class TaiKhoan
    {
        [Key]
        [StringLength(10)]
        public string MaDangNhap { get; set; }
        
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(50)]
        public string TenDangNhap { get; set; }
        
        [Required(ErrorMessage = "Mật khẩu không được để trống")]
        [StringLength(255)]
        [MinLength(6, ErrorMessage = "Mật khẩu phải từ 6 ký tự trở lên")]
        public string MatKhau { get; set; }
        
        public Role VaiTro { get; set; } = Role.Customer;

        // Chống dò mật khẩu: đếm lần sai liên tiếp, đủ ngưỡng thì khoá tạm đến KhoaDen
        public int SoLanSai { get; set; } = 0;
        public DateTime? KhoaDen { get; set; }

        public NhanVien NhanVien { get; set; }
        public KhachHang KhachHang { get; set; }
    }

    public class NhanVien
    {
        [Key]
        [StringLength(10)]
        public string MaNhanVien { get; set; }
        
        [StringLength(10)]
        [ForeignKey("TaiKhoan")]
        public string? MaDangNhap { get; set; }
        
        [Required(ErrorMessage = "Tên nhân viên không được để trống")]
        [StringLength(100)]
        public string TenNhanVien { get; set; }
        
        [StringLength(50)]
        public string ChucVu { get; set; }
        
        [StringLength(15)]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})\b", ErrorMessage = "Số điện thoại không hợp lệ (gồm 10 số, bắt đầu bằng 03, 05, 07, 08, 09)")]
        public string SDT { get; set; }
        
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Email sai định dạng")]
        public string Email { get; set; }
        
        public DateTime NgayVaoLam { get; set; }
        
        public EmployeeStatus TrangThai { get; set; } = EmployeeStatus.Working;

        public TaiKhoan TaiKhoan { get; set; }
        public ICollection<PhieuDatPhong> PhieuDatPhongs { get; set; }
        public ICollection<HoaDon> HoaDonsLap { get; set; }
    }

    public class KhachHang
    {
        [Key]
        [StringLength(10)]
        public string MaKhachHang { get; set; }
        
        [StringLength(10)]
        [ForeignKey("TaiKhoan")]
        public string? MaDangNhap { get; set; }
        
        [StringLength(50)]
        [Required(ErrorMessage = "Họ không được để trống")]
        public string Ho { get; set; }
        
        [StringLength(50)]
        [Required(ErrorMessage = "Tên không được để trống")]
        public string Ten { get; set; }
        
        public DateTime NgaySinh { get; set; }
        
        [StringLength(20)]
        [Required(ErrorMessage = "CCCD/Passport không được để trống")]
        [RegularExpression(@"^[0-9]{9,12}$", ErrorMessage = "CCCD/Passport phải bao gồm 9 đến 12 chữ số")]
        public string CCCD_Passport { get; set; }
        
        public int DiemTichLuy { get; set; } = 0;

        // Tổng điểm đã đổi trừ vào hoá đơn; hạng thành viên xét theo DiemTichLuy + DiemDaDoi
        public int DiemDaDoi { get; set; } = 0;

        // Chăm sóc khách: VIP, sở thích lưu trú (khách tự cập nhật được), ghi chú nội bộ (chỉ nhân viên thấy)
        public bool LaVIP { get; set; } = false;

        [StringLength(500)]
        public string? SoThich { get; set; }

        [StringLength(1000)]
        public string? GhiChuNoiBo { get; set; }

        // Hạn chế: không cho đặt phòng trực tuyến (lễ tân vẫn xử lý trực tiếp)
        public bool HanChe { get; set; } = false;

        [StringLength(255)]
        public string? LyDoHanChe { get; set; }

        public TaiKhoan TaiKhoan { get; set; }
        public ICollection<LienHeDiaChi> LienHeDiaChis { get; set; }
        public ICollection<PhieuDatPhong> PhieuDatPhongs { get; set; }
        public ICollection<DanhGia> DanhGias { get; set; }
    }

    public class LienHeDiaChi
    {
        [Key]
        [StringLength(10)]
        public string MaLienHe { get; set; }
        
        [StringLength(10)]
        [ForeignKey("KhachHang")]
        public string MaKhachHang { get; set; }
        
        [StringLength(15)]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})\b", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string SDT { get; set; }
        
        [StringLength(100)]
        [EmailAddress(ErrorMessage = "Email sai định dạng")]
        public string Email { get; set; }
        
        [StringLength(255)]
        public string DiaChi { get; set; }
        
        [StringLength(50)]
        public string QuocGia { get; set; } = "Việt Nam";

        public KhachHang KhachHang { get; set; }
    }

    // Lịch sử điểm thưởng: mỗi lần tích / đổi / trừ do hoàn tiền / điều chỉnh thủ công
    public class LichSuDiem
    {
        [Key]
        [StringLength(12)]
        public string MaLichSu { get; set; }

        [StringLength(10)]
        [ForeignKey("KhachHang")]
        public string MaKhachHang { get; set; }

        public DateTime ThoiGian { get; set; } = DateTime.Now;

        // Dương: cộng điểm; âm: trừ điểm
        public int SoDiem { get; set; }

        [StringLength(30)]
        public string Loai { get; set; }

        [StringLength(255)]
        public string? NoiDung { get; set; }

        [StringLength(10)]
        public string? MaHoaDon { get; set; }

        [StringLength(10)]
        [ForeignKey("NhanVien")]
        public string? MaNhanVien { get; set; }

        public KhachHang KhachHang { get; set; }
        public NhanVien NhanVien { get; set; }
    }

    // Nhật ký thao tác: ai làm gì, trên đối tượng nào, lúc nào (ghi tự động khi thao tác thành công)
    public class NhatKyThaoTac
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long MaNhatKy { get; set; }

        public DateTime ThoiGian { get; set; } = DateTime.Now;

        [StringLength(10)]
        public string? MaDangNhap { get; set; }

        [StringLength(50)]
        public string? TenDangNhap { get; set; }

        [StringLength(20)]
        public string? VaiTro { get; set; }

        [StringLength(100)]
        public string HanhDong { get; set; }

        [StringLength(50)]
        public string? DoiTuong { get; set; }

        [StringLength(100)]
        public string? MaDoiTuong { get; set; }

        [StringLength(1000)]
        public string? ChiTiet { get; set; }

        [StringLength(50)]
        public string? DiaChiIP { get; set; }
    }
}
