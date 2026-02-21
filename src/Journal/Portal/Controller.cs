using Journal.Databases.MongoDb;
using OpenSearch.Client;

namespace Journal.Portal;

[ApiController]
[Authorize]
[Route("api/portal")]
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


    [HttpGet("side-bar")]
    public async Task<IActionResult> GetSidebar()
    {
        var exercisesPostgresCount = await _context.Exercises.CountAsync();
        var musclesPostgresCount = await _context.Muscles.CountAsync();
        var workoutsPostgresCount = await _context.Workouts.CountAsync();

        long exercisesOpenSearchCount = 0;
        try
        {
            var openSearchResponse = await _openSearchClient.CountAsync<Databases.OpenSearch.Indexes.Exercise.Index>(c => c.Index("exercises"));
            if (openSearchResponse.IsValid)
            {
                exercisesOpenSearchCount = openSearchResponse.Count;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get OpenSearch exercises count");
        }

        long exercisesMongoDbCount = 0;
        try
        {
            exercisesMongoDbCount = await _mongoDbContext.Exercises.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get MongoDB exercises count");
        }

        var items = new List<SideBarItem>
        {
            new()
            {
                Title = "Exercises",
                Url = "/exercises",
                Databases =
                [
                    new() { Title = "PostgreSQL", IsPrimary = true, MetaData = $"{exercisesPostgresCount} rows" },
                    new() { Title = "OpenSearch", IsPrimary = false, MetaData = $"{exercisesOpenSearchCount} objects" },
                    new() { Title = "MongoDb", IsPrimary = false, MetaData = $"{exercisesMongoDbCount} objects" },
                ]
            },
            new()
            {
                Title = "Muscles",
                Url = "/muscles",
                Databases =
                [
                    new() { Title = "PostgreSQL", IsPrimary = true, MetaData = $"{musclesPostgresCount} rows" },
                ]
            },
            new()
            {
                Title = "Workout",
                Url = "/workout",
                Databases =
                [
                    new() { Title = "PostgreSQL", IsPrimary = true, MetaData = $"{workoutsPostgresCount} rows" },
                ]
            }
        };

        return Ok(items);
    }
}

public class SideBarItem
{
    public string Title { get; set; }
    public string Url { get; set; }
    public List<SideBarSubItem> Databases { get; set; }
}

public class SideBarSubItem
{
    public string Title { get; set; }
    public bool IsPrimary { get; set; }
    public string MetaData { get; set; }
}