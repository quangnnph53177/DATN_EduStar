namespace API.ViewModel
{
    public class ClassCreateViewModel
    {
        public string? ClassName { get; set; }
        public int? SubjectId { get; set; }
        public string? SubjectName { get; set; }
        public int? SemesterId { get; set; }
        public int? YearSchool { get; set; }
        public Guid? TeacherId { get; set; }
    }
}
