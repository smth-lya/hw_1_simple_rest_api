namespace HW.Domain.Common;

public abstract class Entity<TId> : IEquatable<Entity<TId>> 
    where TId : notnull
{
    protected Entity(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);
        Id = id;
    }

    public TId Id { get; }
    
    public override bool Equals(object? obj)
        => Equals(obj as Entity<TId>);
    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (other.GetType() != GetType()) return false;

        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
        => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
        => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
        => !Equals(left, right);
}