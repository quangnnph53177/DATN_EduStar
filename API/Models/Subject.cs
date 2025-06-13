using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace API.Models;

public partial class Subject
{
    [Key]
    public int Id { get; set; }

    public string SubjectName { get; set; } = null!;
    public string subjectCode { get; set; } = null!;

    public string? Description { get; set; }

    public int? NumberOfCredits { get; set; }
    public bool? Status { get; set; }
    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}
