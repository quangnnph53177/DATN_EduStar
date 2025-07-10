namespace API.ViewModel
{
    public class ClassWithStudentsViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public List<UserDTO> StudentsInfor { get; set; } = new();
    }
}
