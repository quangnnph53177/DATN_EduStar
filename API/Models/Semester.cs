using System.Diagnostics;

namespace API.Models
{
    public class Semester
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;             // VD: HK1 2025-2026
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = false;

        // Quan hệ ngược
        public ICollection<TeachingRegistration> TeachingRegistrations { get; set; } = new List<TeachingRegistration>();
        public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
        public ICollection<UserProfile> UserProfiles { get; set; } = new List<UserProfile>();
        public ICollection<ClassChange> ClassChanges { get; set; } = new List<ClassChange>();
        public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
        public override string ToString()
        {
            return $"{Name} ({StartDate:dd/MM/yyyy} - {EndDate:dd/MM/yyyy})";
        }
    }
}
