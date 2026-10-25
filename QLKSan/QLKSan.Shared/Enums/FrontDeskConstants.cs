namespace QLKSan.Enums
{
    // Kênh đặt phòng (PhieuDatPhong.NguonDat) — giá trị lưu trong CSDL
    public static class BookingSources
    {
        public const string Online = "Online";
        public const string DienThoai = "Điện thoại";
        public const string Email = "Email";
        public const string TaiQuay = "Tại quầy";
        public const string DaiLy = "Đại lý";
        public const string VangLai = "Vãng lai";

        // Lễ tân chọn khi tạo đặt phòng qua quầy
        public static readonly string[] QuaQuay = { DienThoai, Email, TaiQuay, DaiLy };
    }

    // Phương thức thu tiền tại quầy (cọc, thanh toán hoá đơn)
    public static class PaymentMethods
    {
        public const string TienMat = "Tiền mặt";
        public const string The = "Thẻ tín dụng";
        public const string ChuyenKhoan = "Chuyển khoản";

        public static readonly string[] TaiQuay = { TienMat, The, ChuyenKhoan };
    }
}
