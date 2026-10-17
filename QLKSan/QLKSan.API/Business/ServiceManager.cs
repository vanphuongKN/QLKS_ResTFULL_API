using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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

        public async Task<IEnumerable<Phong>> GetOccupiedRoomsAsync()
        {
            return await _context.Phongs
                .Where(p => p.TrangThai == RoomStatus.Occupied)
                .ToListAsync();
        }

        public async Task<IEnumerable<PhieuDatPhong>> GetActiveStaysAsync()
        {
            return await _context.PhieuDatPhongs
                .Include(p => p.KhachHang)
                .Include(p => p.ChiTietDatPhongs).ThenInclude(c => c.Phong)
                .Include(p => p.SuDungDichVus).ThenInclude(s => s.DichVu)
                .Include(p => p.HoaDon)
                .Where(p => p.TrangThai == BookingStatus.CheckedIn)
                .OrderByDescending(p => p.NgayCheckInThucTe)
                .ToListAsync();
        }

        public async Task<string> GetActiveBookingIdByRoomAsync(string maPhong)
        {
            var detail = await _context.ChiTietDatPhongs
                .Include(c => c.PhieuDatPhong)
                .Where(c => c.MaPhong == maPhong && c.PhieuDatPhong.TrangThai == BookingStatus.CheckedIn)
                .OrderByDescending(c => c.PhieuDatPhong.NgayCheckInThucTe)
                .FirstOrDefaultAsync();

            return detail?.MaDatPhong;
        }

        public async Task<bool> OrderServiceAsync(string maDatPhong, string maDichVu, int soLuong)
        {
            try
            {
                var dichVu = await _context.DichVus.FindAsync(maDichVu);
                if (dichVu == null) return false;

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
                        MaSDDV = "SD" + new Random().Next(10000000, 99999999).ToString(),
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
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
