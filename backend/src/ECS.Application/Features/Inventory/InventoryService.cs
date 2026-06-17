using System.Linq.Expressions;
using ECS.Application.Common;
using ECS.Application.Common.Interfaces;
using ECS.Application.Features.Alerts;
using ECS.Application.Features.Inventory.Dtos;
using ECS.Domain.Entities;
using ECS.Domain.Exceptions;
using ECS.Shared.Pagination;
using ECS.Shared.Results;
using FluentValidation;

namespace ECS.Application.Features.Inventory;

public sealed class InventoryService : IInventoryService
{
    private readonly IRepository<Part> _parts;
    private readonly IRepository<StockMovement> _movements;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogService _audit;
    private readonly IAlertService _alerts;
    private readonly INumberGenerator _numbers;
    private readonly IValidator<CreatePartRequest> _createValidator;
    private readonly IValidator<UpdatePartRequest> _updateValidator;
    private readonly IValidator<ReceiveStockRequest> _receiveValidator;
    private readonly IValidator<IssueStockRequest> _issueValidator;
    private readonly IValidator<AdjustStockRequest> _adjustValidator;

    public InventoryService(
        IRepository<Part> parts,
        IRepository<StockMovement> movements,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        ICurrentUserService currentUser,
        IAuditLogService audit,
        IAlertService alerts,
        INumberGenerator numbers,
        IValidator<CreatePartRequest> createValidator,
        IValidator<UpdatePartRequest> updateValidator,
        IValidator<ReceiveStockRequest> receiveValidator,
        IValidator<IssueStockRequest> issueValidator,
        IValidator<AdjustStockRequest> adjustValidator)
    {
        _parts = parts;
        _movements = movements;
        _uow = uow;
        _clock = clock;
        _currentUser = currentUser;
        _audit = audit;
        _alerts = alerts;
        _numbers = numbers;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _receiveValidator = receiveValidator;
        _issueValidator = issueValidator;
        _adjustValidator = adjustValidator;
    }

    public async Task<Result<PartDto>> CreatePartAsync(CreatePartRequest request, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<PartDto>(Error.Validation(validation.ToMessage()));
        }

        Part part;
        try
        {
            part = Part.Create(request.PartNo, request.Name, request.Unit, request.MinimumStock, request.UnitCost);
            part.AssignWarehouse(request.WarehouseId);
            part.AssignSupplier(request.SupplierId);
        }
        catch (DomainException ex)
        {
            return Result.Failure<PartDto>(Error.Validation(ex.Message));
        }

        if (await _parts.AnyAsync(p => p.PartNo == part.PartNo, ct))
        {
            return Result.Failure<PartDto>(Error.Conflict($"A part with number {part.PartNo} already exists."));
        }

        await using var tx = await _uow.BeginTransactionAsync(ct);

        await _parts.AddAsync(part, ct);
        await _uow.SaveChangesAsync(ct);

        if (request.InitialStock > 0)
        {
            part.IncreaseStock(request.InitialStock);
            var openingMovement = StockMovement.In(
                part.Id,
                request.InitialStock,
                request.UnitCost,
                request.WarehouseId,
                request.SupplierId,
                "Baslangic stogu");
            openingMovement.StampBalance(part.QuantityInStock);
            openingMovement.AssignNumber(await _numbers.NextStockMovementNoAsync(ct));

            await _movements.AddAsync(openingMovement, ct);
            _parts.Update(part);
            await _uow.SaveChangesAsync(ct);
        }

        await _audit.LogAsync("PartCreated", nameof(Part), part.Id.ToString(), new { part.PartNo }, ct);
        await tx.CommitAsync(ct);
        return Result.Success(MapPart(part));
    }

    public async Task<Result<PartDto>> UpdatePartAsync(Guid id, UpdatePartRequest request, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<PartDto>(Error.Validation(validation.ToMessage()));
        }

        var part = await _parts.GetByIdAsync(id, ct);
        if (part is null || part.IsDeleted)
        {
            return Result.Failure<PartDto>(Error.NotFound($"Part {id} was not found."));
        }

        part.SetMinimumStock(request.MinimumStock);
        part.SetUnitCost(request.UnitCost);
        _parts.Update(part);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("PartUpdated", nameof(Part), part.Id.ToString(), null, ct);
        return Result.Success(MapPart(part));
    }

    public async Task<Result<PartDto>> GetPartAsync(Guid id, CancellationToken ct = default)
    {
        var part = await _parts.GetByIdAsync(id, ct);
        return part is null || part.IsDeleted
            ? Result.Failure<PartDto>(Error.NotFound($"Part {id} was not found."))
            : Result.Success(MapPart(part));
    }

    public async Task<Result<PagedList<PartDto>>> GetPartsPagedAsync(PagedQuery query, CancellationToken ct = default)
    {
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        Expression<Func<Part, bool>> predicate = search is null
            ? p => !p.IsDeleted
            : p => !p.IsDeleted && (p.PartNo.Contains(search) || p.Name.Contains(search));

        Expression<Func<Part, object>>? orderBy = query.SortBy?.ToLowerInvariant() switch
        {
            "partno" => p => p.PartNo,
            "name" => p => p.Name,
            "quantity" => p => p.QuantityInStock,
            "createdat" => p => p.CreatedAt,
            _ => null
        };

        var (items, total) = await _parts.PagedAsync(predicate, orderBy, query.SortDescending, query.Skip, query.PageSize, ct);
        var dtos = items.Select(MapPart).ToList();
        return Result.Success(new PagedList<PartDto>(dtos, total, query.Page, query.PageSize));
    }

    public async Task<Result<StockMovementDto>> ReceiveStockAsync(ReceiveStockRequest request, CancellationToken ct = default)
    {
        var validation = await _receiveValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<StockMovementDto>(Error.Validation(validation.ToMessage()));
        }

        var part = await _parts.GetByIdAsync(request.PartId, ct);
        if (part is null || part.IsDeleted)
        {
            return Result.Failure<StockMovementDto>(Error.NotFound($"Part {request.PartId} was not found."));
        }

        await using var tx = await _uow.BeginTransactionAsync(ct);

        part.IncreaseStock(request.Quantity);
        var movement = StockMovement.In(part.Id, request.Quantity, request.UnitCost, request.WarehouseId, note: request.Note);
        movement.StampBalance(part.QuantityInStock);
        movement.AssignNumber(await _numbers.NextStockMovementNoAsync(ct));

        await _movements.AddAsync(movement, ct);
        _parts.Update(part);
        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("StockReceived", nameof(StockMovement), movement.Id.ToString(),
            new { part.PartNo, request.Quantity, part.QuantityInStock }, ct);
        await tx.CommitAsync(ct);

        return Result.Success(MapMovement(movement));
    }

    public async Task<Result<StockMovementDto>> IssueStockAsync(IssueStockRequest request, CancellationToken ct = default)
    {
        var validation = await _issueValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<StockMovementDto>(Error.Validation(validation.ToMessage()));
        }

        var part = await _parts.GetByIdAsync(request.PartId, ct);
        if (part is null || part.IsDeleted)
        {
            return Result.Failure<StockMovementDto>(Error.NotFound($"Part {request.PartId} was not found."));
        }
        if (part.QuantityInStock < request.Quantity)
        {
            return Result.Failure<StockMovementDto>(Error.Conflict(
                $"Insufficient stock for {part.PartNo}: available {part.QuantityInStock}, requested {request.Quantity}."));
        }

        await using var tx = await _uow.BeginTransactionAsync(ct);

        part.DecreaseStock(request.Quantity);
        var movement = StockMovement.Out(part.Id, request.Quantity, part.UnitCost, note: request.Note);
        movement.StampBalance(part.QuantityInStock);
        movement.AssignNumber(await _numbers.NextStockMovementNoAsync(ct));

        await _movements.AddAsync(movement, ct);
        _parts.Update(part);

        if (part.IsBelowMinimum)
        {
            await _alerts.EnsureCriticalStockAlertAsync(part.Id, part.Name, part.QuantityInStock, part.MinimumStock, ct);
        }

        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("StockIssued", nameof(StockMovement), movement.Id.ToString(),
            new { part.PartNo, request.Quantity, part.QuantityInStock }, ct);
        await tx.CommitAsync(ct);

        return Result.Success(MapMovement(movement));
    }

    public async Task<Result<StockMovementDto>> AdjustStockAsync(AdjustStockRequest request, CancellationToken ct = default)
    {
        var validation = await _adjustValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Result.Failure<StockMovementDto>(Error.Validation(validation.ToMessage()));
        }

        var part = await _parts.GetByIdAsync(request.PartId, ct);
        if (part is null || part.IsDeleted)
        {
            return Result.Failure<StockMovementDto>(Error.NotFound($"Part {request.PartId} was not found."));
        }
        if (part.QuantityInStock + request.SignedQuantity < 0)
        {
            return Result.Failure<StockMovementDto>(Error.Conflict("Adjustment would drive stock negative."));
        }

        await using var tx = await _uow.BeginTransactionAsync(ct);

        part.AdjustStock(request.SignedQuantity);
        var movement = StockMovement.Adjustment(part.Id, request.SignedQuantity, part.UnitCost, note: request.Note);
        movement.StampBalance(part.QuantityInStock);
        movement.AssignNumber(await _numbers.NextStockMovementNoAsync(ct));

        await _movements.AddAsync(movement, ct);
        _parts.Update(part);

        if (part.IsBelowMinimum)
        {
            await _alerts.EnsureCriticalStockAlertAsync(part.Id, part.Name, part.QuantityInStock, part.MinimumStock, ct);
        }

        await _uow.SaveChangesAsync(ct);
        await _audit.LogAsync("StockAdjusted", nameof(StockMovement), movement.Id.ToString(),
            new { part.PartNo, request.SignedQuantity, part.QuantityInStock }, ct);
        await tx.CommitAsync(ct);

        return Result.Success(MapMovement(movement));
    }

    public async Task<Result<IReadOnlyList<PartDto>>> GetCriticalStocksAsync(CancellationToken ct = default)
    {
        var parts = await _parts.ListAsync(p => p.IsActive && !p.IsDeleted && p.QuantityInStock <= p.MinimumStock, ct);
        IReadOnlyList<PartDto> dtos = parts.Select(MapPart).ToList();
        return Result.Success(dtos);
    }

    private static PartDto MapPart(Part p) => new(
        p.Id, p.PartNo, p.Name, p.Category, p.Unit, p.QuantityInStock, p.MinimumStock,
        p.UnitCost, p.IsBelowMinimum, p.WarehouseId, p.SupplierId);

    private static StockMovementDto MapMovement(StockMovement m) => new(
        m.Id, m.MovementNo, m.PartId, m.MovementType.ToString(), m.Quantity, m.BalanceAfter, m.UnitCost, m.CreatedAt);
}
