namespace HW.Domain.ValueObjects;

public record class Password
{
    public string Value { get; }
    
    private Password(string value) => Value = value;

    public static Password Create(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(password));
        
        if (password.Length < 8)
            throw new ArgumentException("Password must have at least 8 characters.", nameof(password));
        
        if (!HasMinimumComplexity(password))
            throw new ArgumentException("Password does not meet complexity requirements.", nameof(password));

        return new Password(password);
    }
    
    private static bool HasMinimumComplexity(string password)
    {
        return password.Length >= 8 &&
               password.Any(char.IsUpper) &&
               password.Any(char.IsLower) && 
               password.Any(char.IsDigit);
    }
}