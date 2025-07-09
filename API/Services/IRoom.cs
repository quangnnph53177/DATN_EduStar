using API.Models;
using API.ViewModel;

namespace API.Services
{
    public interface IRoom
    {
        Task<List<Room>> GetAll();
        Task<Room> GetById(int id);
        Task Create(RoomDTO ro);
        Task Update(RoomDTO ro);
    }
}
