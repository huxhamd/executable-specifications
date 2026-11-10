using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

public partial class Api
{
    private readonly IChannel _channel;
    private readonly SpecContext _context;

    internal Api(IChannel channel, SpecContext context)
    {
        _channel = channel;
        _context = context;
    }
}
