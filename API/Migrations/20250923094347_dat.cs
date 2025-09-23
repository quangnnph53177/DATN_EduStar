using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class dat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DayOfWeeks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Weekdays = table.Column<int>(type: "int", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DayOfWeek__3214EC079A470A2D", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permission",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermissionName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Permissi__3214EC07B10A1991", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(28)", maxLength: 28, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Roles__3214EC0791E90BA9", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoomCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: true),
                    Device = table.Column<string>(type: "nvarchar(90)", maxLength: 90, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Rooms__3214EC073B8FA24F", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudyShifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudyShiftName = table.Column<string>(type: "nvarchar(90)", maxLength: 90, nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StudyShi__3214EC07D946077D", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Subjectcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: true, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Subjects__3214EC0741AE97DC", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PassWordHash = table.Column<string>(type: "nvarchar(90)", maxLength: 90, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(90)", maxLength: 90, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: true),
                    Statuss = table.Column<bool>(type: "bit", nullable: true),
                    IsConfirm = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__User__3214EC079BED008F", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermission",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__RolePerm__6400A1A882414258", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK__RolePermi__Permi__3C69FB99",
                        column: x => x.PermissionId,
                        principalTable: "Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__RolePermi__RoleI__3B75D760",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "auditlog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Userid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Active = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    NewData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PerformeBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__auditlog__3214EC074C8DE149", x => x.Id);
                    table.ForeignKey(
                        name: "FK__auditlog__Userid__50C3F9A6",
                        column: x => x.PerformeBy,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_auditlog_User_Userid",
                        column: x => x.Userid,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Complaints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ComplaintType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Statuss = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProofUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    ProcessedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    ProcessedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponseNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Complain__3214EC076F183F2B", x => x.Id);
                    table.ForeignKey(
                        name: "FK__Complaint__Proce__6EF57B66",
                        column: x => x.ProcessedBy,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__Complaint__Stude__6D0D32F4",
                        column: x => x.StudentId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassName = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SubjectId = table.Column<int>(type: "int", nullable: true),
                    RoomId = table.Column<int>(type: "int", nullable: true),
                    StudyShiftId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsersId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Schedules_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Schedules_StudyShiftId",
                        column: x => x.StudyShiftId,
                        principalTable: "StudyShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Schedules_Subject",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Schedules_Users",
                        column: x => x.UsersId,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StudentsInfor",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentsCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FaceFeatures = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Students__1788CC4CCCC2035F", x => x.UserId);
                    table.ForeignKey(
                        name: "FK__StudentsI__UserI__52593CB8",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(90)", maxLength: 90, nullable: true),
                    UserCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Gender = table.Column<bool>(type: "bit", nullable: true),
                    Avatar = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(225)", maxLength: 225, nullable: true),
                    DOB = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserProf__1788CC4C445D0929", x => x.UserId);
                    table.ForeignKey(
                        name: "FK__UserProfi__UserI__46E78A0C",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                columns: table => new
                {
                    Userid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Roleid = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__UserRole__2F388C87DEAF1C0D", x => new { x.Userid, x.Roleid });
                    table.ForeignKey(
                        name: "FK__UserRole__Roleid__440B1D61",
                        column: x => x.Roleid,
                        principalTable: "Roles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__UserRole__Userid__4316F928",
                        column: x => x.Userid,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attendance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchedulesId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SessionCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreateAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    Starttime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Endtime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Attendan__3214EC076DAB41EB", x => x.Id);
                    table.ForeignKey(
                        name: "FK__Attendanc__Sched__6477ECF3",
                        column: x => x.SchedulesId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__Attendanc__UserI__656C112C",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceDetailsComplaints",
                columns: table => new
                {
                    ComplaintId = table.Column<int>(type: "int", nullable: false),
                    SchedulesId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Attendan__740D898FC96B649B", x => x.ComplaintId);
                    table.ForeignKey(
                        name: "FK__Attendanc__Compl__71D1E811",
                        column: x => x.ComplaintId,
                        principalTable: "Complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__Attendanc__Sched__72C60C4A",
                        column: x => x.SchedulesId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ClassChange",
                columns: table => new
                {
                    ComplaintId = table.Column<int>(type: "int", nullable: false),
                    CurrentClassId = table.Column<int>(type: "int", nullable: true),
                    RequestedClassId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ClassCha__740D898F5A03A251", x => x.ComplaintId);
                    table.ForeignKey(
                        name: "FK__ClassChan__Compl__7A672E12",
                        column: x => x.ComplaintId,
                        principalTable: "Complaints",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__ClassChan__Curre__7B5B524B",
                        column: x => x.CurrentClassId,
                        principalTable: "Schedules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__ClassChan__Reque__7C4F7684",
                        column: x => x.RequestedClassId,
                        principalTable: "Schedules",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SchedulesInDays",
                columns: table => new
                {
                    ScheduleId = table.Column<int>(type: "int", nullable: false),
                    DayOfWeekkId = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchedulesInDays", x => new { x.ScheduleId, x.DayOfWeekkId });
                    table.ForeignKey(
                        name: "FK_SchedulesInDays_DayOfWeeks_DayOfWeekkId",
                        column: x => x.DayOfWeekkId,
                        principalTable: "DayOfWeeks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SchedulesInDays_Schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeachingRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleID = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    CreateAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()"),
                    IsApproved = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeachingRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeachingRegistrations_Schedules_ScheduleID",
                        column: x => x.ScheduleID,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeachingRegistrations_User_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeachingRegistrations_User_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleStudentsInfors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchedulesId = table.Column<int>(type: "int", nullable: false),
                    StudentsUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleStudentsInfors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleStudentsInfor_Schedule",
                        column: x => x.SchedulesId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScheduleStudentsInfor_Student",
                        column: x => x.StudentsUserId,
                        principalTable: "StudentsInfor",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttendanceId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", maxLength: 20, nullable: false),
                    CheckinTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Attendan__3214EC07676DEB47", x => x.Id);
                    table.ForeignKey(
                        name: "FK__Attendanc__Atten__6A30C649",
                        column: x => x.AttendanceId,
                        principalTable: "Attendance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__Attendanc__Stude__693CA210",
                        column: x => x.StudentId,
                        principalTable: "StudentsInfor",
                        principalColumn: "UserId");
                });

            migrationBuilder.InsertData(
                table: "DayOfWeeks",
                columns: new[] { "Id", "Weekdays" },
                values: new object[,]
                {
                    { 1, 0 },
                    { 2, 1 },
                    { 3, 2 },
                    { 4, 3 },
                    { 5, 4 },
                    { 6, 5 },
                    { 7, 6 }
                });

            migrationBuilder.InsertData(
                table: "Permission",
                columns: new[] { "Id", "PermissionName" },
                values: new object[,]
                {
                    { 1, "Create" },
                    { 2, "Detail" },
                    { 3, "Edit" },
                    { 4, "Search" },
                    { 5, "ProcessComplaint" },
                    { 6, "AddRole" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "RoleName" },
                values: new object[,]
                {
                    { 1, "Admin" },
                    { 2, "Teacher" },
                    { 3, "Student" }
                });

            migrationBuilder.InsertData(
                table: "Rooms",
                columns: new[] { "Id", "Capacity", "Device", "RoomCode" },
                values: new object[,]
                {
                    { 1, null, "Projector, Whiteboard", "Room 101" },
                    { 2, null, "Projector, Whiteboard", "Room 102" },
                    { 3, null, "Projector, Whiteboard", "Room 103" },
                    { 4, null, "Projector, Whiteboard", "Room 104" },
                    { 5, null, "Projector, Whiteboard", "Room 105" }
                });

            migrationBuilder.InsertData(
                table: "StudyShifts",
                columns: new[] { "Id", "EndTime", "StartTime", "StudyShiftName" },
                values: new object[,]
                {
                    { 1, new TimeOnly(9, 15, 0), new TimeOnly(7, 15, 0), "Ca 1" },
                    { 2, new TimeOnly(11, 25, 0), new TimeOnly(9, 25, 0), "Ca 2" },
                    { 3, new TimeOnly(14, 0, 0), new TimeOnly(12, 0, 0), "Ca 3" },
                    { 4, new TimeOnly(16, 10, 0), new TimeOnly(14, 10, 0), "Ca 4" },
                    { 5, new TimeOnly(18, 20, 0), new TimeOnly(16, 20, 0), "Ca 5" },
                    { 6, new TimeOnly(20, 30, 0), new TimeOnly(18, 30, 0), "Ca 6" },
                    { 7, new TimeOnly(23, 59, 59, 999).Add(TimeSpan.FromTicks(9999)), new TimeOnly(0, 0, 0), "Ca 7" }
                });

            migrationBuilder.InsertData(
                table: "RolePermission",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 1 },
                    { 3, 1 },
                    { 4, 1 },
                    { 5, 1 },
                    { 6, 1 },
                    { 2, 2 },
                    { 3, 2 },
                    { 4, 2 },
                    { 5, 2 },
                    { 2, 3 },
                    { 3, 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_SchedulesId",
                table: "Attendance",
                column: "SchedulesId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_UserId",
                table: "Attendance",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDetails_AttendanceId",
                table: "AttendanceDetails",
                column: "AttendanceId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDetails_StudentId",
                table: "AttendanceDetails",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceDetailsComplaints_SchedulesId",
                table: "AttendanceDetailsComplaints",
                column: "SchedulesId");

            migrationBuilder.CreateIndex(
                name: "IX_auditlog_PerformeBy",
                table: "auditlog",
                column: "PerformeBy");

            migrationBuilder.CreateIndex(
                name: "IX_auditlog_Userid",
                table: "auditlog",
                column: "Userid");

            migrationBuilder.CreateIndex(
                name: "IX_ClassChange_CurrentClassId",
                table: "ClassChange",
                column: "CurrentClassId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassChange_RequestedClassId",
                table: "ClassChange",
                column: "RequestedClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_ProcessedBy",
                table: "Complaints",
                column: "ProcessedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_StudentId",
                table: "Complaints",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_PermissionId",
                table: "RolePermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_ClassName",
                table: "Schedules",
                column: "ClassName");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_RoomId",
                table: "Schedules",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_StudyShiftId",
                table: "Schedules",
                column: "StudyShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_SubjectId",
                table: "Schedules",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_UsersId",
                table: "Schedules",
                column: "UsersId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleDays_DayOfWeekkId",
                table: "SchedulesInDays",
                column: "DayOfWeekkId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleStudentsInfors_SchedulesId",
                table: "ScheduleStudentsInfors",
                column: "SchedulesId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleStudentsInfors_StudentsUserId",
                table: "ScheduleStudentsInfors",
                column: "StudentsUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingRegistrations_ApprovedBy",
                table: "TeachingRegistrations",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingRegistrations_ScheduleID",
                table: "TeachingRegistrations",
                column: "ScheduleID");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingRegistrations_TeacherId",
                table: "TeachingRegistrations",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_Roleid",
                table: "UserRole",
                column: "Roleid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceDetails");

            migrationBuilder.DropTable(
                name: "AttendanceDetailsComplaints");

            migrationBuilder.DropTable(
                name: "auditlog");

            migrationBuilder.DropTable(
                name: "ClassChange");

            migrationBuilder.DropTable(
                name: "RolePermission");

            migrationBuilder.DropTable(
                name: "SchedulesInDays");

            migrationBuilder.DropTable(
                name: "ScheduleStudentsInfors");

            migrationBuilder.DropTable(
                name: "TeachingRegistrations");

            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropTable(
                name: "UserRole");

            migrationBuilder.DropTable(
                name: "Attendance");

            migrationBuilder.DropTable(
                name: "Complaints");

            migrationBuilder.DropTable(
                name: "Permission");

            migrationBuilder.DropTable(
                name: "DayOfWeeks");

            migrationBuilder.DropTable(
                name: "StudentsInfor");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Schedules");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "StudyShifts");

            migrationBuilder.DropTable(
                name: "Subjects");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
