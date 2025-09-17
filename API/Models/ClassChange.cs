using System;
using System.Collections.Generic;

namespace API.Models;

public partial class ClassChange
{
    public int ComplaintId { get; set; }

    public int? CurrentClassId { get; set; }

    public int? RequestedClassId { get; set; }
    public virtual Complaint Complaint { get; set; } = null!;

    public virtual Schedule? CurrentClass { get; set; }

    public virtual Schedule? RequestedClass { get; set; }
}
