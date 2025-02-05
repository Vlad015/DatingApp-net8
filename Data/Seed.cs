using API.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace API.Data
{
    public class Seed
    {
        public static async Task SeedUsers(AppDbContext context)
        {
            Console.WriteLine("Seeding users..."); // Debugging step

            if (await context.Users.AnyAsync())
            {
                Console.WriteLine("Users already exist. Skipping seeding.");
                return;
            }

            var userData = await File.ReadAllTextAsync("Data/UserSeedData.json");

            if (string.IsNullOrWhiteSpace(userData))
            {
                Console.WriteLine("UserSeedData.json is empty or not found!");
                return;
            }

            Console.WriteLine("UserSeedData.json loaded successfully.");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var users = JsonSerializer.Deserialize<List<AppUser>>(userData, options);

            if (users == null || users.Count == 0)
            {
                Console.WriteLine("No users found in JSON file.");
                return;
            }

            foreach (var user in users)
            {
                using var hmac = new HMACSHA512();
                user.Username = user.Username;
                user.PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes("Admin123"));
                user.PasswordSalt = hmac.Key;

                Console.WriteLine($"Adding user: {user.Username}");

                context.Users.Add(user);
            }

            await context.SaveChangesAsync();
            Console.WriteLine("Users successfully seeded!");
        }

    }
}
