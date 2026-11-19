using AutoMapper;
using AutoMapper.QueryableExtensions;
using AwesomeNetwork.Data;
using AwesomeNetwork.Data.Repository;
using AwesomeNetwork.Models.Users;
using AwesomeNetwork.ViewModels.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AwesomeNetwork.Controllers.Account
{
    public class AccountManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly MessageRepository _messageRepository;

        public AccountManagerController(
            ApplicationDbContext context,
            IMapper mapper,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            MessageRepository messageRepository)
        {
            _context = context;
            _mapper = mapper;
            _userManager = userManager;
            _signInManager = signInManager;
            _messageRepository = messageRepository;
        }

        [Route("Login")]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [Route("Login")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.UserName, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
                return RedirectToAction(nameof(MyPage));

            ModelState.AddModelError(string.Empty, "Неправильный логин или пароль.");
            return View(model);
        }

        [Route("Logout")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [Route("MyPage")]
        [HttpGet]
        public async Task<IActionResult> MyPage()
        {
            var user = User;

            var result = await _userManager.GetUserAsync(user);

            var model = new UserViewModel(result);

            model.Friends = await GetAllFriend(model.User);

            return View(model);
        }

        private async Task<List<User>> GetAllFriend(User user)
        {
            if (user == null) return [];

            // Вариант с Union (более читаемый и безопасный для EF Core)
            var userId = user.Id;
            var friends = await _context.Friends
                .Where(f => f.UserId == userId)
                .Select(f => f.CurrentFriend)
                .Union(
                    _context.Friends
                        .Where(f => f.CurrentFriendId == userId)
                        .Select(f => f.User)
                )
                .ToListAsync();

            return friends;
        }

        private async Task<List<User>> GetAllFriend()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return [];

            return await GetAllFriend(currentUser);
        }

        [Authorize]
        [Route("EditUserProfile")]
        [HttpGet]
        public async Task<IActionResult> EditUserProfile()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            return View(_mapper.Map<UserEditViewModel>(currentUser));
        }

        [Authorize]
        [Route("Update")]
        [HttpPost]
        public async Task<IActionResult> Update(UserEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Некорректные данные");
                return View(nameof(EditUserProfile), model);
            }

            var userFromDb = await _userManager.FindByIdAsync(model.UserId.ToString());
            if (userFromDb == null)
                return NotFound();

            _mapper.Map(model, userFromDb);

            var updateResult = await _userManager.UpdateAsync(userFromDb);
            if (updateResult.Succeeded)
                return RedirectToAction(nameof(MyPage));

            foreach (var error in updateResult.Errors)
                ModelState.AddModelError(error.Code, error.Description);

            return View(nameof(EditUserProfile), model);
        }

        [Authorize]
        [Route("UserList")]
        [HttpPost]
        public async Task<IActionResult> UserList(string search)
        {
            var model = await CreateSearch(search);
            return View("UserList", model);
        }

        [Authorize]
        [Route("AddFriend")]
        [HttpPost]
        public async Task<IActionResult> AddFriend(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null || string.IsNullOrEmpty(id))
                return NotFound();

            var targetUser = await _userManager.FindByIdAsync(id);

            if (targetUser == null)
                return NotFound();

            bool isAlreadyFriends = await _context.Friends.AnyAsync(f =>
                (f.UserId == currentUser.Id && f.CurrentFriendId == targetUser.Id) ||
                (f.UserId == targetUser.Id && f.CurrentFriendId == currentUser.Id));

            if (isAlreadyFriends)
                return RedirectToAction(nameof(MyPage));

            var friendship = new Friend()
            {
                UserId = currentUser.Id,
                CurrentFriendId = targetUser.Id
            };

            await _context.Friends.AddAsync(friendship);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyPage));
        }

        [Authorize]
        [Route("DeleteFriend")]
        [HttpPost]
        public async Task<IActionResult> DeleteFriend(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null || string.IsNullOrEmpty(id))
                return NotFound();

            var targetUser = await _userManager.FindByIdAsync(id);

            if (targetUser == null)
                return NotFound();

            var friendship = await _context.Friends.FirstOrDefaultAsync(f =>
                (f.UserId == currentUser.Id && f.CurrentFriendId == targetUser.Id) ||
                (f.UserId == targetUser.Id && f.CurrentFriendId == currentUser.Id));

            if (friendship == null)
                return RedirectToAction(nameof(MyPage));

            _context.Friends.Remove(friendship);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyPage));
        }

        private async Task<SearchViewModel> CreateSearch(string search)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return new SearchViewModel();             }

            var friendIds = await GetAllFriendIds(currentUser.Id);

            var usersQuery = _userManager.Users.AsQueryable();


            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim().ToLowerInvariant();
                usersQuery = usersQuery.Where(u =>
                    u.FirstName.ToLower().Contains(normalizedSearch) ||
                    u.LastName.ToLower().Contains(normalizedSearch));
            }

            var users = await usersQuery.ToListAsync();

            var data = new List<UserWithFriendExt>();
            foreach (var user in users)
            {
                var userViewModel = _mapper.Map<UserWithFriendExt>(user);
                userViewModel.IsFriendWithCurrent = friendIds.Contains(user.Id);
                data.Add(userViewModel);
            }

            return new SearchViewModel { UserList = data };
        }


        private async Task<HashSet<string>> GetAllFriendIds(string userId)
        {
            return await _context.Friends
                .Where(f => f.UserId == userId || f.CurrentFriendId == userId)
                .Select(f => f.UserId == userId ? f.CurrentFriendId : f.UserId)
                .ToHashSetAsync();
        }

        [Authorize]
        [Route("Chat")]
        [HttpGet]
        public async Task<IActionResult> Chat()
        {
            var friendId = Request.Query["id"].ToString();

            if (string.IsNullOrEmpty(friendId))
            {
                return NotFound("ID пользователя не указан.");
            }

            var model = await GenerateChat(friendId);

            if (model == null)
            {
                return NotFound("Пользователь для чата не найден.");
            }

            return View("Chat", model);
        }

        [Authorize]
        [Route("Chat")]
        [HttpPost]
        public async Task<IActionResult> Chat(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("ID пользователя не указан.");
            }

            return RedirectToAction("Chat", new { id = id });
        }

        [Authorize]
        [Route("NewMessage")]
        [HttpPost]
        public async Task<IActionResult> NewMessage(string id, ChatViewModel chatViewModel)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var friend = await _userManager.FindByIdAsync(id);

            if (currentUser == null || friend == null)
            {
                return NotFound();
            }

            var message = new Message
            {
                Sender = currentUser,
                Recipient = friend,
                Text = chatViewModel.NewMessage.Text,
            };

            await _messageRepository.CreateAsync(message);
            await _context.SaveChangesAsync();

            var model = await GenerateChat(id);

            return View("Chat", model);
        }

        private async Task<ChatViewModel> GenerateChat(string friendId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var friend = await _userManager.FindByIdAsync(friendId);

            if (currentUser == null || friend == null)
            {
                return null;
            }

            var messages = await _messageRepository.GetMessagesAsync(currentUser, friend);

            return new ChatViewModel
            {
                You = currentUser,
                ToWhom = friend,
                History = messages.OrderBy(x => x.Id).ToList(),
            };
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetChatHistory(string friendId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var friend = await _userManager.FindByIdAsync(friendId);

            if (currentUser == null || friend == null)
            {
                return NotFound();
            }

            var model = await GenerateChat(friendId);

            return PartialView("_ChatHistory", model);
        }
    }
}