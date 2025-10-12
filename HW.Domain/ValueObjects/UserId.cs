namespace HW.Domain.Common;

public record UserId(Guid Value) : EntityId<Guid>(Value)
{
    public static UserId Create(Guid value) => new(value);
    public static UserId New() => Create(Guid.NewGuid());
    
    public static implicit operator UserId(Guid value) => Create(value);
    
}