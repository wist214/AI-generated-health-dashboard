using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailySummaries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    SleepScore = table.Column<int>(type: "int", nullable: true),
                    ReadinessScore = table.Column<int>(type: "int", nullable: true),
                    ActivityScore = table.Column<int>(type: "int", nullable: true),
                    Steps = table.Column<int>(type: "int", nullable: true),
                    CaloriesBurned = table.Column<int>(type: "int", nullable: true),
                    Weight = table.Column<double>(type: "float", nullable: true),
                    BodyFatPercentage = table.Column<double>(type: "float", nullable: true),
                    HeartRateAvg = table.Column<int>(type: "int", nullable: true),
                    HeartRateMin = table.Column<int>(type: "int", nullable: true),
                    HeartRateMax = table.Column<int>(type: "int", nullable: true),
                    TotalSleepDuration = table.Column<int>(type: "int", nullable: true),
                    DeepSleepDuration = table.Column<int>(type: "int", nullable: true),
                    RemSleepDuration = table.Column<int>(type: "int", nullable: true),
                    SleepEfficiency = table.Column<int>(type: "int", nullable: true),
                    HrvAverage = table.Column<int>(type: "int", nullable: true),
                    CaloriesConsumed = table.Column<int>(type: "int", nullable: true),
                    ProteinGrams = table.Column<double>(type: "float", nullable: true),
                    CarbsGrams = table.Column<double>(type: "float", nullable: true),
                    FatGrams = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailySummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MetricTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MinValue = table.Column<double>(type: "float", nullable: true),
                    MaxValue = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sources",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProviderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    ApiKeyEncrypted = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Measurements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MetricTypeId = table.Column<int>(type: "int", nullable: false),
                    SourceId = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2(7)", nullable: false),
                    RawDataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Measurements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Measurements_MetricTypes_MetricTypeId",
                        column: x => x.MetricTypeId,
                        principalTable: "MetricTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Measurements_Sources_SourceId",
                        column: x => x.SourceId,
                        principalTable: "Sources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "MetricTypes",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "MaxValue", "MinValue", "Name", "Unit", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Sleep", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Overall sleep quality score", 100.0, 0.0, "sleep_score", "score", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "Sleep", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Total time asleep", 86400.0, 0.0, "total_sleep_duration", "seconds", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "Sleep", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Time in deep sleep stage", 43200.0, 0.0, "deep_sleep_duration", "seconds", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "Sleep", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Time in REM sleep stage", 43200.0, 0.0, "rem_sleep_duration", "seconds", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "Sleep", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Time in light sleep stage", 43200.0, 0.0, "light_sleep_duration", "seconds", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, "Sleep", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Time awake during sleep period", 43200.0, 0.0, "awake_time", "seconds", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, "Sleep", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Percentage of time in bed actually sleeping", 100.0, 0.0, "sleep_efficiency", "%", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, "Sleep", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Time to fall asleep", 7200.0, 0.0, "sleep_latency", "seconds", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, "Sleep", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Total time in bed", 86400.0, 0.0, "time_in_bed", "seconds", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, "Sleep", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Number of restless periods during sleep", 100.0, 0.0, "restless_periods", "count", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, "Sleep", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Daily readiness score", 100.0, 0.0, "readiness_score", "score", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 12, "Sleep", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Body temperature deviation from baseline", 5.0, -5.0, "temperature_deviation", "°C", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 13, "Activity", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Overall activity score", 100.0, 0.0, "activity_score", "score", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 14, "Activity", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Total steps taken", 100000.0, 0.0, "steps", "steps", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 15, "Activity", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Calories burned from activity", 10000.0, 0.0, "active_calories", "kcal", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 16, "Activity", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Total calories burned including BMR", 10000.0, 0.0, "total_calories", "kcal", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 17, "Activity", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Equivalent walking distance", 100000.0, 0.0, "equivalent_walking_distance", "meters", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 18, "Activity", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Time in high intensity activity", 86400.0, 0.0, "high_activity_time", "seconds", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 19, "Activity", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Time in medium intensity activity", 86400.0, 0.0, "medium_activity_time", "seconds", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 20, "Activity", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Time in low intensity activity", 86400.0, 0.0, "low_activity_time", "seconds", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 21, "Activity", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Time spent sedentary", 86400.0, 0.0, "sedentary_time", "seconds", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 22, "Activity", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Number of inactivity alerts", 50.0, 0.0, "inactivity_alerts", "count", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 23, "Body", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Body weight", 300.0, 20.0, "weight", "kg", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 24, "Body", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Body Mass Index", 60.0, 10.0, "bmi", "kg/m²", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 25, "Body", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Body fat percentage", 70.0, 3.0, "body_fat", "%", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 26, "Body", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Body water percentage", 80.0, 30.0, "body_water", "%", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 27, "Body", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Bone mass", 10.0, 1.0, "bone_mass", "kg", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 28, "Body", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Metabolic age estimate", 100.0, 10.0, "metabolic_age", "years", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 29, "Body", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Visceral fat level", 30.0, 1.0, "visceral_fat", "level", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 30, "Body", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Basal metabolic rate", 5000.0, 500.0, "basal_metabolism", "kcal", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 31, "Body", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Skeletal muscle mass", 100.0, 10.0, "skeletal_muscle_mass", "kg", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 32, "Heart", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Average heart rate", 220.0, 30.0, "heart_rate_avg", "bpm", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 33, "Heart", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Minimum/resting heart rate", 220.0, 30.0, "heart_rate_min", "bpm", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 34, "Heart", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Maximum heart rate", 220.0, 30.0, "heart_rate_max", "bpm", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 35, "Heart", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Average heart rate variability", 300.0, 0.0, "hrv_average", "ms", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 36, "Heart", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Average breathing rate during sleep", 30.0, 5.0, "average_breath", "breaths/min", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 37, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Total calories consumed", 10000.0, 0.0, "calories_consumed", "kcal", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 38, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Protein intake", 500.0, 0.0, "protein", "g", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 39, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Carbohydrate intake", 1000.0, 0.0, "carbs", "g", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 40, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Fat intake", 500.0, 0.0, "fat", "g", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 41, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Fiber intake", 200.0, 0.0, "fiber", "g", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 42, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sugar intake", 500.0, 0.0, "sugars", "g", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 43, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Net carbs (carbs minus fiber)", 1000.0, 0.0, "net_carbs", "g", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 44, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sodium intake", 10000.0, 0.0, "sodium", "mg", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 45, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cholesterol intake", 2000.0, 0.0, "cholesterol", "mg", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 46, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Water intake", 10000.0, 0.0, "water", "ml", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 47, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Caffeine intake", 2000.0, 0.0, "caffeine", "mg", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 48, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Alcohol intake", 500.0, 0.0, "alcohol", "g", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 49, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Vitamin D intake", 10000.0, 0.0, "vitamin_d", "IU", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 50, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Vitamin B12 intake", 1000.0, 0.0, "vitamin_b12", "µg", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 51, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Iron intake", 100.0, 0.0, "iron", "mg", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 52, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Calcium intake", 5000.0, 0.0, "calcium", "mg", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 53, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Magnesium intake", 2000.0, 0.0, "magnesium", "mg", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 54, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Zinc intake", 100.0, 0.0, "zinc", "mg", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 55, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Potassium intake", 10000.0, 0.0, "potassium", "mg", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 56, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Vitamin C intake", 5000.0, 0.0, "vitamin_c", "mg", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 57, "Nutrition", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Omega-3 fatty acids intake", 50.0, 0.0, "omega_3", "g", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Sources",
                columns: new[] { "Id", "ApiKeyEncrypted", "ConfigurationJson", "CreatedAt", "IsEnabled", "LastSyncedAt", "ProviderName", "UpdatedAt" },
                values: new object[,]
                {
                    { 1L, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, "Oura", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2L, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, "Picooc", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3L, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, "Cronometer", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4L, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, null, "Manual", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "UQ_DailySummaries_Date",
                table: "DailySummaries",
                column: "Date",
                unique: true,
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_MetricType_Timestamp",
                table: "Measurements",
                columns: new[] { "MetricTypeId", "Timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_Source_Timestamp",
                table: "Measurements",
                columns: new[] { "SourceId", "Timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Measurements_Timestamp",
                table: "Measurements",
                column: "Timestamp",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "UQ_Measurements_Unique",
                table: "Measurements",
                columns: new[] { "MetricTypeId", "SourceId", "Timestamp" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetricTypes_Category",
                table: "MetricTypes",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "UQ_MetricTypes_Name",
                table: "MetricTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sources_IsEnabled",
                table: "Sources",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "UQ_Sources_ProviderName",
                table: "Sources",
                column: "ProviderName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailySummaries");

            migrationBuilder.DropTable(
                name: "Measurements");

            migrationBuilder.DropTable(
                name: "MetricTypes");

            migrationBuilder.DropTable(
                name: "Sources");
        }
    }
}
