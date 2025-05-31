namespace API.ViewModel
{
    public class AuditLogViewModel
    {
        public int Id { get; set; }

        public string UserName { get; set; }

        public string Active { get; set; }

        public string OldData { get; set; }
        public string NewData { get; set; }
        public string PerformeByName { get; set; }

        public DateTime? Timestamp { get; set; }
    }
}
