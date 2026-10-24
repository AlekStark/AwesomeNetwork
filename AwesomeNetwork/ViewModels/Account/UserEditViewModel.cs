using System.ComponentModel.DataAnnotations;

namespace AwesomeNetwork.ViewModels.Account
{
    public class UserEditViewModel
    {
        // Идентификатор пользователя (GUID). Только для отображения.
        [Display(Name = "Идентификатор")]
        public string UserId { get; set; }

        // Имя пользователя (логин).
        [Required(ErrorMessage = "Имя обязательно.")]
        [StringLength(20, MinimumLength = 3)]
        [Display(Name = "Имя")]
        public string FirstName { get; set; }

        // Фамилия.
        [Required]
        [StringLength(30, MinimumLength = 2)]
        [Display(Name = "Фамилия")]
        public string LastName { get; set; }

        // Отчество.
        [Display(Name = "Отчество")]
        public string MiddleName { get; set; }

        // Ник пользователя.
        //[Required(ErrorMessage = "Ник обязательно.")]
        //[StringLength(20, MinimumLength = 3)]
        //[Display(Name = "Ник")]
        public string UserName => Email;

        // Дата рождения.
        [DataType(DataType.Date)]
        [Display(Name = "Дата рождения")]
        public DateTime? BirthDate { get; set; }

        // Email.
        [EmailAddress(ErrorMessage = "Некорректный адрес электронной почты.")]
        [Display(Name = "Электронная почта")]
        public string Email { get; set; }

        // Ссылка на аватарку.
        [Url(ErrorMessage = "Введите корректную ссылку.")]
        [Display(Name = "Аватарка")]
        public string Image { get; set; }

        // О себе.
        [MaxLength(200)]
        [Display(Name = "Обо мне")]
        public string About { get; set; }

        // Статус.
        [Display(Name = "Статус")]
        public string Status { get; set; }

    }
}