using System;
using System.Collections.Generic;

namespace API.Models;

public partial class ClassChange
{
    public int ComplaintId { get; set; }

    public int? CurrentClassId { get; set; }

    public int? RequestedClassId { get; set; }
    public int? SemesterId { get; set; }
    public Semester Semester { get; set; } = null!;
    public virtual Complaint Complaint { get; set; } = null!;

    public virtual Class? CurrentClass { get; set; }

    public virtual Class? RequestedClass { get; set; }
}
