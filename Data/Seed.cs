using API.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace API.Data
{
    public class Seed
    {
        public static async Task SeedUsers(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
        {

            if (await userManager.Users.AnyAsync())
            {
                return;
            }

            var userData = await File.ReadAllTextAsync("Data/UserSeedData.json");

            if (string.IsNullOrWhiteSpace(userData))
            {
                return;
            }


            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var users = JsonSerializer.Deserialize<List<AppUser>>(userData, options);

            if (users == null || users.Count == 0)
            {
                return;
            }
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

            foreach (var user in users)
            {
                user.UserName = user.UserName!.ToLower();
                await userManager.CreateAsync(user, "Admin123");
                await userManager.AddToRoleAsync(user, "Member");
            }

            var admin = new AppUser
            {
                UserName = "admin",
                KnownAs = "admin",
                Gender = "",
                City = "",
                Country = "",
            };

            await userManager.CreateAsync(admin, "Admin123");
            await userManager.AddToRolesAsync(admin, ["Admin", "Moderator"]);
            
        }

    }
}
