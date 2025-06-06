using API.ViewModel;

namespace Web.ViewModels
{
    public class DashboardViewModel
    {
        public List<StudentByAddressDTO>? studentByAddress { get; set; }
        public List<StudentByClassDTO>? studentByClass { get; set; }
        public  List<StudentByGenderDTO>? studentByGender { get; set; }
        public List<StudentByStatusDTO> ?studentByStatus { get; set; }
    }
}
