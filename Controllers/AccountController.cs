using API.Data;
using API.Dto;
using API.Entities;
using API.Interfaces;
using API.NewFolder;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace API.Controllers

{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController(UserManager<AppUser> userManager, ITokenService tokenService, IMapper mapper, IEmailService emailService) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState); 
            }

            if (await UserExists(registerDto.Username))
                return BadRequest("Username is taken");

            var user = mapper.Map<AppUser>(registerDto);
            registerDto.Username = registerDto.Username.Trim();
            user.UserName = registerDto.Username.ToLower();
            user.Email = registerDto.Email.ToLower();

            var result = await userManager.CreateAsync(user, registerDto.Password);
            await userManager.AddToRolesAsync(user, new[] { "Member" });

            if (!result.Succeeded)
            {
                return BadRequest(new
                {
                    errors = result.Errors.Select(e => e.Description).ToList()
                });

            }
            foreach (var error in result.Errors)
            {
                Console.WriteLine($" {error.Code}: {error.Description}");
            }

            return new UserDto
            {
                Username = user.UserName,
                Token = await tokenService.CreateToken(user),
                Gender = user.Gender,
                KnownAs = user.KnownAs,


            };
        }

        private async Task<bool> UserExists(string username)
        {
            return await userManager.Users.AnyAsync(x => x.NormalizedUserName == username.ToUpper());
        }
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await userManager.Users
                .Include(u => u.Photos)
                .FirstOrDefaultAsync(u => u.NormalizedUserName == loginDto.Username.ToUpper());

            if (user == null || user.UserName == null)
                return Unauthorized("Invalid username");

            var signInManager = HttpContext.RequestServices.GetRequiredService<SignInManager<AppUser>>();

            var result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: false);

            if (!result.Succeeded)
                return Unauthorized("Invalid password");

            var roles = await userManager.GetRolesAsync(user);

            return new UserDto
            {
                Username = user.UserName,
                KnownAs = user.KnownAs,
                Token = await tokenService.CreateToken(user),
                Gender = user.Gender,
                PhotoUrl = user.Photos?.FirstOrDefault(x => x.IsMain)?.Url
            };
        }

        [HttpPost("forgot-password")]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user == null) return BadRequest("No user found");

            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            var resetLink = $"https://localhost:4200/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email)}";

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "EmailTemplate.html");
            var htmlBody = await System.IO.File.ReadAllTextAsync(templatePath);
            htmlBody = htmlBody.Replace("{{resetLink}}", resetLink);
            await emailService.SendEmailAsync(dto.Email, "Reset your password", htmlBody);

            return Ok(new { message = "Reset link sent" });
        }
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user == null) return BadRequest("Invalid request");

            

            var result = await userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);

            if (!result.Succeeded)
            {
                var errorMessages = result.Errors.Select(e => e.Description).ToArray();
                return BadRequest(new { errors = errorMessages });
            }
            await userManager.UpdateSecurityStampAsync(user);

            return Ok(new { message = "Password has been reset successfully." });
        }


    }
}
