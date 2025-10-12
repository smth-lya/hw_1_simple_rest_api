namespace HW1.Api.Domain.Models;

public class UserRole
{
    public int UserRoleId { get; set; }
    public Guid UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}