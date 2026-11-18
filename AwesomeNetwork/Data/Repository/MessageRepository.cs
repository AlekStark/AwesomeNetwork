using AwesomeNetwork.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace AwesomeNetwork.Data.Repository
{
    public class MessageRepository : Repository<Message>
    {
        private readonly ApplicationDbContext _context;

        public MessageRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<Message>> GetMessagesAsync(User sender, User recipient)
        {
            var messages = await _context.Messages
                .Where(m =>
                    (m.SenderId == sender.Id && m.RecipientId == recipient.Id) ||
                    (m.SenderId == recipient.Id && m.RecipientId == sender.Id))
                .Include(m => m.Sender)
                .Include(m => m.Recipient)
                .OrderBy(m => m.Id)
                .ToListAsync();

            return messages;
        }
        public async Task CreateAsync(Message message) 
        { 
            await _context.Messages.AddAsync(message); await _context.SaveChangesAsync(); 
        }
    }
}