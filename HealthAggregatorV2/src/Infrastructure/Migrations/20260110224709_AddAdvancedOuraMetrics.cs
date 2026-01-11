using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedOuraMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CardiovascularAge",
                table: "DailySummaries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DailyStress",
                table: "DailySummaries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OptimalBedtimeEnd",
                table: "DailySummaries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OptimalBedtimeStart",
                table: "DailySummaries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResilienceLevel",
                table: "DailySummaries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SpO2Average",
                table: "DailySummaries",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Vo2Max",
                table: "DailySummaries",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkoutCount",
                table: "DailySummaries",
                type: "int",
                nullable: true);

            migrationBuilder.InsertData(
                table: "MetricTypes",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "MaxValue", "MinValue", "Name", "Unit", "UpdatedAt" },
                values: new object[,]
                {
                    { 58, "Sleep", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Daily stress level (0=restored, 1=normal, 2=stressful)", 2.0, 0.0, "daily_stress", "level", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 59, "Sleep", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Resilience level (0=limited, 1=adequate, 2=solid, 3=strong, 4=exceptional)", 4.0, 0.0, "resilience_level", "level", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 60, "Heart", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "VO2 Max cardio fitness estimate", 100.0, 10.0, "vo2_max", "ml/kg/min", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 61, "Heart", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cardiovascular age estimate", 150.0, 10.0, "cardiovascular_age", "years", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 62, "Heart", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Average SpO2 blood oxygen saturation", 100.0, 70.0, "spo2_average", "%", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 63, "Sleep", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Optimal bedtime start offset from midnight", 43200.0, -43200.0, "optimal_bedtime_start", "seconds", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 64, "Sleep", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Optimal bedtime end offset from midnight", 43200.0, -43200.0, "optimal_bedtime_end", "seconds", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 65, "Activity", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Number of workouts recorded", 20.0, 0.0, "workout_count", "count", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MetricTypes",
                keyColumn: "Id",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "MetricTypes",
                keyColumn: "Id",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "MetricTypes",
                keyColumn: "Id",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "MetricTypes",
                keyColumn: "Id",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "MetricTypes",
                keyColumn: "Id",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "MetricTypes",
                keyColumn: "Id",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "MetricTypes",
                keyColumn: "Id",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "MetricTypes",
                keyColumn: "Id",
                keyValue: 65);

            migrationBuilder.DropColumn(
                name: "CardiovascularAge",
                table: "DailySummaries");

            migrationBuilder.DropColumn(
                name: "DailyStress",
                table: "DailySummaries");

            migrationBuilder.DropColumn(
                name: "OptimalBedtimeEnd",
                table: "DailySummaries");

            migrationBuilder.DropColumn(
                name: "OptimalBedtimeStart",
                table: "DailySummaries");

            migrationBuilder.DropColumn(
                name: "ResilienceLevel",
                table: "DailySummaries");

            migrationBuilder.DropColumn(
                name: "SpO2Average",
                table: "DailySummaries");

            migrationBuilder.DropColumn(
                name: "Vo2Max",
                table: "DailySummaries");

            migrationBuilder.DropColumn(
                name: "WorkoutCount",
                table: "DailySummaries");
        }
    }
}
