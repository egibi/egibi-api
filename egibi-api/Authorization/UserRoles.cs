namespace egibi_api.Authorization
{
    /// <summary>
    /// Application role constants. Extensible — add new roles here.
    /// Roles are stored as strings on AppUser.Role.
    /// </summary>
    public static class UserRoles
    {
        public const string Admin = "admin";
        public const string User = "user";

        /// <summary>
        /// All valid roles. Used for validation.
        /// </summary>
        public static readonly string[] All = { Admin, User };

        public static bool IsValid(string role) =>
            All.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Authorization policy names. Reference these in [Authorize(Policy = ...)].
    /// </summary>
    public static class Policies
    {
        public const string RequireAdmin = "RequireAdmin";
    }
}
