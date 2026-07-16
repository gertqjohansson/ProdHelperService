namespace ProdHelperService.Auth;

// Deliberately opaque, server-side, non-JWT tokens for the "password verified,
// MFA still pending" window — a JWT here could be mistaken for a real access
// token by code that forgets to check its purpose; an opaque one can't be.
public interface IMfaChallengeStore
{
    string CreateChallenge(string userId);

    // Looks up the userId without consuming the challenge, so a wrong code
    // doesn't burn the caller's only shot at retrying. Returns null if the
    // token is unknown or has expired.
    string? Resolve(string challengeToken);

    // Removes the challenge - call only once the code has been verified correct.
    void Consume(string challengeToken);
}
