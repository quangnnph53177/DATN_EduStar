using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class fixteacherrediter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeachingRegistration_Semester",
                table: "TeachingRegistrations");

            migrationBuilder.DropForeignKey(
                name: "FK_TeachingRegistrations_DayOfWeeks_DayId",
                table: "TeachingRegistrations");

            migrationBuilder.DropForeignKey(
                name: "FK_TeachingRegistrations_Schedules_ClassId",
                table: "TeachingRegistrations");

            migrationBuilder.DropForeignKey(
                name: "FK_TeachingRegistrations_StudyShifts_StudyShiftId",
                table: "TeachingRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_TeachingRegistrations_ClassId",
                table: "TeachingRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_TeachingRegistrations_DayId",
                table: "TeachingRegistrations");

            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "TeachingRegistrations");

            migrationBuilder.DropColumn(
                name: "DayId",
                table: "TeachingRegistrations");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "TeachingRegistrations");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "TeachingRegistrations");

            migrationBuilder.RenameColumn(
                name: "StudyShiftId",
                table: "TeachingRegistrations",
                newName: "ScheduleID");

            migrationBuilder.RenameIndex(
                name: "IX_TeachingRegistrations_StudyShiftId",
                table: "TeachingRegistrations",
                newName: "IX_TeachingRegistrations_ScheduleID");

            migrationBuilder.AlterColumn<int>(
                name: "SemesterId",
                table: "TeachingRegistrations",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedBy",
                table: "TeachingRegistrations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "TeachingRegistrations",
                type: "datetime2",
                nullable: true,
                defaultValueSql: "getdate()");

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "TeachingRegistrations",
                type: "bit",
                nullable: true,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TeachingRegistrations_ApprovedBy",
                table: "TeachingRegistrations",
                column: "ApprovedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_TeachingRegistrations_Schedules_ScheduleID",
                table: "TeachingRegistrations",
                column: "ScheduleID",
                principalTable: "Schedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeachingRegistrations_Semesters_SemesterId",
                table: "TeachingRegistrations",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeachingRegistrations_User_ApprovedBy",
                table: "TeachingRegistrations",
                column: "ApprovedBy",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeachingRegistrations_Schedules_ScheduleID",
                table: "TeachingRegistrations");

            migrationBuilder.DropForeignKey(
                name: "FK_TeachingRegistrations_Semesters_SemesterId",
                table: "TeachingRegistrations");

            migrationBuilder.DropForeignKey(
                name: "FK_TeachingRegistrations_User_ApprovedBy",
                table: "TeachingRegistrations");

            migrationBuilder.DropIndex(
                name: "IX_TeachingRegistrations_ApprovedBy",
                table: "TeachingRegistrations");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "TeachingRegistrations");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "TeachingRegistrations");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "TeachingRegistrations");

            migrationBuilder.RenameColumn(
                name: "ScheduleID",
                table: "TeachingRegistrations",
                newName: "StudyShiftId");

            migrationBuilder.RenameIndex(
                name: "IX_TeachingRegistrations_ScheduleID",
                table: "TeachingRegistrations",
                newName: "IX_TeachingRegistrations_StudyShiftId");

            migrationBuilder.AlterColumn<int>(
                name: "SemesterId",
                table: "TeachingRegistrations",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClassId",
                table: "TeachingRegistrations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DayId",
                table: "TeachingRegistrations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "TeachingRegistrations",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "TeachingRegistrations",
                type: "datetime",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeachingRegistrations_ClassId",
                table: "TeachingRegistrations",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_TeachingRegistrations_DayId",
                table: "TeachingRegistrations",
                column: "DayId");

            migrationBuilder.AddForeignKey(
                name: "FK_TeachingRegistration_Semester",
                table: "TeachingRegistrations",
                column: "SemesterId",
                principalTable: "Semesters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeachingRegistrations_DayOfWeeks_DayId",
                table: "TeachingRegistrations",
                column: "DayId",
                principalTable: "DayOfWeeks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeachingRegistrations_Schedules_ClassId",
                table: "TeachingRegistrations",
                column: "ClassId",
                principalTable: "Schedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TeachingRegistrations_StudyShifts_StudyShiftId",
                table: "TeachingRegistrations",
                column: "StudyShiftId",
                principalTable: "StudyShifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
