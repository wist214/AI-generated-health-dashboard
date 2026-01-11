IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110202155_InitialCreate'
)
BEGIN
    CREATE TABLE [DailySummaries] (
        [Id] bigint NOT NULL IDENTITY,
        [Date] date NOT NULL,
        [SleepScore] int NULL,
        [ReadinessScore] int NULL,
        [ActivityScore] int NULL,
        [Steps] int NULL,
        [CaloriesBurned] int NULL,
        [Weight] float NULL,
        [BodyFatPercentage] float NULL,
        [HeartRateAvg] int NULL,
        [HeartRateMin] int NULL,
        [HeartRateMax] int NULL,
        [TotalSleepDuration] int NULL,
        [DeepSleepDuration] int NULL,
        [RemSleepDuration] int NULL,
        [SleepEfficiency] int NULL,
        [HrvAverage] int NULL,
        [CaloriesConsumed] int NULL,
        [ProteinGrams] float NULL,
        [CarbsGrams] float NULL,
        [FatGrams] float NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2(7) NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_DailySummaries] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110202155_InitialCreate'
)
BEGIN
    CREATE TABLE [MetricTypes] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(100) NOT NULL,
        [Unit] nvarchar(50) NOT NULL,
        [Category] nvarchar(50) NOT NULL,
        [Description] nvarchar(500) NULL,
        [MinValue] float NULL,
        [MaxValue] float NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2(7) NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_MetricTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110202155_InitialCreate'
)
BEGIN
    CREATE TABLE [Sources] (
        [Id] bigint NOT NULL IDENTITY,
        [ProviderName] nvarchar(100) NOT NULL,
        [IsEnabled] bit NOT NULL DEFAULT CAST(1 AS bit),
        [LastSyncedAt] datetime2(7) NULL,
        [ApiKeyEncrypted] nvarchar(500) NULL,
        [ConfigurationJson] nvarchar(max) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2(7) NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Sources] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110202155_InitialCreate'
)
BEGIN
    CREATE TABLE [Measurements] (
        [Id] bigint NOT NULL IDENTITY,
        [MetricTypeId] int NOT NULL,
        [SourceId] bigint NOT NULL,
        [Value] float NOT NULL,
        [Timestamp] datetime2(7) NOT NULL,
        [RawDataJson] nvarchar(max) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (GETUTCDATE()),
        [UpdatedAt] datetime2(7) NOT NULL DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Measurements] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Measurements_MetricTypes_MetricTypeId] FOREIGN KEY ([MetricTypeId]) REFERENCES [MetricTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Measurements_Sources_SourceId] FOREIGN KEY ([SourceId]) REFERENCES [Sources] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110202155_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAt', N'Description', N'MaxValue', N'MinValue', N'Name', N'Unit', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[MetricTypes]'))
        SET IDENTITY_INSERT [MetricTypes] ON;
    EXEC(N'INSERT INTO [MetricTypes] ([Id], [Category], [CreatedAt], [Description], [MaxValue], [MinValue], [Name], [Unit], [UpdatedAt])
    VALUES (1, N''Sleep'', ''2026-01-01T00:00:00.0000000Z'', N''Overall sleep quality score'', 100.0E0, 0.0E0, N''sleep_score'', N''score'', ''2026-01-01T00:00:00.0000000Z''),
    (2, N''Sleep'', ''2026-01-01T00:00:00.0000000Z'', N''Total time asleep'', 86400.0E0, 0.0E0, N''total_sleep_duration'', N''seconds'', ''2026-01-01T00:00:00.0000000Z''),
    (3, N''Sleep'', ''2026-01-01T00:00:00.0000000Z'', N''Time in deep sleep stage'', 43200.0E0, 0.0E0, N''deep_sleep_duration'', N''seconds'', ''2026-01-01T00:00:00.0000000Z''),
    (4, N''Sleep'', ''2026-01-01T00:00:00.0000000Z'', N''Time in REM sleep stage'', 43200.0E0, 0.0E0, N''rem_sleep_duration'', N''seconds'', ''2026-01-01T00:00:00.0000000Z''),
    (5, N''Sleep'', ''2026-01-01T00:00:00.0000000Z'', N''Time in light sleep stage'', 43200.0E0, 0.0E0, N''light_sleep_duration'', N''seconds'', ''2026-01-01T00:00:00.0000000Z''),
    (6, N''Sleep'', ''2026-01-01T00:00:00.0000000Z'', N''Time awake during sleep period'', 43200.0E0, 0.0E0, N''awake_time'', N''seconds'', ''2026-01-01T00:00:00.0000000Z''),
    (7, N''Sleep'', ''2026-01-01T00:00:00.0000000Z'', N''Percentage of time in bed actually sleeping'', 100.0E0, 0.0E0, N''sleep_efficiency'', N''%'', ''2026-01-01T00:00:00.0000000Z''),
    (8, N''Sleep'', ''2026-01-01T00:00:00.0000000Z'', N''Time to fall asleep'', 7200.0E0, 0.0E0, N''sleep_latency'', N''seconds'', ''2026-01-01T00:00:00.0000000Z''),
    (9, N''Sleep'', ''2026-01-01T00:00:00.0000000Z'', N''Total time in bed'', 86400.0E0, 0.0E0, N''time_in_bed'', N''seconds'', ''2026-01-01T00:00:00.0000000Z''),
    (10, N''Sleep'', ''2026-01-01T00:00:00.0000000Z'', N''Number of restless periods during sleep'', 100.0E0, 0.0E0, N''restless_periods'', N''count'', ''2026-01-01T00:00:00.0000000Z''),
    (11, N''Sleep'', ''2026-01-01T00:00:00.0000000Z'', N''Daily readiness score'', 100.0E0, 0.0E0, N''readiness_score'', N''score'', ''2026-01-01T00:00:00.0000000Z''),
    (12, N''Sleep'', ''2026-01-01T00:00:00.0000000Z'', N''Body temperature deviation from baseline'', 5.0E0, -5.0E0, N''temperature_deviation'', N''°C'', ''2026-01-01T00:00:00.0000000Z''),
    (13, N''Activity'', ''2026-01-01T00:00:00.0000000Z'', N''Overall activity score'', 100.0E0, 0.0E0, N''activity_score'', N''score'', ''2026-01-01T00:00:00.0000000Z''),
    (14, N''Activity'', ''2026-01-01T00:00:00.0000000Z'', N''Total steps taken'', 100000.0E0, 0.0E0, N''steps'', N''steps'', ''2026-01-01T00:00:00.0000000Z''),
    (15, N''Activity'', ''2026-01-01T00:00:00.0000000Z'', N''Calories burned from activity'', 10000.0E0, 0.0E0, N''active_calories'', N''kcal'', ''2026-01-01T00:00:00.0000000Z''),
    (16, N''Activity'', ''2026-01-01T00:00:00.0000000Z'', N''Total calories burned including BMR'', 10000.0E0, 0.0E0, N''total_calories'', N''kcal'', ''2026-01-01T00:00:00.0000000Z''),
    (17, N''Activity'', ''2026-01-01T00:00:00.0000000Z'', N''Equivalent walking distance'', 100000.0E0, 0.0E0, N''equivalent_walking_distance'', N''meters'', ''2026-01-01T00:00:00.0000000Z''),
    (18, N''Activity'', ''2026-01-01T00:00:00.0000000Z'', N''Time in high intensity activity'', 86400.0E0, 0.0E0, N''high_activity_time'', N''seconds'', ''2026-01-01T00:00:00.0000000Z''),
    (19, N''Activity'', ''2026-01-01T00:00:00.0000000Z'', N''Time in medium intensity activity'', 86400.0E0, 0.0E0, N''medium_activity_time'', N''seconds'', ''2026-01-01T00:00:00.0000000Z''),
    (20, N''Activity'', ''2026-01-01T00:00:00.0000000Z'', N''Time in low intensity activity'', 86400.0E0, 0.0E0, N''low_activity_time'', N''seconds'', ''2026-01-01T00:00:00.0000000Z''),
    (21, N''Activity'', ''2026-01-01T00:00:00.0000000Z'', N''Time spent sedentary'', 86400.0E0, 0.0E0, N''sedentary_time'', N''seconds'', ''2026-01-01T00:00:00.0000000Z''),
    (22, N''Activity'', ''2026-01-01T00:00:00.0000000Z'', N''Number of inactivity alerts'', 50.0E0, 0.0E0, N''inactivity_alerts'', N''count'', ''2026-01-01T00:00:00.0000000Z''),
    (23, N''Body'', ''2026-01-01T00:00:00.0000000Z'', N''Body weight'', 300.0E0, 20.0E0, N''weight'', N''kg'', ''2026-01-01T00:00:00.0000000Z''),
    (24, N''Body'', ''2026-01-01T00:00:00.0000000Z'', N''Body Mass Index'', 60.0E0, 10.0E0, N''bmi'', N''kg/m²'', ''2026-01-01T00:00:00.0000000Z''),
    (25, N''Body'', ''2026-01-01T00:00:00.0000000Z'', N''Body fat percentage'', 70.0E0, 3.0E0, N''body_fat'', N''%'', ''2026-01-01T00:00:00.0000000Z''),
    (26, N''Body'', ''2026-01-01T00:00:00.0000000Z'', N''Body water percentage'', 80.0E0, 30.0E0, N''body_water'', N''%'', ''2026-01-01T00:00:00.0000000Z''),
    (27, N''Body'', ''2026-01-01T00:00:00.0000000Z'', N''Bone mass'', 10.0E0, 1.0E0, N''bone_mass'', N''kg'', ''2026-01-01T00:00:00.0000000Z''),
    (28, N''Body'', ''2026-01-01T00:00:00.0000000Z'', N''Metabolic age estimate'', 100.0E0, 10.0E0, N''metabolic_age'', N''years'', ''2026-01-01T00:00:00.0000000Z''),
    (29, N''Body'', ''2026-01-01T00:00:00.0000000Z'', N''Visceral fat level'', 30.0E0, 1.0E0, N''visceral_fat'', N''level'', ''2026-01-01T00:00:00.0000000Z''),
    (30, N''Body'', ''2026-01-01T00:00:00.0000000Z'', N''Basal metabolic rate'', 5000.0E0, 500.0E0, N''basal_metabolism'', N''kcal'', ''2026-01-01T00:00:00.0000000Z''),
    (31, N''Body'', ''2026-01-01T00:00:00.0000000Z'', N''Skeletal muscle mass'', 100.0E0, 10.0E0, N''skeletal_muscle_mass'', N''kg'', ''2026-01-01T00:00:00.0000000Z''),
    (32, N''Heart'', ''2026-01-01T00:00:00.0000000Z'', N''Average heart rate'', 220.0E0, 30.0E0, N''heart_rate_avg'', N''bpm'', ''2026-01-01T00:00:00.0000000Z''),
    (33, N''Heart'', ''2026-01-01T00:00:00.0000000Z'', N''Minimum/resting heart rate'', 220.0E0, 30.0E0, N''heart_rate_min'', N''bpm'', ''2026-01-01T00:00:00.0000000Z''),
    (34, N''Heart'', ''2026-01-01T00:00:00.0000000Z'', N''Maximum heart rate'', 220.0E0, 30.0E0, N''heart_rate_max'', N''bpm'', ''2026-01-01T00:00:00.0000000Z''),
    (35, N''Heart'', ''2026-01-01T00:00:00.0000000Z'', N''Average heart rate variability'', 300.0E0, 0.0E0, N''hrv_average'', N''ms'', ''2026-01-01T00:00:00.0000000Z''),
    (36, N''Heart'', ''2026-01-01T00:00:00.0000000Z'', N''Average breathing rate during sleep'', 30.0E0, 5.0E0, N''average_breath'', N''breaths/min'', ''2026-01-01T00:00:00.0000000Z''),
    (37, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Total calories consumed'', 10000.0E0, 0.0E0, N''calories_consumed'', N''kcal'', ''2026-01-01T00:00:00.0000000Z''),
    (38, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Protein intake'', 500.0E0, 0.0E0, N''protein'', N''g'', ''2026-01-01T00:00:00.0000000Z''),
    (39, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Carbohydrate intake'', 1000.0E0, 0.0E0, N''carbs'', N''g'', ''2026-01-01T00:00:00.0000000Z''),
    (40, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Fat intake'', 500.0E0, 0.0E0, N''fat'', N''g'', ''2026-01-01T00:00:00.0000000Z''),
    (41, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Fiber intake'', 200.0E0, 0.0E0, N''fiber'', N''g'', ''2026-01-01T00:00:00.0000000Z''),
    (42, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Sugar intake'', 500.0E0, 0.0E0, N''sugars'', N''g'', ''2026-01-01T00:00:00.0000000Z'');
    INSERT INTO [MetricTypes] ([Id], [Category], [CreatedAt], [Description], [MaxValue], [MinValue], [Name], [Unit], [UpdatedAt])
    VALUES (43, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Net carbs (carbs minus fiber)'', 1000.0E0, 0.0E0, N''net_carbs'', N''g'', ''2026-01-01T00:00:00.0000000Z''),
    (44, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Sodium intake'', 10000.0E0, 0.0E0, N''sodium'', N''mg'', ''2026-01-01T00:00:00.0000000Z''),
    (45, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Cholesterol intake'', 2000.0E0, 0.0E0, N''cholesterol'', N''mg'', ''2026-01-01T00:00:00.0000000Z''),
    (46, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Water intake'', 10000.0E0, 0.0E0, N''water'', N''ml'', ''2026-01-01T00:00:00.0000000Z''),
    (47, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Caffeine intake'', 2000.0E0, 0.0E0, N''caffeine'', N''mg'', ''2026-01-01T00:00:00.0000000Z''),
    (48, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Alcohol intake'', 500.0E0, 0.0E0, N''alcohol'', N''g'', ''2026-01-01T00:00:00.0000000Z''),
    (49, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Vitamin D intake'', 10000.0E0, 0.0E0, N''vitamin_d'', N''IU'', ''2026-01-01T00:00:00.0000000Z''),
    (50, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Vitamin B12 intake'', 1000.0E0, 0.0E0, N''vitamin_b12'', N''µg'', ''2026-01-01T00:00:00.0000000Z''),
    (51, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Iron intake'', 100.0E0, 0.0E0, N''iron'', N''mg'', ''2026-01-01T00:00:00.0000000Z''),
    (52, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Calcium intake'', 5000.0E0, 0.0E0, N''calcium'', N''mg'', ''2026-01-01T00:00:00.0000000Z''),
    (53, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Magnesium intake'', 2000.0E0, 0.0E0, N''magnesium'', N''mg'', ''2026-01-01T00:00:00.0000000Z''),
    (54, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Zinc intake'', 100.0E0, 0.0E0, N''zinc'', N''mg'', ''2026-01-01T00:00:00.0000000Z''),
    (55, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Potassium intake'', 10000.0E0, 0.0E0, N''potassium'', N''mg'', ''2026-01-01T00:00:00.0000000Z''),
    (56, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Vitamin C intake'', 5000.0E0, 0.0E0, N''vitamin_c'', N''mg'', ''2026-01-01T00:00:00.0000000Z''),
    (57, N''Nutrition'', ''2026-01-01T00:00:00.0000000Z'', N''Omega-3 fatty acids intake'', 50.0E0, 0.0E0, N''omega_3'', N''g'', ''2026-01-01T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Category', N'CreatedAt', N'Description', N'MaxValue', N'MinValue', N'Name', N'Unit', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[MetricTypes]'))
        SET IDENTITY_INSERT [MetricTypes] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110202155_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ApiKeyEncrypted', N'ConfigurationJson', N'CreatedAt', N'IsEnabled', N'LastSyncedAt', N'ProviderName', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Sources]'))
        SET IDENTITY_INSERT [Sources] ON;
    EXEC(N'INSERT INTO [Sources] ([Id], [ApiKeyEncrypted], [ConfigurationJson], [CreatedAt], [IsEnabled], [LastSyncedAt], [ProviderName], [UpdatedAt])
    VALUES (CAST(1 AS bigint), NULL, NULL, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), NULL, N''Oura'', ''2026-01-01T00:00:00.0000000Z''),
    (CAST(2 AS bigint), NULL, NULL, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), NULL, N''Picooc'', ''2026-01-01T00:00:00.0000000Z''),
    (CAST(3 AS bigint), NULL, NULL, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), NULL, N''Cronometer'', ''2026-01-01T00:00:00.0000000Z''),
    (CAST(4 AS bigint), NULL, NULL, ''2026-01-01T00:00:00.0000000Z'', CAST(1 AS bit), NULL, N''Manual'', ''2026-01-01T00:00:00.0000000Z'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ApiKeyEncrypted', N'ConfigurationJson', N'CreatedAt', N'IsEnabled', N'LastSyncedAt', N'ProviderName', N'UpdatedAt') AND [object_id] = OBJECT_ID(N'[Sources]'))
        SET IDENTITY_INSERT [Sources] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110202155_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_DailySummaries_Date] ON [DailySummaries] ([Date] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110202155_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Measurements_MetricType_Timestamp] ON [Measurements] ([MetricTypeId], [Timestamp] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110202155_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Measurements_Source_Timestamp] ON [Measurements] ([SourceId], [Timestamp] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110202155_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Measurements_Timestamp] ON [Measurements] ([Timestamp] DESC);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110202155_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Measurements_Unique] ON [Measurements] ([MetricTypeId], [SourceId], [Timestamp]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110202155_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_MetricTypes_Category] ON [MetricTypes] ([Category]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110202155_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_MetricTypes_Name] ON [MetricTypes] ([Name]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110202155_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Sources_IsEnabled] ON [Sources] ([IsEnabled]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110202155_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [UQ_Sources_ProviderName] ON [Sources] ([ProviderName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260110202155_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260110202155_InitialCreate', N'8.0.11');
END;
GO

COMMIT;
GO

