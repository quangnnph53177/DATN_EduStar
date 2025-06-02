using System;
using System.Collections.Generic;
using API.Models;
using Microsoft.EntityFrameworkCore;

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

    public virtual DbSet<Class> Classes { get; set; }

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

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

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

            entity.Property(e => e.Statuss).HasMaxLength(20);

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

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Classes__3214EC07EDA37A0C");

            entity.HasIndex(e => e.SubjectId, "IX_Classes_SubjectId");

            entity.Property(e => e.NameClass).HasMaxLength(90);
            entity.Property(e => e.Semester).HasMaxLength(10);

            entity.HasOne(d => d.Subject).WithMany(p => p.Classes)
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("FK__Classes__Subject__4F7CD00D");

            entity.HasMany(d => d.Students).WithMany(p => p.Classes)
                .UsingEntity<Dictionary<string, object>>(
                    "StudentInClass",
                    r => r.HasOne<StudentsInfor>().WithMany()
                        .HasForeignKey("StudentId")
                        .HasConstraintName("FK__StudentIn__Stude__5629CD9C"),
                    l => l.HasOne<Class>().WithMany()
                        .HasForeignKey("ClassId")
                        .HasConstraintName("FK__StudentIn__Class__5535A963"),
                    j =>
                    {
                        j.HasKey("ClassId", "StudentId").HasName("PK__StudentI__483575791F083B9F");
                        j.ToTable("StudentInClass");
                        j.HasIndex(new[] { "StudentId" }, "IX_StudentInClass_StudentId");
                    });
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
            entity.HasKey(e => e.Id).HasName("PK__DayOfWee__3214EC079A470A2D");

            entity.Property(e => e.Weekdays).HasMaxLength(10);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Permissi__3214EC07B10A1991");

            entity.ToTable("Permission");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3214EC0791E90BA9");

            entity.Property(e => e.RoleName).HasMaxLength(28);

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
                    });
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Rooms__3214EC073B8FA24F");

            entity.Property(e => e.Device).HasMaxLength(90);
            entity.Property(e => e.RoomCode).HasMaxLength(50);
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Schedule__3214EC0741018C97");

            entity.HasIndex(e => e.ClassId, "IX_Schedules_ClassId");

            entity.HasIndex(e => e.DayId, "IX_Schedules_DayId");

            entity.HasIndex(e => e.RoomId, "IX_Schedules_RoomId");

            entity.HasIndex(e => e.StudyShiftId, "IX_Schedules_StudyShiftId");

            entity.HasOne(d => d.Class).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.ClassId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK__Schedules__Class__5EBF139D");

            entity.HasOne(d => d.Day).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.DayId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Schedules__DayId__60A75C0F");

            entity.HasOne(d => d.Room).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Schedules__RoomI__5FB337D6");

            entity.HasOne(d => d.StudyShift).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.StudyShiftId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Schedules__Study__619B8048");
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
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Subjects__3214EC0750D0CDEB");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.SubjectName).HasMaxLength(200);
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
