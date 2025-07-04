namespace API.ViewModel
{
    public class ComplaintDTO
    {
        public int Id { get; set; }
        public string? ComplaintType { get; set; }
        public string? Statuss { get; set; }
        public string? Reason { get; set; }
        public string? ProofUrl { get; set; }
        public string? StudentName { get; set; }
        public string? ProcessedByName { get; set; }
        public DateTime? CreateAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ResponseNote { get; set; }
    }
}
