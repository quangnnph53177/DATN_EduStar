using System;
using System.Collections.Generic;

namespace API.Models;

public partial class Subject
{
    public int Id { get; set; }

    public string SubjectName { get; set; } = null!;

    public string? Description { get; set; }

    public string? Subjectcode { get; set; }

    public bool? Status { get; set; }

    public virtual ICollection<Schedule> Classes { get; set; } = new List<Schedule>();
}
