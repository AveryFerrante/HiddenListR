using System.Security.Claims;
using HiddenListR.Domain.Repositories;
using HiddenListR.Services;

namespace HiddenListR.Middleware;

public class UserContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository, RequestUserContext userContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdValue, out var userId))
            {
                userContext.User = await userRepository.GetByIdAsync(userId, context.RequestAborted);
            }
        }

        await next(context);
    }
}
