using System;
using System.Collections.Generic;

namespace API.Models;

public partial class Subject
{
    public int Id { get; set; }

    public string SubjectName { get; set; } = null!;

    public string? Description { get; set; }

    public int? NumberOfCredits { get; set; }

    public string? Subjectcode { get; set; }

    public bool? Status { get; set; }
    public int? SemesterId { get; set; }
    public Semester Semester { get; set; } = null!;

    public virtual ICollection<Schedule> Classes { get; set; } = new List<Schedule>();
}
