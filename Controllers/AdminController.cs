using API.Controllers;
using API.Dto;
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

    
    [HttpGet("users-with-roles")]
    public async Task<ActionResult> GetUsersWithRoles()
    {
        var users = await _userManager.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .Include(u=>u.Photos)
            .OrderBy(x => x.UserName)
            .ToListAsync();

        var result = users.Select(x => new
        {
            x.Id,
            Username = x.UserName,
            Roles = x.UserRoles.Select(r => r.Role.Name).ToList(),
            PhotoUrl=x.Photos.FirstOrDefault(p=>p.IsMain)?.Url
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
    [HttpGet("user-photos/{username}")]
    public async Task<ActionResult> GetUsersPhotosByUsername(string username)
    {
        var user = await _userManager.Users
            .Include(u => u.Photos)
            .FirstOrDefaultAsync(u=>u.UserName==username);
        if(user==null) return NotFound("User Not Found!");

        var photos = user.Photos.Select(user => new PhotoDto1
        {
            Id = user.Id,
            Url = user.Url,
            IsMain = user.IsMain,
            Username=username,
        }).ToList();

        return Ok(photos);
            
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
