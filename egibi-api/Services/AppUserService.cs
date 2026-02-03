#nullable disable
using egibi_api.Data;
using egibi_api.Data.Entities;
using egibi_api.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace egibi_api.Services
{
    public class AppUserService
    {
        private readonly EgibiDbContext _db;
        private readonly IEncryptionService _encryption;
        private readonly ILogger<AppUserService> _logger;

        public AppUserService(
            EgibiDbContext db,
            IEncryptionService encryption,
            ILogger<AppUserService> logger)
        {
            _db = db;
            _encryption = encryption;
            _logger = logger;
        }

        // =============================================
        // USER QUERIES
        // =============================================

        public async Task<AppUser> GetByIdAsync(int id)
        {
            return await _db.AppUsers.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
        }

        public async Task<AppUser> GetByEmailAsync(string email)
        {
            return await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<List<AppUser>> GetAllUsersAsync()
        {
            return await _db.AppUsers.Where(u => u.IsActive).ToListAsync();
        }

        // =============================================
        // ADMIN SEEDING
        // =============================================

        /// <summary>
        /// Ensures the default admin account exists. Called on startup.
        /// If admin already exists, this is a no-op.
        /// </summary>
        public async Task SeedAdminAsync()
        {
            const string adminEmail = "admin@egibi.io";

            var existing = await _db.AppUsers.FirstOrDefaultAsync(u => u.Email == adminEmail);
            if (existing != null)
            {
                _logger.LogInformation("Admin account already exists ({Email}).", adminEmail);
                return;
            }

            // Default admin password — change after first login
            const string defaultPassword = "Admin123!";

            var admin = new AppUser
            {
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "Egibi",
                Role = "admin",
                PasswordHash = _encryption.HashPassword(defaultPassword),
                EncryptedDataKey = _encryption.GenerateUserKey(),
                KeyVersion = 1,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _db.AppUsers.AddAsync(admin);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Admin account created: {Email} (default password — change after first login).", adminEmail);
        }
    }
}