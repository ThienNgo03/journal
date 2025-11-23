using Journal.Databases.MongoDb;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using OpenSearch.Client;

namespace Journal.Initializer;

[ApiController]
[Route("api/initializer")]
public class Controller(JournalDbContext journalDbContext,
                        IdentityContext identityDbContext,
                        IOpenSearchClient openSearchClient,
                        MongoDbContext mongoDbContext): ControllerBase
{
    private readonly JournalDbContext journalDbContext = journalDbContext;
    private readonly IdentityContext identityDbContext = identityDbContext;
    private Databases.App.SeedFactory journalSeeder = new Databases.App.SeedFactory();
    private Databases.Identity.SeedFactory identitySeeder = new Databases.Identity.SeedFactory();
    private readonly IOpenSearchClient openSearchClient = openSearchClient;
    private readonly MongoDbContext mongoDbContext = mongoDbContext;

    [HttpPost("add-init-admin")]
    [AllowAnonymous]
    public async Task<IActionResult> AddInitAdmin()
    {
        await identitySeeder.SeedAdmins(identityDbContext);
        await journalSeeder.SeedAdmins(journalDbContext);
        return Created(string.Empty, "SystemTester Created");
    }

    [HttpPost("add-init-master-data")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> AddInitMasterData()
    {
        await journalSeeder.SeedExercise(journalDbContext);
        await journalSeeder.SeedMuscle(journalDbContext);
        await journalSeeder.SeedExerciseMuscle(journalDbContext);
        await journalSeeder.CopyExercisesToMongoDb(journalDbContext, mongoDbContext);
        await journalSeeder.CopyWorkoutsToMongoDb(journalDbContext, mongoDbContext);
        await journalSeeder.CopyExercisesToOpenSearch(journalDbContext, openSearchClient);
        return Created(string.Empty, "Master data Seeded.");
    }

}
