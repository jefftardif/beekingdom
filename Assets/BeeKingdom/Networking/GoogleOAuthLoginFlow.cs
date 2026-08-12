using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BeeKingdom.Networking
{
    public static class GoogleOAuthLoginFlow
    {
        // HttpListener exige un prefixe se terminant par "/", mais Google exige une
        // correspondance EXACTE (caractere pour caractere) avec l'URI de redirection
        // enregistree dans Google Cloud Console (sans slash final). HttpListener recoit quand
        // meme les requetes sans slash final adressees a ce prefixe, donc les deux valeurs
        // peuvent diverger sans probleme.
        private const string ListenerPrefix = "http://127.0.0.1:53682/oauth/callback/";
        private const string RedirectUri = "http://127.0.0.1:53682/oauth/callback";
        private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";

        public sealed class Result
        {
            public string AuthorizationCode;
            public string CodeVerifier;
            public string RedirectUri;
        }

        public static async Task<Result> RunAsync(string clientId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new MobileAccountSessionException(MobileAccountSessionError.NotConfigured, "auth.not_configured");

            string codeVerifier = GenerateUrlSafeToken(32);
            string codeChallenge = ComputeCodeChallenge(codeVerifier);
            string state = GenerateUrlSafeToken(16);

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(ListenerPrefix);
            try
            {
                listener.Start();
            }
            catch (Exception)
            {
                throw new MobileAccountSessionException(MobileAccountSessionError.TransportFailure, "auth.google_sign_in_failed");
            }

            try
            {
                string authorizationUrl = BuildAuthorizationUrl(clientId, codeChallenge, state);
                Application.OpenURL(authorizationUrl);

                using (cancellationToken.Register(() => SafeStop(listener)))
                {
                    HttpListenerContext context;
                    try
                    {
                        context = await listener.GetContextAsync().ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        throw new MobileAccountSessionException(MobileAccountSessionError.TransportFailure, "auth.google_sign_in_failed");
                    }

                    string code = context.Request.QueryString["code"];
                    string returnedState = context.Request.QueryString["state"];
                    string error = context.Request.QueryString["error"];
                    bool success = error == null && !string.IsNullOrWhiteSpace(code) && string.Equals(state, returnedState, StringComparison.Ordinal);

                    await RespondToBrowserAsync(context, success).ConfigureAwait(false);

                    if (!success)
                        throw new MobileAccountSessionException(MobileAccountSessionError.AuthenticationRejected, "auth.google_sign_in_failed");

                    return new Result { AuthorizationCode = code, CodeVerifier = codeVerifier, RedirectUri = RedirectUri };
                }
            }
            finally
            {
                SafeStop(listener);
            }
        }

        private static async Task RespondToBrowserAsync(HttpListenerContext context, bool success)
        {
            string html = success
                ? "<html><head><meta charset='utf-8'></head><body style='font-family:sans-serif;text-align:center;padding-top:64px;'><h2>Bee Kingdom</h2><p>Connexion reussie. Tu peux fermer cet onglet et revenir au jeu.</p></body></html>"
                : "<html><head><meta charset='utf-8'></head><body style='font-family:sans-serif;text-align:center;padding-top:64px;'><h2>Bee Kingdom</h2><p>La connexion a echoue. Tu peux fermer cet onglet et reessayer dans le jeu.</p></body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(html);
            try
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                context.Response.ContentLength64 = buffer.Length;
                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
            finally
            {
                try { context.Response.OutputStream.Close(); } catch (Exception) { }
            }
        }

        private static void SafeStop(HttpListener listener)
        {
            try { listener.Stop(); } catch (Exception) { }
            try { listener.Close(); } catch (Exception) { }
        }

        private static string GenerateUrlSafeToken(int byteCount)
        {
            byte[] bytes = new byte[byteCount];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return Base64UrlEncode(bytes);
        }

        private static string ComputeCodeChallenge(string codeVerifier)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
                return Base64UrlEncode(hash);
            }
        }

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string BuildAuthorizationUrl(string clientId, string codeChallenge, string state)
        {
            return AuthorizationEndpoint +
                "?client_id=" + Uri.EscapeDataString(clientId) +
                "&redirect_uri=" + Uri.EscapeDataString(RedirectUri) +
                "&response_type=code" +
                "&scope=" + Uri.EscapeDataString("openid email profile") +
                "&code_challenge=" + codeChallenge +
                "&code_challenge_method=S256" +
                "&state=" + state +
                "&prompt=select_account";
        }
    }
}
