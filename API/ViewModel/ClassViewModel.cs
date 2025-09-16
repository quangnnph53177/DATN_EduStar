namespace API.ViewModel
{
    public class ClassViewModel
    {
        public int schedulesId { get; set; }
        public string? ClassName { get; set; }
        public string? Description { get; set; }
        public string? SubjectName { get; set; }
        public int? SemesterId { get; set; }
        public int? NumberOfCredits { get; set; }
        //Thêm 2 trường này
        public string? RoomCode { get; set; }
        public string? StudyShiftName { get; set; }
        public TimeOnly? starttime { get; set; }
        public TimeOnly? endtime { get; set; }
        public string? WeekDay { get; set; }
        public string? TeacherName { get; set; }
        public List<StudentViewModels>? Students { get; set; }
    }
}
