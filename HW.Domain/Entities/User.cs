using System.Diagnostics.CodeAnalysis;
using HW.Domain.Common;
using HW.Domain.Models;
using HW.Domain.ValueObjects;

namespace HW.Domain.Entities;

public sealed class User : AggregateRoot<UserId>
{
    private User(UserId userId) : base(userId)
    { }

    public Username Username { get; set; }
    public PasswordHash PasswordHash { get; private set; }
    public Gender? Gender { get; private set; }
    public DateTime CreatedAt { get; init; } 
    public DateTime UpdatedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static User Create(Username username, Password password)
    {
        ArgumentNullException.ThrowIfNull(username);
        ArgumentNullException.ThrowIfNull(password);

        var user = new User(UserId.New())
        {
            Username = username,
            PasswordHash = PasswordHash.Create(password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        //user.AddDomainEvent(new UserRegisteredEvent(user.Id));
        return user;
    }
    
    
    public void ChangePassword(Password currentPassword, Password newPassword)
    {
        if (!PasswordHash.Verify(currentPassword))
            throw new Exception("Current password is incorrect");
            
        PasswordHash = PasswordHash.Create(newPassword);
        UpdatedAt = DateTime.UtcNow;
        //AddDomainEvent(new UserPasswordChangedEvent(Id));
    }
    
    public void Deactivate(string reason)
    {
        if (!IsActive) return;
        
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        //AddDomainEvent(new UserDeactivatedEvent(Id, reason));
    }
    
    public void UpdateProfile(Username username, Gender? gender)
    {
        Username = username;
        Gender = gender;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public bool CanLogin() => IsActive;
    public bool HasPasswordExpired() => 
        DateTime.UtcNow - UpdatedAt > TimeSpan.FromDays(90);
}