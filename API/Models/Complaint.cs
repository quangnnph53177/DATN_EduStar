using System;
using System.Collections.Generic;

namespace API.Models;

public partial class Complaint
{
    public int Id { get; set; }

    public Guid? StudentId { get; set; }

    public string? ComplaintType { get; set; }

    public string? Statuss { get; set; }

    public string? Reason { get; set; }

    public string? ProofUrl { get; set; }

    public DateTime? CreateAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public Guid? ProcessedBy { get; set; }

    public string? ResponseNote { get; set; }

    public virtual AttendanceDetailsComplaint? AttendanceDetailsComplaint { get; set; }

    public virtual ClassChange? ClassChange { get; set; }

    public virtual User? ProcessedByNavigation { get; set; }

    public virtual User? Student { get; set; }
}
