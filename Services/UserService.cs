using MailArchiver.Data;
using MailArchiver.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace MailArchiver.Services
{
    public class UserService : IUserService
    {
        private readonly MailArchiverDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(MailArchiverDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User> CreateUserAsync(string username, string email, string password, bool isAdmin = false)
        {
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = HashPassword(password),
                IsAdmin = isAdmin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new user: {Username} (ID: {UserId})", username, user.Id);

            return user;
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated user: {Username} (ID: {UserId})", user.Username, user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {Username} (ID: {UserId})", user.Username, user.Id);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;

            // Remove user's mail account associations
            var userMailAccounts = await _context.UserMailAccounts
                .Where(uma => uma.UserId == id)
                .ToListAsync();
            
            _context.UserMailAccounts.RemoveRange(userMailAccounts);
            
            // Remove the user
            _context.Users.Remove(user);
            
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted user: {Username} (ID: {UserId})", user.Username, user.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {Username} (ID: {UserId})", user.Username, user.Id);
                return false;
            }
        }

        public async Task<bool> AuthenticateUserAsync(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower() && u.IsActive);

            if (user == null)
                return false;

            return VerifyPassword(password, user.PasswordHash);
        }

        public async Task<bool> SetUserActiveStatusAsync(int id, bool isActive)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;

            user.IsActive = isActive;
            _context.Users.Update(user);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Set user {Username} (ID: {UserId}) active status to {IsActive}", 
                    user.Username, user.Id, isActive);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting user active status: {Username} (ID: {UserId})", user.Username, user.Id);
                return false;
            }
        }

        public async Task<List<MailAccount>> GetUserMailAccountsAsync(int userId)
        {
            return await _context.UserMailAccounts
                .Where(uma => uma.UserId == userId)
                .Include(uma => uma.MailAccount)
                .Select(uma => uma.MailAccount)
                .ToListAsync();
        }

        public async Task<bool> AssignMailAccountToUserAsync(int userId, int mailAccountId)
        {
            // Check if association already exists
            var existing = await _context.UserMailAccounts
                .FirstOrDefaultAsync(uma => uma.UserId == userId && uma.MailAccountId == mailAccountId);

            if (existing != null)
                return true; // Already assigned

            var userMailAccount = new UserMailAccount
            {
                UserId = userId,
                MailAccountId = mailAccountId
            };

            _context.UserMailAccounts.Add(userMailAccount);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Assigned mail account {MailAccountId} to user {UserId}", mailAccountId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning mail account {MailAccountId} to user {UserId}", mailAccountId, userId);
                return false;
            }
        }

        public async Task<bool> RemoveMailAccountFromUserAsync(int userId, int mailAccountId)
        {
            var userMailAccount = await _context.UserMailAccounts
                .FirstOrDefaultAsync(uma => uma.UserId == userId && uma.MailAccountId == mailAccountId);

            if (userMailAccount == null)
                return false;

            _context.UserMailAccounts.Remove(userMailAccount);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Removed mail account {MailAccountId} from user {UserId}", mailAccountId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing mail account {MailAccountId} from user {UserId}", mailAccountId, userId);
                return false;
            }
        }

        public async Task<bool> IsUserAdminAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.IsAdmin ?? false;
        }

        public async Task<bool> IsUserAuthorizedForAccountAsync(int userId, int mailAccountId)
        {
            // Admin users have access to all accounts
            var isAdmin = await IsUserAdminAsync(userId);
            if (isAdmin)
            {
                _logger.LogInformation("User {UserId} is admin, granting access to account {MailAccountId}", userId, mailAccountId);
                return true;
            }

            // Check if user has direct access to the account
            var hasDirectAccess = await _context.UserMailAccounts
                .AnyAsync(uma => uma.UserId == userId && uma.MailAccountId == mailAccountId);
                
            _logger.LogInformation("User {UserId} access check for account {MailAccountId}: {HasAccess}", 
                userId, mailAccountId, hasDirectAccess ? "Granted" : "Denied");
                
            return hasDirectAccess;
        }

        public async Task<int> GetAdminCountAsync()
        {
            return await _context.Users.CountAsync(u => u.IsAdmin && u.IsActive);
        }

        public string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        #region Password Hashing

        private bool VerifyPassword(string password, string hash)
        {
            var hashedInput = HashPassword(password);
            return hashedInput == hash;
        }

        #endregion
    }
}
