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
}
