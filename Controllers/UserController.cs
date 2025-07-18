﻿using API.Data;
using API.Dto;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;


namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController(IUserRepository userRepository, IMapper mapper,
        IPhotoService photoService) : BaseApiController
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers([FromQuery]UserParams userParams)
        {
            userParams.CurrentUsername= User.GetUsername();
            var users= await userRepository.GetMembersAsync(userParams);

            Response.AddPaginationHeader(users);
            
            return Ok(users);
        }
        [Authorize]
        [HttpGet("{username}")]
        public async Task <ActionResult<MemberDto>> GetUser(string username)
        {
            var user = await userRepository.GetMemberAsync(username);
            

            if (user == null) return NotFound();

            return user;
        }
        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
        {
            
            var user = await userRepository.GetUserByUsernameAsync(User.GetUsername());
            if (user==null)
            {
                return BadRequest("Could not find a user");
            }
            mapper.Map(memberUpdateDto, user);
            if (await userRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to update the user");
        }
        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            var user =await userRepository.GetUserByUsernameAsync(User.GetUsername());
            if (user == null) return BadRequest("Cannot update user");

            var result = await photoService.AddPhotoAsync(file);
            if(result.Error != null) return BadRequest(result.Error.Message);

            var photo=new Photo
            {
                Url=result.SecureUrl.AbsoluteUri,
                PublicId=result.PublicId,
            };
            if (user.Photos.Count == 0) photo.IsMain = true;
            user.Photos.Add(photo);
            if(await userRepository.SaveAllAsync()) return CreatedAtAction(nameof(GetUser),
                new {username =user.UserName}, mapper.Map<PhotoDto>(photo)); 

            return BadRequest();
        }
        [HttpPut("set-main-photo/{photoId:int}")]
        public async Task<ActionResult>SetMainPhoto(int photoId)
        {
            var user = await userRepository.GetUserByUsernameAsync(User.GetUsername());
            if (user == null) return BadRequest("Could not find user");
            var photo=user.Photos.FirstOrDefault(x=>x.Id ==photoId);

            if (photo == null || photo.IsMain) return BadRequest("Cannot use this as main photo");
            var currentMain = user.Photos.FirstOrDefault(x => x.IsMain);
            if(currentMain !=null) { currentMain.IsMain = false; }
            photo.IsMain = true;
            if (await userRepository.SaveAllAsync())
            {
                return NoContent();
            }
            else
            {
                return BadRequest("Problem setting main photo");
            }
        }
        [HttpDelete("delete-photo/{photoId:int}")]
        public async Task<ActionResult>Deletephoto(int photoId)
        {
            var user = await userRepository.GetUserByUsernameAsync(User.GetUsername());
            if (user == null) return BadRequest("User not found");
            var photo=user.Photos.FirstOrDefault(x=>x.Id==photoId);
            if(photo == null || photo.IsMain) { return BadRequest("This photo cannot be deleted"); }
            if(photo.PublicId != null)
            {
                var result=await photoService.DeletePhotoAsync(photo.PublicId);
                if(result.Error!=null) return BadRequest(result.Error.Message);
            }
            user.Photos.Remove(photo);
            if (await userRepository.SaveAllAsync())
                return Ok();

            return BadRequest("Problem deleting photo");
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("delete-photo1/{username}/{photoId}")]
        public async Task<ActionResult> Deletephoto1(int photoId, string username)
        {
            var user = await userRepository.GetUserByUsernameAsync(username);
            if (user == null) return BadRequest("User not found");
            var photo = user.Photos.FirstOrDefault(x => x.Id == photoId);
            if (photo == null) { return BadRequest("This photo cannot be deleted because it doesn't exist"); }
            if (photo.PublicId != null)
            {
                var result = await photoService.DeletePhotoAsync(photo.PublicId);
                if (result.Error != null) return BadRequest(result.Error.Message);
            }
            user.Photos.Remove(photo);
            if (await userRepository.SaveAllAsync())
                return Ok("Photo deleted successfully" );

            return BadRequest("Problem deleting photo");
        }

    }
}
