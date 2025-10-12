using System.Security.Cryptography;

namespace HW.Domain.ValueObjects;

public record PasswordHash
{
    public string Value { get; }
    
    // Нужно иметь в виду, что в разных сборках измененное значение без перекомпиляции не подставится 
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

    private PasswordHash(string value) => Value = value;
    public static PasswordHash Create(Password password)
    {
        var value = HashPasswordInternal(password.Value);
        return new PasswordHash(value);
    }

    private static string HashPasswordInternal(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);
        
        // Формат: algorithm|iterations|salt|hash
        return $"pbkdf2|{Iterations}|{Convert.ToBase64String(salt)}|{Convert.ToBase64String(hash)}";
    }
    
    public static PasswordHash FromDatabase(string storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(storedHash));

        return new PasswordHash(storedHash);
    }

    public bool Verify(Password password)
    {
        ArgumentNullException.ThrowIfNull(password);

        try
        {
            return VerifyPasswordInternal(password.Value, Value);
        }
        catch 
        {
            return false;
        }
    }
    private static bool VerifyPasswordInternal(string password, string storedHash)
    {
        var parts = storedHash.Split('|');
        if (parts.Length != 4)
            return false;
        
        var algorithm = parts[0];
        var iterations = int.Parse(parts[1]);
        var salt = Convert.FromBase64String(parts[2]);
        var expectedHash = Convert.FromBase64String(parts[3]);
        
        if (algorithm != "pbkdf2")
            return false;
        
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, iterations, Algorithm, expectedHash.Length);
        
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}