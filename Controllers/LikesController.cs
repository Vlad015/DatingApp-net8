using API.Dto;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    public class LikesController(ILikesRepository likesRepository) : BaseApiController
    {
        [HttpPost("{targetUserId:int}")]
        public async Task<ActionResult> ToggleLike(int targetUserId)
        {
            var sourceuserId=User.GetUserId();
            if(sourceuserId == targetUserId) { return BadRequest("You cannot like yourself"); }
            var existingLike= await likesRepository.GetUserLike(sourceuserId,targetUserId);
            if(existingLike == null) 
            {
                var like = new UserLike
                {
                    SourceUserId = sourceuserId,
                    TargetUserId = targetUserId,
                };
                likesRepository.AddLike(like);
            }
            else
            {
                likesRepository.DeleteLike(existingLike);
            }
            if(await likesRepository.SaveChanges()) return Ok();

            return BadRequest("Failed to update like");
        }

        [HttpGet("list")]
        public async Task<ActionResult<IEnumerable<int>>> GetCurrentUserLikeIds()
        {
            return Ok(await likesRepository.GetCurrentUserLikeIds(User.GetUserId()));
        }
        [HttpGet]
        public async Task<ActionResult<PagedList<MemberDto>>> GetUserLikes([FromQuery]LikesParams likesParams)
        {
            likesParams.UserId = User.GetUserId();
            var users=await likesRepository.GetUsersLike(likesParams);

            Response.AddPaginationHeader(users);

            return Ok(users);
        }
    }
}
