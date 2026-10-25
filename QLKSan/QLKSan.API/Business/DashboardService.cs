using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Mappings;
using QLKSan.Models.DTOs;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    // Bảng điều khiển điều hành cho quản lý: số liệu hôm nay, hiệu quả kinh doanh 30 ngày (ADR, RevPAR),
    // công suất đã đặt 14 ngày tới và các việc cần xử lý. Doanh thu / công suất dùng chung cách tính với trang Báo cáo.
    public interface IDashboardService
    {
        Task<DashboardDto> GetAsync();
    }

    public class DashboardService : IDashboardService
    {
        private const int SoNgayKy = 30;
        private readonly ApplicationDbContext _context;
        private readonly IAccountingService _accounting;

        public DashboardService(ApplicationDbContext context, IAccountingService accounting)
        {
            _context = context;
            _accounting = accounting;
        }

        public async Task<DashboardDto> GetAsync()
        {
            var today = DateTime.Today;
            var dto = new DashboardDto { NgayCapNhat = DateTime.Now, TongSoPhong = await _context.Phongs.CountAsync() };

            await FillTodayAsync(dto, today);
            dto.KyNay = await _accounting.GetPeriodAsync(today.AddDays(-(SoNgayKy - 1)), today);
            dto.KyTruoc = await _accounting.GetPeriodAsync(today.AddDays(-(2 * SoNgayKy - 1)), today.AddDays(-SoNgayKy));
            dto.DuBao14Ngay = await ForecastAsync(today, 14, dto.TongSoPhong);

            var nam = await _accounting.GetRevenueReportAsync(new DateTime(today.Year, 1, 1), today);
            dto.DoanhThuTheoThang = Enumerable.Range(1, 12)
                .Select(m => new DashboardMonthDto
                {
                    Thang = m,
                    DoanhThuThuan = nam.TheoNgay.Where(d => d.Ngay.Month == m).Sum(d => d.DoanhThu - d.HoanTien)
                })
                .ToList();

            await FillTasksAsync(dto, today);
            await FillReviewsAsync(dto);
            return dto;
        }

        private async Task FillTodayAsync(DashboardDto dto, DateTime today)
        {
            var occupancy = await _accounting.GetOccupancyReportAsync(today, today);
            dto.PhongCoKhach = occupancy.TheoNgay.FirstOrDefault()?.SoPhongCoKhach ?? 0;
            dto.CongSuatHomNay = occupancy.TyLeTrungBinh;
            dto.KhachDangLuuTru = await _context.PhieuDatPhongs.CountAsync(p => p.TrangThai == BookingStatus.CheckedIn);

            var tomorrow = today.AddDays(1);
            var query = _context.PhieuDatPhongs
                .Include(p => p.KhachHang)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong).ThenInclude(r => r.LoaiPhong);

            // Khách đến: phiếu nhận phòng hôm nay (chưa nhận hoặc đã nhận trong hôm nay)
            var den = await query
                .Where(p => p.NgayCheckInDuKien >= today && p.NgayCheckInDuKien < tomorrow
                    && (p.TrangThai == BookingStatus.Pending || p.TrangThai == BookingStatus.Approved || p.TrangThai == BookingStatus.CheckedIn))
                .ToListAsync();
            dto.KhachDen = den.OrderBy(p => p.TrangThai == BookingStatus.CheckedIn).ThenBy(p => p.GioDenDuKien)
                .Select(p => ToStay(p, p.TrangThai == BookingStatus.CheckedIn)).ToList();
            dto.SoKhachDen = dto.KhachDen.Count;
            dto.SoKhachDenDaNhan = dto.KhachDen.Count(k => k.DaXuLy);

            // Khách đi: phiếu dự kiến trả phòng hôm nay (đang ở hoặc đã trả trong hôm nay)
            var di = await query
                .Where(p => p.NgayCheckOutDuKien >= today && p.NgayCheckOutDuKien < tomorrow
                    && (p.TrangThai == BookingStatus.CheckedIn || p.TrangThai == BookingStatus.CheckedOut))
                .ToListAsync();
            dto.KhachDi = di.OrderBy(p => p.TrangThai == BookingStatus.CheckedOut)
                .Select(p => ToStay(p, p.TrangThai == BookingStatus.CheckedOut)).ToList();
            dto.SoKhachDi = dto.KhachDi.Count;
            dto.SoKhachDiDaTra = dto.KhachDi.Count(k => k.DaXuLy);
        }

        private static DashboardStayDto ToStay(PhieuDatPhong p, bool daXuLy)
        {
            var phong = p.ChiTietDatPhongs.FirstOrDefault()?.Phong;
            return new DashboardStayDto
            {
                MaDatPhong = p.MaDatPhong,
                HoTenKhachHang = p.KhachHang == null ? "" : $"{p.KhachHang.Ho} {p.KhachHang.Ten}".Trim(),
                SoPhong = phong?.SoPhong,
                TenLoaiPhong = phong?.LoaiPhong?.TenLoai,
                SoKhach = p.SoLuongKhach,
                GioDenDuKien = p.GioDenDuKien,
                YeuCauDacBiet = p.YeuCauDacBiet,
                LaVIP = p.KhachHang?.LaVIP ?? false,
                SoThich = p.KhachHang?.SoThich,
                DaXuLy = daXuLy
            };
        }

        // Công suất đã đặt: số phòng có phiếu (chờ duyệt / đã duyệt / đang ở) phủ lên từng đêm
        private async Task<List<OccupancyByDayDto>> ForecastAsync(DateTime from, int soNgay, int tongSoPhong)
        {
            var to = from.AddDays(soNgay);
            var stays = await _context.ChiTietDatPhongs
                .Where(c => (c.PhieuDatPhong.TrangThai == BookingStatus.Pending || c.PhieuDatPhong.TrangThai == BookingStatus.Approved
                             || c.PhieuDatPhong.TrangThai == BookingStatus.CheckedIn)
                         && c.PhieuDatPhong.NgayCheckInDuKien < to && c.PhieuDatPhong.NgayCheckOutDuKien > from)
                .Select(c => new { c.MaPhong, Tu = c.PhieuDatPhong.NgayCheckInThucTe ?? c.PhieuDatPhong.NgayCheckInDuKien, Den = c.PhieuDatPhong.NgayCheckOutDuKien })
                .ToListAsync();

            return Enumerable.Range(0, soNgay).Select(i =>
            {
                var day = from.AddDays(i);
                var soPhong = stays.Where(s => s.Tu.Date <= day && day < s.Den.Date).Select(s => s.MaPhong).Distinct().Count();
                return new OccupancyByDayDto { Ngay = day, SoPhongCoKhach = soPhong, TyLe = tongSoPhong == 0 ? 0 : Math.Round(soPhong * 100m / tongSoPhong, 1) };
            }).ToList();
        }

        private async Task FillTasksAsync(DashboardDto dto, DateTime today)
        {
            dto.PhieuChoDuyet = await _context.PhieuDatPhongs.CountAsync(p => p.TrangThai == BookingStatus.Pending);
            dto.PhongBaoTri = await _context.Phongs.CountAsync(p => p.TrangThai == RoomStatus.Maintenance);
            dto.PhongDangDon = await _context.Phongs.CountAsync(p => p.TrangThai == RoomStatus.Cleaning);
            dto.ViecBuongPhongChuaXong = await _context.PhanCongDonPhongs.CountAsync(t => t.TrangThai != HousekeepingTaskStatus.Completed);
            dto.SuCoBuongPhong = await _context.PhanCongDonPhongs.CountAsync(t => t.TrangThai == HousekeepingTaskStatus.Issue);

            // Khuyến mãi đang chạy kết thúc trong 7 ngày tới
            var han = today.AddDays(7);
            dto.KhuyenMaiSapHetHan = (await _context.KhuyenMais
                    .Where(k => k.KichHoat && k.NgayBatDau <= today && k.NgayKetThuc >= today && k.NgayKetThuc <= han)
                    .OrderBy(k => k.NgayKetThuc).ToListAsync())
                .Select(k => k.ToDto()).ToList();
        }

        private async Task FillReviewsAsync(DashboardDto dto)
        {
            dto.TongSoDanhGia = await _context.DanhGias.CountAsync();
            dto.DiemDanhGiaTrungBinh = dto.TongSoDanhGia == 0 ? null : Math.Round(await _context.DanhGias.AverageAsync(d => (double)d.SoSao), 1);
            dto.DanhGiaMoi = (await _context.DanhGias
                    .Include(d => d.KhachHang)
                    .Include(d => d.NhanVienPhanHoi)
                    .Include(d => d.Phong).ThenInclude(p => p.LoaiPhong)
                    .OrderByDescending(d => d.NgayDanhGia).Take(5).ToListAsync())
                .Select(d => d.ToDto()).ToList();
        }
    }
}
