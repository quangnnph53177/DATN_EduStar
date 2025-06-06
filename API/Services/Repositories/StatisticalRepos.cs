using API.Data;
using API.ViewModel;

namespace API.Services.Repositories
{
    public class StatisticalRepos : IStatistical
    {
        private readonly AduDbcontext _context;
        public StatisticalRepos(AduDbcontext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RoomStudy>> GetRoomStudies()
        {
            var room = _context.Rooms
                
                .Select(r => new RoomStudy
                {
                    RoomCode = r.RoomCode,
                    Total = r.Device.Count()
                }).ToList();
            return room;
        }

        public async Task<IEnumerable<StudentByAddressDTO>> GetStudentByAddress()
        {
            var address = _context.UserProfiles
                .GroupBy(p => p.Address)
                .Select(p => new StudentByAddressDTO
                {
                    Address= p.Key,
                    Total = p.Count()
                }).ToList();
            return address;
        }

        public async Task<IEnumerable<StudentByClassDTO>> GetStudentByClass()
        {
            var students =  _context.Classes
                 .Select(c => new StudentByClassDTO
                 {
                     ClassName = c.NameClass,
                     Total = c.Students.Count,
                 }).ToList();
            return students;
        }

        public async Task<IEnumerable<StudentByGenderDTO>> GetStudentByGender()
        {
            var gender = _context.UserProfiles
                .GroupBy(c => c.Gender)
                .Select(u => new StudentByGenderDTO
                {
                    Gender= u.Key == true? "Nam":"Nữ",
                    Total = u.Count()
                }).ToList();
            return gender;
        }

        public async Task<IEnumerable<StudentByStatusDTO>> GetStudentByStatus()
        {
            var status = _context.Users
                .GroupBy(c => c.Statuss)
                .Select(s => new StudentByStatusDTO
                {
                    status = s.Key == true ? "Hoạt Động" : "Không hoạt động",
                    Total = s.Count()
                }).ToList();
            return status;
        }
    }
}
