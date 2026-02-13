CREATE SCHEMA journal;

CREATE TABLE journal.competition (
    "Id" uuid NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedById" uuid NOT NULL,
    "LastUpdated" timestamp with time zone,
    "UpdatedById" uuid,
    "Title" text NOT NULL,
    "Description" text NOT NULL,
    "ParticipantIds" uuid[] NOT NULL,
    "ExerciseId" uuid NOT NULL,
    "Location" text NOT NULL,
    "DateTime" timestamp with time zone NOT NULL,
    "Type" text NOT NULL,
    "RefereeId" uuid,
    CONSTRAINT "PK_competition" PRIMARY KEY ("Id")
);

CREATE TABLE journal."exercise-muscles" (
    "Id" uuid NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedById" uuid NOT NULL,
    "LastUpdated" timestamp with time zone,
    "UpdatedById" uuid,
    "ExerciseId" uuid NOT NULL,
    "MuscleId" uuid NOT NULL,
    CONSTRAINT "PK_exercise-muscles" PRIMARY KEY ("Id")
);

CREATE TABLE journal.exercises (
    "Id" uuid NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedById" uuid NOT NULL,
    "LastUpdated" timestamp with time zone,
    "UpdatedById" uuid,
    "Name" text NOT NULL,
    "Description" text NOT NULL,
    "Type" text NOT NULL,
    CONSTRAINT "PK_exercises" PRIMARY KEY ("Id")
);

CREATE TABLE journal.gadgets (
    "Id" uuid NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedById" uuid NOT NULL,
    "LastUpdated" timestamp with time zone,
    "UpdatedById" uuid,
    "Name" text NOT NULL,
    "Description" text NOT NULL,
    "Brand" text NOT NULL,
    "Date" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_gadgets" PRIMARY KEY ("Id")
);

CREATE TABLE journal."journey-gadgets" (
    "Id" uuid NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedById" uuid NOT NULL,
    "LastUpdated" timestamp with time zone,
    "UpdatedById" uuid,
    "JourneyId" uuid NOT NULL,
    "GadgetId" uuid NOT NULL,
    CONSTRAINT "PK_journey-gadgets" PRIMARY KEY ("Id")
);

CREATE TABLE journal."journey-users" (
    "Id" uuid NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedById" uuid NOT NULL,
    "LastUpdated" timestamp with time zone,
    "UpdatedById" uuid,
    "UserId" uuid NOT NULL,
    "JourneyId" uuid NOT NULL,
    CONSTRAINT "PK_journey-users" PRIMARY KEY ("Id")
);

CREATE TABLE journal.journeys (
    "Id" uuid NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedById" uuid NOT NULL,
    "LastUpdated" timestamp with time zone,
    "UpdatedById" uuid,
    "Content" text NOT NULL,
    "Location" text,
    "Date" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_journeys" PRIMARY KEY ("Id")
);

CREATE TABLE journal.meetups (
    "Id" uuid NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedById" uuid NOT NULL,
    "LastUpdated" timestamp with time zone,
    "UpdatedById" uuid,
    "ParticipantIds" text NOT NULL,
    "Title" text NOT NULL,
    "DateTime" timestamp with time zone NOT NULL,
    "Location" text NOT NULL,
    "CoverImage" text NOT NULL,
    CONSTRAINT "PK_meetups" PRIMARY KEY ("Id")
);

CREATE TABLE journal.muscles (
    "Id" uuid NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedById" uuid NOT NULL,
    "LastUpdated" timestamp with time zone,
    "UpdatedById" uuid,
    "Name" text NOT NULL,
    CONSTRAINT "PK_muscles" PRIMARY KEY ("Id")
);

CREATE TABLE journal.notes (
    "Id" uuid NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedById" uuid NOT NULL,
    "LastUpdated" timestamp with time zone,
    "UpdatedById" uuid,
    "JourneyId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Content" text NOT NULL,
    "Mood" text NOT NULL,
    "Date" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_notes" PRIMARY KEY ("Id")
);

CREATE TABLE journal.profiles (
    "Id" uuid NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedById" uuid NOT NULL,
    "LastUpdated" timestamp with time zone,
    "UpdatedById" uuid,
    "Name" text NOT NULL,
    "Email" text NOT NULL,
    "PhoneNumber" text NOT NULL,
    "ProfilePicture" text,
    CONSTRAINT "PK_profiles" PRIMARY KEY ("Id")
);

CREATE TABLE journal."solo-pools" (
    "Id" uuid NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedById" uuid NOT NULL,
    "LastUpdated" timestamp with time zone,
    "UpdatedById" uuid,
    "WinnerId" uuid NOT NULL,
    "LoserId" uuid NOT NULL,
    "CompetitionId" uuid NOT NULL,
    CONSTRAINT "PK_solo-pools" PRIMARY KEY ("Id")
);

CREATE TABLE journal.sports (
    "Id" uuid NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedById" uuid NOT NULL,
    "LastUpdated" timestamp with time zone,
    "UpdatedById" uuid,
    "Name" text NOT NULL,
    "Description" text NOT NULL,
    CONSTRAINT "PK_sports" PRIMARY KEY ("Id")
);

CREATE TABLE journal."team-pools" (
    "Id" uuid NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedById" uuid NOT NULL,
    "LastUpdated" timestamp with time zone,
    "UpdatedById" uuid,
    "ParticipantId" uuid NOT NULL,
    "Position" integer NOT NULL,
    "CompetitionId" uuid NOT NULL,
    CONSTRAINT "PK_team-pools" PRIMARY KEY ("Id")
);

CREATE TABLE journal."week-plan-sets" (
    "Id" uuid NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedById" uuid NOT NULL,
    "LastUpdated" timestamp with time zone,
    "UpdatedById" uuid,
    "WeekPlanId" uuid NOT NULL,
    "Value" integer NOT NULL,
    CONSTRAINT "PK_week-plan-sets" PRIMARY KEY ("Id")
);

CREATE TABLE journal."week-plans" (
    "Id" uuid NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedById" uuid NOT NULL,
    "LastUpdated" timestamp with time zone,
    "UpdatedById" uuid,
    "WorkoutId" uuid NOT NULL,
    "DateOfWeek" text NOT NULL,
    "Time" interval NOT NULL,
    CONSTRAINT "PK_week-plans" PRIMARY KEY ("Id")
);

CREATE TABLE journal."work-out-logs" (
    "Id" uuid NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedById" uuid NOT NULL,
    "LastUpdated" timestamp with time zone,
    "UpdatedById" uuid,
    "WorkoutId" uuid NOT NULL,
    "WorkoutDate" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_work-out-logs" PRIMARY KEY ("Id")
);

CREATE TABLE journal."workout-log-sets" (
    "Id" uuid NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedById" uuid NOT NULL,
    "LastUpdated" timestamp with time zone,
    "UpdatedById" uuid,
    "WorkoutLogId" uuid NOT NULL,
    "Value" integer NOT NULL,
    CONSTRAINT "PK_workout-log-sets" PRIMARY KEY ("Id")
);

CREATE TABLE journal.workouts (
    "Id" uuid NOT NULL,
    "CreatedDate" timestamp with time zone NOT NULL,
    "CreatedById" uuid NOT NULL,
    "LastUpdated" timestamp with time zone,
    "UpdatedById" uuid,
    "ExerciseId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    CONSTRAINT "PK_workouts" PRIMARY KEY ("Id")
);
