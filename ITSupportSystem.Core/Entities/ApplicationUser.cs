using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ITSupportSystem.Core.Entities;

public class ApplicationUser : IdentityUser
{
    /*
    Inherited from IdentityUser (columns in AspNetUsers):

    Identity
      string   Id                    - Primary key (GUID as text), auto-generated
      string?  UserName              - Login name (set to Email)
      string?  NormalizedUserName    - Uppercased UserName for lookups (managed by UserManager)
      string?  Email                 - Email address
      string?  NormalizedEmail       - Uppercased Email for lookups (managed by UserManager)
      bool     EmailConfirmed        - Whether the email has been confirmed
      string?  PhoneNumber           - Optional phone number
      bool     PhoneNumberConfirmed  - Whether the phone number has been confirmed

    Security (never expose in DTOs)
      string?  PasswordHash          - Hashed password (managed by UserManager)
      string?  SecurityStamp         - Changes when credentials change; invalidates sessions
      string?  ConcurrencyStamp      - Changes on every save; prevents conflicting edits
      bool     TwoFactorEnabled      - Whether 2FA is enabled

    Lockout
      bool             LockoutEnabled     - Whether the user can be locked out
      int              AccessFailedCount  - Consecutive failed login attempts
      DateTimeOffset?  LockoutEnd         - When the lockout expires (null = not locked)

    Roles and claims are stored in separate tables (AspNetUserRoles, AspNetUserClaims),
    not on this class. Use UserManager.GetRolesAsync(user) to read them.
    */

    // Custom fields
    [Required]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
}
