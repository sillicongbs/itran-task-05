using System.ComponentModel.DataAnnotations;

namespace UserAuthManage.Domains
{
    public  class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, MaxLength(128)]
        public string Name { get; set; } = default!;

        [Required, MaxLength(256)]
        public string Email { get; set; } = default!;

        // IMPORTANT: persisted computed column created by migration as LOWER(Email)
        public string? NormalizedEmail { get; private set; }

        [Required]
        public string PasswordHash { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public string Address { get; set; } = default!;

        public UserStatus Status { get; set; } = UserStatus.Unverified;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginUtc { get; set; }
        public DateTime? LastActivityUtc { get; set; }

        public Guid? EmailConfirmToken { get; set; }
        public DateTime? EmailConfirmedAtUtc { get; set; }
    }
}
