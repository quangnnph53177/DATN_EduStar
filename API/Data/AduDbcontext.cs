using System;
using System.Collections.Generic;
using API.Models;
using API.ViewModel;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol.Resources;
using static API.Models.TeachingRegistration;

namespace API.Data;

public partial class AduDbcontext : DbContext
{
    public AduDbcontext()
    {
    }

    public AduDbcontext(DbContextOptions<AduDbcontext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<AttendanceDetail> AttendanceDetails { get; set; }

    public virtual DbSet<AttendanceDetailsComplaint> AttendanceDetailsComplaints { get; set; }

    public virtual DbSet<Auditlog> Auditlogs { get; set; }

   // public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<ClassChange> ClassChanges { get; set; }

    public virtual DbSet<Complaint> Complaints { get; set; }

    public virtual DbSet<DayOfWeekk> DayOfWeeks { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Schedule> Schedules { get; set; }

    public virtual DbSet<StudentsInfor> StudentsInfors { get; set; }

    public virtual DbSet<StudyShift> StudyShifts { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<TeachingRegistration> TeachingRegistrations { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }
    public virtual DbSet<SchedulesInDay> SchedulesInDays { get; set; }
    public virtual DbSet<ScheduleStudentsInfor> ScheduleStudentsInfors { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Attendan__3214EC076DAB41EB");

            entity.ToTable("Attendance");

            entity.HasIndex(e => e.SchedulesId, "IX_Attendance_SchedulesId");

            entity.HasIndex(e => e.UserId, "IX_Attendance_UserId");

            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Schedules).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.SchedulesId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Attendanc__Sched__6477ECF3");

            entity.HasOne(d => d.User).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Attendanc__UserI__656C112C");
        });

        modelBuilder.Entity<AttendanceDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Attendan__3214EC07676DEB47");

            entity.HasIndex(e => e.AttendanceId, "IX_AttendanceDetails_AttendanceId");

            entity.HasIndex(e => e.StudentId, "IX_AttendanceDetails_StudentId");

            entity.Property(e => e.Status).HasMaxLength(20);

            entity.HasOne(d => d.Attendance).WithMany(p => p.AttendanceDetails)
                .HasForeignKey(d => d.AttendanceId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Attendanc__Atten__6A30C649");

            entity.HasOne(d => d.Student).WithMany(p => p.AttendanceDetails)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__Attendanc__Stude__693CA210");
        });

        modelBuilder.Entity<AttendanceDetailsComplaint>(entity =>
        {
            entity.HasKey(e => e.ComplaintId).HasName("PK__Attendan__740D898FC96B649B");

            entity.HasIndex(e => e.SchedulesId, "IX_AttendanceDetailsComplaints_SchedulesId");

            entity.Property(e => e.ComplaintId).ValueGeneratedNever();

            entity.HasOne(d => d.Complaint).WithOne(p => p.AttendanceDetailsComplaint)
                .HasForeignKey<AttendanceDetailsComplaint>(d => d.ComplaintId)
                .HasConstraintName("FK__Attendanc__Compl__71D1E811");

            entity.HasOne(d => d.Schedules).WithMany(p => p.AttendanceDetailsComplaints)
                .HasForeignKey(d => d.SchedulesId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Attendanc__Sched__72C60C4A");
        });

        modelBuilder.Entity<Auditlog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__auditlog__3214EC074C8DE149");

            entity.ToTable("auditlog");

            entity.HasIndex(e => e.PerformeBy, "IX_auditlog_PerformeBy");

            entity.HasIndex(e => e.Userid, "IX_auditlog_Userid");

            entity.Property(e => e.Timestamp)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.PerformeByNavigation).WithMany(p => p.AuditlogPerformeByNavigations)
                .HasForeignKey(d => d.PerformeBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__auditlog__Userid__50C3F9A6");

            entity.HasOne(d => d.User).WithMany(p => p.AuditlogUsers).HasForeignKey(d => d.Userid);
        });

        modelBuilder.Entity<ClassChange>(entity =>
        {
            entity.HasKey(e => e.ComplaintId).HasName("PK__ClassCha__740D898F5A03A251");

            entity.ToTable("ClassChange");

            entity.HasIndex(e => e.CurrentClassId, "IX_ClassChange_CurrentClassId");

            entity.HasIndex(e => e.RequestedClassId, "IX_ClassChange_RequestedClassId");

            entity.Property(e => e.ComplaintId).ValueGeneratedNever();

            entity.HasOne(d => d.Complaint).WithOne(p => p.ClassChange)
                .HasForeignKey<ClassChange>(d => d.ComplaintId)
                .HasConstraintName("FK__ClassChan__Compl__7A672E12");

            entity.HasOne(d => d.CurrentClass).WithMany(p => p.ClassChangeCurrentClasses)
                .HasForeignKey(d => d.CurrentClassId)
                .HasConstraintName("FK__ClassChan__Curre__7B5B524B");

            entity.HasOne(d => d.RequestedClass).WithMany(p => p.ClassChangeRequestedClasses)
                .HasForeignKey(d => d.RequestedClassId)
                .HasConstraintName("FK__ClassChan__Reque__7C4F7684");
        });

        modelBuilder.Entity<Complaint>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Complain__3214EC076F183F2B");

            entity.HasIndex(e => e.ProcessedBy, "IX_Complaints_ProcessedBy");

            entity.HasIndex(e => e.StudentId, "IX_Complaints_StudentId");

            entity.Property(e => e.ComplaintType).HasMaxLength(50);
            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ProcessedAt).HasColumnType("datetime");
            entity.Property(e => e.ProofUrl).HasMaxLength(255);
            entity.Property(e => e.Statuss).HasMaxLength(20);

            entity.HasOne(d => d.ProcessedByNavigation).WithMany(p => p.ComplaintProcessedByNavigations)
                .HasForeignKey(d => d.ProcessedBy)
                .HasConstraintName("FK__Complaint__Proce__6EF57B66");

            entity.HasOne(d => d.Student).WithMany(p => p.ComplaintStudents)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__Complaint__Stude__6D0D32F4");
        });

        modelBuilder.Entity<DayOfWeekk>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DayOfWeek__3214EC079A470A2D");

            entity.Property(e => e.Weekdays).HasMaxLength(10);
            entity.HasData(
               new DayOfWeekk { Id = 1, Weekdays = Weekday.Sunday },
               new DayOfWeekk { Id = 2, Weekdays = Weekday.Monday },
               new DayOfWeekk { Id = 3, Weekdays = Weekday.Tuesday },
               new DayOfWeekk { Id = 4, Weekdays = Weekday.Wednesday },
               new DayOfWeekk { Id = 5, Weekdays = Weekday.Thursday },
               new DayOfWeekk { Id = 6, Weekdays = Weekday.Friday },
               new DayOfWeekk { Id = 7, Weekdays = Weekday.Saturday }
           );
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Permissi__3214EC07B10A1991");

            entity.ToTable("Permission");
            entity.HasData(
               new Permission { Id = 1, PermissionName = "Create" },
               new Permission { Id = 2, PermissionName = "Detail" },
               new Permission { Id = 3, PermissionName = "Edit" },
               new Permission { Id = 4, PermissionName = "Search" },
               new Permission { Id = 5, PermissionName = "ProcessComplaint" },
               new Permission { Id = 6, PermissionName = "AddRole" }
           );
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC0791E90BA9");

            entity.Property(e => e.RoleName).HasMaxLength(28);
            entity.HasData(
                new Role { Id = 1, RoleName = "Admin" },
                new Role { Id = 2, RoleName = "Teacher" },
                new Role { Id = 3, RoleName = "Student" }
            );

            entity.HasMany(d => d.Permissions).WithMany(p => p.Roles)
                .UsingEntity<Dictionary<string, object>>(
                    "RolePermission",
                    r => r.HasOne<Permission>().WithMany()
                        .HasForeignKey("PermissionId")
                        .HasConstraintName("FK__RolePermi__Permi__3C69FB99"),
                    l => l.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .HasConstraintName("FK__RolePermi__RoleI__3B75D760"),
                    j =>
                    {
                        j.HasKey("RoleId", "PermissionId").HasName("PK__RolePerm__6400A1A882414258");
                        j.ToTable("RolePermission");
                        j.HasIndex(new[] { "PermissionId" }, "IX_RolePermission_PermissionId");

                        j.HasData(
                           // Admin (RoleId = 1)
                           new { RoleId = 1, PermissionId = 1 },
                           new { RoleId = 1, PermissionId = 2 },
                           new { RoleId = 1, PermissionId = 3 },
                           new { RoleId = 1, PermissionId = 4 },
                           new { RoleId = 1, PermissionId = 5 },
                           new { RoleId = 1, PermissionId = 6 },
                            // Teacher (RoleId = 2)
                            new { RoleId = 2, PermissionId = 2 },
                            new { RoleId = 2, PermissionId = 3 },
                            new { RoleId = 2, PermissionId = 4 },
                            new { RoleId = 2, PermissionId = 5 },
                            // Student (RoleId = 3)
                            new { RoleId = 3, PermissionId = 2 },
                            new { RoleId = 3, PermissionId = 3 }

                           );
                    }
                );
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Rooms__3214EC073B8FA24F");

            entity.Property(e => e.Device).HasMaxLength(90);
            entity.Property(e => e.RoomCode).HasMaxLength(50);
            entity.HasData(
                new Room { Id = 1, RoomCode = "Room 101", Device = "Projector, Whiteboard" },
                new Room { Id = 2, RoomCode = "Room 102", Device = "Projector, Whiteboard" },
                new Room { Id = 3, RoomCode = "Room 103", Device = "Projector, Whiteboard" },
                new Room { Id = 4, RoomCode = "Room 104", Device = "Projector, Whiteboard" },
                new Room { Id = 5, RoomCode = "Room 105", Device = "Projector, Whiteboard" }
            );
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasIndex(e => e.ClassName, "IX_Schedules_ClassName");
            entity.HasIndex(e => e.RoomId, "IX_Schedules_RoomId");
            entity.HasIndex(e => e.StudyShiftId, "IX_Schedules_StudyShiftId");
            entity.HasIndex(e => e.SubjectId, "IX_Schedules_SubjectId");
            entity.HasIndex(e => e.UsersId, "IX_Schedules_UsersId");
            entity.HasIndex(e => e.SemesterId, "IX_Schedules_SemesterId"); // Add index for SemesterId

            // ✅ ADD THIS: Configure Schedule → Semester relationship
          

            entity.HasOne(d => d.Room).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Schedules_RoomId");

            entity.HasOne(d => d.StudyShift).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.StudyShiftId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Schedules_StudyShiftId");

            entity.HasOne(d => d.Subject).WithMany(p => p.Classes)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Schedules_Subject");

            entity.HasOne(d => d.User).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.UsersId)
                .HasConstraintName("FK_Schedules_Users");

            entity.HasMany(d => d.ScheduleStudents)
               .WithOne(p => p.Schedule)
               .HasForeignKey(p => p.SchedulesId)
               .OnDelete(DeleteBehavior.Cascade)
               .HasConstraintName("FK_ScheduleStudentsInfor_Schedule");
        });
        modelBuilder.Entity<ScheduleStudentsInfor>(entity =>
        {

            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Student)
                .WithMany(s => s.ScheduleStudents)
                .HasForeignKey(e => e.StudentsUserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ScheduleStudentsInfor_Student");

            entity.HasOne(e => e.Schedule)
                .WithMany(s => s.ScheduleStudents)
                .HasForeignKey(e => e.SchedulesId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_ScheduleStudentsInfor_Schedule");
        });

        modelBuilder.Entity<SchedulesInDay>(entity =>
        {
            entity.HasKey(e => new { e.ScheduleId, e.DayOfWeekkId });

            entity.HasIndex(e => e.DayOfWeekkId, "IX_ScheduleDays_DayOfWeekkId");

            entity.HasOne(d => d.DayOfWeekk).WithMany(p => p.ScheduleDays).HasForeignKey(d => d.DayOfWeekkId);

            entity.HasOne(d => d.Schedule).WithMany(p => p.ScheduleDays).HasForeignKey(d => d.ScheduleId);
        });

      
            modelBuilder.Entity<StudentsInfor>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Students__1788CC4CCCC2035F");

            entity.ToTable("StudentsInfor");

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.StudentsCode).HasMaxLength(50);
            
            entity.HasOne(d => d.User).WithOne(p => p.StudentsInfor)
                .HasForeignKey<StudentsInfor>(d => d.UserId)
                .HasConstraintName("FK__StudentsI__UserI__52593CB8");

        });

        modelBuilder.Entity<StudyShift>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StudyShi__3214EC07D946077D");

            entity.Property(e => e.StudyShiftName).HasMaxLength(90);
            entity.HasData(
                new StudyShift { Id = 1, StudyShiftName = "Ca 1", StartTime = TimeOnly.Parse("07:15"), EndTime = TimeOnly.Parse("09:15") },
                new StudyShift { Id = 2, StudyShiftName = "Ca 2", StartTime = TimeOnly.Parse("09:25"), EndTime = TimeOnly.Parse("11:25") },
                new StudyShift { Id = 3, StudyShiftName = "Ca 3", StartTime = TimeOnly.Parse("12:00"), EndTime = TimeOnly.Parse("14:00") },
                new StudyShift { Id = 4, StudyShiftName = "Ca 4", StartTime = TimeOnly.Parse("14:10"), EndTime = TimeOnly.Parse("16:10") },
                new StudyShift { Id = 5, StudyShiftName = "Ca 5", StartTime = TimeOnly.Parse("16:20"), EndTime = TimeOnly.Parse("18:20") },
                new StudyShift { Id = 6, StudyShiftName = "Ca 6", StartTime = TimeOnly.Parse("18:30"), EndTime = TimeOnly.Parse("20:30") },
                new StudyShift { Id = 7, StudyShiftName = "Ca 7", StartTime = TimeOnly.MinValue, EndTime = TimeOnly.MaxValue });
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Subjects__3214EC0741AE97DC");

            entity.Property(e => e.SubjectName).HasMaxLength(200).IsRequired();

            entity.Property(e => e.Description).HasColumnType("nvarchar(max)");

            entity.Property(e => e.Subjectcode).HasMaxLength(50);

            entity.Property(e => e.Status).HasDefaultValue(true);
          
        });

        modelBuilder.Entity<TeachingRegistration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasDefaultValue(true);
           // entity.Property(e => e.IsConfirmed).HasDefaultValue(false);
            entity.Property(e => e.CreateAt).HasDefaultValueSql("getdate()");
            entity.Property(e => e.IsApproved).HasDefaultValue(ApprovedStatus.Pending);
            entity.Property(e => e.ApprovedDate).IsRequired(false);

            entity.HasOne(e => e.Approver)
                  .WithMany()
                  .HasForeignKey(e => e.ApprovedBy)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Teacher)
                  .WithMany() // hoặc .WithMany(t => t.TeachingRegistrations) nếu bạn có điều hướng ngược
                  .HasForeignKey(e => e.TeacherId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Schedule)
                  .WithMany() // hoặc .WithMany(c => c.TeachingRegistrations)
                  .HasForeignKey(e => e.ScheduleID)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__User__3214EC079BED008F");

            entity.ToTable("User");

            entity.Property(e => e.Id).HasDefaultValueSql("(newid())");
            entity.Property(e => e.CreateAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(90);
            entity.Property(e => e.PassWordHash).HasMaxLength(90);
            entity.Property(e => e.PhoneNumber).HasMaxLength(12);
            entity.Property(e => e.UserName).HasMaxLength(50);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("Roleid")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK__UserRole__Roleid__440B1D61"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("Userid")
                        .HasConstraintName("FK__UserRole__Userid__4316F928"),
                    j =>
                    {
                        j.HasKey("Userid", "Roleid").HasName("PK__UserRole__2F388C87DEAF1C0D");
                        j.ToTable("UserRole");
                        j.HasIndex(new[] { "Roleid" }, "IX_UserRole_Roleid");
                    });
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__UserProf__1788CC4C445D0929");

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.Address).HasMaxLength(225);
            entity.Property(e => e.Avatar).HasMaxLength(255);
            entity.Property(e => e.Dob).HasColumnName("DOB");
            entity.Property(e => e.FullName).HasMaxLength(90);
            entity.Property(e => e.UserCode).HasMaxLength(50);
            
            entity.HasOne(d => d.User).WithOne(p => p.UserProfile)
                .HasForeignKey<UserProfile>(d => d.UserId)
                .HasConstraintName("FK__UserProfi__UserI__46E78A0C");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
