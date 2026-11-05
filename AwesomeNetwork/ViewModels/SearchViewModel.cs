using AwesomeNetwork.Models.Users;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AwesomeNetwork.ViewModels.Account
{
    public class SearchViewModel
    {
        public IEnumerable<User> UserList { get; internal set; }
    }
}