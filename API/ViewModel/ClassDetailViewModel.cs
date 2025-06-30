namespace API.ViewModel
{
    public class ClassDetailViewModel
    {
        public int ClassId { get; set; }
        public string? ClassName { get; set; }
        public string? Description { get; set; }
        public string? SubjectName { get; set; }
        public string? Semester { get; set; }
        public int YearSchool { get; set; }
        public int NumberOfCredits { get; set; }
        public List<StudentViewModels>? Students { get; set; } 
    }
}