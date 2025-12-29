using Journal.Databases.MongoDb;
using Journal.Workouts.Get;
using OpenSearch.Client;
using OpenSearch.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Journal.Exercises;

[ApiController]
[Authorize]
[AllowAnonymous]
[Route("api/exercises")]
public class Controller(
    IMessageBus messageBus,
    JournalDbContext context,
    ILogger<Controller> logger,
    IHubContext<Hub> hubContext,
    IOpenSearchClient openSearchClient,
    MongoDbContext mongoDbContext) : ControllerBase
{
    private readonly IMessageBus _messageBus = messageBus;
    private readonly JournalDbContext _context = context;
    private readonly ILogger<Controller> _logger = logger;
    private readonly IHubContext<Hub> _hubContext = hubContext;
    private readonly IOpenSearchClient _openSearchClient = openSearchClient;
    private readonly MongoDbContext _mongoDbContext = mongoDbContext;

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

        // Handle SearchTerm with OpenSearch
        List<Guid> searchIds = new();
        if (!string.IsNullOrEmpty(parameters.SearchTerm))
        {
            try
            {
                var searchResponse = await _openSearchClient.SearchAsync<Databases.OpenSearch.Indexes.Exercise.Index>(s => s
                    .Index("exercises")
                    .Source(src => src.Includes(i => i.Field(f => f.Id)))
                    .Query(q => q
                        .MultiMatch(mm => mm
                            .Query(parameters.SearchTerm)
                            .Fields(f => f
                                .Field(ff => ff.Name)
                                .Field(ff => ff.Description)
                                .Field(ff => ff.Muscles.First().Name)
                                .Field(ff => ff.Type)
                            )
                            .Fuzziness(Fuzziness.Auto)
                        )
                    )
                );

                if (searchResponse.IsValid)
                {
                    searchIds = searchResponse.Documents.Select(doc => doc.Id).ToList();
                }
                else
                {
                    Console.WriteLine($"OpenSearch query failed: {searchResponse.ServerError?.Error?.Reason ?? searchResponse.DebugInformation}");
                    Console.WriteLine("Continuing without search results...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OpenSearch error: {ex.Message}");
                Console.WriteLine("Continuing without search results...");
            }
        }

        // ===== TRY MONGODB FIRST (ONLY IF INCLUDE EXISTS) =====
        if (hasInclude)
        {
            try
            {
                Console.WriteLine("Attempting to use MongoDB...");

                var mongoTask = Task.Run(async () =>
                {
                    var mongoQuery = _mongoDbContext.Exercises.AsQueryable();

                    // Handle Ids from both SearchTerm and parameters.Ids
                    List<Guid> ids = new List<Guid>(searchIds);
                    if (!string.IsNullOrEmpty(parameters.Ids))
                    {
                        var parameterIds = parameters.Ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : (Guid?)null)
                            .Where(guid => guid.HasValue)
                            .Select(guid => guid.Value)
                            .ToList();
                        ids = ids.Union(parameterIds).ToList();
                    }

                    if (ids.Any())
                        mongoQuery = mongoQuery.Where(x => ids.Contains(x.Id));

                    // Apply filters
                    if (!string.IsNullOrEmpty(parameters.Name))
                        mongoQuery = mongoQuery.Where(x => x.Name.Contains(parameters.Name));

                    if (!string.IsNullOrEmpty(parameters.Description))
                        mongoQuery = mongoQuery.Where(x => x.Description.Contains(parameters.Description));

                    if (!string.IsNullOrEmpty(parameters.Type))
                        mongoQuery = mongoQuery.Where(x => x.Type.Contains(parameters.Type));

                    if (parameters.CreatedDate.HasValue)
                        mongoQuery = mongoQuery.Where(x => x.CreatedDate == parameters.CreatedDate);

                    if (parameters.LastUpdated.HasValue)
                        mongoQuery = mongoQuery.Where(x => x.LastUpdated == parameters.LastUpdated);

                    // Apply sorting
                    if (!string.IsNullOrEmpty(parameters.SortBy))
                    {
                        var sortBy = typeof(Databases.MongoDb.Collections.Exercise.Collection)
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
                    foreach (var exercise in mongoResult.Data)
                    {
                        var response = new Get.Response
                        {
                            Id = exercise.Id,
                            Name = exercise.Name,
                            Description = exercise.Description,
                            Type = exercise.Type,
                            CreatedDate = exercise.CreatedDate,
                            LastUpdated = exercise.LastUpdated,
                            Muscles = null
                        };

                        // Handle Muscles include
                        var musclesInclude = includes.FirstOrDefault(i => i.StartsWith("muscles"));
                        if (musclesInclude != null && exercise.Muscles != null && exercise.Muscles.Any())
                        {
                            response.Muscles = exercise.Muscles.Select(m => new Get.Muscle
                            {
                                Id = m.Id,
                                Name = m.Name,
                                CreatedDate = m.CreatedDate,
                                LastUpdated = m.LastUpdated
                            }).ToList();

                            // Apply muscle sorting if specified
                            if (!string.IsNullOrEmpty(parameters.MusclesSortBy))
                            {
                                var normalizeProp = typeof(Muscles.Table)
                                    .GetProperties()
                                    .FirstOrDefault(p => p.Name.Equals(parameters.MusclesSortBy, StringComparison.OrdinalIgnoreCase))
                                    ?.Name;

                                if (normalizeProp != null)
                                {
                                    var prop = typeof(Get.Muscle).GetProperty(normalizeProp);
                                    if (prop != null)
                                    {
                                        var isDescending = parameters.MusclesSortOrder?.ToLower() == "desc";
                                        response.Muscles = isDescending
                                            ? response.Muscles.OrderByDescending(m => prop.GetValue(m)).ToList()
                                            : response.Muscles.OrderBy(m => prop.GetValue(m)).ToList();
                                    }
                                }
                            }
                        }

                        responses.Add(response);
                    }

                    useMongoDb = true;
                    Console.WriteLine($"✓ Successfully fetched {responses.Count} exercises from MongoDB");
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

            var sqlQuery = _context.Exercises.AsQueryable();
            var allQuery = sqlQuery;

            // Handle Ids from both SearchTerm and parameters.Ids
            List<Guid> ids = new List<Guid>(searchIds);
            if (!string.IsNullOrEmpty(parameters.Ids))
            {
                var parameterIds = parameters.Ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => Guid.TryParse(id.Trim(), out var guid) ? guid : (Guid?)null)
                    .Where(guid => guid.HasValue)
                    .Select(guid => guid.Value)
                    .ToList();
                ids = ids.Union(parameterIds).ToList();
            }

            if (ids.Any())
                sqlQuery = sqlQuery.Where(x => ids.Contains(x.Id));

            // Apply filters
            if (!string.IsNullOrEmpty(parameters.Name))
                sqlQuery = sqlQuery.Where(x => x.Name.Contains(parameters.Name));

            if (!string.IsNullOrEmpty(parameters.Description))
                sqlQuery = sqlQuery.Where(x => x.Description.Contains(parameters.Description));

            if (!string.IsNullOrEmpty(parameters.Type))
                sqlQuery = sqlQuery.Where(x => x.Type.Contains(parameters.Type));

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
            responses = sqlResult.Select(exercise => new Get.Response
            {
                Id = exercise.Id,
                Name = exercise.Name,
                Description = exercise.Description,
                Type = exercise.Type,
                CreatedDate = exercise.CreatedDate,
                LastUpdated = exercise.LastUpdated,
                Muscles = null
            }).ToList();

            Console.WriteLine($"✓ Successfully fetched {responses.Count} exercises from SQL");

            // If Include exists, fetch nested data from SQL
            if (includes.Any() && includes.Any(inc => inc.Split(".")[0] == "muscles"))
            {
                var exerciseIds = responses.Select(x => x.Id).ToList();
                var exerciseMuscles = await _context.ExerciseMuscles
                    .Where(x => exerciseIds.Contains(x.ExerciseId))
                    .ToListAsync();

                var muscleIds = exerciseMuscles.Select(x => x.MuscleId).Distinct().ToList();

                if (muscleIds.Any())
                {
                    var muscles = await _context.Muscles
                        .Where(x => muscleIds.Contains(x.Id))
                        .ToDictionaryAsync(x => x.Id);

                    var exerciseMuscleGroups = exerciseMuscles
                        .GroupBy(x => x.ExerciseId)
                        .ToDictionary(g => g.Key, g => g.Select(em => em.MuscleId));

                    foreach (var response in responses)
                    {
                        if (!exerciseMuscleGroups.TryGetValue(response.Id, out var responseMuscleIds))
                            continue;

                        response.Muscles = responseMuscleIds
                            .Where(muscleId => muscles.ContainsKey(muscleId))
                            .Select(muscleId => new Get.Muscle
                            {
                                Id = muscles[muscleId].Id,
                                Name = muscles[muscleId].Name,
                                CreatedDate = muscles[muscleId].CreatedDate,
                                LastUpdated = muscles[muscleId].LastUpdated
                            })
                            .ToList();

                        // Apply muscle sorting if specified
                        if (!string.IsNullOrEmpty(parameters.MusclesSortBy) && response.Muscles?.Any() == true)
                        {
                            var normalizeProp = typeof(Muscles.Table)
                                .GetProperties()
                                .FirstOrDefault(p => p.Name.Equals(parameters.MusclesSortBy, StringComparison.OrdinalIgnoreCase))
                                ?.Name;

                            if (normalizeProp != null)
                            {
                                var prop = typeof(Get.Muscle).GetProperty(normalizeProp);
                                if (prop != null)
                                {
                                    var isDescending = parameters.MusclesSortOrder?.ToLower() == "desc";
                                    response.Muscles = isDescending
                                        ? response.Muscles.OrderByDescending(m => prop.GetValue(m)).ToList()
                                        : response.Muscles.OrderBy(m => prop.GetValue(m)).ToList();
                                }
                            }
                        }
                    }

                    Console.WriteLine("  ✓ Fetched muscles from SQL");
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
    public async Task<IActionResult> SyncDataToMongoDB()
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
            return Ok("Exercises already synced to MongoDB. Skipping...");
        }

        try
        {
            var exercises = await _context.Exercises.AsNoTracking().ToListAsync();
            var exerciseIds = exercises.Select(x => x.Id).ToList();

            if (!exerciseIds.Any())
                return Ok("No exercises to sync.");

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

            return Ok($"Successfully synced {savedCount} exercises to MongoDB.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error syncing exercises: {ex.Message}");
        }
    }

    [HttpPost("sync-open-search")]
    public async Task<IActionResult> SeedingOpenSearch()
    {
        var isOpenSearchConnected = false;
        const int maxRetries = 3;
        const int delayMilliseconds = 2000;
        const int timeoutMilliseconds = 10000;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var pingTask = Task.Run(async () =>
                {
                    await _openSearchClient.PingAsync(); // hoặc CountAsync nếu không có Ping
                });

                if (await Task.WhenAny(pingTask, Task.Delay(timeoutMilliseconds)) == pingTask)
                {
                    isOpenSearchConnected = true;
                    break;
                }
                else
                {
                    Console.WriteLine($"⏳ OpenSearch ping timeout (attempt {i + 1}/{maxRetries})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ OpenSearch ping failed (attempt {i + 1}/{maxRetries}): {ex.Message}");
            }

            await Task.Delay(delayMilliseconds);
        }

        if (!isOpenSearchConnected)
        {
            throw new Exception("❌ Unable to connect to OpenSearch after multiple attempts.");
        }

        if (await _openSearchClient.CountAsync<Databases.OpenSearch.Indexes.Exercise.Index>(c => c.Index("exercises")) is { Count: > 0 })
        {
            Console.WriteLine("✓ Exercises already indexed to OpenSearch. Skipping...");
            return Ok("Exercises already indexed to OpenSearch. Skipping...");
        }

        try
        {
            var exercises = await _context.Exercises.AsNoTracking().ToListAsync();
            var exerciseIds = exercises.Select(x => x.Id).ToList();

            if (!exerciseIds.Any())
                return Ok("No exercises to index.");

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

            // Create documents to index
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

            // Bulk index using high-level client
            var bulkResponse = await _openSearchClient.BulkAsync(b => b
                .Index("exercises")
                .IndexMany(documentsToIndex, (descriptor, doc) => descriptor
                    .Id(doc.Id.ToString())
                    .Document(doc)
                )
            );

            if (!bulkResponse.IsValid)
            {
                return StatusCode(500, $"Bulk indexing failed: {bulkResponse.ServerError?.Error?.Reason ?? bulkResponse.DebugInformation}");
            }

            return Ok($"Successfully indexed {exercises.Count} exercises.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred during indexing: {ex.Message}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Post.Payload payload)
    {
        // if (User.Identity is null)
        //    return Unauthorized();

        //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //if (userId is null)
        //    return Unauthorized("User Id not found");

        Table exercise = new()
        {
            Id = Guid.NewGuid(),
            Name = payload.Name,
            Description = payload.Description,
            Type = payload.Type,
            CreatedDate = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();
        await _messageBus.PublishAsync(new Post.Messager.Message(exercise));
        await _hubContext.Clients.All.SendAsync("exercise-created", exercise.Id);
        return CreatedAtAction(nameof(Get), exercise.Id);
    }

    [HttpPut]

    public async Task<IActionResult> Put([FromBody] Update.Payload payload)
    {
        // if (User.Identity is null)
        //    return Unauthorized();

        //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //if (userId is null)
        //    return Unauthorized("User Id not found");

        var exercise = await _context.Exercises.FindAsync(payload.Id);
        if (exercise == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Exercise not found",
                Detail = $"Exercise with ID {payload.Id} does not exist.",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        exercise.Name = payload.Name;
        exercise.Description = payload.Description;
        exercise.Type = payload.Type;
        exercise.LastUpdated = DateTime.UtcNow;
        _context.Exercises.Update(exercise);
        await _context.SaveChangesAsync();
        await _messageBus.PublishAsync(new Update.Messager.Message(exercise));
        await _hubContext.Clients.All.SendAsync("exercise-updated", payload.Id);
        return NoContent();
    }

    [HttpPatch]
    public async Task<IActionResult> Patch([FromQuery] Guid id,
                                           [FromBody] JsonPatchDocument<Table> patchDoc,
                                           CancellationToken cancellationToken = default!)
    {
        //if (User.Identity is null)
        //    return Unauthorized();

        //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //if (userId is null)
        //    return Unauthorized("User Id not found");

        var changes = new List<(string Path, object? Value)>();

        foreach (var op in patchDoc.Operations)
        {
            if (op.OperationType != OperationType.Replace && op.OperationType != OperationType.Test)
                return BadRequest("Only Replace and Test operations are allowed in this patch request.");
            changes.Add((op.path, op.value));
        }


        if (patchDoc is null)
            return BadRequest("Patch document cannot be null.");

        var entity = await _context.Exercises.FindAsync(id, cancellationToken);
        if (entity == null)
            return NotFound(new ProblemDetails
            {
                Title = "Exercise not found",
                Detail = $"Exercise with ID {id} does not exist.",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });

        patchDoc.ApplyTo(entity);
        //take the column had changed and it value so i can send it to messagebus to sync the data in Opensearch database

        entity.LastUpdated = DateTime.UtcNow;

        _context.Exercises.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        await _hubContext.Clients.All.SendAsync("exercise-updated", entity.Id);
        await _messageBus.PublishAsync(new Patch.Messager.Message(entity, changes));
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

        var exercise = await _context.Exercises.FindAsync(parameters.Id);
        if (exercise == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Exercise not found",
                Detail = $"Exercise with ID {parameters.Id} does not exist.",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }

        _context.Exercises.Remove(exercise);
        await _context.SaveChangesAsync();
        await _messageBus.PublishAsync(new Delete.Messager.Message(parameters.Id, parameters.IsDeleteWorkouts));
        await _hubContext.Clients.All.SendAsync("exercise-deleted", parameters.Id);
        return NoContent();
    }

}