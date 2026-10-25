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
    // Màn hình đầu ca của lễ tân: ai đến, ai đi, ai đang ở hôm nay, phiếu sắp đến và tình trạng phòng
    public interface IFrontDeskService
    {
        Task<FrontDeskTodayDto> GetTodayAsync();
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
            return dto;
        }

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
                KhachDiCung = p.KhachDiCungs?.OrderBy(k => k.MaKhachDiCung).Select(k => k.ToDto()).ToList() ?? new List<CompanionDto>()
            };
        }
    }
}
