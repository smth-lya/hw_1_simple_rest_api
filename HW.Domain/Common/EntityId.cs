namespace HW.Domain.Common;

public record EntityId<T>(T Value)
{
    public static implicit operator T(EntityId<T> id) => id.Value;
}