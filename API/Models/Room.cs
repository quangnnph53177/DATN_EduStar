using System;
using System.Collections.Generic;

namespace API.Models;

public partial class Room
{
    public int Id { get; set; }

    public string? RoomCode { get; set; }

    public int? Capacity { get; set; }

    public string? Device { get; set; }

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
