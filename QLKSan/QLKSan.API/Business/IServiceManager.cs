using System.Collections.Generic;
using System.Threading.Tasks;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    public interface IServiceManager
    {
        Task<IEnumerable<DichVu>> GetAllServicesAsync();
        Task<IEnumerable<Phong>> GetOccupiedRoomsAsync();
        Task<IEnumerable<PhieuDatPhong>> GetActiveStaysAsync();
        Task<bool> OrderServiceAsync(string maDatPhong, string maDichVu, int soLuong);
        Task<string> GetActiveBookingIdByRoomAsync(string maPhong);
    }
}
