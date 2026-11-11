using AwesomeNetwork.Models.Users; // Для наследования User
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AwesomeNetwork.ViewModels.Account
{
    /// <summary>
    /// Вью-модель для страницы поиска пользователей.
    /// </summary>
    public class SearchViewModel
    {
        /// <summary>
        /// Список найденных пользователей с информацией о дружбе.
        /// </summary>
        public IEnumerable<UserWithFriendExt> Users { get; set; } = Enumerable.Empty<UserWithFriendExt>();

        /// <summary>
        /// Текущий поисковый запрос пользователя.
        /// </summary>
        [Display(Name = "Поиск")]
        public string Query { get; set; }
    }
}