using AuthService.Domain.Models;

namespace AuthService.Application.Interfaces;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email);
    Task<bool> PhoneExistsAsync(string phone);
    Task AddAsync(User user);
    Task<User?> FindByIdAsync(Guid id);
    Task<User?> FindByEmailAsync(string email);
    Task<User?> FindByIdWithKycAsync(Guid id);
    Task<User?> FindActiveByEmailAsync(string email);
    Task SaveChangesAsync();
}
