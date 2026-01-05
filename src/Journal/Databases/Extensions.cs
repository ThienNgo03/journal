using Cassandra;
using Journal.Databases.CassandraCql;
using Journal.Databases.Identity;
using Journal.Databases.MongoDb;
using Journal.Databases.Sql;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using OpenSearch.Client;

namespace Journal.Databases;

public static class Extensions
{
    public static IServiceCollection AddDatabases(this IServiceCollection services, IConfiguration configuration)
    {
        #region Cassandra
        var cassandraDbConfig = configuration.GetSection("CassandraDb").Get<CassandraConfig>();
        if (cassandraDbConfig != null)
        {
            try
            {
                var cluster = Cluster.Builder()
                    .AddContactPoint(cassandraDbConfig.ContactPoint)
                    .WithPort(cassandraDbConfig.Port)
                    .WithLoadBalancingPolicy(new DCAwareRoundRobinPolicy(cassandraDbConfig.DataCenter))
                    .Build();

                // Nếu chưa có keyspace, có thể Connect() không tham số trước
                Cassandra.ISession session;
                if (!string.IsNullOrEmpty(cassandraDbConfig.Keyspace))
                {
                    session = cluster.Connect(cassandraDbConfig.Keyspace);
                }
                else
                {
                    session = cluster.Connect();
                }

                services.AddSingleton<CassandraCql.Context>();
                services.AddSingleton(session);

                Console.WriteLine("✅ Cassandra connected");
            }
            catch (Cassandra.NoHostAvailableException)
            {
                Console.WriteLine("⚠️ Cassandra not available, skipping...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Cassandra error: {ex.Message}, skipping...");
            }
        }
        #endregion
        var journalDbConfig = configuration.GetSection("JournalDb").Get<Sql.DbConfig>();
        var identityDbConfig = configuration.GetSection("IdentityDb").Get<Sql.DbConfig>();

        if (journalDbConfig == null)
        {
            throw new ArgumentNullException(nameof(journalDbConfig), "JournalDb configuration section is missing or invalid.");
        }
        if (identityDbConfig == null)
        {
            throw new ArgumentNullException(nameof(identityDbConfig), "IdentityDb configuration section is missing or invalid.");
        }
        var journalConnectionString = new Sql.ConnectionStringBuilder()
            .WithHost(journalDbConfig.Host)
            .WithPort(journalDbConfig.Port)
            .WithDatabase(journalDbConfig.Database)
            .WithUsername(journalDbConfig.Username)
            .WithPassword(journalDbConfig.Password)
            //.WithTrustedConnection()
            .WithTrustServerCertificate()
            .Build();

        var identityConnectionString = new Sql.ConnectionStringBuilder()
            .WithHost(identityDbConfig.Host)
            .WithPort(identityDbConfig.Port)
            .WithDatabase(identityDbConfig.Database)
            .WithUsername(identityDbConfig.Username)
            .WithPassword(identityDbConfig.Password)
            //.WithTrustedConnection()
            .WithTrustServerCertificate()
            .Build();

        services.AddDbContext<JournalDbContext>(x =>
        {
            x.EnableSensitiveDataLogging();
            x.UseSqlServer(journalConnectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null));
        });

        services.AddDbContext<IdentityContext>(x => 
            x.UseSqlServer(identityConnectionString, sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null)));

        services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<IdentityContext>()
                .AddDefaultTokenProviders();

        var mongoDbConfig = configuration.GetSection("MongoDb").Get<MongoDb.DbConfig>();
        if (mongoDbConfig == null)
        {
            throw new ArgumentNullException(nameof(mongoDbConfig), "MongoDb configuration section is missing or invalid.");
        }

        var mongoConnectionStringBuilder = new MongoDb.ConnectionStringBuilder()
            .WithHost(mongoDbConfig.Host)
            .WithPort(mongoDbConfig.Port)
            .WithDatabase(mongoDbConfig.Database)
            .WithUsername(mongoDbConfig.Username)
            .WithPassword(mongoDbConfig.Password)
            .WithAuthDatabase(mongoDbConfig.AuthDatabase);

        var mongoConnectionString = mongoConnectionStringBuilder.Build();
        var databaseName = mongoConnectionStringBuilder.GetDatabaseName();

        var client = new MongoClient(mongoConnectionString);
        var database = client.GetDatabase(databaseName);

        services.AddDbContext<MongoDbContext>(options =>
            options.UseMongoDB(client, database.DatabaseNamespace.DatabaseName)
        );

        var openSearchConfig = configuration.GetSection("OpenSearch").Get<OpenSearchConfig>();
        if (openSearchConfig == null)
        {
            throw new ArgumentNullException(nameof(openSearchConfig), "OpenSearch configuration section is missing or invalid.");
        }

        var openSearchConnectionString = new OpenSearch.ConnectionStringBuilder()
            .WithHost(openSearchConfig.Host)
            .WithPort(openSearchConfig.Port)
            .WithSsl()
            .Build();

        var connectionSettings = new ConnectionSettings(new Uri(openSearchConnectionString))
            .BasicAuthentication(openSearchConfig.Username, openSearchConfig.Password)
            .ServerCertificateValidationCallback((o, certificate, chain, errors) => true)
            .DisableDirectStreaming();

        var openSearchClient = new OpenSearchClient(connectionSettings);
        services.AddSingleton<IOpenSearchClient>(openSearchClient);

        return services;
    }
}