using AuthService.Application.Interfaces;
using AuthService.Domain.Models;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _db;

    public UserRepository(AuthDbContext db) => _db = db;

    public Task<bool> EmailExistsAsync(string email) =>
        _db.Users.AnyAsync(u => u.Email == email);

    public Task<bool> PhoneExistsAsync(string phone) =>
        _db.Users.AnyAsync(u => u.PhoneNumber == phone);

    public async Task AddAsync(User user) => await _db.Users.AddAsync(user);

    public async Task<User?> FindByIdAsync(Guid id) => await _db.Users.FindAsync(id);

    public Task<User?> FindByEmailAsync(string email) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> FindByIdWithKycAsync(Guid id) =>
        _db.Users.Include(u => u.KycDocument).FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> FindActiveByEmailAsync(string email) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email && u.Status == "Active");

    public Task SaveChangesAsync() => _db.SaveChangesAsync();
}
