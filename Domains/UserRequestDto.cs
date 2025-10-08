using System.ComponentModel.DataAnnotations;

namespace UserAuthManage.Domains
{
    public class UserRequestDto
    {   
        [Required, MaxLength(128)]
        public string Name { get; set; } = default!;

        [Required, MaxLength(256)]
        public string Email { get; set; } = default!;
        [Required]
        public string Password { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string Address { get; set; } = default!;
        
    }
}
