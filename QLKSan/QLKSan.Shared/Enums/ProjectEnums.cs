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
        Admin,
        [Display(Name = "Nhân viên buồng phòng")]
        Housekeeping,
        [Display(Name = "Kế toán")]
        Accountant
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

    // Công việc buồng phòng (bảng PhanCongDonPhong)
    public enum HousekeepingTaskStatus
    {
        [Display(Name = "Chờ giao")]
        Unassigned,
        [Display(Name = "Đã giao")]
        Assigned,
        [Display(Name = "Đang dọn")]
        InProgress,
        [Display(Name = "Hoàn thành")]
        Completed,
        [Display(Name = "Sự cố")]
        Issue
    }

    // Yêu cầu của khách tại quầy (bảng YeuCauKhach)
    public enum GuestRequestStatus
    {
        [Display(Name = "Mới")]
        New,
        [Display(Name = "Đang xử lý")]
        InProgress,
        [Display(Name = "Hoàn thành")]
        Done,
        [Display(Name = "Đã huỷ")]
        Cancelled
    }

    // Loại giao dịch trong bảng ThanhToan
    public enum PaymentType
    {
        [Display(Name = "Thu")]
        Payment,
        [Display(Name = "Hoàn")]
        Refund
    }
}
