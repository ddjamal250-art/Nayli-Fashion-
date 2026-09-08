using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Common;

namespace NayliFashion.Core.Models.Users;

/// <summary>
/// المستخدمين ومسؤولي النظام والكاشير
/// </summary>
public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Cashier;
    public string? PhoneNumber { get; set; }
    public DateTime? LastLoginAt { get; set; }
}
