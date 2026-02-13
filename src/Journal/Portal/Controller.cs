using Journal.Databases.MongoDb;
using OpenSearch.Client;

namespace Journal.Portal;

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


}
