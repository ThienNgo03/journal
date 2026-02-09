using BFF.Databases.App;
using BFF.Databases.Identity;
using BFF.Databases.Messages;
using Cassandra;
using Cassandra.Mapping;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BFF.Databases;

public static class Extension
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        //#region Cassandra
        //var cassandraDbConfig = configuration.GetSection("CassandraDb").Get<CassandraConfig>();
        //if (cassandraDbConfig != null)
        //{
        //    Cluster cluster = Cluster.Builder()
        //        .AddContactPoint(cassandraDbConfig.ContactPoint)
        //        .WithPort(cassandraDbConfig.Port)
        //        .Build();

        //    Cassandra.ISession session = cluster.Connect(cassandraDbConfig.Keyspace);
        //    services.AddSingleton<Context>();
        //    services.AddSingleton(session);
        //}
        //#endregion

        #region [ MSSQL ]

        var journalDbConfig = configuration.GetSection("JournalDb").Get<DbConfig>();
        var identityDbConfig = configuration.GetSection("IdentityDb").Get<DbConfig>();

        if (journalDbConfig == null)
        {
            throw new ArgumentNullException(nameof(journalDbConfig), "JournalDb configuration section is missing or invalid.");
        }
        if (identityDbConfig == null)
        {
            throw new ArgumentNullException(nameof(identityDbConfig), "IdentityDb configuration section is missing or invalid.");
        }
        var journalConnectionString = new ConnectionStringBuilder()
            .WithHost(journalDbConfig.Host)
            .WithPort(journalDbConfig.Port)
            .WithDatabase(journalDbConfig.Database)
            .WithUsername(journalDbConfig.Username)
            .WithPassword(journalDbConfig.Password)
            .WithTrustedConnection()
            .WithTrustServerCertificate()
            .Build();

        var identityConnectionString = new ConnectionStringBuilder()
            .WithHost(identityDbConfig.Host)
            .WithPort(identityDbConfig.Port)
            .WithDatabase(identityDbConfig.Database)
            .WithUsername(identityDbConfig.Username)
            .WithPassword(identityDbConfig.Password)
            .WithTrustedConnection()
            .WithTrustServerCertificate()
            .Build();

        services.AddDbContext<JournalDbContext>(x =>
        {
            x.EnableSensitiveDataLogging();
            x.UseSqlServer(journalConnectionString);
        });

        services.AddDbContext<IdentityContext>(x =>
        {
            x.EnableSensitiveDataLogging();
            x.UseSqlServer(identityConnectionString);
        });

        services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<IdentityContext>()
                .AddDefaultTokenProviders();

        #endregion
        return services;

    }
}
