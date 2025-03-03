using API.Dto;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class UserRepository (AppDbContext context, IMapper mapper ): IUserRepository
    {

        public async Task<AppUser?> GetUserByIdAsync(int id)
        {
            return await context.Users.FindAsync(id);
        }

        public async Task<AppUser?> GetUserByUsernameAsync(string username)
        {
            return await context.Users
                .Include(x => x.Photos)
                .SingleOrDefaultAsync(x => x.Username == username);
        }
        public async Task<IEnumerable<AppUser>> GetUserAsync()
        {
            return await context.Users
                .Include(x=> x.Photos)
                .ToListAsync();
        }

        public async Task<bool> SaveAllAsync()
        {
            return await context.SaveChangesAsync()>0;
        }

        public void Update(AppUser user)
        {
            context.Entry(user).State = EntityState.Modified;
        }

        public async Task<PagedList<MemberDto>> GetMembersAsync( UserParams userParams)
        {
            var query = context.Users.AsQueryable();
            query = query.Where(x => x.Username != userParams.CurrentUsername);

            if (!string.IsNullOrEmpty(userParams.Gender))
            {
                query=query.Where(x=> x.Gender == userParams.Gender);
            }

            var minDob = DateTime.Today.AddYears(-userParams.MaxAge - 1);
            var maxDob = DateTime.Today.AddYears(-userParams.MinAge);

            query = query.Where(x => x.DateOfBirth >= DateOnly.FromDateTime(minDob) &&
                                      x.DateOfBirth <= DateOnly.FromDateTime(maxDob));
            query = userParams.OrderBy switch
            {
                "created"=>query.OrderByDescending(x=>x.Created),
                _ =>query.OrderByDescending(x=>x.LastActive)
            };


            return await PagedList<MemberDto>.CreateAsync(
                query.AsNoTracking().ProjectTo<MemberDto>(mapper.ConfigurationProvider),
                userParams.PageNumber, userParams.PageSize
            );


        }

        public async Task<MemberDto?> GetMemberAsync(string username)
        {
            return await context.Users
                .Where(x => x.Username == username)
                .ProjectTo<MemberDto>(mapper.ConfigurationProvider)
                .SingleOrDefaultAsync();
          
        }
    }
}
