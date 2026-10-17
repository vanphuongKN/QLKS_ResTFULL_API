using System.Collections.Generic;
using System.Threading.Tasks;
using QLKSan.Models.Entities;

namespace QLKSan.Business
{
    public interface IRoomService
    {
        Task<IEnumerable<Phong>> GetAllRoomsAsync();
        Task<Phong> GetRoomByIdAsync(string maPhong);
        Task UpdateRoomStatusAsync(string maPhong, Enums.RoomStatus status);
    }
}
