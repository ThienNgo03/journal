namespace Journal.Databases.Sql;

public class ConnectionStringBuilder
{
    private string _host;
    private int _port = 5432;
    private string _database;
    private string _username;
    private string _password;
    private bool _integratedSecurity = false;
    private bool _trustServerCertificate = false;

    public ConnectionStringBuilder WithHost(string host)
    {
        _host = host;
        return this;
    }

    public ConnectionStringBuilder WithPort(int port)
    {
        _port = port;
        return this;
    }

    public ConnectionStringBuilder WithDatabase(string database)    
    {
        _database = database;
        return this;
    }

    public ConnectionStringBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public ConnectionStringBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    public ConnectionStringBuilder WithIntegratedSecurity()
    {
        _integratedSecurity = true;
        return this;
    }

    public ConnectionStringBuilder WithTrustServerCertificate()
    {
        _trustServerCertificate = true;
        return this;
    }

    public string Build()
    {
        var connectionString = $"Host={_host};" +
               $"Port={_port};" +
               $"Database={_database};" +
               $"Username={_username};" +
               $"Password={_password};";

        if (_integratedSecurity)
            connectionString += "Integrated Security=true;";

        if (_trustServerCertificate)
            connectionString += "Trust Server Certificate=true;";

        return connectionString;
    }
}