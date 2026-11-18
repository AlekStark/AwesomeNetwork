using AwesomeNetwork.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace AwesomeNetwork.Data.Repository
{
    public class FriendRepository
    {
        private readonly ApplicationDbContext _context; 
        public FriendRepository(ApplicationDbContext context)  
            
        { 
            _context = context ?? throw new ArgumentNullException(nameof(context)); 
        }

        public async Task AddFriendAsync(User targetUser, User friendUser)
        {
            if (targetUser == null || friendUser == null)
                return; // Или бросайте исключение

            var existingFriendship = await _context.Friends
                .Where(f => f.UserId == targetUser.Id && f.CurrentFriendId == friendUser.Id)
                .FirstOrDefaultAsync();

            if (existingFriendship != null)
                return; 

            var friendship = new Friend()
            {
                UserId = targetUser.Id,
                CurrentFriendId = friendUser.Id,
            };

            await _context.Friends.AddAsync(friendship);
            await _context.SaveChangesAsync(); 
        }

        /// <summary>
        /// Возвращает пользователей, которые являются друзьями указанного пользователя.
        /// </summary>
        public async Task<List<User>> GetFriendsByUserAsync(User user)
        {
            if (user == null)
                return [];

            return await _context.Friends
                .Include(x => x.CurrentFriend) 
                .Where(x => x.UserId == user.Id)
                .Select(x => x.CurrentFriend)
                .ToListAsync();
        }
        public async Task RemoveFriendAsync(User targetUser, User friendUser)
        {
            if (targetUser == null || friendUser == null)
                return;

            var friendship = await _context.Friends
                .Where(f => f.UserId == targetUser.Id && f.CurrentFriendId == friendUser.Id)
                .FirstOrDefaultAsync();

            if (friendship == null)
                return;

            _context.Friends.Remove(friendship); 
            await _context.SaveChangesAsync();
        }

    }
}
