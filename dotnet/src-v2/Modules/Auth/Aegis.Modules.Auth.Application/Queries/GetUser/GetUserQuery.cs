using Aegis.Shared.Contracts.DTOs.Users;
using Aegis.Shared.Kernel.Results;
using MediatR;

namespace Aegis.Modules.Auth.Application.Queries.GetUser;

/// <summary>
/// Query to get user by ID
/// </summary>
public record GetUserQuery(Guid UserId) : IRequest<Result<UserDto>>;
