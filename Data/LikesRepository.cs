using API.Dto;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class LikesRepository(AppDbContext _context, IMapper mapper) : ILikesRepository
    {
        public void AddLike(UserLike like)
        {
            _context.Likes.Add(like);
        }

        public void DeleteLike(UserLike like)
        {
            _context.Likes.Remove(like);
        }

        public async Task<IEnumerable<int>> GetCurrentUserLikeIds(int currentUserId)
        {
           return await _context.Likes
                .Where(x=>x.SourceUserId==currentUserId)
                .Select(x=>x.TargetUserId)
                .ToListAsync();
        }

        public async Task<UserLike?> GetUserLike(int sourceUserId, int targetUserId)
        {
            return await _context.Likes.FindAsync(sourceUserId, targetUserId);
        }

        public async Task<PagedList<MemberDto>> GetUsersLike(LikesParams likesParams)
        {
            var likes= _context.Likes.AsQueryable();
            IQueryable<MemberDto> query;

            switch (likesParams.Predicate)
            {
                case "likes":
                    query = likes
                       .Where(x => x.SourceUserId == likesParams.UserId)
                       .Select(x => x.TargetUser)
                       .ProjectTo<MemberDto>(mapper.ConfigurationProvider);
                    break;
                case "likedBy":
                    query=likes
                        .Where(x => x.TargetUserId == likesParams.UserId)
                        .Select(x => x.SourceUser)
                        .ProjectTo<MemberDto>(mapper.ConfigurationProvider);
                        break;

                default:
                    var likeIds = await GetCurrentUserLikeIds(likesParams.UserId);

                    query = likes
                        .Where(x => x.TargetUserId == likesParams.UserId && likeIds.Contains(x.SourceUserId))
                        .Select(x => x.SourceUser)
                        .ProjectTo<MemberDto>(mapper.ConfigurationProvider);
                    break;
                        
            }
            return await PagedList<MemberDto>.CreateAsync(query, likesParams.PageNumber, likesParams.PageSize);
        }

        public async Task<bool> SaveChanges()
        {
            return await _context.SaveChangesAsync()>0;
        }
    }
}
