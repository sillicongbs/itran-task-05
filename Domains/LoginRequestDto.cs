using System.ComponentModel.DataAnnotations;

namespace UserAuthManage.Domains
{
    public class LoginRequestDto
    {
        [Required, MaxLength(256)]
        public string Email { get; set; } = default!;
        [Required]
        public string Password { get; set; } = default!;
    }
}
