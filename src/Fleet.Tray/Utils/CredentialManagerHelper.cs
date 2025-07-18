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
    /// If true, credential is persisted machine‑wide; otherwise user‑only session.
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
                               ? CredentialPersistence.LocalMachine
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
}
