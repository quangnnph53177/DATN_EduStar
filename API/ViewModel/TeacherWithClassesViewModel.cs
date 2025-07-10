namespace API.ViewModel
{
    public class TeacherWithClassesViewModel
    {
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; }
        public List<ClassWithStudentsViewModel> Classes { get; set; } = new();
    }
}
