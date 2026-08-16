using Lubnan.Application.Abstractions;

namespace Lubnan.Infrastructure.Time;

/// <summary>The real clock. The only implementation outside tests.</summary>
internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
