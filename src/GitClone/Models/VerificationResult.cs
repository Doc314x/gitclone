namespace GitClone.Models;

public sealed record VerificationResult(bool Ok, string Message)
{
    public static VerificationResult Success(string message = "Backup verifiziert.") => new(true, message);
    public static VerificationResult Fail(string why) => new(false, why);
}
