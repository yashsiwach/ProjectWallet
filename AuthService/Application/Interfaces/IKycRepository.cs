using AuthService.Domain.Models;

namespace AuthService.Application.Interfaces;

public interface IKycRepository
{
    Task AddAsync(KycDocument kyc);
    Task RemoveAsync(KycDocument kyc);
    Task SaveChangesAsync();
}
