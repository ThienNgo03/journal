namespace Core.Subscriptions.Details.AllCompanies;

public class Response
{
    public Guid Id { get; set; }
    public string Company { get; set; } = string.Empty;
    public List<Subscription> Subscriptions { get; set; } = new();
}

public class Subscription
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
