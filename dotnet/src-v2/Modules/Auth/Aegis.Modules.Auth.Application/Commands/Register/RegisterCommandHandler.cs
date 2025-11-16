using Aegis.Modules.Auth.Application.Abstractions;
using Aegis.Modules.Auth.Domain.Entities;
using Aegis.Modules.Auth.Domain.Repositories;
using Aegis.Shared.Contracts.DTOs.Auth;
using Aegis.Shared.Infrastructure.EventBus;
using Aegis.Shared.Infrastructure.Persistence;
using Aegis.Shared.Kernel.Results;
using Aegis.Shared.Kernel.ValueObjects;
using MediatR;

namespace Aegis.Modules.Auth.Application.Commands.Register;

/// <summary>
/// Handler for RegisterCommand
/// </summary>
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventBus _eventBus;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IEventBus eventBus)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _eventBus = eventBus;
    }

    public async Task<Result<RegisterResponse>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        // Check if username already exists
        if (await _userRepository.UsernameExistsAsync(request.Username, cancellationToken))
        {
            return Result.Failure<RegisterResponse>(new Error(
                "User.UsernameAlreadyExists",
                $"Username '{request.Username}' is already taken"));
        }

        // Check if email already exists
        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
        {
            return Result.Failure<RegisterResponse>(new Error(
                "User.EmailAlreadyExists",
                $"Email '{request.Email}' is already registered"));
        }

        // Create value objects
        var usernameResult = Username.Create(request.Username);
        if (usernameResult.IsFailure)
        {
            return Result.Failure<RegisterResponse>(usernameResult.Error);
        }

        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<RegisterResponse>(emailResult.Error);
        }

        PhoneNumber? phoneNumber = null;
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var phoneResult = PhoneNumber.Create(request.PhoneNumber);
            if (phoneResult.IsFailure)
            {
                return Result.Failure<RegisterResponse>(phoneResult.Error);
            }
            phoneNumber = phoneResult.Value;
        }

        // Hash password
        var hashedPassword = _passwordHasher.HashPassword(request.Password);

        // Create user
        var userResult = User.Create(
            usernameResult.Value,
            emailResult.Value,
            hashedPassword,
            phoneNumber);

        if (userResult.IsFailure)
        {
            return Result.Failure<RegisterResponse>(userResult.Error);
        }

        var user = userResult.Value;

        // Save to database
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        // Publish domain events
        await _eventBus.PublishManyAsync(user.DomainEvents, cancellationToken);
        user.ClearDomainEvents();

        // Return response
        var response = new RegisterResponse(
            user.Id,
            user.Username.Value,
            user.Email.Value);

        return Result.Success(response);
    }
}
