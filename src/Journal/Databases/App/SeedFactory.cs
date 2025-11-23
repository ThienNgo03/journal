using ExcelDataReader;
using Journal.Databases.MongoDb;
using OpenSearch.Client;
using OpenSearch.Net;
using System.Data;
namespace Journal.Databases.App;

public class SeedFactory
{
    public async Task SeedExercise(JournalDbContext context)
    {
        if (context.Exercises.Any())
        {
            Console.WriteLine("✓ Exercises already seeded. Skipping...");
            return;
        }

        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exercises/Exercises.xlsx");
        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var config = new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration
            {
                UseHeaderRow = true
            }
        };

        var result = reader.AsDataSet(config);

        // Assuming the first sheet contains your data
        var table = result.Tables[0];

        var exercise = table.AsEnumerable()
                        .Where(x => x.Field<string>("Id") != null &&
                                    x.Field<string>("Name") != null &&
                                    x.Field<string>("Description") != null &&
                                    x.Field<string>("Type") != null)
                        .Select(row => new Exercises.Table
                        {
                            Id = Guid.Parse(row["Id"].ToString()!),
                            Name = row["Name"].ToString()!,
                            Description = row["Description"].ToString()!,
                            Type = row["Type"].ToString()!,
                            CreatedDate = DateTime.Now,
                        }).ToList();
        context.Exercises.AddRange(exercise);
        await context.SaveChangesAsync();
    }
    public async Task SeedMuscle(JournalDbContext context)
    {

        if (context.Muscles.Any())
        {
            Console.WriteLine("✓ Exercises already seeded. Skipping...");
            return;
        }


        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Muscles/Muscles.xlsx");
        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var config = new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration
            {
                UseHeaderRow = true
            }
        };

        var result = reader.AsDataSet(config);

        // Assuming the first sheet contains your data
        var table = result.Tables[0];

        var muscle = table.AsEnumerable()
                        .Where(x => x.Field<string>("Id") != null &&
                                    x.Field<string>("Name") != null)
                        .Select(row => new Muscles.Table
                        {
                            Id = Guid.Parse(row["Id"].ToString()!),
                            Name = row["Name"].ToString()!,
                            CreatedDate = DateTime.Now,
                        }).ToList();
        context.Muscles.AddRange(muscle);
        await context.SaveChangesAsync();
    }
    public async Task SeedExerciseMuscle(JournalDbContext context)
    {

        if (context.ExerciseMuscles.Any())
        {
            Console.WriteLine("✓ Exercises already seeded. Skipping...");
            return;
        }


        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ExerciseMuscles/ExerciseMuscles.xlsx");
        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var config = new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration
            {
                UseHeaderRow = true
            }
        };

        var result = reader.AsDataSet(config);

        // Assuming the first sheet contains your data
        var table = result.Tables[0];

        var exerciseMuscle = table.AsEnumerable()
                        .Where(x => x.Field<string>("Id") != null &&
                                    x.Field<string>("ExerciseId") != null &&
                                    x.Field<string>("MuscleId") != null)
                        .Select(row => new ExerciseMuscles.Table
                        {
                            Id = Guid.Parse(row["Id"].ToString()!),
                            ExerciseId = Guid.Parse(row["ExerciseId"].ToString()!),
                            MuscleId = Guid.Parse(row["MuscleId"].ToString()!),
                            CreatedDate = DateTime.Now,
                        }).ToList();
        context.ExerciseMuscles.AddRange(exerciseMuscle);
        await context.SaveChangesAsync();
    }

    public async Task SeedAdmins(JournalDbContext context)
    {
        var existsInJournal = await context.Profiles
            .AnyAsync(p => p.Id == Guid.Parse("fdfa4136-ada3-41dc-b16e-8fd9556d4574")
                        || p.Email == "systemtester@journal.com");

        if (existsInJournal)
        {
            Console.WriteLine("✓ Admin profile already seeded. Skipping...");
            return;
        }

        var id = Guid.Parse("fdfa4136-ada3-41dc-b16e-8fd9556d4574");
        var testAdmin = new Profiles.Table
        {
            Id = id,
            Name = "systemtester",
            Email = "systemtester@journal.com",
            PhoneNumber = "0564330462",
            ProfilePicture = null,
            CreatedDate = DateTime.UtcNow
        };
        context.Profiles.Add(testAdmin);
        await context.SaveChangesAsync();
    }

    public async Task CopyExercisesToMongoDb(JournalDbContext _context, MongoDbContext _mongoDbContext)
    {
        try
        {
            var isMongoDbConnected = false;
            const int maxRetries = 5;
            const int delayMilliseconds = 2000;
            const int timeoutMilliseconds = 10000;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var pingTask = Task.Run(async () =>
                    {
                        await _mongoDbContext.Exercises.FirstOrDefaultAsync();
                    });

                    if (await Task.WhenAny(pingTask, Task.Delay(timeoutMilliseconds)) == pingTask)
                    {
                        isMongoDbConnected = true;
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"⏳ MongoDB ping timeout (attempt {i + 1}/{maxRetries})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ MongoDB ping failed (attempt {i + 1}/{maxRetries}): {ex.Message}");
                }

                await Task.Delay(delayMilliseconds);
            }

            if (!isMongoDbConnected)
            {
                throw new Exception("❌ Unable to connect to MongoDB after multiple attempts.");
            }

            if (_mongoDbContext.Exercises.Any())
            {
                Console.WriteLine("✓ Exercises already synced to MongoDB. Skipping...");
                return;
            }

            var exercises = await _context.Exercises.AsNoTracking().ToListAsync();
            var exerciseIds = exercises.Select(x => x.Id).ToList();

            if (!exerciseIds.Any())
            {
                Console.WriteLine("No exercises found to sync.");
                return;
            }

            var exerciseMuscles = await _context.ExerciseMuscles
                .Where(x => exerciseIds.Contains(x.ExerciseId))
                .ToListAsync();

            var muscleIds = exerciseMuscles.Select(x => x.MuscleId).Distinct().ToList();
            var muscles = await _context.Muscles
                .Where(x => muscleIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            var exerciseMuscleGroups = exerciseMuscles
                .GroupBy(x => x.ExerciseId)
                .ToDictionary(g => g.Key, g => g.Select(em => em.MuscleId).ToList());

            // Create documents to sync
            var documentsToSync = new List<Journal.Databases.MongoDb.Collections.Exercise.Collection>();

            foreach (var exercise in exercises)
            {
                var musclesToSync = new List<Journal.Databases.MongoDb.Collections.Exercise.Muscle>();

                if (exerciseMuscleGroups.TryGetValue(exercise.Id, out var muscleIdsForExercise))
                {
                    musclesToSync = muscleIdsForExercise
                        .Where(muscleId => muscles.ContainsKey(muscleId))
                        .Select(muscleId => new Journal.Databases.MongoDb.Collections.Exercise.Muscle
                        {
                            Id = muscles[muscleId].Id,
                            Name = muscles[muscleId].Name,
                            CreatedDate = muscles[muscleId].CreatedDate,
                            LastUpdated = muscles[muscleId].LastUpdated
                        })
                        .ToList();
                }

                documentsToSync.Add(new Journal.Databases.MongoDb.Collections.Exercise.Collection
                {
                    Id = exercise.Id,
                    Name = exercise.Name,
                    Description = exercise.Description,
                    Type = exercise.Type,
                    Muscles = musclesToSync,
                    CreatedDate = exercise.CreatedDate,
                    LastUpdated = exercise.LastUpdated
                });
            }

            // Clear existing data and add new documents
            _mongoDbContext.Exercises.RemoveRange(_mongoDbContext.Exercises);
            _mongoDbContext.Exercises.AddRange(documentsToSync);

            var savedCount = await _mongoDbContext.SaveChangesAsync();
            Console.WriteLine($"✓ Synced {savedCount} exercises to MongoDB.");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error syncing exercises to MongoDB: {ex.Message}");
        }
    }

    public async Task CopyWorkoutsToMongoDb(JournalDbContext _context, MongoDbContext _mongoDbContext)
    {
        try
        {
            var isMongoDbConnected = false;
            const int maxRetries = 5;
            const int delayMilliseconds = 2000;
            const int timeoutMilliseconds = 10000;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var pingTask = Task.Run(async () =>
                    {
                        await _mongoDbContext.Workouts.FirstOrDefaultAsync();
                    });

                    if (await Task.WhenAny(pingTask, Task.Delay(timeoutMilliseconds)) == pingTask)
                    {
                        isMongoDbConnected = true;
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"⏳ MongoDB ping timeout (attempt {i + 1}/{maxRetries})");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ MongoDB ping failed (attempt {i + 1}/{maxRetries}): {ex.Message}");
                }

                await Task.Delay(delayMilliseconds);
            }

            if (!isMongoDbConnected)
            {
                throw new Exception("❌ Unable to connect to MongoDB after multiple attempts.");
            }

            if (_mongoDbContext.Workouts.Any())
            {
                Console.WriteLine("✓ Workouts already synced to MongoDB. Skipping...");
                return;
            }

            var workouts = await _context.Workouts.AsNoTracking().ToListAsync();
            var workoutIds = workouts.Select(x => x.Id).ToList();

            if (!workoutIds.Any())
            {
                Console.WriteLine("No workouts found to sync.");
                return;
            }

            var weekPlans = await _context.WeekPlans
                .Where(wp => workoutIds.Contains(wp.WorkoutId))
                .ToListAsync();

            var weekPlanIds = weekPlans.Select(wp => wp.Id).ToList();

            var weekPlanSets = await _context.WeekPlanSets
                .Where(wps => weekPlanIds.Contains(wps.WeekPlanId))
                .ToListAsync();

            var exerciseIds = workouts.Select(w => w.ExerciseId).Distinct().ToList();
            var exercises = await _context.Exercises
                .Where(e => exerciseIds.Contains(e.Id))
                .ToListAsync();

            var exerciseMuscles = await _context.ExerciseMuscles
                .Where(em => exerciseIds.Contains(em.ExerciseId))
                .ToListAsync();

            var muscleIds = exerciseMuscles.Select(em => em.MuscleId).Distinct().ToList();
            var muscles = await _context.Muscles
                .Where(m => muscleIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id);

            var musclesByExerciseId = exerciseMuscles
                .GroupBy(em => em.ExerciseId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Where(em => muscles.ContainsKey(em.MuscleId))
                          .Select(em => new Databases.MongoDb.Collections.Workout.Muscle
                          {
                              Id = muscles[em.MuscleId].Id,
                              Name = muscles[em.MuscleId].Name,
                              CreatedDate = muscles[em.MuscleId].CreatedDate,
                              LastUpdated = muscles[em.MuscleId].LastUpdated
                          }).ToList()
                );

            var weekPlanSetsByWeekPlanId = weekPlanSets
                .GroupBy(wps => wps.WeekPlanId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(wps => new Databases.MongoDb.Collections.Workout.WeekPlanSet
                    {
                        Id = wps.Id,
                        Value = wps.Value,
                        WeekPlanId = wps.WeekPlanId,
                        CreatedById = wps.CreatedById,
                        UpdatedById = wps.UpdatedById,
                        LastUpdated = wps.LastUpdated,
                        CreatedDate = wps.CreatedDate
                    }).ToList()
                );

            var weekPlansByWorkoutId = weekPlans
                .GroupBy(wp => wp.WorkoutId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(wp => new Databases.MongoDb.Collections.Workout.WeekPlan
                    {
                        Id = wp.Id,
                        DateOfWeek = wp.DateOfWeek,
                        Time = wp.Time,
                        WorkoutId = wp.WorkoutId,
                        CreatedDate = wp.CreatedDate,
                        LastUpdated = wp.LastUpdated,
                        WeekPlanSets = weekPlanSetsByWeekPlanId.GetValueOrDefault(wp.Id, new List<Databases.MongoDb.Collections.Workout.WeekPlanSet>())
                    }).ToList()
                );

            var workoutCollections = new List<Databases.MongoDb.Collections.Workout.Collection>();

            foreach (var workout in workouts)
            {
                var exercise = exercises.FirstOrDefault(e => e.Id == workout.ExerciseId);

                var workoutCollection = new Databases.MongoDb.Collections.Workout.Collection
                {
                    Id = workout.Id,
                    ExerciseId = workout.ExerciseId,
                    UserId = workout.UserId,
                    CreatedDate = workout.CreatedDate,
                    LastUpdated = workout.LastUpdated,
                    Exercise = exercise != null ? new Databases.MongoDb.Collections.Workout.Exercise
                    {
                        Id = exercise.Id,
                        Name = exercise.Name,
                        Description = exercise.Description,
                        Type = exercise.Type,
                        CreatedDate = exercise.CreatedDate,
                        LastUpdated = exercise.LastUpdated,
                        Muscles = musclesByExerciseId.GetValueOrDefault(exercise.Id, new List<Databases.MongoDb.Collections.Workout.Muscle>())
                    } : null,
                    WeekPlans = weekPlansByWorkoutId.GetValueOrDefault(workout.Id, new List<Databases.MongoDb.Collections.Workout.WeekPlan>())
                };

                workoutCollections.Add(workoutCollection);
            }

            _mongoDbContext.Workouts.RemoveRange(_mongoDbContext.Workouts);
            _mongoDbContext.Workouts.AddRange(workoutCollections);

            var savedCount = await _mongoDbContext.SaveChangesAsync();
            Console.WriteLine($"✓ Synced {savedCount} workouts to MongoDB.");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error syncing exercises to MongoDB: {ex.Message}");
        }
    }

    public async Task CopyExercisesToOpenSearch(JournalDbContext _context, IOpenSearchClient _openSearchClient)
    {
        try
        {
            var isOpenSearchReady = false;
            const int maxRetries = 5;
            const int delayMilliseconds = 12000;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var healthResponse = await _openSearchClient.Cluster.HealthAsync();

                    if (healthResponse != null && healthResponse.IsValid)
                    {
                        var status = healthResponse.Status.ToString()?.ToLowerInvariant();
                        if (status == "green" || status == "yellow")
                        {
                            isOpenSearchReady = true;
                            Console.WriteLine($"✓ OpenSearch is ready (status: {status}) (attempt {i + 1}/{maxRetries})");
                            break;
                        }
                        else
                        {
                            Console.WriteLine($"⏳ OpenSearch not ready yet (status: {status}) (attempt {i + 1}/{maxRetries})");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"⚠️ OpenSearch health response invalid (attempt {i + 1}/{maxRetries})");
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ OpenSearch health check error (attempt {i + 1}/{maxRetries}): {ex.Message}");
                }

                if (i < maxRetries - 1)
                {
                    await Task.Delay(delayMilliseconds);
                }
            }


            if (!isOpenSearchReady)
            {
                throw new Exception("❌ Unable to connect to OpenSearch after multiple attempts.");
            }

            if (await _openSearchClient.CountAsync<Databases.OpenSearch.Indexes.Exercise.Index>(c => c.Index("exercises")) is { Count: > 0 })
            {
                Console.WriteLine("✓ Exercises already indexed to OpenSearch. Skipping...");
                return;
            }

            var exercises = await _context.Exercises.AsNoTracking().ToListAsync();
            var exerciseIds = exercises.Select(x => x.Id).ToList();

            if (!exerciseIds.Any())
            {
                Console.WriteLine("No exercises found to index.");
                return;
            }

            var exerciseMuscles = await _context.ExerciseMuscles
                .Where(x => exerciseIds.Contains(x.ExerciseId))
                .ToListAsync();

            var muscleIds = exerciseMuscles.Select(x => x.MuscleId).Distinct().ToList();
            var muscles = await _context.Muscles
                .Where(x => muscleIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            var exerciseMuscleGroups = exerciseMuscles
                .GroupBy(x => x.ExerciseId)
                .ToDictionary(g => g.Key, g => g.Select(em => em.MuscleId).ToList());

            var documentsToIndex = new List<Databases.OpenSearch.Indexes.Exercise.Index>();

            foreach (var exercise in exercises)
            {
                var musclesToIndex = new List<Databases.OpenSearch.Indexes.Muscle.Index>();

                if (exerciseMuscleGroups.TryGetValue(exercise.Id, out var muscleIdsForExercise))
                {
                    musclesToIndex = muscleIdsForExercise
                        .Where(muscleId => muscles.ContainsKey(muscleId))
                        .Select(muscleId => new Databases.OpenSearch.Indexes.Muscle.Index
                        {
                            Id = muscles[muscleId].Id,
                            Name = muscles[muscleId].Name,
                            CreatedDate = muscles[muscleId].CreatedDate,
                            LastUpdated = muscles[muscleId].LastUpdated
                        })
                        .ToList();
                }

                documentsToIndex.Add(new Databases.OpenSearch.Indexes.Exercise.Index
                {
                    Id = exercise.Id,
                    Name = exercise.Name,
                    Description = exercise.Description,
                    Type = exercise.Type,
                    Muscles = musclesToIndex,
                    CreatedDate = exercise.CreatedDate,
                    LastUpdated = exercise.LastUpdated
                });
            }

            var bulkResponse = await _openSearchClient.BulkAsync(b => b
                .Index("exercises")
                .IndexMany(documentsToIndex, (descriptor, doc) => descriptor
                    .Id(doc.Id.ToString())
                    .Document(doc)
                )
            );

            if (!bulkResponse.IsValid)
            {
                Console.WriteLine($"❌ Bulk indexing failed: {bulkResponse.ServerError?.Error?.Reason ?? bulkResponse.DebugInformation}");
                return;
            }

            Console.WriteLine($"✓ Indexed {documentsToIndex.Count} exercises to OpenSearch.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error indexing exercises to OpenSearch: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
