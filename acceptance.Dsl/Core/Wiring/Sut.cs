using acceptance.Core.Drivers;

namespace acceptance.Core.Dsl;

public partial class Sut
{
    private readonly IChannel _channel;
    private readonly SpecContext _context;

    internal Sut(IChannel channel, SpecContext context)
    {
        _channel = channel;
        _context = context;
    }
}
