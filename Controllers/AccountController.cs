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
    public class AccountController: ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        public AccountController(UserManager<AppUser> userManager, ITokenService tokenService, IMapper mapper)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        [HttpPost("register")]// account/register
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto )
        {
            if (await UserExists(registerDto.Username))
                return BadRequest("Username is taken");

            var user = _mapper.Map<AppUser>(registerDto);

            user.UserName = registerDto.Username.ToLower();
            
            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            return new UserDto
            {
                Username = user.UserName,
                Token = await _tokenService.CreateToken(user),
                Gender = user.Gender,
                KnownAs=user.KnownAs
            };
        }

        private async Task<bool> UserExists(string username)
        {
            return await _userManager.Users.AnyAsync(x => x.NormalizedUserName == username.ToUpper());
        }
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _userManager.Users
                .Include(u => u.Photos)
                .FirstOrDefaultAsync(u => u.NormalizedUserName == loginDto.Username.ToUpper());

            if (user == null||user.UserName==null) return Unauthorized("Invalid username");

            var roles = await _userManager.GetRolesAsync(user);

            

            return new UserDto
            {
                Username = user.UserName,
                KnownAs = user.KnownAs,
                Token = await _tokenService.CreateToken(user),
                Gender = user.Gender,
                PhotoUrl = user.Photos?.FirstOrDefault(x => x.IsMain)?.Url
            };
        }
    }
}
