using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace API.Migrations
{
    /// <inheritdoc />
    public partial class hia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "DayOfWeeks",
                columns: new[] { "Id", "Weekdays" },
                values: new object[,]
                {
                    { 1, "Monday" },
                    { 2, "Tuesday" },
                    { 3, "Wednesday" },
                    { 4, "Thursday" },
                    { 5, "Friday" },
                    { 6, "Saturday" },
                    { 7, "Sunday" }
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "DayOfWeeks",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "DayOfWeeks",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "DayOfWeeks",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "DayOfWeeks",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "DayOfWeeks",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "DayOfWeeks",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "DayOfWeeks",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "StudyShifts",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "StudyShifts",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "StudyShifts",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "StudyShifts",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "StudyShifts",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "StudyShifts",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "StudyShifts",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}
