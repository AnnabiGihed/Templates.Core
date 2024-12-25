namespace Templates.Core.Blazor.Services.Security;

public class TokenProvider
{
	public string IdentityToken { get; set; }
	public string XsrfToken { get; set; }
	public string AccessToken { get; set; }
	public string RefreshToken { get; set; }
	public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>
/// Author  : Emmanuel Nuyttens
/// Date    : 06-2023
/// Purpose : Initial application state.
/// </summary>
public class InitialApplicationState
{
	public string IdentityToken { get; set; }
	public string XsrfToken { get; set; }
	public string AccessToken { get; set; }
	public string RefreshToken { get; set; }
	public DateTimeOffset ExpiresAt { get; set; }
}
