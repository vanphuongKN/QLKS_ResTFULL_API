using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Mappings;
using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    // Màn hình đầu ca của lễ tân: ai đến, ai đi, ai đang ở hôm nay, phiếu sắp đến và tình trạng phòng
    public interface IFrontDeskService
    {
        Task<FrontDeskTodayDto> GetTodayAsync();

        // Tiền nhân viên đã thu / chi từ lần chốt ca gần nhất (trong 24 giờ) hoặc từ 0h hôm nay tới hiện tại
        Task<ShiftSummaryDto> GetShiftSummaryAsync(string maNhanVien);
        Task<ShiftHandoverDto> CloseShiftAsync(string maNhanVien, decimal tienMatThucTe, string ghiChuBanGiao);
        Task<List<ShiftHandoverDto>> GetHandoversAsync(int soNgay);
    }

    public class FrontDeskService : IFrontDeskService
    {
        private readonly ApplicationDbContext _context;

        public FrontDeskService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<FrontDeskTodayDto> GetTodayAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var dto = new FrontDeskTodayDto { Ngay = today, NgayCapNhat = DateTime.Now };

            var phong = await _context.Phongs.Select(p => p.TrangThai).ToListAsync();
            dto.TongSoPhong = phong.Count;
            dto.PhongCoKhach = phong.Count(t => t == RoomStatus.Occupied);
            dto.PhongDangDon = phong.Count(t => t == RoomStatus.Cleaning);
            dto.PhongBaoTri = phong.Count(t => t == RoomStatus.Maintenance);

            // Một lần tải: phiếu chưa nhận phòng, đang ở, và đã trả trong hôm nay
            var phieu = await _context.PhieuDatPhongs
                .Include(p => p.KhachHang).ThenInclude(k => k.LienHeDiaChis)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong).ThenInclude(r => r.LoaiPhong)
                .Include(p => p.KhachDiCungs)
                .Where(p => p.TrangThai == BookingStatus.Pending || p.TrangThai == BookingStatus.Approved || p.TrangThai == BookingStatus.CheckedIn
                         || (p.TrangThai == BookingStatus.CheckedOut && p.NgayCheckOutThucTe >= today && p.NgayCheckOutThucTe < tomorrow))
                .AsSplitQuery()
                .ToListAsync();

            bool ChuaNhan(PhieuDatPhong p) => p.TrangThai is BookingStatus.Pending or BookingStatus.Approved;
            bool DangO(PhieuDatPhong p) => p.TrangThai == BookingStatus.CheckedIn;

            dto.KhachDen = phieu
                .Where(p => (ChuaNhan(p) && p.NgayCheckInDuKien.Date == today)
                         || (DangO(p) && p.NgayCheckInThucTe >= today && p.NgayCheckInThucTe < tomorrow))
                .OrderBy(p => DangO(p)).ThenBy(p => p.GioDenDuKien ?? "~").ThenBy(p => SoPhong(p))
                .Select(p => ToStay(p, DangO(p), today))
                .ToList();

            dto.KhachDi = phieu
                .Where(p => (DangO(p) && p.NgayCheckOutDuKien.Date <= today) || p.TrangThai == BookingStatus.CheckedOut)
                .OrderBy(p => p.TrangThai == BookingStatus.CheckedOut).ThenBy(p => SoPhong(p))
                .Select(p => ToStay(p, p.TrangThai == BookingStatus.CheckedOut, today))
                .ToList();

            dto.DangO = phieu.Where(DangO).OrderBy(p => SoPhong(p)).Select(p => ToStay(p, false, today)).ToList();

            dto.SapDen = phieu
                .Where(p => ChuaNhan(p) && p.NgayCheckInDuKien.Date > today)
                .OrderBy(p => p.NgayCheckInDuKien).ThenBy(p => SoPhong(p))
                .Select(p => ToStay(p, false, today))
                .ToList();

            // Trống tối nay: phòng sạch, không có khách sắp nhận phòng hôm nay (dùng cho khách vãng lai)
            var phongDaGiu = dto.KhachDen.Where(k => !k.DaXuLy && k.MaPhong != null).Select(k => k.MaPhong).ToHashSet();
            dto.PhongTrong = await _context.Phongs.CountAsync(p => p.TrangThai == RoomStatus.Available)
                - await _context.Phongs.CountAsync(p => p.TrangThai == RoomStatus.Available && phongDaGiu.Contains(p.MaPhong));

            dto.SoKhachDen = dto.KhachDen.Count;
            dto.SoKhachDenDaNhan = dto.KhachDen.Count(k => k.DaXuLy);
            dto.SoKhachDi = dto.KhachDi.Count;
            dto.SoKhachDiDaTra = dto.KhachDi.Count(k => k.DaXuLy);
            dto.PhieuChoDuyet = phieu.Count(p => p.TrangThai == BookingStatus.Pending);

            dto.ViecBuongPhongChuaXong = await _context.PhanCongDonPhongs.CountAsync(t => t.TrangThai != HousekeepingTaskStatus.Completed);
            dto.SuCoBuongPhong = await _context.PhanCongDonPhongs.CountAsync(t => t.TrangThai == HousekeepingTaskStatus.Issue);

            // Yêu cầu khách chưa xong: giờ hẹn gần nhất trước (không hẹn giờ xếp sau)
            var yeuCau = await _context.YeuCauKhachs
                .Include(y => y.Phong)
                .Include(y => y.PhieuDatPhong).ThenInclude(p => p.KhachHang)
                .Include(y => y.NhanVienTao)
                .Where(y => y.TrangThai == GuestRequestStatus.New || y.TrangThai == GuestRequestStatus.InProgress)
                .ToListAsync();
            dto.YeuCauDangMo = yeuCau.Count;
            dto.YeuCauSapToi = yeuCau.OrderBy(y => y.ThoiGianHen ?? DateTime.MaxValue).ThenBy(y => y.ThoiGianTao).Take(6).Select(y => y.ToDto()).ToList();

            var caTruoc = await _context.GiaoCas.Include(g => g.NhanVien).OrderByDescending(g => g.KetThuc).FirstOrDefaultAsync();
            dto.CaTruoc = caTruoc == null ? null : ToHandover(caTruoc);
            return dto;
        }

        public async Task<ShiftSummaryDto> GetShiftSummaryAsync(string maNhanVien)
        {
            var now = DateTime.Now;
            var lanChotTruoc = await _context.GiaoCas.Where(g => g.MaNhanVien == maNhanVien && g.KetThuc >= now.AddHours(-24))
                .OrderByDescending(g => g.KetThuc).Select(g => (DateTime?)g.KetThuc).FirstOrDefaultAsync();
            var tu = lanChotTruoc ?? DateTime.Today;
            var dto = new ShiftSummaryDto
            {
                MaNhanVien = maNhanVien,
                TenNhanVien = (await _context.NhanViens.FindAsync(maNhanVien))?.TenNhanVien,
                TuLuc = tu,
                DenLuc = now
            };

            // Thu / hoàn tiền hoá đơn (không tính phần trả bằng điểm thưởng) và thu / hoàn cọc do nhân viên này thực hiện
            var thanhToan = await _context.ThanhToans
                .Where(t => t.MaNhanVien == maNhanVien && t.NgayThanhToan > tu && t.NgayThanhToan <= now && t.PhuongThuc != LoyaltyPolicy.PhuongThucDiem)
                .ToListAsync();
            var coc = await _context.GiaoDichCocs
                .Where(g => g.MaNhanVien == maNhanVien && g.ThoiGian > tu && g.ThoiGian <= now)
                .ToListAsync();

            dto.GiaoDich = thanhToan.Select(t => new ShiftTransactionDto
            {
                ThoiGian = t.NgayThanhToan,
                Loai = t.LoaiGiaoDich == PaymentType.Refund ? "Hoàn tiền hoá đơn" : "Thanh toán hoá đơn",
                MaThamChieu = t.MaHoaDon,
                PhuongThuc = t.PhuongThuc ?? "Không rõ",
                SoTien = t.LoaiGiaoDich == PaymentType.Refund ? -t.SoTienDaTra : t.SoTienDaTra,
                GhiChu = t.GhiChu
            }).Concat(coc.Select(g => new ShiftTransactionDto
            {
                ThoiGian = g.ThoiGian,
                Loai = g.LoaiGiaoDich == PaymentType.Refund ? "Hoàn cọc" : "Thu cọc",
                MaThamChieu = g.MaDatPhong,
                PhuongThuc = g.PhuongThuc ?? "Không rõ",
                SoTien = g.LoaiGiaoDich == PaymentType.Refund ? -g.SoTien : g.SoTien,
                GhiChu = g.GhiChu
            })).Where(x => x.SoTien != 0).OrderBy(x => x.ThoiGian).ToList();

            dto.TheoPhuongThuc = dto.GiaoDich.GroupBy(x => x.PhuongThuc)
                .Select(g => new ShiftMethodDto
                {
                    PhuongThuc = g.Key,
                    SoGiaoDich = g.Count(),
                    Thu = g.Where(x => x.SoTien > 0).Sum(x => x.SoTien),
                    Chi = -g.Where(x => x.SoTien < 0).Sum(x => x.SoTien),
                    Rong = g.Sum(x => x.SoTien)
                })
                .OrderBy(m => m.PhuongThuc != PaymentMethods.TienMat).ThenByDescending(m => m.Rong)
                .ToList();
            dto.TongThu = dto.TheoPhuongThuc.Sum(m => m.Thu);
            dto.TongChi = dto.TheoPhuongThuc.Sum(m => m.Chi);
            dto.TongThuRong = dto.TongThu - dto.TongChi;
            dto.TienMatHeThong = dto.TheoPhuongThuc.FirstOrDefault(m => m.PhuongThuc == PaymentMethods.TienMat)?.Rong ?? 0;
            return dto;
        }

        public async Task<ShiftHandoverDto> CloseShiftAsync(string maNhanVien, decimal tienMatThucTe, string ghiChuBanGiao)
        {
            var tomTat = await GetShiftSummaryAsync(maNhanVien);
            var ca = new GiaoCa
            {
                MaNhanVien = maNhanVien,
                BatDau = tomTat.TuLuc,
                KetThuc = tomTat.DenLuc,
                TongThuRong = tomTat.TongThuRong,
                TienMatHeThong = tomTat.TienMatHeThong,
                TienMatThucTe = Math.Round(tienMatThucTe, 0),
                ChenhLech = Math.Round(tienMatThucTe, 0) - tomTat.TienMatHeThong,
                GhiChuBanGiao = string.IsNullOrWhiteSpace(ghiChuBanGiao) ? null : ghiChuBanGiao.Trim(),
                ChiTiet = JsonSerializer.Serialize(tomTat.TheoPhuongThuc)
            };
            _context.GiaoCas.Add(ca);
            await _context.SaveChangesAsync();
            await _context.Entry(ca).Reference(g => g.NhanVien).LoadAsync();
            return ToHandover(ca);
        }

        public async Task<List<ShiftHandoverDto>> GetHandoversAsync(int soNgay)
        {
            var moc = DateTime.Today.AddDays(-Math.Clamp(soNgay, 1, 90));
            return (await _context.GiaoCas.Include(g => g.NhanVien).Where(g => g.KetThuc >= moc).OrderByDescending(g => g.KetThuc).ToListAsync())
                .Select(ToHandover).ToList();
        }

        private static ShiftHandoverDto ToHandover(GiaoCa g) => new ShiftHandoverDto
        {
            MaGiaoCa = g.MaGiaoCa,
            MaNhanVien = g.MaNhanVien,
            TenNhanVien = g.NhanVien?.TenNhanVien,
            BatDau = g.BatDau,
            KetThuc = g.KetThuc,
            TongThuRong = g.TongThuRong,
            TienMatHeThong = g.TienMatHeThong,
            TienMatThucTe = g.TienMatThucTe,
            ChenhLech = g.ChenhLech,
            GhiChuBanGiao = g.GhiChuBanGiao,
            TheoPhuongThuc = string.IsNullOrEmpty(g.ChiTiet) ? new List<ShiftMethodDto>() : JsonSerializer.Deserialize<List<ShiftMethodDto>>(g.ChiTiet)
        };

        private static string SoPhong(PhieuDatPhong p) => p.ChiTietDatPhongs.FirstOrDefault()?.Phong?.SoPhong ?? "";

        private static FrontDeskStayDto ToStay(PhieuDatPhong p, bool daXuLy, DateTime today)
        {
            var phong = p.ChiTietDatPhongs.FirstOrDefault()?.Phong;
            var khach = p.KhachHang;
            return new FrontDeskStayDto
            {
                MaDatPhong = p.MaDatPhong,
                TrangThai = p.TrangThai,
                MaKhachHang = p.MaKhachHang,
                HoTenKhachHang = khach == null ? "" : $"{khach.Ho} {khach.Ten}".Trim(),
                SDT = khach?.LienHeDiaChis?.FirstOrDefault()?.SDT,
                LaVIP = khach?.LaVIP ?? false,
                SoThich = khach?.SoThich,
                HanChe = khach?.HanChe ?? false,
                LyDoHanChe = khach?.LyDoHanChe,
                MaPhong = phong?.MaPhong,
                SoPhong = phong?.SoPhong,
                TenLoaiPhong = phong?.LoaiPhong?.TenLoai,
                TrangThaiPhong = phong?.TrangThai,
                NgayNhan = p.NgayCheckInDuKien,
                NgayTra = p.NgayCheckOutDuKien,
                NgayCheckInThucTe = p.NgayCheckInThucTe,
                NgayCheckOutThucTe = p.NgayCheckOutThucTe,
                SoDem = BillingCalculator.TinhSoDem(p.NgayCheckInThucTe ?? p.NgayCheckInDuKien, p.NgayCheckOutDuKien),
                SoKhach = p.SoLuongKhach,
                GioDenDuKien = p.GioDenDuKien,
                YeuCauDacBiet = p.YeuCauDacBiet,
                TienCoc = p.TienCoc,
                MaKhuyenMai = p.MaKhuyenMai,
                NguonDat = p.NguonDat,
                DaXuLy = daXuLy,
                QuaHanTra = p.TrangThai == BookingStatus.CheckedIn && p.NgayCheckOutDuKien.Date < today,
                KhachDiCung = p.KhachDiCungs?.OrderBy(k => k.MaKhachDiCung).Select(k => k.ToDto()).ToList() ?? new List<CompanionDto>(),
                KhongLamPhien = p.KhongLamPhienNgay?.Date == today,
                GioDonMongMuon = p.GioDonMongMuon,
                TuChoiDonHangNgay = p.TuChoiDonHangNgay
            };
        }
    }
}
