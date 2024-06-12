using FindMusik.Application.Abstractions;
using FindMusik.Domain.Models;
using FindMusik.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace FindMusik.Application.Services;

public class SaveUsersToDatabase:ISaveToDatabase
{
    private readonly DatabaseContext _context;

    public SaveUsersToDatabase(DatabaseContext context)
    {
        _context = context;
    }
    public async Task AddAsync(User user)
    {
            await _context.Users.AddAsync(user);
    }

    public async Task DeleteAsync(long userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user!=null)
        {
            _context.Users.Remove(user);
        }
    }

    public async Task UpdateAsync(User user,long userId)
    {
        var selectedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (selectedUser!=null)
        {
            selectedUser.UserName = user.UserName;
            selectedUser.Name = user.Name;
            selectedUser.LastName = user.LastName;
            selectedUser.isActive = user.isActive;
            
        }
    }

    public async Task<User> ReadAsync(long userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user!=null)
        {
            return user;
        }

        return null;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}