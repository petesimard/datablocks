using Google.GData.Client;
using Google.GData.Spreadsheets;
using UnityEditor;

/// <summary>
///     Helper class for working with Google Sheets API
/// </summary>
public class SheetsAPI
{
    private string AccessToken;
    private string CLIENT_ID = "1023910329103-fqr3qbllkbmfekvjs42sii8a7uncme5t.apps.googleusercontent.com";
    private string CLIENT_SECRET = "UqLSSjRYOUthtXNPIAw0_bH_";
    private string REDIRECT_URI = "urn:ietf:wg:oauth:2.0:oob";
    private string RefreshToken;
    private string SCOPE = "https://spreadsheets.google.com/feeds";
    private OAuth2Parameters oAuthParameters;

    private SpreadsheetsService service;

    /// <summary>
    ///     Spreadsheets Service
    /// </summary>
    public SpreadsheetsService Service
    {
        get { return service; }
        private set { service = value; }
    }

    /// <summary>
    ///     Initilize sheets API with saved token
    /// </summary>
    public void Initilize()
    {
        PermissiveCert.Instate();
        RefreshToken = EditorPrefs.GetString("Datablocks_RefreshToken");
        AccessToken = EditorPrefs.GetString("Datablocks_AccessToken");

        Service = new SpreadsheetsService("Datablocks for Unity");

        // OAuth2Parameters holds all the parameters related to OAuth 2.0.
        oAuthParameters = new OAuth2Parameters();

        // Set your OAuth 2.0 Client Id (which you can register at
        oAuthParameters.ClientId = CLIENT_ID;

        // Set your OAuth 2.0 Client Secret, which can be obtained at
        oAuthParameters.ClientSecret = CLIENT_SECRET;

        // Set your Redirect URI, which can be registered at
        oAuthParameters.RedirectUri = REDIRECT_URI;

        // Set the scope for this particular service. 
        oAuthParameters.Scope = SCOPE;

        if (!string.IsNullOrEmpty(RefreshToken))
        {
            oAuthParameters.RefreshToken = RefreshToken;
            oAuthParameters.AccessToken = AccessToken;

            var requestFactory = new GOAuth2RequestFactory(null, "Datablocks for Unity", oAuthParameters);
            Service.RequestFactory = requestFactory;
        }
    }

    /// <summary>
    ///     Check for access token
    /// </summary>
    /// <returns>True if token is found</returns>
    public bool HasAccessToken()
    {
        return string.IsNullOrEmpty(AccessToken);
    }


    /// <summary>
    ///     Clear the current OAuth token
    /// </summary>
    public void ClearOAuthToken()
    {
        AccessToken = null;
        RefreshToken = null;

        EditorPrefs.SetString("Datablocks_RefreshToken", "");
        EditorPrefs.SetString("Datablocks_AccessToken", "");
    }

    /// <summary>
    ///     Get the Auth URL
    /// </summary>
    /// <returns>Auth URL</returns>
    public string AuthURL()
    {
        return OAuthUtil.CreateOAuth2AuthorizationUrl(oAuthParameters);
    }

    /// <summary>
    ///     Set the access code from Google
    /// </summary>
    /// <param name="accessCode">Access code</param>
    public void SetAccessCode(string accessCode)
    {
        oAuthParameters.AccessCode = accessCode;

        OAuthUtil.GetAccessToken(oAuthParameters);
        OAuthUtil.RefreshAccessToken(oAuthParameters);

        string accessToken = oAuthParameters.AccessToken;
        string refreshToken = oAuthParameters.RefreshToken;

        EditorPrefs.SetString("Datablocks_RefreshToken", refreshToken);
        EditorPrefs.SetString("Datablocks_AccessToken", accessToken);

        RefreshToken = refreshToken;
        AccessToken = accessToken;

        var requestFactory = new GOAuth2RequestFactory(null, "Datablocks for Unity", oAuthParameters);
        service.RequestFactory = requestFactory;
    }
}