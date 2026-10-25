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

        // Tự tạo việc dọn hằng ngày / chỉnh trang buổi tối (tác vụ nền chạy mỗi ChuKyTaoViecPhut phút)
        public bool TuDongTaoViecHangNgay { get; set; } = true;
        public int ChuKyTaoViecPhut { get; set; } = 30;

        // Từ giờ này trong ngày mới tạo việc dọn hằng ngày cho phòng có khách ở tiếp
        public int GioDonHangNgay { get; set; } = 8;

        public TurndownPolicy ChinhTrangToi { get; set; } = new TurndownPolicy();

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

        // Khách được chỉnh trang buổi tối: khách VIP (nếu bật) hoặc phòng thuộc các hạng cấu hình
        public bool DuocChinhTrang(bool laVIP, string maLoai)
            => ChinhTrangToi.ApDung && ((ChinhTrangToi.ApDungKhachVIP && laVIP) || (maLoai != null && ChinhTrangToi.HangPhong.Contains(maLoai)));

        // Danh mục kiểm tra cho một hạng phòng: danh mục chung (hoặc mặc định) + mục riêng của hạng
        public List<string> DanhMucCho(string maLoai)
        {
            var chung = DanhMucKiemTra.Count > 0 ? DanhMucKiemTra : DanhMucMacDinh.ToList();
            return maLoai != null && DanhMucTheoHang.TryGetValue(maLoai, out var rieng)
                ? chung.Concat(rieng).Distinct().ToList()
                : chung.ToList();
        }
    }

    // Chỉnh trang phòng buổi tối (turndown)
    public class TurndownPolicy
    {
        public bool ApDung { get; set; } = true;

        // Từ giờ này tạo việc chỉnh trang (mặc định 17h, trước giờ khách về phòng buổi tối)
        public int GioBatDau { get; set; } = 17;

        public bool ApDungKhachVIP { get; set; } = true;

        // Mã hạng phòng cao luôn được chỉnh trang (để trống mặc định vì binder nối thêm vào list)
        public List<string> HangPhong { get; set; } = new List<string>();
    }
}
