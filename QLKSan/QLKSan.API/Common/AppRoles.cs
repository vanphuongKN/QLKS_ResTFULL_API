using QLKSan.Enums;

namespace QLKSan.Common
{
    // Tên vai trò dùng trong [Authorize(Roles = ...)], khớp với giá trị claim Role trong JWT (Role.ToString())
    public static class AppRoles
    {
        public const string Admin = nameof(Role.Admin);
        public const string Receptionist = nameof(Role.Receptionist);
        public const string Housekeeping = nameof(Role.Housekeeping);
        public const string Accountant = nameof(Role.Accountant);
        public const string Customer = nameof(Role.Customer);

        // ===== Nhóm quyền theo ma trận phân quyền =====

        // Quầy lễ tân: đặt phòng, check-in/out, gọi dịch vụ, giao việc buồng phòng
        public const string FrontDesk = Admin + "," + Receptionist;

        // Lập hoá đơn và thu tiền khi trả phòng; xem phòng, phiếu đặt, khách hàng
        public const string Cashier = Admin + "," + Receptionist + "," + Accountant;

        // Tài chính: danh sách hoá đơn, hoàn tiền, báo cáo doanh thu/công suất
        public const string Finance = Admin + "," + Accountant;

        // Người trực tiếp làm việc buồng phòng (nhận việc, dọn xong, báo sự cố)
        public const string HousekeepingWorkers = Admin + "," + Housekeeping;

        // Xem tổng quan buồng phòng
        public const string HousekeepingTeam = Admin + "," + Receptionist + "," + Housekeeping;

        // Kiểm tra phòng sau khi dọn (đạt → phòng sẵn sàng đón khách).
        // Khi có vai trò Giám sát buồng phòng: thêm vào Role enum + StaffRoles và nối vào hằng số này là đủ
        public const string RoomInspectors = Admin + "," + Receptionist;

        // Mọi nhân viên khách sạn
        public const string AllStaff = Admin + "," + Receptionist + "," + Housekeeping + "," + Accountant;

        // Vai trò được phép gán cho tài khoản nhân viên
        public static readonly Role[] StaffRoles = { Role.Admin, Role.Receptionist, Role.Housekeeping, Role.Accountant };
    }

    // Tên các claim riêng của hệ thống trong JWT
    public static class AppClaimTypes
    {
        public const string MaDangNhap = "ma_dang_nhap";
        public const string MaNhanVien = "ma_nhan_vien";
        public const string MaKhachHang = "ma_khach_hang";
    }
}
