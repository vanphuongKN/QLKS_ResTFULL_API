using System.ComponentModel.DataAnnotations;

namespace QLKSan.Enums
{
    public enum Role
    {
        [Display(Name = "Khách hàng")]
        Customer,
        [Display(Name = "Nhân viên lễ tân")]
        Receptionist,
        [Display(Name = "Admin")]
        Admin
    }

    public enum EmployeeStatus
    {
        [Display(Name = "Đang làm việc")]
        Working,
        [Display(Name = "Nghỉ việc")]
        Resigned
    }

    public enum RoomStatus
    {
        [Display(Name = "Trống")]
        Available,
        [Display(Name = "Đang ở")]
        Occupied,
        [Display(Name = "Đã đặt")]
        Booked,
        [Display(Name = "Đang dọn")]
        Cleaning,
        [Display(Name = "Bảo trì")]
        Maintenance
    }

    public enum PromotionStatus
    {
        [Display(Name = "Đang áp dụng")]
        Active,
        [Display(Name = "Hết hạn")]
        Expired
    }

    public enum BookingStatus
    {
        [Display(Name = "Chờ duyệt")]
        Pending,
        [Display(Name = "Đã duyệt")]
        Approved,
        [Display(Name = "Đã nhận phòng")]
        CheckedIn,
        [Display(Name = "Đã trả phòng")]
        CheckedOut,
        [Display(Name = "Đã hủy")]
        Cancelled,
        [Display(Name = "Hoàn thành")]
        Completed
    }

    public enum RentalType
    {
        [Display(Name = "Theo ngày")]
        Daily,
        [Display(Name = "Theo giờ")]
        Hourly
    }

    public enum InvoiceStatus
    {
        [Display(Name = "Chưa thanh toán")]
        Unpaid,
        [Display(Name = "Đã thanh toán")]
        Paid,
        [Display(Name = "Đã hủy")]
        Cancelled
    }

    public enum TransactionStatus
    {
        [Display(Name = "Thành công")]
        Success,
        [Display(Name = "Thất bại")]
        Failed
    }

    public enum ServiceStatus
    {
        [Display(Name = "Sẵn sàng")]
        Available,
        [Display(Name = "Đã hết")]
        Unavailable
    }
}
