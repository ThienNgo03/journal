using Journal.Databases.MongoDb;
using MongoDB.Driver;

namespace Journal.Workouts;

[ApiController]
[Authorize]
[AllowAnonymous]
[Route("api/workouts")]
public class Controller : ControllerBase
{
    private readonly IMessageBus _messageBus;
    private readonly ILogger<Controller> _logger;
    private readonly JournalDbContext _context;
    private readonly IHubContext<Hub> _hubContext;
    private readonly MongoDbContext _mongoDbContext;

    public Controller(IMessageBus messageBus,
                      ILogger<Controller> logger,
                      JournalDbContext context,
                      IHubContext<Hub> hubContext,
                      MongoDbContext mongoDbContext)
    {
        _messageBus = messageBus;
        _logger = logger;
        _context = context;
        _hubContext = hubContext;
        _mongoDbContext = mongoDbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] Get.Parameters parameters)
    {
        List<Get.Response> responses = new();
        int totalCount = 0;
        bool useMongoDb = false;

        // Parse includes if exists
        List<string> includes = new();
        bool hasInclude = !string.IsNullOrEmpty(parameters.Include);
        if (hasInclude)
        {
            includes = parameters.Include.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(i => i.Trim().ToLower())
                                         .ToList();
        }

        // ===== TRY MONGODB FIRST (ONLY IF INCLUDE EXISTS) =====
        if (hasInclude)
        {
            try
            {
                Console.WriteLine("Attempting to use MongoDB...");

                var mongoTask = Task.Run(async () =>
                {
                    var mongoQuery = _mongoDbContext.Workouts.AsQueryable();

                    // Handle Ids parameter
                    if (!string.IsNullOrEmpty(parameters.Ids))
                    {
                        var ids = parameters.Ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : (Guid?)null)
                            .Where(guid => guid.HasValue)
                            .Select(guid => guid.Value)
                            .ToList();
                        mongoQuery = mongoQuery.Where(x => ids.Contains(x.Id));
                    }

                    // Apply filters
                    if (parameters.ExerciseId.HasValue)
                        mongoQuery = mongoQuery.Where(x => x.ExerciseId == parameters.ExerciseId);

                    if (parameters.UserId.HasValue)
                        mongoQuery = mongoQuery.Where(x => x.UserId == parameters.UserId);

                    if (parameters.CreatedDate.HasValue)
                        mongoQuery = mongoQuery.Where(x => x.CreatedDate == parameters.CreatedDate);

                    if (parameters.LastUpdated.HasValue)
                        mongoQuery = mongoQuery.Where(x => x.LastUpdated == parameters.LastUpdated);

                    // Apply sorting
                    if (!string.IsNullOrEmpty(parameters.SortBy))
                    {
                        var sortBy = typeof(Databases.MongoDb.Collections.Workout.Collection)
                            .GetProperties()
                            .FirstOrDefault(p => p.Name.Equals(parameters.SortBy, StringComparison.OrdinalIgnoreCase))
                            ?.Name;

                        if (sortBy != null)
                        {
                            mongoQuery = parameters.SortOrder?.ToLower() == "desc"
                                ? mongoQuery.OrderByDescending(x => EF.Property<object>(x, sortBy))
                                : mongoQuery.OrderBy(x => EF.Property<object>(x, sortBy));
                        }
                    }

                    // Get total count before pagination
                    var count = await mongoQuery.CountAsync();

                    // Apply pagination
                    if (parameters.PageSize.HasValue && parameters.PageIndex.HasValue &&
                        parameters.PageSize > 0 && parameters.PageIndex >= 0)
                    {
                        mongoQuery = mongoQuery.Skip(parameters.PageIndex.Value * parameters.PageSize.Value)
                                               .Take(parameters.PageSize.Value);
                    }

                    var result = await mongoQuery.ToListAsync();

                    return new { TotalCount = count, Data = result };
                });

                // Wait for MongoDB with 3 second timeout
                if (await Task.WhenAny(mongoTask, Task.Delay(3000)) == mongoTask)
                {
                    var mongoResult = await mongoTask;
                    totalCount = mongoResult.TotalCount;

                    // Map MongoDB data to responses (1:1 mapping with includes)
                    foreach (var workout in mongoResult.Data)
                    {
                        var response = new Get.Response
                        {
                            Id = workout.Id,
                            ExerciseId = workout.ExerciseId,
                            UserId = workout.UserId,
                            CreatedDate = workout.CreatedDate,
                            LastUpdated = workout.LastUpdated,
                            Exercise = null,
                            WeekPlans = null
                        };

                        // Handle Exercise include
                        var exerciseInclude = includes.FirstOrDefault(i => i.StartsWith("exercise"));
                        if (exerciseInclude != null && workout.Exercise != null)
                        {
                            var exerciseIncludeParts = exerciseInclude.Split(".");
                            response.Exercise = new Get.Exercise
                            {
                                Id = workout.Exercise.Id,
                                Name = workout.Exercise.Name,
                                Description = workout.Exercise.Description,
                                Type = workout.Exercise.Type,
                                CreatedDate = workout.Exercise.CreatedDate,
                                LastUpdated = workout.Exercise.LastUpdated,
                                Muscles = null
                            };

                            // Handle exercise.muscles
                            if (exerciseIncludeParts.Length > 1 && exerciseIncludeParts[1] == "muscles" &&
                                workout.Exercise.Muscles != null && workout.Exercise.Muscles.Any())
                            {
                                response.Exercise.Muscles = workout.Exercise.Muscles.Select(m => new Get.Muscle
                                {
                                    Id = m.Id,
                                    Name = m.Name,
                                    CreatedDate = m.CreatedDate,
                                    LastUpdated = m.LastUpdated
                                }).ToList();
                            }
                        }

                        // Handle WeekPlans include
                        var weekPlansInclude = includes.FirstOrDefault(i => i.StartsWith("weekplans"));
                        if (weekPlansInclude != null && workout.WeekPlans != null && workout.WeekPlans.Any())
                        {
                            var weekPlansIncludeParts = weekPlansInclude.Split(".");
                            response.WeekPlans = new List<Get.WeekPlan>();

                            foreach (var wp in workout.WeekPlans)
                            {
                                var weekPlan = new Get.WeekPlan
                                {
                                    Id = wp.Id,
                                    DateOfWeek = wp.DateOfWeek,
                                    Time = wp.Time,
                                    WorkoutId = wp.WorkoutId,
                                    CreatedDate = wp.CreatedDate,
                                    LastUpdated = wp.LastUpdated,
                                    WeekPlanSets = null
                                };

                                // Handle weekplans.weekplansets
                                if (weekPlansIncludeParts.Length > 1 && weekPlansIncludeParts[1] == "weekplansets" &&
                                    wp.WeekPlanSets != null && wp.WeekPlanSets.Any())
                                {
                                    weekPlan.WeekPlanSets = wp.WeekPlanSets.Select(wps => new Get.WeekPlanSet
                                    {
                                        Id = wps.Id,
                                        Value = wps.Value,
                                        WeekPlanId = wps.WeekPlanId,
                                        CreatedById = wps.CreatedById,
                                        UpdatedById = wps.UpdatedById,
                                        LastUpdated = wps.LastUpdated,
                                        CreatedDate = wps.CreatedDate
                                    }).ToList();
                                }

                                response.WeekPlans.Add(weekPlan);
                            }
                        }

                        responses.Add(response);
                    }

                    useMongoDb = true;
                    Console.WriteLine($"✓ Successfully fetched {responses.Count} workouts from MongoDB");
                }
                else
                {
                    Console.WriteLine("✗ MongoDB timeout (3s exceeded), falling back to SQL");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ MongoDB failed: {ex.Message}, falling back to SQL");
            }
        }
        else
        {
            Console.WriteLine("No include parameter, using SQL directly...");
        }

        // ===== FALLBACK TO SQL =====
        if (!useMongoDb)
        {
            Console.WriteLine("Using SQL for filtering and data retrieval...");

            var sqlQuery = _context.Workouts.AsQueryable();
            var allQuery = sqlQuery;

            // Handle Ids parameter
            if (!string.IsNullOrEmpty(parameters.Ids))
            {
                var ids = parameters.Ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : (Guid?)null)
                    .Where(guid => guid.HasValue)
                    .Select(guid => guid.Value)
                    .ToList();
                sqlQuery = sqlQuery.Where(x => ids.Contains(x.Id));
            }

            // Apply filters
            if (parameters.ExerciseId.HasValue)
                sqlQuery = sqlQuery.Where(x => x.ExerciseId == parameters.ExerciseId);

            if (parameters.UserId.HasValue)
                sqlQuery = sqlQuery.Where(x => x.UserId == parameters.UserId);

            if (parameters.CreatedDate.HasValue)
                sqlQuery = sqlQuery.Where(x => x.CreatedDate == parameters.CreatedDate);

            if (parameters.LastUpdated.HasValue)
                sqlQuery = sqlQuery.Where(x => x.LastUpdated == parameters.LastUpdated);

            // Apply sorting
            if (!string.IsNullOrEmpty(parameters.SortBy))
            {
                var sortBy = typeof(Table)
                    .GetProperties()
                    .FirstOrDefault(p => p.Name.Equals(parameters.SortBy, StringComparison.OrdinalIgnoreCase))
                    ?.Name;

                if (sortBy != null)
                {
                    sqlQuery = parameters.SortOrder?.ToLower() == "desc"
                        ? sqlQuery.OrderByDescending(x => EF.Property<object>(x, sortBy))
                        : sqlQuery.OrderBy(x => EF.Property<object>(x, sortBy));
                }
            }

            // Get total count before pagination
            totalCount = await allQuery.CountAsync();

            // Apply pagination
            if (parameters.PageSize.HasValue && parameters.PageIndex.HasValue &&
                parameters.PageSize > 0 && parameters.PageIndex >= 0)
            {
                sqlQuery = sqlQuery.Skip(parameters.PageIndex.Value * parameters.PageSize.Value)
                                   .Take(parameters.PageSize.Value);
            }

            var sqlResult = await sqlQuery.AsNoTracking().ToListAsync();

            // Build base responses
            responses = sqlResult.Select(workout => new Get.Response
            {
                Id = workout.Id,
                ExerciseId = workout.ExerciseId,
                UserId = workout.UserId,
                CreatedDate = workout.CreatedDate,
                LastUpdated = workout.LastUpdated,
                Exercise = null,
                WeekPlans = null
            }).ToList();

            Console.WriteLine($"✓ Successfully fetched {responses.Count} workouts from SQL");

            // If Include exists, fetch nested data from SQL
            if (includes.Any())
            {
                var workoutIds = responses.Select(w => w.Id).ToList();

                foreach (var inc in includes)
                {
                    var includeParts = inc.Split(".");
                    var mainInclude = includeParts[0];

                    if (mainInclude == "exercise")
                    {
                        var exerciseIds = sqlResult.Select(w => w.ExerciseId).Distinct().ToList();
                        var exercises = await _context.Exercises
                            .Where(e => exerciseIds.Contains(e.Id))
                            .ToListAsync();

                        Dictionary<Guid, List<Get.Muscle>> musclesByExerciseId = new();

                        if (includeParts.Length > 1 && includeParts[1] == "muscles")
                        {
                            var exerciseMuscleRelations = await _context.ExerciseMuscles
                                .Where(em => exerciseIds.Contains(em.ExerciseId))
                                .ToListAsync();

                            var muscleIds = exerciseMuscleRelations.Select(em => em.MuscleId).Distinct().ToList();

                            if (muscleIds.Any())
                            {
                                var muscles = await _context.Muscles
                                    .Where(m => muscleIds.Contains(m.Id))
                                    .ToDictionaryAsync(m => m.Id);

                                foreach (var relation in exerciseMuscleRelations)
                                {
                                    if (!musclesByExerciseId.ContainsKey(relation.ExerciseId))
                                        musclesByExerciseId[relation.ExerciseId] = new List<Get.Muscle>();

                                    if (muscles.TryGetValue(relation.MuscleId, out var muscle))
                                    {
                                        musclesByExerciseId[relation.ExerciseId].Add(new Get.Muscle
                                        {
                                            Id = muscle.Id,
                                            Name = muscle.Name,
                                            CreatedDate = muscle.CreatedDate,
                                            LastUpdated = muscle.LastUpdated
                                        });
                                    }
                                }
                            }
                        }

                        var exerciseDict = exercises.ToDictionary(e => e.Id);

                        foreach (var response in responses)
                        {
                            if (exerciseDict.TryGetValue(response.ExerciseId, out var exercise))
                            {
                                response.Exercise = new Get.Exercise
                                {
                                    Id = exercise.Id,
                                    Name = exercise.Name,
                                    Description = exercise.Description,
                                    Type = exercise.Type,
                                    CreatedDate = exercise.CreatedDate,
                                    LastUpdated = exercise.LastUpdated,
                                    Muscles = musclesByExerciseId.ContainsKey(exercise.Id)
                                             ? musclesByExerciseId[exercise.Id]
                                             : null
                                };
                            }
                        }

                        Console.WriteLine("  ✓ Fetched exercises from SQL");
                    }
                    else if (mainInclude == "weekplans")
                    {
                        var weekPlans = await _context.WeekPlans
                            .Where(wp => workoutIds.Contains(wp.WorkoutId))
                            .ToListAsync();

                        var weekPlanIds = weekPlans.Select(wp => wp.Id).ToList();

                        Dictionary<Guid, List<Get.WeekPlanSet>> weekPlanSetsByWeekPlanId = new();

                        if (includeParts.Length > 1 && includeParts[1] == "weekplansets" && weekPlanIds.Any())
                        {
                            var weekPlanSets = await _context.WeekPlanSets
                                .Where(wps => weekPlanIds.Contains(wps.WeekPlanId))
                                .ToListAsync();

                            foreach (var set in weekPlanSets)
                            {
                                if (!weekPlanSetsByWeekPlanId.ContainsKey(set.WeekPlanId))
                                    weekPlanSetsByWeekPlanId[set.WeekPlanId] = new List<Get.WeekPlanSet>();

                                weekPlanSetsByWeekPlanId[set.WeekPlanId].Add(new Get.WeekPlanSet
                                {
                                    Id = set.Id,
                                    Value = set.Value,
                                    WeekPlanId = set.WeekPlanId,
                                    CreatedById = set.CreatedById,
                                    UpdatedById = set.UpdatedById,
                                    LastUpdated = set.LastUpdated,
                                    CreatedDate = set.CreatedDate
                                });
                            }
                        }

                        var workoutWeekPlans = new Dictionary<Guid, List<Get.WeekPlan>>();

                        foreach (var weekPlan in weekPlans)
                        {
                            if (!workoutWeekPlans.ContainsKey(weekPlan.WorkoutId))
                                workoutWeekPlans[weekPlan.WorkoutId] = new List<Get.WeekPlan>();

                            workoutWeekPlans[weekPlan.WorkoutId].Add(new Get.WeekPlan
                            {
                                Id = weekPlan.Id,
                                DateOfWeek = weekPlan.DateOfWeek,
                                Time = weekPlan.Time,
                                WorkoutId = weekPlan.WorkoutId,
                                CreatedDate = weekPlan.CreatedDate,
                                LastUpdated = weekPlan.LastUpdated,
                                WeekPlanSets = weekPlanSetsByWeekPlanId.ContainsKey(weekPlan.Id)
                                              ? weekPlanSetsByWeekPlanId[weekPlan.Id]
                                              : null
                            });
                        }

                        foreach (var response in responses)
                        {
                            if (workoutWeekPlans.ContainsKey(response.Id))
                            {
                                response.WeekPlans = workoutWeekPlans[response.Id];
                            }
                        }

                        Console.WriteLine("  ✓ Fetched weekplans from SQL");
                    }
                }
            }
        }

        // ===== BUILD PAGINATION RESULTS =====
        var paginationResults = new Builder<Get.Response>()
            .WithAll(totalCount)
            .WithIndex(parameters.PageIndex)
            .WithSize(parameters.PageSize)
            .WithTotal(responses.Count)
            .WithItems(responses)
            .Build();

        return Ok(paginationResults);
    }

    [HttpPost("sync-data-to-mongodb")]
    public async Task<IActionResult> SyncData()
    {
        var isMongoDbConnected = false;
        const int maxRetries = 3;
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
            return Ok("Workouts already synced to MongoDB. Skipping...");
        }

        try
        {
            var workouts = await _context.Workouts.AsNoTracking().ToListAsync();
            var workoutIds = workouts.Select(x => x.Id).ToList();

            if (!workoutIds.Any())
                return Ok("No workouts to sync.");

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

            return Ok($"Successfully synced {savedCount} workouts to MongoDB.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error syncing workouts: {ex.Message}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Post.Payload payload)
    {
        //if (User.Identity is null)
        //    return Unauthorized();

        //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //if (userId is null)
        //    return Unauthorized("User Id not found");

        var existingExercise = await _context.Exercises.FindAsync(payload.ExerciseId);
        if (existingExercise == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Exercise not found",
                Detail = $"Exercise with ID {payload.ExerciseId} does not exist.",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }
        var existingUser = await _context.Profiles.FindAsync(payload.UserId);
        if (existingUser == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found",
                Detail = $"User with ID {payload.UserId} does not exist.",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }
        var workout = new Table
        {
            Id = Guid.NewGuid(),
            ExerciseId = payload.ExerciseId,
            UserId = payload.UserId,
            CreatedDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        _context.Workouts.Add(workout);
        await _context.SaveChangesAsync();
        await _messageBus.PublishAsync(new Post.Messager.Message(workout, payload.WeekPlans));
        await _hubContext.Clients.All.SendAsync("workout-created", workout.Id);
        return CreatedAtAction(nameof(Get), workout.Id);
    }

    [HttpPatch]
    public async Task<IActionResult> Patch([FromQuery] Guid id,
                                       [FromBody] JsonPatchDocument<Table> patchDoc,
                                       CancellationToken cancellationToken = default!)
    {
        if (User.Identity is null)
            return Unauthorized();

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            return Unauthorized("User Id not found");

        var changes = new List<(string Path, object? Value)>();
        foreach (var op in patchDoc.Operations)
        {
            if (op.OperationType != OperationType.Replace && op.OperationType != OperationType.Test)
                return BadRequest("Only Replace and Test operations are allowed in this patch request.");
            changes.Add((op.path, op.value));
        }
            if (patchDoc is null)
            return BadRequest("Patch document cannot be null.");

        var entity = await _context.Workouts.FindAsync(id, cancellationToken);
        if (entity == null)
            return NotFound(new ProblemDetails
            {
                Title = "Workout not found",
                Detail = $"Workout with ID {id} does not exist.",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });

        patchDoc.ApplyTo(entity);

        entity.LastUpdated = DateTime.UtcNow;

        _context.Workouts.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        await _hubContext.Clients.All.SendAsync("workout-updated", entity.Id);
        await _messageBus.PublishAsync(new Patch.Messager.Message(entity, changes));
        return NoContent();
    }

    [HttpPut]
    public async Task<IActionResult> Put([FromBody] Update.Payload payload)
    {
        //if (User.Identity is null)
        //    return Unauthorized();

        //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //if (userId is null)
        //    return Unauthorized("User Id not found");

        var workout = await _context.Workouts.FindAsync(payload.Id);
        if (workout == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Workout not found",
                Detail = $"Workout with ID {payload.Id} does not exist.",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }
        var existingExercise = await _context.Exercises.FindAsync(payload.ExerciseId);
        if (existingExercise == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Exercise not found",
                Detail = $"Exercise with ID {payload.ExerciseId} does not exist.",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }
        var existingUser = await _context.Profiles.FindAsync(payload.UserId);
        if (existingUser == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "User not found",
                Detail = $"User with ID {payload.UserId} does not exist.",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        workout.ExerciseId = payload.ExerciseId;
        workout.UserId = payload.UserId;
        workout.LastUpdated = DateTime.UtcNow;
        _context.Workouts.Update(workout);
        await _context.SaveChangesAsync();
        await _messageBus.PublishAsync(new Update.Messager.Message(workout));
        await _hubContext.Clients.All.SendAsync("workout-updated", payload.Id);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] Delete.Parameters parameters)
    {
        //if (User.Identity is null)
        //    return Unauthorized();

        //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //if (userId is null)
        //    return Unauthorized("User Id not found");

        var workout = await _context.Workouts.FindAsync(parameters.Id);
        if (workout == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Workout not found",
                Detail = $"Workout with ID {parameters.Id} does not exist.",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        _context.Workouts.Remove(workout);
        await _context.SaveChangesAsync();
        await _messageBus.PublishAsync(new Delete.Messager.Message(parameters.Id, 
                                                                   parameters.IsWeekPlanDelete, 
                                                                   parameters.IsWeekPlanSetDelete));
        await _hubContext.Clients.All.SendAsync("workout-deleted", parameters.Id);
        return NoContent();
    }
}
