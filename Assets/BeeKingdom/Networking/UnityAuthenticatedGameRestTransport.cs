using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace BeeKingdom.Networking
{
    public sealed class UnityAuthenticatedGameRestTransport : IAuthenticatedGameRestTransport
    {
        private const int MaxRequestBytes = 512 * 1024;
        private const int MaxResponseBytes = 1024 * 1024;

        private readonly string baseUrl;
        private readonly int timeoutSeconds;
        private readonly IGameJsonCodec codec;
        private readonly Action<bool> connectionSignal;

        public UnityAuthenticatedGameRestTransport(
            string baseUrl,
            IGameJsonCodec codec,
            int timeoutSeconds = 20,
            bool allowInsecureLoopbackForDevelopment = false,
            Action<bool> connectionSignal = null)
        {
            Uri uri;
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out uri) ||
                (uri.Scheme != Uri.UriSchemeHttps &&
                 !(allowInsecureLoopbackForDevelopment && uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
                throw new ArgumentException("Official game transport requires HTTPS or an explicitly allowed development loopback.", nameof(baseUrl));
            if (timeoutSeconds < 5 || timeoutSeconds > 120)
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds));
            this.codec = codec ?? throw new ArgumentNullException(nameof(codec));
            this.baseUrl = uri.AbsoluteUri.TrimEnd('/');
            this.timeoutSeconds = timeoutSeconds;
            this.connectionSignal = connectionSignal;
        }

        public async Task<T> SendAsync<T>(
            AuthenticatedGameRestRequest request,
            string bearerAccessToken,
            CancellationToken cancellationToken)
        {
            ValidateRequest(request, bearerAccessToken);
            cancellationToken.ThrowIfCancellationRequested();

            string json = request.Body == null ? null : Serialize(request.Body);
            byte[] upload = json == null ? null : Encoding.UTF8.GetBytes(json);
            if (upload != null && upload.Length > MaxRequestBytes)
                throw InvalidResponse("game.request_too_large");

            var webRequest = new UnityWebRequest(baseUrl + request.Path, request.Method);
            webRequest.timeout = timeoutSeconds;
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Accept", "application/json");
            webRequest.SetRequestHeader("Authorization", "Bearer " + bearerAccessToken);
            if (upload != null)
            {
                webRequest.uploadHandler = new UploadHandlerRaw(upload);
                webRequest.SetRequestHeader("Content-Type", "application/json");
            }

            UnityWebRequestAsyncOperation operation = webRequest.SendWebRequest();
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            operation.completed += _ => completion.TrySetResult(true);
            SynchronizationContext unityContext = SynchronizationContext.Current;

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            using (cts.Token.Register(() =>
            {
                if (operation.isDone) return;
                if (unityContext != null)
                    unityContext.Post(_ => { try { if (!operation.isDone) webRequest.Abort(); } catch { } }, null);
                else
                    try { if (!operation.isDone) webRequest.Abort(); } catch { };
            }))
            {
                try
                {
                    await completion.Task;
                }
                finally
                {
                    cts.Cancel();
                }
            }
            cancellationToken.ThrowIfCancellationRequested();

            byte[] responseBytes = webRequest.downloadHandler == null ? null : webRequest.downloadHandler.data;
            if (responseBytes != null && responseBytes.Length > MaxResponseBytes)
                throw InvalidResponse("game.response_too_large");
            string body = webRequest.downloadHandler == null ? string.Empty : webRequest.downloadHandler.text;
            int statusCode = (int)webRequest.responseCode;
            bool success = statusCode >= 200 && statusCode <= 299;
            if (!success && webRequest.result != UnityWebRequest.Result.ProtocolError)
            {
                connectionSignal?.Invoke(false);
                throw new AuthenticatedGameRestException(
                    AuthenticatedGameRestError.NetworkFailure,
                    "game.network_unavailable");
            }
            if (statusCode == 401)
                throw new AuthenticatedGameRestException(
                    AuthenticatedGameRestError.Unauthorized,
                    "game.session_required",
                    statusCode);
            if (!success)
                throw new AuthenticatedGameRestException(
                    AuthenticatedGameRestError.RemoteRejected,
                    ParseSafeErrorCode(body),
                    statusCode);

            connectionSignal?.Invoke(true);

            if (string.Equals(request.Method, "GET", StringComparison.Ordinal))
            {
                string cacheControl = webRequest.GetResponseHeader("Cache-Control") ?? string.Empty;
                if (cacheControl.IndexOf("private", StringComparison.OrdinalIgnoreCase) < 0 ||
                    cacheControl.IndexOf("no-store", StringComparison.OrdinalIgnoreCase) < 0)
                    throw InvalidResponse("game.read_cache_boundary_missing");
            }

            try
            {
                return codec.Deserialize<T>(body);
            }
            catch (AuthenticatedGameRestException)
            {
                throw;
            }
            catch
            {
                throw InvalidResponse("game.response_invalid");
            }
            finally
            {
                webRequest.Dispose();
            }
        }

        public static string[] ProofRows()
        {
            return new[]
            {
                "game_transport_tls_required:true",
                "game_transport_custom_certificate_handler:false",
                "game_transport_automatic_retry:false",
                "game_transport_retry_owner:authenticated_game_client",
                "game_transport_request_bound_bytes:" + MaxRequestBytes,
                "game_transport_response_bound_bytes:" + MaxResponseBytes,
                "game_transport_get_requires_private_no_store:true",
                "game_transport_logs_bearer_or_body:false"
            };
        }

        private string Serialize(object body)
        {
            try
            {
                return codec.Serialize(body);
            }
            catch
            {
                throw InvalidResponse("game.request_invalid");
            }
        }

        private string ParseSafeErrorCode(string body)
        {
            try
            {
                ErrorWire error = codec.Deserialize<ErrorWire>(body);
                string code = error == null ? string.Empty : error.code;
                if (IsSafeGameCode(code)) return code;
            }
            catch
            {
            }
            return "game.rejected";
        }

        private static bool IsSafeGameCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code) || code.Length > 96 ||
                !code.StartsWith("game.", StringComparison.Ordinal)) return false;
            for (int index = 0; index < code.Length; index++)
            {
                char value = code[index];
                if (!(value == '.' || value == '_' || (value >= 'a' && value <= 'z') ||
                      (value >= '0' && value <= '9'))) return false;
            }
            return true;
        }

        private static void ValidateRequest(AuthenticatedGameRestRequest request, string bearerAccessToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if ((!string.Equals(request.Method, "GET", StringComparison.Ordinal) &&
                 !string.Equals(request.Method, "POST", StringComparison.Ordinal)) ||
                string.IsNullOrWhiteSpace(request.Path) || request.Path.Length > 512 ||
                !request.Path.StartsWith("/game/v1/", StringComparison.Ordinal) ||
                request.Path.IndexOf("..", StringComparison.Ordinal) >= 0 ||
                request.Path.IndexOf("://", StringComparison.Ordinal) >= 0)
                throw InvalidResponse("game.request_invalid");
            if (string.IsNullOrWhiteSpace(bearerAccessToken) || bearerAccessToken.Length > 8192 ||
                bearerAccessToken.IndexOf('\r') >= 0 || bearerAccessToken.IndexOf('\n') >= 0)
                throw new AuthenticatedGameRestException(
                    AuthenticatedGameRestError.Unauthorized,
                    "game.session_required",
                    401);
            if (string.Equals(request.Method, "GET", StringComparison.Ordinal) && request.Body != null)
                throw InvalidResponse("game.request_invalid");
        }

        private static AuthenticatedGameRestException InvalidResponse(string code)
        {
            return new AuthenticatedGameRestException(AuthenticatedGameRestError.InvalidResponse, code);
        }

        public sealed class ErrorWire
        {
            public string code { get; set; }
        }
    }
}
