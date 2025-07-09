using System;
using System.Collections.Generic;

namespace API.Models;

public partial class Class
{
    public int Id { get; set; }

    public string? NameClass { get; set; }

    public int? SubjectId { get; set; }

    public string? Semester { get; set; }

    public int? YearSchool { get; set; }

    public bool? Status { get; set; }
    public Guid? TeacherId { get; set; }
    public virtual User? User { get; set; }

    public virtual ICollection<ClassChange> ClassChangeCurrentClasses { get; set; } = new List<ClassChange>();

    public virtual ICollection<ClassChange> ClassChangeRequestedClasses { get; set; } = new List<ClassChange>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual Subject? Subject { get; set; }

    public virtual ICollection<StudentsInfor> Students { get; set; } = new List<StudentsInfor>();
}
