namespace QLKSan.Infrastructure
{
    // Định dạng hiển thị dùng trong View (chỉ thuộc tầng giao diện Web, không phải hợp đồng dữ liệu dùng chung)
    public static class FormatHelper
    {
        // Định dạng số tiền giống mọi chỗ khác trong giao diện (Ví dụ: 1000000 -> 1,000,000 ₫)
        public static string ToVND(decimal amount)
        {
            return amount.ToString("N0") + " ₫";
        }
    }
}
