using System.Text.RegularExpressions;

namespace HW.Domain.ValueObjects;

public record Username
{
    public string Value { get; }

    public Username(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(value));
        
        if (value.Length < 3 || value.Length > 32)
            throw new ArgumentException("Value must be between 3 and 32 characters.", nameof(value));
        
        if (!Regex.IsMatch(value, "^[a-zA-Z0-9]+$"))
            throw new ArgumentException("Value must be alphanumeric.", nameof(value));
        
        Value = value.Trim();
    }
    
    public static implicit operator string(Username username) => username.Value;

}