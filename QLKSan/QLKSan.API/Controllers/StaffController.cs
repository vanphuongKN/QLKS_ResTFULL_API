using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Enums;
using QLKSan.Mappings;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/staff")]
    [Authorize(Roles = AppRoles.Admin)]
    public class StaffController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public StaffController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        /// <summary>Danh sách nhân viên.</summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StaffDto>>> GetAll()
        {
            var staff = await _adminService.GetAllStaffAsync();
            return Ok(staff.Select(n => n.ToDto()));
        }

        /// <summary>Chi tiết nhân viên.</summary>
        [HttpGet("{maNhanVien}")]
        public async Task<ActionResult<StaffDto>> GetById(string maNhanVien)
        {
            var staff = await _adminService.GetStaffByIdAsync(maNhanVien);
            if (staff == null) return NotFound();
            return staff.ToDto();
        }

        /// <summary>Thêm nhân viên kèm tài khoản đăng nhập (vai trò: Admin, Lễ tân, Buồng phòng, Kế toán — mặc định Lễ tân).</summary>
        [HttpPost]
        [Audit("Thêm nhân viên", "Nhân viên")]
        public async Task<ActionResult<StaffDto>> Create(CreateStaffRequest request)
        {
            var maNhanVien = await _adminService.GenerateNextStaffIdAsync();
            var staff = request.ToEntity(maNhanVien);
            staff.NgayVaoLam = DateTime.Now;
            await _adminService.AddStaffAsync(staff, request.TenDangNhap.Trim(), request.MatKhau, request.VaiTro ?? Role.Receptionist);

            var created = await _adminService.GetStaffByIdAsync(maNhanVien);
            return CreatedAtAction(nameof(GetById), new { maNhanVien }, created.ToDto());
        }

        /// <summary>Cập nhật thông tin, vai trò, trạng thái làm việc (Nghỉ việc = khoá đăng nhập).</summary>
        [HttpPut("{maNhanVien}")]
        [Audit("Sửa nhân viên", "Nhân viên")]
        public async Task<IActionResult> Update(string maNhanVien, UpdateStaffRequest request)
        {
            await _adminService.UpdateStaffAsync(request.ToEntity(maNhanVien), request.VaiTro, request.TrangThai, User.GetMaNhanVien());
            return NoContent();
        }

        /// <summary>Đặt lại mật khẩu đăng nhập cho nhân viên.</summary>
        [HttpPost("{maNhanVien}/reset-password")]
        [Audit("Đặt lại mật khẩu nhân viên", "Nhân viên")]
        public async Task<IActionResult> ResetPassword(string maNhanVien, ResetPasswordRequest request)
        {
            await _adminService.ResetPasswordAsync(maNhanVien, request.MatKhauMoi);
            return NoContent();
        }

        /// <summary>Xoá nhân viên chưa có lịch sử giao dịch (có lịch sử thì nên chuyển sang Nghỉ việc).</summary>
        [HttpDelete("{maNhanVien}")]
        [Audit("Xoá nhân viên", "Nhân viên")]
        public async Task<IActionResult> Delete(string maNhanVien)
        {
            await _adminService.DeleteStaffAsync(maNhanVien, User.GetMaNhanVien());
            return NoContent();
        }
    }
}
