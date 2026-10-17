using System.Collections.Generic;
using System.Threading.Tasks;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    public interface IHousekeepingService
    {
        Task<IEnumerable<Phong>> GetRoomsNeedingCleaningAsync();
        Task<IEnumerable<Phong>> GetAllRoomsWithStatusAsync();
        Task<bool> MarkRoomAsCleanAsync(string maPhong);
        Task<int> GetCountByStatusAsync(Enums.RoomStatus status);
        Task<Phong> GetAttentionRoomAsync();
        Task<int> GetVipCountAsync();
        Task<IEnumerable<NhanVien>> GetHousekeepingStaffAsync();
    }
}
