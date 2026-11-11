using AwesomeNetwork.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace AwesomeNetwork.Data.Repository
{
    //public class FriendsRepository : Repository<Friend>
    public class FriendService
    {
        private readonly ApplicationDbContext _context; 
        public FriendService(ApplicationDbContext context)  // Конструктор для DI
            
        { 
            _context = context ?? throw new ArgumentNullException(nameof(context)); 
        }

        /*        public void AddFriend(User target, User Friend)
                {
                    var friends = Set.AsEnumerable().FirstOrDefault(x => x.UserId == target.Id && x.CurrentFriendId == Friend.Id);

                    if (friends == null)
                    {
                        var item = new Friend()
                        {
                            UserId = target.Id,
                            User = target,
                            CurrentFriend = Friend,
                            CurrentFriendId = Friend.Id,
                        };

                        Create(item);
                    }
                }*/
        public async Task AddFriendAsync(User targetUser, User friendUser)
        {
            if (targetUser == null || friendUser == null)
                return; // Или бросайте исключение

            var existingFriendship = await _context.Friends
                .Where(f => f.UserId == targetUser.Id && f.CurrentFriendId == friendUser.Id)
                .FirstOrDefaultAsync();

            if (existingFriendship != null)
                return; // Отношения уже существуют

            var friendship = new Friend()
            {
                UserId = targetUser.Id,
                CurrentFriendId = friendUser.Id,
                // Навигационные свойства (User и CurrentFriend) НЕ нужно заполнять при создании новой сущности.
                // Если у вас включена ленивая загрузка или прокси-классы, Entity Framework сам их настроит.
            };

            await _context.Friends.AddAsync(friendship);
            await _context.SaveChangesAsync(); // Вот она, Unit of Work! Контекст сам управляет транзакцией.
        }

        /*        public List<User> GetFriendsByUser(User target)
                {
                    var friends = Set.Include(x => x.CurrentFriend).Include(x => x.User).AsEnumerable().Where(x => x.User.Id == target.Id).Select(x => x.CurrentFriend);

                    return friends.ToList();
                }*/
        /// <summary>
        /// Возвращает пользователей, которые являются друзьями указанного пользователя.
        /// </summary>
        public async Task<List<User>> GetFriendsByUserAsync(User user)
        {
            if (user == null)
                return [];

            return await _context.Friends
                .Include(x => x.CurrentFriend) // Загружаем друга
                .Where(x => x.UserId == user.Id)
                .Select(x => x.CurrentFriend)
                .ToListAsync();
        }

        /*        public void DeleteFriend(User target, User Friend)
                {
                    var friends = Set.AsEnumerable().FirstOrDefault(x => x.UserId == target.Id && x.CurrentFriendId == Friend.Id);

                    if (friends != null)
                    {
                        Delete(friends);
                    }
                }*/
        public async Task RemoveFriendAsync(User targetUser, User friendUser)
        {
            if (targetUser == null || friendUser == null)
                return;

            var friendship = await _context.Friends
                .Where(f => f.UserId == targetUser.Id && f.CurrentFriendId == friendUser.Id)
                .FirstOrDefaultAsync();

            if (friendship == null)
                return;

            _context.Friends.Remove(friendship); // Нет необходимости вызывать Delete()
            await _context.SaveChangesAsync();
        }

    }
}
