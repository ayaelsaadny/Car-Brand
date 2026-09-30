using Limousine.Core.Entities;

namespace Limousine.Application.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken(ApplicationUser user);
    }
}
