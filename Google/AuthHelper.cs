using System.Reflection;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using DicresPhotosUploader.Localization;

namespace DicresPhotosUploader.Google;

public static class AuthHelper
{
    // Scope "appendonly": only allows creating albums and uploading/adding photos,
    // it does not allow reading or modifying the rest of your Google Photos library.
    // It's exactly what this application needs and avoids requesting extra permissions.
    private static readonly string[] Scopes = { "https://www.googleapis.com/auth/photoslibrary.appendonly" };

    private const string ClientSecretResourceName = "client_secret.json";

    /// <summary>
    /// The first time it opens the browser so you can sign in and authorize the app.
    /// Subsequent times it reuses the token saved in TokenStorePath (and refreshes it
    /// automatically if it has expired), so you don't need to sign in
    /// again every day.
    /// </summary>
    public static async Task<UserCredential> GetCredentialAsync(string tokenStorePath)
    {
        await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ClientSecretResourceName)
            ?? throw new FileNotFoundException(Loc.Get("Auth_MissingClientSecret"));

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            (await GoogleClientSecrets.FromStreamAsync(stream)).Secrets,
            Scopes,
            "local-user",
            CancellationToken.None,
            new FileDataStore(tokenStorePath, true));

        return credential;
    }
}
