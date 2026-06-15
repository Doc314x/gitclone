namespace GitClone.Models;

public sealed record VerificationResult(bool Ok, string Message)
{
    public static VerificationResult Success() => new(true, "Backup verifiziert.");
    public static VerificationResult Fail(string why) => new(false, why);
}
