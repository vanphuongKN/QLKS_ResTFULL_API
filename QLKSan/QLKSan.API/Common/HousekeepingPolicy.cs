using System.Collections.Generic;
using System.Linq;

namespace QLKSan.Common
{
    // Quy chuẩn buồng phòng, ánh xạ từ mục "Housekeeping" trong appsettings.json
    public class HousekeepingPolicy
    {
        public const string SectionName = "Housekeeping";

        // Danh mục kiểm tra phòng sau khi dọn (áp dụng mọi hạng phòng)
        // Để trống mặc định: configuration binder NỐI THÊM vào list có sẵn chứ không thay thế
        public List<string> DanhMucKiemTra { get; set; } = new List<string>();

        // Mục kiểm tra thêm theo mã hạng phòng (vd: Suite có phòng khách, bồn tắm)
        public Dictionary<string, List<string>> DanhMucTheoHang { get; set; } = new Dictionary<string, List<string>>();

        public static readonly string[] DanhMucMacDinh =
        {
            "Giường, ga gối phẳng và sạch",
            "Phòng tắm sạch, khô, đủ khăn",
            "Đồ dùng phòng tắm đầy đủ",
            "Minibar đủ và đúng vị trí",
            "Sàn, thảm, bề mặt không bụi",
            "Đèn, điều hoà, TV hoạt động",
            "Mùi phòng dễ chịu, rèm và cửa sổ gọn"
        };

        // Danh mục kiểm tra cho một hạng phòng: danh mục chung (hoặc mặc định) + mục riêng của hạng
        public List<string> DanhMucCho(string maLoai)
        {
            var chung = DanhMucKiemTra.Count > 0 ? DanhMucKiemTra : DanhMucMacDinh.ToList();
            return maLoai != null && DanhMucTheoHang.TryGetValue(maLoai, out var rieng)
                ? chung.Concat(rieng).Distinct().ToList()
                : chung.ToList();
        }
    }
}
