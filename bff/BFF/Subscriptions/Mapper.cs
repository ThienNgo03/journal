namespace BFF.Subscriptions;

public interface IMapper
{
    All.IMapper All { get; }
}

public class Mapper : IMapper
{
    public All.IMapper All { get; }
    public Mapper(All.IMapper all)
    {
        All = all;
    }
}
