namespace HiddenListR.Domain.Entities;

public static class UserExtensions
{
    public static bool IsAdmin(this User? user) => user?.Role == UserRole.Admin;
}
