using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    public class ServiceManager : IServiceManager
    {
        private readonly ApplicationDbContext _context;

        public ServiceManager(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DichVu>> GetAllServicesAsync()
        {
            return await _context.DichVus.ToListAsync();
        }

        public async Task<IEnumerable<PhieuDatPhong>> GetActiveStaysAsync()
        {
            return await _context.PhieuDatPhongs
                .Include(p => p.KhachHang).ThenInclude(k => k.LienHeDiaChis)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong).ThenInclude(r => r.LoaiPhong)
                .Include(p => p.SuDungDichVus).ThenInclude(s => s.DichVu)
                .Include(p => p.HoaDon)
                .Where(p => p.TrangThai == BookingStatus.CheckedIn)
                .OrderByDescending(p => p.NgayCheckInThucTe)
                .ToListAsync();
        }

        public async Task OrderServiceAsync(string maDatPhong, string maDichVu, int soLuong)
        {
            var booking = await _context.PhieuDatPhongs.FindAsync(maDatPhong)
                ?? throw new NotFoundException($"Không tìm thấy phiếu đặt phòng '{maDatPhong}'.");
            if (booking.TrangThai != BookingStatus.CheckedIn)
                throw new ConflictException("Chỉ có thể gọi dịch vụ cho khách đang lưu trú.");

            var dichVu = await _context.DichVus.FindAsync(maDichVu)
                ?? throw new NotFoundException($"Không tìm thấy dịch vụ '{maDichVu}'.");
            if (dichVu.TrangThai != ServiceStatus.Available)
                throw new ConflictException($"Dịch vụ '{dichVu.TenDichVu}' hiện đã hết.");

            var existingOrder = await _context.SuDungDichVus
                .FirstOrDefaultAsync(s => s.MaDatPhong == maDatPhong && s.MaDichVu == maDichVu);

            if (existingOrder != null)
            {
                existingOrder.SoLuong += soLuong;
                existingOrder.NgaySuDung = DateTime.Now;
            }
            else
            {
                var order = new SuDungDichVu
                {
                    MaSDDV = await IdGenerator.NextAsync(_context.SuDungDichVus.Select(s => s.MaSDDV), "SD", 4),
                    MaDatPhong = maDatPhong,
                    MaDichVu = maDichVu,
                    SoLuong = soLuong,
                    NgaySuDung = DateTime.Now,
                    GiaThucTe = dichVu.GiaDichVu,
                    ThanhTien = soLuong * dichVu.GiaDichVu
                };
                _context.SuDungDichVus.Add(order);
            }

            await _context.SaveChangesAsync();
        }
    }
}
