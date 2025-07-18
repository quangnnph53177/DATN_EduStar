﻿using API.ViewModel;
using System;
using System.Collections.Generic;

namespace API.Models;

public partial class DayOfWeekk
{
    public int Id { get; set; }
   
    public Weekday Weekdays { get; set; }

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
