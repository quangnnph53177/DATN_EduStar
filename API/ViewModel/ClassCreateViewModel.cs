namespace API.ViewModel
{
    public class ClassCreateViewModel
    {
        public string? ClassName { get; set; }
        public int? SubjectId { get; set; }
        public string? Semester { get; set; }
        public int? YearSchool { get; set; }
        public Guid? TeacherId { get; set; }
    }
}
