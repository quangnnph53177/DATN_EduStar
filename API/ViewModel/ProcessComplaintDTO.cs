namespace API.ViewModel
{
    public class ProcessComplaintDTO
    {
        public int ComplaintId { get; set; }
        public string Status { get; set; } = "Processing";  // Approved / Rejected
        public string Note { get; set; } = string.Empty;
    }
}
