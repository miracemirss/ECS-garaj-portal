using ECS.Application.Features.Assignments.Dtos;
using ECS.Shared.Results;

namespace ECS.Application.Features.Assignments;

public interface IAssignmentService
{
    // Transactional: close active link(s) on both sides, then open a new one.
    Task<Result<VehicleTrailerAssignmentDto>> AssignVehicleTrailerAsync(AssignVehicleTrailerRequest request, CancellationToken ct = default);
    Task<Result> EndVehicleTrailerAssignmentAsync(Guid assignmentId, CancellationToken ct = default);
    Task<Result<VehicleTrailerAssignmentDto>> GetActiveVehicleTrailerAsync(Guid vehicleId, CancellationToken ct = default);

    Task<Result<DriverVehicleAssignmentDto>> AssignDriverVehicleAsync(AssignDriverVehicleRequest request, CancellationToken ct = default);
    Task<Result> EndDriverVehicleAssignmentAsync(Guid assignmentId, CancellationToken ct = default);
    Task<Result<DriverVehicleAssignmentDto>> GetActiveDriverVehicleAsync(Guid driverId, CancellationToken ct = default);
}
