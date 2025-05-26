using System;
using System.Collections.Generic;

namespace API.Models;

public partial class Subject
{
    public int Id { get; set; }

    public string SubjectName { get; set; } = null!;

    public string? Description { get; set; }

    public int? NumberOfCredits { get; set; }

    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}
