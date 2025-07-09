using API.Data;
using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Repositories
{
    public class RoomRepos : IRoom
    {
        private readonly AduDbcontext _context;
        public RoomRepos(AduDbcontext context)
        {
            _context = context;
        }
        public async Task Create(RoomDTO ro)
        {
            var room = new Room
            {
                RoomCode =ro.RoomCode,
                Capacity =ro.Capacity,
                Device =ro.Device,
            };
            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Room>> GetAll()
        {
            var lstroom =await _context.Rooms.ToListAsync();
            return lstroom;
        }

        public async Task<Room> GetById(int id)
        {
            var onr = await _context.Rooms.FindAsync(id);
            return onr;
        }

        public async Task Update(RoomDTO ro)
        {
            var room = new Room
            {
                RoomCode=ro.RoomCode,
                Capacity =ro.Capacity,
                Device =ro.Device,
            };
            _context.Rooms.Update(room );
            await _context.SaveChangesAsync();
        }
    }
}
