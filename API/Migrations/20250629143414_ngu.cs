using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class ngu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Subjects_New");

            migrationBuilder.DropColumn(
                name: "Statuss",
                table: "AttendanceDetails");

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckinTime",
                table: "AttendanceDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "AttendanceDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "AttendanceDetails",
                type: "int",
                maxLength: 20,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "Endtime",
                table: "Attendance",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Starttime",
                table: "Attendance",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckinTime",
                table: "AttendanceDetails");

            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "AttendanceDetails");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AttendanceDetails");

            migrationBuilder.DropColumn(
                name: "Endtime",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "Starttime",
                table: "Attendance");

            migrationBuilder.AddColumn<string>(
                name: "Statuss",
                table: "AttendanceDetails",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Subjects_New",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NumberOfCredits = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: true),
                    SubjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Subjectcode = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Subjects__3214EC07BAD5CEE7", x => x.Id);
                });
        }
    }
}
