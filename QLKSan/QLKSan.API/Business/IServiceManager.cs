using System.Collections.Generic;
using System.Threading.Tasks;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    public interface IServiceManager
    {
        Task<IEnumerable<DichVu>> GetAllServicesAsync();
        Task<IEnumerable<PhieuDatPhong>> GetActiveStaysAsync();
        // Ném NotFoundException/BusinessException nếu phiếu đặt hoặc dịch vụ không hợp lệ
        Task OrderServiceAsync(string maDatPhong, string maDichVu, int soLuong);
    }
}
