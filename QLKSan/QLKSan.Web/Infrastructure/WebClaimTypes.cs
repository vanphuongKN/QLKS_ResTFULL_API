namespace QLKSan.Infrastructure
{
    // Tên các claim lưu trong cookie đăng nhập của Web
    public static class WebClaimTypes
    {
        public const string AccessToken = "access_token";
        public const string MaDangNhap = "ma_dang_nhap";
        public const string MaNhanVien = "ma_nhan_vien";
        public const string MaKhachHang = "ma_khach_hang";
        public const string HoTen = "ho_ten";
    }

    // Tên vai trò dùng trong [Authorize(Roles = ...)] — khớp với Role enum và nhóm quyền bên API (AppRoles)
    public static class WebRoles
    {
        public const string Admin = "Admin";
        public const string Receptionist = "Receptionist";
        public const string Housekeeping = "Housekeeping";
        public const string Accountant = "Accountant";
        public const string Customer = "Customer";

        // Quầy lễ tân: đặt phòng, check-in/out, dịch vụ, giao việc buồng phòng
        public const string FrontDesk = Admin + "," + Receptionist;

        // Trả phòng (lập hoá đơn, thu tiền), xem sơ đồ phòng
        public const string Cashier = Admin + "," + Receptionist + "," + Accountant;

        // Kế toán: hoá đơn, hoàn tiền, báo cáo
        public const string Finance = Admin + "," + Accountant;

        // Nhân viên buồng phòng làm việc được giao
        public const string HousekeepingWorkers = Admin + "," + Housekeeping;

        // Bộ phận buồng phòng nói chung: xem sơ đồ, ghi minibar, báo kỹ thuật
        public const string HousekeepingTeam = Admin + "," + Receptionist + "," + Housekeeping;

        // Kiểm tra phòng sau khi dọn — khớp AppRoles.RoomInspectors bên API (thêm vai trò Giám sát buồng phòng vào đây khi có)
        public const string RoomInspectors = Admin + "," + Receptionist;
    }
}
