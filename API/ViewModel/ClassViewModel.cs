namespace API.ViewModel
{
    public class ClassViewModel
    {
        public int ClassId { get; set; }
        public string? ClassName { get; set; }
        public string? Description { get; set; }
        public string? SubjectName { get; set; }
        public int? Semester { get; set; }
        public int YearSchool { get; set; }
        public int NumberOfCredits { get; set; }
        //Thêm 2 trường này
        public string? TeacherName { get; set; }
        public List<StudentViewModels>? Students { get; set; }
    }
}
