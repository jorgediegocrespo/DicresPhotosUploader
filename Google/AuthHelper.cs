using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using GooglePhotosUploader.Localization;

namespace GooglePhotosUploader.Google;

public static class AuthHelper
{
    // Scope "appendonly": only allows creating albums and uploading/adding photos,
    // it does not allow reading or modifying the rest of your Google Photos library.
    // It's exactly what this application needs and avoids requesting extra permissions.
    private static readonly string[] Scopes = { "https://www.googleapis.com/auth/photoslibrary.appendonly" };

    /// <summary>
    /// The first time it opens the browser so you can sign in and authorize the app.
    /// Subsequent times it reuses the token saved in TokenStorePath (and refreshes it
    /// automatically if it has expired), so you don't need to sign in
    /// again every day.
    /// </summary>
    public static async Task<UserCredential> GetCredentialAsync(string clientSecretsPath, string tokenStorePath)
    {
        if (!File.Exists(clientSecretsPath))
        {
            throw new FileNotFoundException(Loc.Format("Auth_MissingClientSecret", clientSecretsPath));
        }

        await using var stream = new FileStream(clientSecretsPath, FileMode.Open, FileAccess.Read);

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            (await GoogleClientSecrets.FromStreamAsync(stream)).Secrets,
            Scopes,
            "local-user",
            CancellationToken.None,
            new FileDataStore(tokenStorePath, true));

        return credential;
    }
}
