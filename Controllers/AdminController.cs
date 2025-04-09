using API.Controllers;
using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

[Route("api/admin")] 
[ApiController]
public class AdminController : BaseApiController
{
    private readonly UserManager<AppUser> _userManager;

    public AdminController(UserManager<AppUser> userManager)
    {
        Console.WriteLine("AdminController initialized!");
        _userManager = userManager;
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("users-with-roles")]
    public async Task<ActionResult> GetUsersWithRoles()
    {
        var users = await _userManager.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(x => x.UserName)
            .ToListAsync();

        var result = users.Select(x => new
        {
            x.Id,
            Username = x.UserName,
            Roles = x.UserRoles.Select(r => r.Role.Name).ToList()
        }).ToList();

        return Ok(result);
    }


    [Authorize]
    [HttpGet("photos-to-moderate")]
    public ActionResult GetPhotosFromModeration()
    {
        return Ok("Admins or moderators can see this");
    }
    [Authorize]
    [HttpGet("debug-token")]
    public IActionResult DebugToken()
    {
        var identity = HttpContext.User.Identity as ClaimsIdentity;
        if (identity == null)
        {
            return Unauthorized("No identity found");
        }

        var claims = identity.Claims.Select(c => new { c.Type, c.Value }).ToList();

        return Ok(new { claims });
    }
    [Authorize(Roles = "Admin")]
    [HttpPost("edit-roles/{username}")]
    public async Task<IActionResult>EditRoles(string username, string roles)
    {
        if(string.IsNullOrEmpty(roles))
        {
            return BadRequest("You must select at least a role");
        }
        var selectedRoles=roles.Split(",").ToArray();
        var user= await _userManager.FindByNameAsync(username);
        if ((user==null))
        {
            return BadRequest("User not found");
        }

        var userRoles=await _userManager.GetRolesAsync(user); 
        var result=await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));

        if (!result.Succeeded)
            return BadRequest("Failed to add to roles");
        result =await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

        if (!result.Succeeded)
        {
            return BadRequest("failed to remove from roles");
        }
        return Ok(await _userManager.GetRolesAsync(user));
    }

    

    


}
