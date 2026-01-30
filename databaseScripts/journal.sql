Create database "ssto-database"




CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

IF SCHEMA_ID(N'journal') IS NULL EXEC(N'CREATE SCHEMA [journal];');

CREATE TABLE [journal].[competition] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] uniqueidentifier NOT NULL,
    [LastUpdated] datetime2 NULL,
    [UpdatedById] uniqueidentifier NULL,
    [Title] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [ParticipantIds] nvarchar(max) NOT NULL,
    [ExerciseId] uniqueidentifier NOT NULL,
    [Location] nvarchar(max) NOT NULL,
    [DateTime] datetime2 NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    [RefereeId] uniqueidentifier NULL,
    CONSTRAINT [PK_competition] PRIMARY KEY ([Id])
);

CREATE TABLE [journal].[exercise-muscles] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] uniqueidentifier NOT NULL,
    [LastUpdated] datetime2 NULL,
    [UpdatedById] uniqueidentifier NULL,
    [ExerciseId] uniqueidentifier NOT NULL,
    [MuscleId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_exercise-muscles] PRIMARY KEY ([Id])
);

CREATE TABLE [journal].[exercises] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] uniqueidentifier NOT NULL,
    [LastUpdated] datetime2 NULL,
    [UpdatedById] uniqueidentifier NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Type] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_exercises] PRIMARY KEY ([Id])
);

CREATE TABLE [journal].[gadgets] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] uniqueidentifier NOT NULL,
    [LastUpdated] datetime2 NULL,
    [UpdatedById] uniqueidentifier NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [Brand] nvarchar(max) NOT NULL,
    [Date] datetime2 NOT NULL,
    CONSTRAINT [PK_gadgets] PRIMARY KEY ([Id])
);

CREATE TABLE [journal].[journey-gadgets] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] uniqueidentifier NOT NULL,
    [LastUpdated] datetime2 NULL,
    [UpdatedById] uniqueidentifier NULL,
    [JourneyId] uniqueidentifier NOT NULL,
    [GadgetId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_journey-gadgets] PRIMARY KEY ([Id])
);

CREATE TABLE [journal].[journey-users] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] uniqueidentifier NOT NULL,
    [LastUpdated] datetime2 NULL,
    [UpdatedById] uniqueidentifier NULL,
    [UserId] uniqueidentifier NOT NULL,
    [JourneyId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_journey-users] PRIMARY KEY ([Id])
);

CREATE TABLE [journal].[journeys] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] uniqueidentifier NOT NULL,
    [LastUpdated] datetime2 NULL,
    [UpdatedById] uniqueidentifier NULL,
    [Content] nvarchar(max) NOT NULL,
    [Location] nvarchar(max) NULL,
    [Date] datetime2 NOT NULL,
    CONSTRAINT [PK_journeys] PRIMARY KEY ([Id])
);

CREATE TABLE [journal].[meetups] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] uniqueidentifier NOT NULL,
    [LastUpdated] datetime2 NULL,
    [UpdatedById] uniqueidentifier NULL,
    [ParticipantIds] nvarchar(max) NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [DateTime] datetime2 NOT NULL,
    [Location] nvarchar(max) NOT NULL,
    [CoverImage] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_meetups] PRIMARY KEY ([Id])
);

CREATE TABLE [journal].[muscles] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] uniqueidentifier NOT NULL,
    [LastUpdated] datetime2 NULL,
    [UpdatedById] uniqueidentifier NULL,
    [Name] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_muscles] PRIMARY KEY ([Id])
);

CREATE TABLE [journal].[notes] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] uniqueidentifier NOT NULL,
    [LastUpdated] datetime2 NULL,
    [UpdatedById] uniqueidentifier NULL,
    [JourneyId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [Mood] nvarchar(max) NOT NULL,
    [Date] datetime2 NOT NULL,
    CONSTRAINT [PK_notes] PRIMARY KEY ([Id])
);

CREATE TABLE [journal].[profiles] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] uniqueidentifier NOT NULL,
    [LastUpdated] datetime2 NULL,
    [UpdatedById] uniqueidentifier NULL,
    [Name] nvarchar(max) NOT NULL,
    [Email] nvarchar(max) NOT NULL,
    [PhoneNumber] nvarchar(max) NOT NULL,
    [ProfilePicture] nvarchar(max) NULL,
    CONSTRAINT [PK_profiles] PRIMARY KEY ([Id])
);

CREATE TABLE [journal].[solo-pools] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] uniqueidentifier NOT NULL,
    [LastUpdated] datetime2 NULL,
    [UpdatedById] uniqueidentifier NULL,
    [WinnerId] uniqueidentifier NOT NULL,
    [LoserId] uniqueidentifier NOT NULL,
    [CompetitionId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_solo-pools] PRIMARY KEY ([Id])
);

CREATE TABLE [journal].[sports] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] uniqueidentifier NOT NULL,
    [LastUpdated] datetime2 NULL,
    [UpdatedById] uniqueidentifier NULL,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_sports] PRIMARY KEY ([Id])
);

CREATE TABLE [journal].[team-pools] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] uniqueidentifier NOT NULL,
    [LastUpdated] datetime2 NULL,
    [UpdatedById] uniqueidentifier NULL,
    [ParticipantId] uniqueidentifier NOT NULL,
    [Position] int NOT NULL,
    [CompetitionId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_team-pools] PRIMARY KEY ([Id])
);

CREATE TABLE [journal].[week-plan-sets] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] uniqueidentifier NOT NULL,
    [LastUpdated] datetime2 NULL,
    [UpdatedById] uniqueidentifier NULL,
    [WeekPlanId] uniqueidentifier NOT NULL,
    [Value] int NOT NULL,
    CONSTRAINT [PK_week-plan-sets] PRIMARY KEY ([Id])
);

CREATE TABLE [journal].[week-plans] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] uniqueidentifier NOT NULL,
    [LastUpdated] datetime2 NULL,
    [UpdatedById] uniqueidentifier NULL,
    [WorkoutId] uniqueidentifier NOT NULL,
    [DateOfWeek] nvarchar(max) NOT NULL,
    [Time] time NOT NULL,
    CONSTRAINT [PK_week-plans] PRIMARY KEY ([Id])
);

CREATE TABLE [journal].[work-out-logs] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] uniqueidentifier NOT NULL,
    [LastUpdated] datetime2 NULL,
    [UpdatedById] uniqueidentifier NULL,
    [WorkoutId] uniqueidentifier NOT NULL,
    [WorkoutDate] datetime2 NOT NULL,
    CONSTRAINT [PK_work-out-logs] PRIMARY KEY ([Id])
);

CREATE TABLE [journal].[workout-log-sets] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] uniqueidentifier NOT NULL,
    [LastUpdated] datetime2 NULL,
    [UpdatedById] uniqueidentifier NULL,
    [WorkoutLogId] uniqueidentifier NOT NULL,
    [Value] int NOT NULL,
    CONSTRAINT [PK_workout-log-sets] PRIMARY KEY ([Id])
);

CREATE TABLE [journal].[workouts] (
    [Id] uniqueidentifier NOT NULL,
    [CreatedDate] datetime2 NOT NULL,
    [CreatedById] uniqueidentifier NOT NULL,
    [LastUpdated] datetime2 NULL,
    [UpdatedById] uniqueidentifier NULL,
    [ExerciseId] uniqueidentifier NOT NULL,
    [UserId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_workouts] PRIMARY KEY ([Id])
);