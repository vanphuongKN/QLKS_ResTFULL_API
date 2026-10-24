using System;
using System.Collections.Generic;
using QLKSan.Common;

namespace QLKSan.Business
{
    public class BillingInput
    {
        // Giá gốc 1 đêm (dùng tính phụ thu trả muộn và cho các đêm không có trong GiaTungDem)
        public decimal GiaMotDem { get; set; }

        // Giá từng đêm sau khi áp dụng quy tắc giá mùa/cuối tuần (null = mọi đêm theo giá gốc)
        public IReadOnlyList<decimal> GiaTungDem { get; set; }

        // Thời điểm nhận phòng thực tế (nếu chưa check-in thì dùng ngày dự kiến)
        public DateTime NgayNhanPhong { get; set; }

        // Thời điểm trả phòng (lúc lập hoá đơn)
        public DateTime ThoiDiemTraPhong { get; set; }

        public decimal TongTienDichVu { get; set; }

        // "Phần trăm" (giảm % tiền phòng) hoặc giảm số tiền cố định; null nếu không có khuyến mãi
        public string LoaiGiam { get; set; }
        public decimal GiaTriGiam { get; set; }

        public decimal TienCoc { get; set; }
    }

    public class BillingResult
    {
        public int SoDem { get; set; }
        public decimal TienPhong { get; set; }
        public decimal PhuThuTraMuon { get; set; }
        public int PhanTramPhuThu { get; set; }
        public decimal GiamGia { get; set; }
        public decimal TienTruocThue { get; set; }
        public decimal ThueVAT { get; set; }
        public decimal TongTien { get; set; }
        public decimal TienCoc { get; set; }

        // Số tiền khách còn phải trả (âm = khách cọc thừa, cần hoàn lại)
        public decimal SoTienConLai { get; set; }
    }

    // Tính tiền phòng theo ngày thực tế — hàm thuần (không truy cập CSDL) nên kiểm thử được bằng unit test.
    // Công thức VAT/tổng tiền khớp với cột tính toán trong bảng HoaDon.
    public static class BillingCalculator
    {
        public const decimal VatRate = 0.08m;

        // Số đêm tính tiền: chênh lệch ngày giữa nhận và trả, tối thiểu 1 đêm
        public static int TinhSoDem(DateTime ngayNhan, DateTime ngayTra)
            => Math.Max(1, (ngayTra.Date - ngayNhan.Date).Days);

        // % phụ thu trả muộn (0 nếu trả đúng giờ hoặc trả trong ngày nhận phòng)
        public static int TinhPhanTramPhuThu(DateTime ngayNhan, DateTime thoiDiemTra, HotelPolicy policy)
        {
            if (thoiDiemTra.Date <= ngayNhan.Date) return 0;

            var gioTra = thoiDiemTra.TimeOfDay;
            if (gioTra <= TimeSpan.FromHours(policy.GioTraPhong)) return 0;

            foreach (var muc in policy.PhuThuTraMuon.OrderBy(m => m.DenGio))
            {
                if (gioTra <= TimeSpan.FromHours(muc.DenGio)) return muc.PhanTram;
            }
            return 100;
        }

        public static BillingResult Calculate(BillingInput input, HotelPolicy policy)
        {
            var soDem = TinhSoDem(input.NgayNhanPhong, input.ThoiDiemTraPhong);
            decimal tienPhong = 0;
            for (int i = 0; i < soDem; i++)
                tienPhong += input.GiaTungDem != null && i < input.GiaTungDem.Count ? input.GiaTungDem[i] : input.GiaMotDem;
            var phanTramPhuThu = TinhPhanTramPhuThu(input.NgayNhanPhong, input.ThoiDiemTraPhong, policy);
            var phuThu = Round(input.GiaMotDem * phanTramPhuThu / 100m);

            decimal giamGia = 0;
            if (!string.IsNullOrEmpty(input.LoaiGiam))
            {
                giamGia = input.LoaiGiam == "Phần trăm"
                    ? Round(tienPhong * input.GiaTriGiam / 100m)
                    : input.GiaTriGiam;
                giamGia = Math.Min(giamGia, tienPhong + input.TongTienDichVu + phuThu);
            }

            var tienTruocThue = tienPhong + input.TongTienDichVu + phuThu - giamGia;
            var tongTien = tienTruocThue * (1 + VatRate);

            return new BillingResult
            {
                SoDem = soDem,
                TienPhong = tienPhong,
                PhuThuTraMuon = phuThu,
                PhanTramPhuThu = phanTramPhuThu,
                GiamGia = giamGia,
                TienTruocThue = tienTruocThue,
                ThueVAT = tienTruocThue * VatRate,
                TongTien = tongTien,
                TienCoc = input.TienCoc,
                SoTienConLai = tongTien - input.TienCoc
            };
        }

        private static decimal Round(decimal value) => Math.Round(value, 0, MidpointRounding.AwayFromZero);
    }
}
