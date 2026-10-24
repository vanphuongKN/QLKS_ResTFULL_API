using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QLKSan.Common;
using QLKSan.Data;
using QLKSan.Enums;
using QLKSan.Models.Entities;
using QLKSan.Models.DTOs;

namespace QLKSan.Business
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;
        public AdminService(ApplicationDbContext context) { _context = context; }

        // Room Types
        public async Task<IEnumerable<LoaiPhong>> GetAllRoomTypesAsync()
            => await _context.LoaiPhongs.ToListAsync();

        public async Task<LoaiPhong> GetRoomTypeByIdAsync(string maLoai)
            => await _context.LoaiPhongs.FindAsync(maLoai);

        public async Task<string> GenerateNextRoomTypeIdAsync()
        {
            var ids = await _context.LoaiPhongs.Select(s => s.MaLoai).ToListAsync();
            int max = 0;
            foreach (var id in ids)
            {
                if (id.StartsWith("LP") && int.TryParse(id.Substring(2), out int num))
                {
                    if (num > max) max = num;
                }
            }
            return "LP" + (max + 1).ToString("D2");
        }

        public async Task AddRoomTypeAsync(LoaiPhong roomType)
        {
            _context.LoaiPhongs.Add(roomType);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRoomTypeAsync(LoaiPhong roomType)
        {
            var existing = await _context.LoaiPhongs.FindAsync(roomType.MaLoai)
                ?? throw new NotFoundException($"Không tìm thấy loại phòng '{roomType.MaLoai}'.");
            existing.TenLoai = roomType.TenLoai;
            existing.SoNguoiToiDa = roomType.SoNguoiToiDa;
            existing.GiaThueNgay = roomType.GiaThueNgay;
            existing.GiaThueGio = roomType.GiaThueGio;
            existing.MoTa = roomType.MoTa;
            existing.DienTich = roomType.DienTich;
            existing.LoaiGiuong = roomType.LoaiGiuong;
            existing.HuongNhin = roomType.HuongNhin;
            existing.MoTaChiTiet = roomType.MoTaChiTiet;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteRoomTypeAsync(string maLoai)
        {
            var roomType = await _context.LoaiPhongs.FindAsync(maLoai)
                ?? throw new NotFoundException($"Không tìm thấy loại phòng '{maLoai}'.");

            // Prevent deletion if rooms are associated
            bool hasRooms = await _context.Phongs.AnyAsync(p => p.MaLoai == maLoai);
            if (hasRooms) throw new ConflictException("Không thể xóa (có phòng đang dùng loại này).");
            if (await _context.KhuyenMais.AnyAsync(k => k.MaLoai == maLoai))
                throw new ConflictException("Không thể xóa (có khuyến mãi chỉ áp dụng cho hạng phòng này).");

            _context.LoaiPhongs.Remove(roomType);
            await _context.SaveChangesAsync();
        }

        // Rooms
        public async Task<IEnumerable<Phong>> GetAllRoomsAsync()
            => await _context.Phongs.Include(p => p.LoaiPhong).OrderBy(p => p.SoPhong).ToListAsync();

        public async Task AddRoomAsync(Phong room)
        {
            if (!await _context.LoaiPhongs.AnyAsync(l => l.MaLoai == room.MaLoai))
                throw new BusinessException($"Loại phòng '{room.MaLoai}' không tồn tại.");
            if (await _context.Phongs.AnyAsync(p => p.MaPhong == room.MaPhong || p.SoPhong == room.SoPhong))
                throw new ConflictException($"Phòng số '{room.SoPhong}' đã tồn tại.");

            _context.Phongs.Add(room);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRoomAsync(Phong room)
        {
            var existing = await _context.Phongs.FindAsync(room.MaPhong)
                ?? throw new NotFoundException($"Không tìm thấy phòng '{room.MaPhong}'.");
            if (!await _context.LoaiPhongs.AnyAsync(l => l.MaLoai == room.MaLoai))
                throw new BusinessException($"Loại phòng '{room.MaLoai}' không tồn tại.");
            if (await _context.Phongs.AnyAsync(p => p.MaPhong != room.MaPhong && p.SoPhong == room.SoPhong))
                throw new ConflictException($"Phòng số '{room.SoPhong}' đã tồn tại.");

            existing.SoPhong = room.SoPhong;
            existing.MaLoai = room.MaLoai;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteRoomAsync(string maPhong)
        {
            var room = await _context.Phongs.FindAsync(maPhong)
                ?? throw new NotFoundException($"Không tìm thấy phòng '{maPhong}'.");

            // Lịch sử dọn phòng là dữ liệu vận hành của chính phòng này nên xoá theo; phiếu đặt/đánh giá vẫn chặn việc xoá
            _context.PhanCongDonPhongs.RemoveRange(_context.PhanCongDonPhongs.Where(t => t.MaPhong == maPhong));
            _context.Phongs.Remove(room);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new ConflictException("Không thể xóa (phòng có liên kết với phiếu đặt, hình ảnh hoặc đánh giá).");
            }
        }

        // Services
        public async Task<IEnumerable<DichVu>> GetAllServicesAsync()
            => await _context.DichVus.ToListAsync();

        public async Task<DichVu> GetServiceByIdAsync(string maDichVu)
            => await _context.DichVus.FindAsync(maDichVu);

        public async Task<string> GenerateNextServiceIdAsync()
        {
            var ids = await _context.DichVus.Select(s => s.MaDichVu).ToListAsync();
            int max = 0;
            foreach (var id in ids)
            {
                if (id.StartsWith("DV") && int.TryParse(id.Substring(2), out int num))
                {
                    if (num > max) max = num;
                }
            }
            return "DV" + (max + 1).ToString("D3");
        }

        public async Task AddServiceAsync(DichVu service)
        {
            _context.DichVus.Add(service);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateServiceAsync(DichVu service)
        {
            var existing = await _context.DichVus.FindAsync(service.MaDichVu)
                ?? throw new NotFoundException($"Không tìm thấy dịch vụ '{service.MaDichVu}'.");
            existing.TenDichVu = service.TenDichVu;
            existing.GiaDichVu = service.GiaDichVu;
            existing.DonViTinh = service.DonViTinh;
            existing.LaTraiNghiem = service.LaTraiNghiem;
            existing.MoTa = service.MoTa;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteServiceAsync(string maDichVu)
        {
            var s = await _context.DichVus.FindAsync(maDichVu)
                ?? throw new NotFoundException($"Không tìm thấy dịch vụ '{maDichVu}'.");
            _context.DichVus.Remove(s);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new ConflictException("Không thể xóa (dịch vụ đã được khách sử dụng). Hãy chuyển sang trạng thái 'Đã hết' thay vì xóa.");
            }
        }

        public async Task<DichVu> SetServiceStatusAsync(string maDichVu, ServiceStatus trangThai)
        {
            var s = await _context.DichVus.FindAsync(maDichVu)
                ?? throw new NotFoundException($"Không tìm thấy dịch vụ '{maDichVu}'.");

            s.TrangThai = trangThai;
            await _context.SaveChangesAsync();
            return s;
        }

        // Staff
        public async Task<IEnumerable<NhanVien>> GetAllStaffAsync()
            => await _context.NhanViens.Include(n => n.TaiKhoan).ToListAsync();

        public async Task<NhanVien> GetStaffByIdAsync(string maNhanVien)
            => await _context.NhanViens.Include(n => n.TaiKhoan).FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien);

        public async Task<string> GenerateNextStaffIdAsync()
        {
            var ids = await _context.NhanViens.Select(s => s.MaNhanVien).ToListAsync();
            int max = 0;
            foreach (var id in ids)
            {
                if (id.StartsWith("NV") && int.TryParse(id.Substring(2), out int num))
                {
                    if (num > max) max = num;
                }
            }
            return "NV" + (max + 1).ToString("D3");
        }

        public async Task AddStaffAsync(NhanVien staff, string tenDangNhap, string matKhau, Role vaiTro)
        {
            if (!AppRoles.StaffRoles.Contains(vaiTro))
                throw new BusinessException("Vai trò nhân viên không hợp lệ.");
            if (await _context.TaiKhoans.AnyAsync(t => t.TenDangNhap == tenDangNhap))
                throw new ConflictException("Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.");

            var account = new TaiKhoan
            {
                MaDangNhap = await IdGenerator.NextAsync(_context.TaiKhoans.Select(t => t.MaDangNhap), "TK", 4),
                TenDangNhap = tenDangNhap,
                MatKhau = PasswordHasher.Hash(matKhau),
                VaiTro = vaiTro
            };
            _context.TaiKhoans.Add(account);
            staff.MaDangNhap = account.MaDangNhap;
            _context.NhanViens.Add(staff);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateStaffAsync(NhanVien staff, Role? vaiTro, EmployeeStatus? trangThai, string maNhanVienThucHien)
        {
            var existing = await _context.NhanViens.Include(n => n.TaiKhoan).FirstOrDefaultAsync(n => n.MaNhanVien == staff.MaNhanVien)
                ?? throw new NotFoundException($"Không tìm thấy nhân viên '{staff.MaNhanVien}'.");

            bool laChinhMinh = existing.MaNhanVien == maNhanVienThucHien;
            if (laChinhMinh && trangThai == EmployeeStatus.Resigned)
                throw new BusinessException("Bạn không thể tự cho mình nghỉ việc.");
            if (laChinhMinh && vaiTro != null && vaiTro != existing.TaiKhoan?.VaiTro)
                throw new BusinessException("Bạn không thể tự đổi vai trò của chính mình.");

            existing.TenNhanVien = staff.TenNhanVien;
            existing.ChucVu = staff.ChucVu;
            existing.SDT = staff.SDT;
            existing.Email = staff.Email;
            if (trangThai != null) existing.TrangThai = trangThai.Value;

            if (vaiTro != null)
            {
                if (!AppRoles.StaffRoles.Contains(vaiTro.Value))
                    throw new BusinessException("Vai trò nhân viên không hợp lệ.");
                if (existing.TaiKhoan == null)
                    throw new BusinessException("Nhân viên này chưa có tài khoản đăng nhập nên không gán vai trò được.");
                existing.TaiKhoan.VaiTro = vaiTro.Value;
            }
            await _context.SaveChangesAsync();
        }

        public async Task ResetPasswordAsync(string maNhanVien, string matKhauMoi)
        {
            var staff = await _context.NhanViens.Include(n => n.TaiKhoan).FirstOrDefaultAsync(n => n.MaNhanVien == maNhanVien)
                ?? throw new NotFoundException($"Không tìm thấy nhân viên '{maNhanVien}'.");
            if (staff.TaiKhoan == null)
                throw new BusinessException("Nhân viên này chưa có tài khoản đăng nhập.");

            staff.TaiKhoan.MatKhau = PasswordHasher.Hash(matKhauMoi);
            // Đặt lại mật khẩu cũng mở khoá tài khoản đang bị khoá tạm
            staff.TaiKhoan.SoLanSai = 0;
            staff.TaiKhoan.KhoaDen = null;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteStaffAsync(string maNhanVien, string maNhanVienThucHien)
        {
            if (maNhanVien == maNhanVienThucHien)
                throw new BusinessException("Bạn không thể tự xoá tài khoản của chính mình.");
            var staff = await _context.NhanViens.FindAsync(maNhanVien)
                ?? throw new NotFoundException($"Không tìm thấy nhân viên '{maNhanVien}'.");

            // Remove associated account if exists
            if (!string.IsNullOrEmpty(staff.MaDangNhap))
            {
                var account = await _context.TaiKhoans.FindAsync(staff.MaDangNhap);
                if (account != null) _context.TaiKhoans.Remove(account);
            }

            _context.NhanViens.Remove(staff);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new ConflictException("Không thể xóa (nhân viên đã có lịch sử phiếu đặt, hoá đơn hoặc công việc). Hãy chuyển sang trạng thái 'Nghỉ việc'.");
            }
        }
    }
}
