using Npgsql;

namespace Journal.Databases.Identity;

public static class Initializer
{
    public static async Task InitDb(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityContext>();
        GenerateSchema(context);

        var seeder = new SeedFactory();
        await seeder.SeedAdmins(context);
    }

    private static void GenerateSchema(IdentityContext context)
    {
        try
        {
            Console.WriteLine("Checking database connection...");

            var canConnect = context.Database.CanConnect();

            if (canConnect)
            {
                Console.WriteLine("✓ Database connection successful");

                var pendingMigrations = context.Database.GetPendingMigrations().ToList();

                if (pendingMigrations.Any())
                {
                    Console.WriteLine($"Applying {pendingMigrations.Count} pending migration(s)...");
                    foreach (var migration in pendingMigrations)
                    {
                        Console.WriteLine($"  - {migration}");
                    }

                    context.Database.Migrate();
                    Console.WriteLine("✓ Migrations applied successfully.");
                }
                else
                {
                    Console.WriteLine("✓ Database is up to date. No pending migrations.");
                }
            }
            else
            {
                Console.WriteLine("Database does not exist. Creating and applying migrations...");
                context.Database.Migrate();
                Console.WriteLine("✓ Database created and migrations applied successfully.");
            }

            var appliedMigrations = context.Database.GetAppliedMigrations().ToList();
            Console.WriteLine($"Total applied migrations: {appliedMigrations.Count}");
        }
            catch (PostgresException ex) when (ex.SqlState == "42P04")
        {
            Console.WriteLine("Database already exists. Checking for pending migrations...");

            var pendingMigrations = context.Database.GetPendingMigrations().ToList();
            if (pendingMigrations.Any())
            {
                Console.WriteLine($"Found {pendingMigrations.Count} pending migration(s). Applying...");
                context.Database.Migrate();
                Console.WriteLine("✓ Migrations applied successfully.");
            }
            else
            {
                Console.WriteLine("✓ Database is up to date.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during database initialization: {ex.Message}");
            Console.WriteLine($"Error details: {ex.GetType().Name}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            throw;
        }
    }
}
