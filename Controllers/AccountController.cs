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
    public class AccountController(AppDbContext context, ITokenService tokenService, IMapper mapper): ControllerBase
    {
        [HttpPost("register")]// account/register
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto )
        {
            if (await UserExists(registerDto.Username))
                return BadRequest("Username is taken");


            using var hmac = new HMACSHA512();

            var user = mapper.Map<AppUser>(registerDto);
            user.Username = registerDto.Username;
            user.PasswordHash=hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password));
            user.PasswordSalt = hmac.Key;

            context.Users.Add(user);
            await context.SaveChangesAsync();

            return new UserDto
            {
                Username = user.Username,
                Token = tokenService.CreateToken(user),
                Gender = user.Gender,
                KnownAs=user.KnownAs
            };
        }

        private async Task<bool> UserExists(string username)
        {
            return await context.Users.AnyAsync(x => x.Username == username);
        }
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>>Login(LoginDto loginDto)
        {
            var user = await context.Users
                .Include(u => u.Photos) // Ensure photos are loaded
    .           FirstOrDefaultAsync(x => x.Username == loginDto.Username);
            if (user==null)
                return Unauthorized("Invalid username");
            using var hmac =new HMACSHA512(user.PasswordSalt);
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
            for(int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid password");
            }
            return new UserDto
            {
                Username = user.Username,
                KnownAs = user.KnownAs,
                Token = tokenService.CreateToken(user),
                Gender = user.Gender,
                PhotoUrl = user.Photos.FirstOrDefault(x => x.IsMain)?.Url
            };
        }
    }
}
