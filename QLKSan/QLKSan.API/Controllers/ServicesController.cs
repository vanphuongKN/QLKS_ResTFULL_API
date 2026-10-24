using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLKSan.Business;
using QLKSan.Common;
using QLKSan.Mappings;
using QLKSan.Models.DTOs;
using QLKSan.Models.Requests;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/services")]
    [Authorize(Roles = AppRoles.AllStaff)]
    public class ServicesController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public ServicesController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        /// <summary>Danh sách dịch vụ (công khai: trang chủ giới thiệu dịch vụ và giá).</summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<ServiceDto>>> GetAll()
        {
            var services = await _adminService.GetAllServicesAsync();
            return Ok(services.Select(s => s.ToDto()));
        }

        /// <summary>Chi tiết dịch vụ.</summary>
        [HttpGet("{maDichVu}")]
        public async Task<ActionResult<ServiceDto>> GetById(string maDichVu)
        {
            var service = await _adminService.GetServiceByIdAsync(maDichVu);
            if (service == null) return NotFound();
            return service.ToDto();
        }

        /// <summary>Thêm dịch vụ (mã tự sinh DVxxx).</summary>
        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<ServiceDto>> Create(ServiceRequest request)
        {
            var maDichVu = await _adminService.GenerateNextServiceIdAsync();
            var service = request.ToEntity(maDichVu);
            await _adminService.AddServiceAsync(service);
            return CreatedAtAction(nameof(GetById), new { maDichVu }, service.ToDto());
        }

        /// <summary>Cập nhật dịch vụ.</summary>
        [HttpPut("{maDichVu}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Update(string maDichVu, ServiceRequest request)
        {
            await _adminService.UpdateServiceAsync(request.ToEntity(maDichVu));
            return NoContent();
        }

        /// <summary>Xoá dịch vụ.</summary>
        [HttpDelete("{maDichVu}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Delete(string maDichVu)
        {
            await _adminService.DeleteServiceAsync(maDichVu);
            return NoContent();
        }

        /// <summary>Đặt trạng thái dịch vụ: body { "TrangThai": "Available" | "Unavailable" }.</summary>
        [HttpPatch("{maDichVu}")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<ActionResult<ServiceDto>> UpdateStatus(string maDichVu, UpdateServiceStatusRequest request)
        {
            var service = await _adminService.SetServiceStatusAsync(maDichVu, request.TrangThai!.Value);
            return service.ToDto();
        }
    }
}
