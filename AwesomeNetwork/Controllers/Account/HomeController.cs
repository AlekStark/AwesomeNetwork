using AwesomeNetwork.Models;
using AwesomeNetwork.Models.Users;
using AwesomeNetwork.ViewModels.Account;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;


namespace AwesomeNetwork.Controllers.Account
{
    public class HomeController : Controller
    {
        private readonly UserManager<User> _userManager; 
        public HomeController(UserManager<User> userManager) 
        { 
            _userManager = userManager; 
        }

        [Route("")]
        [Route("[controller]/[action]")]
        public IActionResult Index()
        {
            return View(new MainViewModel());
            //return View();
        }

        [Route("[action]")]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}