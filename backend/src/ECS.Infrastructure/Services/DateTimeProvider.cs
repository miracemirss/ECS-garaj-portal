using ECS.Application.Common.Interfaces;

namespace ECS.Infrastructure.Services;

/// <summary>Real system clock implementation of <see cref="IDateTimeProvider"/>.</summary>
public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
