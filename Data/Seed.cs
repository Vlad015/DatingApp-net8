using API.Entities;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

public static class Seed
{
    public static async Task SeedUsers(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        Console.WriteLine("SeedUsers method started.");

        if (await userManager.Users.AnyAsync())
        {
            Console.WriteLine("Users already exist. Skipping seeding.");
            return;
        }

        var userData = await File.ReadAllTextAsync("Data/UserSeedData.json");

        if (string.IsNullOrWhiteSpace(userData))
        {
            Console.WriteLine("UserSeedData.json is empty.");
            return;
        }

        Console.WriteLine("UserSeedData.json loaded.");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new DateOnlyJsonConverter() }
        };

        var users = JsonSerializer.Deserialize<List<AppUser>>(userData, options);

        if (users == null || users.Count == 0)
        {
            Console.WriteLine("Failed to deserialize user data or list is empty.");
            return;
        }

        Console.WriteLine($"Users deserialized: {users.Count}");

        // Create roles
        var roles = new List<AppRole>
        {
            new() {Name="Member"},
            new() {Name="Admin"},
            new() {Name="Moderator"}
        };
        foreach (var role in roles)
        {
            await roleManager.CreateAsync(role);
        }

        // Create users
        foreach (var user in users)
        {
            user.UserName = user.UserName!.ToLower();
            var result = await userManager.CreateAsync(user, "Admin123");

            if (!result.Succeeded)
            {
                Console.WriteLine($"Failed to create user: {user.UserName}");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($" - {error.Description}");
                }
                continue;
            }

            await userManager.AddToRoleAsync(user, "Member");
            Console.WriteLine($"User created: {user.UserName}");
        }

        // Create admin user
        var admin = new AppUser
        {
            UserName = "admin",
            Email="admin@seed.com",
            KnownAs = "admin",
            Gender = "",
            City = "",
            Country = "",
        };

        var adminResult = await userManager.CreateAsync(admin, "Admin123");
        if (!adminResult.Succeeded)
        {
            Console.WriteLine("Failed to create admin user.");
            foreach (var error in adminResult.Errors)
            {
                Console.WriteLine($" - {error.Description}");
            }
        }
        else
        {
            await userManager.AddToRolesAsync(admin, new[] { "Admin", "Moderator" });
            Console.WriteLine("Admin user created.");
        }
    }
}