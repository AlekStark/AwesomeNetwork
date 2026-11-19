using AutoMapper;
using AutoMapper.QueryableExtensions;
using AwesomeNetwork.Data;
using AwesomeNetwork.Data.Repository;
using AwesomeNetwork.Models.Users;
using AwesomeNetwork.ViewModels.Account; // Убедитесь, что здесь UserWithFriendExt
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
            //return View();
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

            // Вызываем первый перегруженный метод.
            model.Friends = await GetAllFriend(model.User);

            return View(model);
        }

        // Первый вариант метода: принимает явного пользователя.
        private async Task<List<User>> GetAllFriend(User user)
        {
            if (user == null) return [];

            // Загрузка друзей одним SQL-запросом.
            return await _context.Friends
                .Where(f => f.UserId == user.Id || f.CurrentFriendId == user.Id)
                .Select(f =>
                    f.UserId == user.Id ? f.CurrentFriend : // Если мы инициатор дружбы
                    f.User                        // Иначе берем того, кто инициировал
                )
                .ToListAsync();
        }

        // Второй вариант метода: получает текущего пользователя сам.
        private async Task<List<User>> GetAllFriend()
        {
            // Проверка обязательна! Метод асинхронный, пользователь может быть null.
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return []; // Или throw new UnauthorizedAccessException();

            // Делегируем работу первому методу.
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

            // Передаем вью-модель в AutoMapper, чтобы он сам обновил свойства сущности.
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
            // Получаем текущего пользователя
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null || string.IsNullOrEmpty(id))
                return NotFound();

            // Ищем целевого пользователя по ID
            var targetUser = await _userManager.FindByIdAsync(id);

            if (targetUser == null)
                return NotFound(); // Пользователь не найден

            // Проверяем, существует ли уже такая дружба
            bool isAlreadyFriends = await _context.Friends.AnyAsync(f =>
                (f.UserId == currentUser.Id && f.CurrentFriendId == targetUser.Id) ||
                (f.UserId == targetUser.Id && f.CurrentFriendId == currentUser.Id));

            if (isAlreadyFriends)
                return RedirectToAction(nameof(MyPage)); // Или верните сообщение об ошибке

            // Создаём новую запись дружбы
            var friendship = new Friend()
            {
                UserId = currentUser.Id,
                CurrentFriendId = targetUser.Id
            };

            // Добавляем в базу и сохраняем изменения
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
                return NotFound(); // Пользователь не найден

            // Находим запись дружбы по ID пользователей.
            // Мы используем FirstOrDefaultAsync вместо SingleAsync,
            // так как теоретически может быть несколько записей (если у вас нет уникального индекса).
            var friendship = await _context.Friends.FirstOrDefaultAsync(f =>
                (f.UserId == currentUser.Id && f.CurrentFriendId == targetUser.Id) ||
                (f.UserId == targetUser.Id && f.CurrentFriendId == currentUser.Id));

            if (friendship == null)
                return RedirectToAction(nameof(MyPage)); // Или верните сообщение об ошибке "Не были друзьями"

            // Удаляем сущность и сохраняем изменения.
            _context.Friends.Remove(friendship); // Не нужно вызывать Delete()
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyPage));
        }


        private async Task<SearchViewModel> CreateSearch(string search)
        {
            var currentuser = User;

            var result = await _userManager.GetUserAsync(currentuser);

            var list = _userManager.Users.AsEnumerable().Where(x => x.GetFullName().ToLower().Contains(search.ToLower())).ToList();
            var withfriend = await GetAllFriend();

            var data = new List<UserWithFriendExt>();
            list.ForEach(x =>
            {
                var t = _mapper.Map<UserWithFriendExt>(x);
                t.IsFriendWithCurrent = withfriend.Where(y => y.Id == x.Id || x.Id == result.Id).Count() != 0;
                data.Add(t);
            });

            var model = new SearchViewModel()
            {
                UserList = data
            };

            return model;
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
        /*       [Authorize]
               [Route("Chat")]
               [HttpPost]
               public async Task<IActionResult> Chat(string id)
               {
                   var currentUser = await _userManager.GetUserAsync(User);
                   var friend = await _userManager.FindByIdAsync(id);

                   if (currentUser == null || friend == null)
                   {
                       return NotFound();
                   }

                   var messages = await _messageRepository.GetMessagesAsync(currentUser, friend);

                   var model = new ChatViewModel
                   {
                       You = currentUser,
                       ToWhom = friend,
                       History = messages.OrderBy(x => x.Id).ToList(),
                   };

                   return View("Chat", model);
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

                   var messages = await _messageRepository.GetMessagesAsync(currentUser, friend);

                   var model = new ChatViewModel
                   {
                       You = currentUser,
                       ToWhom = friend,
                       History = messages.OrderBy(x => x.Id).ToList(),
                   };

                   return View("Chat", model);
               }*/
    }
}