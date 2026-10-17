using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QLKSan.Enums;

namespace QLKSan.Models.Entities
{
    public class LoaiPhong
    {
        [Key]
        [StringLength(10)]
        public string MaLoai { get; set; }
        
        [StringLength(50)]
        public string TenLoai { get; set; }
        
        [Range(1, 10, ErrorMessage = "Số người tối đa phải từ 1 đến 10")]
        public int SoNguoiToiDa { get; set; }
        
        [Column(TypeName = "decimal(18,0)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá thuê ngày phải lớn hơn hoặc bằng 0")]
        public decimal GiaThueNgay { get; set; }
        
        [Column(TypeName = "decimal(18,0)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá thuê giờ phải lớn hơn hoặc bằng 0")]
        public decimal GiaThueGio { get; set; }
        
        [StringLength(500)]
        public string? MoTa { get; set; }

        public ICollection<Phong> Phongs { get; set; }
    }

    public class Phong
    {
        [Key]
        [StringLength(10)]
        public string MaPhong { get; set; }
        
        [StringLength(10)]
        [ForeignKey("LoaiPhong")]
        public string MaLoai { get; set; }
        
        [StringLength(10)]
        public string SoPhong { get; set; }
        
        public RoomStatus TrangThai { get; set; } = RoomStatus.Available;

        public LoaiPhong LoaiPhong { get; set; }
        public ICollection<PhongTienIch> PhongTienIchs { get; set; }
        public ICollection<HinhAnh> HinhAnhs { get; set; }
        public ICollection<ChiTietDatPhong> ChiTietDatPhongs { get; set; }
        public ICollection<DanhGia> DanhGias { get; set; }
    }

    public class TienIch
    {
        [Key]
        [StringLength(10)]
        public string MaTienIch { get; set; }
        
        [StringLength(100)]
        public string TenTienIch { get; set; }
        
        [StringLength(50)]
        public string Icon { get; set; }

        public ICollection<PhongTienIch> PhongTienIchs { get; set; }
    }

    public class PhongTienIch
    {
        [StringLength(10)]
        public string MaPhong { get; set; }
        
        [StringLength(10)]
        public string MaTienIch { get; set; }

        [ForeignKey("MaPhong")]
        public Phong Phong { get; set; }
        
        [ForeignKey("MaTienIch")]
        public TienIch TienIch { get; set; }
    }

    public class HinhAnh
    {
        [Key]
        [StringLength(10)]
        public string MaHinhAnh { get; set; }
        
        [StringLength(10)]
        [ForeignKey("Phong")]
        public string MaPhong { get; set; }
        
        [StringLength(255)]
        public string DuongDanAnh { get; set; }

        public Phong Phong { get; set; }
    }
}
