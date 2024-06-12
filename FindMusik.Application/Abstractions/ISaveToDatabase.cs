using FindMusik.Domain.Models;

namespace FindMusik.Application.Abstractions;

public interface ISaveToDatabase
{
     Task AddAsync(User user);
     Task DeleteAsync(long userId);
     Task UpdateAsync(User user,long userId);
     Task<User> ReadAsync(long userId);
     Task SaveChangesAsync();
}