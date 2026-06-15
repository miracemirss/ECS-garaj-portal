using ECS.Application.Common.Interfaces;
using ECS.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ECS.Persistence.Services;

/// <summary>
/// Produces sequential numbers using the PostgreSQL sequences defined in the SQL
/// migrations (seq_work_order_no, seq_stock_movement_no).
/// </summary>
public sealed class NumberGenerator : INumberGenerator
{
    private readonly ApplicationDbContext _context;

    public NumberGenerator(ApplicationDbContext context) => _context = context;

    public Task<string> NextWorkOrderNoAsync(CancellationToken cancellationToken = default)
        => NextAsync("seq_work_order_no", "WO", cancellationToken);

    public Task<string> NextStockMovementNoAsync(CancellationToken cancellationToken = default)
        => NextAsync("seq_stock_movement_no", "SM", cancellationToken);

    private async Task<string> NextAsync(string sequence, string prefix, CancellationToken cancellationToken)
    {
        // Sequence name is a fixed constant (no user input); concatenated (not an
        // interpolated literal) so the EF1002 raw-SQL analyzer is satisfied.
        var sql = "SELECT nextval('" + sequence + "') AS \"Value\"";
        var value = await _context.Database
            .SqlQueryRaw<long>(sql)
            .SingleAsync(cancellationToken);

        return $"{prefix}-{DateTime.UtcNow:yyyy}-{value:D6}";
    }
}
