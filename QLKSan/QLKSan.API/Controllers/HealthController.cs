using Microsoft.AspNetCore.Mvc;
using QLKSan.Data;

namespace QLKSan.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HealthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET api/health — kiểm tra API đang chạy và kết nối được CSDL
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var databaseConnected = await _context.Database.CanConnectAsync();
            var result = new
            {
                status = databaseConnected ? "Healthy" : "Unhealthy",
                database = databaseConnected ? "Connected" : "Disconnected",
                time = DateTime.Now
            };

            return databaseConnected ? Ok(result) : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
        }
    }
}
