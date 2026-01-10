using System.ComponentModel.DataAnnotations;

namespace budget_api.Models.ViewModel
{
    public class ContactMessageViewModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Treść wiadomości jest wymagana.")]
        [MinLength(5, ErrorMessage = "Wiadomość musi mieć co najmniej 5 znaków.")]
        [MaxLength(5000, ErrorMessage = "Wiadomość jest zbyt długa.")]
        public string Message { get; set; } = string.Empty;
    }
}