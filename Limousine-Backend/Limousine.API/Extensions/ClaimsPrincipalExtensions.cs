using System;
using System.Security.Claims;

namespace Limousine.API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static int? GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                              ?? user.FindFirst("uid")?.Value;

            if (int.TryParse(userIdClaim, out var id))
            {
                return id;
            }

            return null;
        }

        public static string? GetUserEmail(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Email)?.Value 
                   ?? user.FindFirst("email")?.Value;
        }

        public static string? GetUserRole(this ClaimsPrincipal user)
        {
            return user.FindFirst(ClaimTypes.Role)?.Value 
                   ?? user.FindFirst("role")?.Value;
        }

        public static bool IsAdmin(this ClaimsPrincipal user)
        {
            return user.IsInRole("Admin");
        }
    }
}
