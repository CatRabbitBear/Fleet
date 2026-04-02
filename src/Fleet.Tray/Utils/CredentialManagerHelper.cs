using System;
using Meziantou.Framework.Win32;

namespace Fleet.Tray.Utils;
internal static class CredentialManagerHelper
{
    /// <summary>
    /// Save (or overwrite) a generic credential.
    /// </summary>
    /// <param name="target">The unique name, e.g. "FleetVaultUri" or "FleetApiKey".</param>
    /// <param name="userName">
    /// Optional metadata—can be blank or hold something like the vault’s tenant ID.
    /// </param>
    /// <param name="secret">The secret string (URI, key, JSON, …).</param>
    /// <param name="useLocalMachine">
    /// If true, credential is persisted for the current user profile; otherwise user session only.
    /// </param>
    public static void SaveCredential(
        string target,
        string userName,
        string secret,
        bool useLocalMachine = true)
    {
        CredentialManager.WriteCredential(
            applicationName: target,
            userName: userName,
            secret: secret,
            comment: null,
            persistence: useLocalMachine
                               ? CredentialPersistence.Enterprise
                               : CredentialPersistence.Session);
    }

    /// <summary>
    /// Read a previously saved credential.
    /// </summary>
    /// <param name="target">The same name you used in SaveCredential.</param>
    /// <returns>
    /// A tuple: UserName (may be null/empty) and Secret (null if not found).
    /// </returns>
    public static (string? UserName, string? Secret) LoadCredential(string target)
    {
        var cred = CredentialManager.ReadCredential(applicationName: target);
        if (cred is null)
            return (null, null);

        return (cred.UserName, cred.Password);
    }

    /// <summary>
    /// Delete a stored credential.
    /// </summary>
    public static void DeleteCredential(string target)
    {
        CredentialManager.DeleteCredential(applicationName: target);
    }

    /// <summary>
    /// Deletes a credential if it exists. Missing credentials are treated as success.
    /// </summary>
    public static bool TryDeleteCredential(string target, out Exception? exception)
    {
        exception = null;
        try
        {
            CredentialManager.DeleteCredential(applicationName: target);
            return true;
        }
        catch (Exception ex)
        {
            // Deleting a credential that does not exist is not an actionable error for callers.
            var message = ex.Message ?? string.Empty;
            if (message.Contains("cannot find", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("element", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            exception = ex;
            return false;
        }
    }
}
