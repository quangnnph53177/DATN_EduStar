using System;
using System.Collections.Generic;

namespace API.Models;

public partial class User
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = null!;

    public string PassWordHash { get; set; } = null!;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public bool? Statuss { get; set; }

    public DateTime? CreateAt { get; set; }
    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Auditlog> AuditlogPerformeByNavigations { get; set; } = new List<Auditlog>();

    public virtual ICollection<Auditlog> AuditlogUsers { get; set; } = new List<Auditlog>();

    public virtual ICollection<Complaint> ComplaintProcessedByNavigations { get; set; } = new List<Complaint>();

    public virtual ICollection<Complaint> ComplaintStudents { get; set; } = new List<Complaint>();

    public virtual StudentsInfor? StudentsInfor { get; set; }

    public virtual UserProfile? UserProfile { get; set; }

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
