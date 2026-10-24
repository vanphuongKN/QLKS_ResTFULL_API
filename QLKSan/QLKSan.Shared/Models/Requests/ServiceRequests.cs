using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QLKSan.Models.Requests
{
    // Dùng cho cả thêm (POST) và sửa (PUT /{id}) dịch vụ
    public class ServiceRequest
    {
        [Required(ErrorMessage = "Vui lòng nhập tên dịch vụ.")]
        [StringLength(100)]
        public string TenDichVu { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá dịch vụ không được âm")]
        public decimal GiaDichVu { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập đơn vị tính.")]
        [StringLength(20)]
        public string DonViTinh { get; set; }
    }

    // PATCH /api/services/{id}: đặt trạng thái cụ thể (không đảo trạng thái kiểu toggle)
    public class UpdateServiceStatusRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn trạng thái dịch vụ.")]
        public Enums.ServiceStatus? TrangThai { get; set; }
    }

    // Gọi nhiều dịch vụ cho một phiếu đặt phòng đang ở
    public class OrderServiceRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "Vui lòng chọn ít nhất một dịch vụ.")]
        public List<ServiceOrderItem> Services { get; set; } = new List<ServiceOrderItem>();
    }

    public class ServiceOrderItem
    {
        [Required]
        public string MaDichVu { get; set; }

        [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100.")]
        public int SoLuong { get; set; }
    }
}
