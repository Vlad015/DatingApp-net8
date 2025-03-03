using System.Security.Claims;

namespace API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetUsername(this ClaimsPrincipal user)
        {
            var username = user.FindFirstValue(ClaimTypes.Name);

            if (username == null)
                throw new Exception("Cannot get username from token");

            return username;
        }
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var userId =int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier));

            return userId;
        }
    }
}
