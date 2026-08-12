using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace BeeKingdom.Gameplay.Communication
{
    public static class ChatHttpSecurityPolicy
    {
        public const int RedirectLimit = 0;
    }

    public static class ChatHttpRequestInvariant
    {
        private const string CapabilitiesPath = "/chat/v1/capabilities";

        public static void Validate(ChatTransportRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            bool get = string.Equals(request.Method, "GET", StringComparison.Ordinal);
            bool post = string.Equals(request.Method, "POST", StringComparison.Ordinal);
            if (!get && !post) throw new ArgumentException("Chat REST method must be GET or POST.", nameof(request));
            if (get && request.Body != null) throw new ArgumentException("Chat GET request cannot contain a body.", nameof(request));
            if (post && request.Body == null) throw new ArgumentException("Chat POST request requires a body.", nameof(request));

            bool capabilities = string.Equals(request.Path, CapabilitiesPath, StringComparison.Ordinal);
            if (capabilities)
            {
                if (!get || !string.IsNullOrEmpty(request.BearerToken) || !request.BypassCache)
                    throw new ArgumentException("Chat capabilities must be a public cache-bypassed GET request.", nameof(request));
                return;
            }

            if (request.BypassCache) throw new ArgumentException("Cache bypass is reserved for chat capabilities.", nameof(request));
            if (!ChatSessionSecurity.IsValidBearerToken(request.BearerToken)) throw new ArgumentException("Authenticated chat route requires a valid Bearer token.", nameof(request));
        }
    }

    public sealed class ChatHttpTimeoutPolicy
    {
        public const int DefaultSeconds = 30;
        public const int MinimumSeconds = 1;
        public const int MaximumSeconds = 120;
        public int TimeoutSeconds { get; }

        public ChatHttpTimeoutPolicy(TimeSpan? timeout = null)
        {
            TimeSpan value = timeout ?? TimeSpan.FromSeconds(DefaultSeconds);
            if (value <= TimeSpan.Zero || value > TimeSpan.FromSeconds(MaximumSeconds))
                throw new ArgumentOutOfRangeException(nameof(timeout), "Chat request timeout must be greater than zero and no more than 120 seconds.");
            TimeoutSeconds = Math.Max(MinimumSeconds, checked((int)Math.Ceiling(value.TotalSeconds)));
        }
    }

    public sealed class ChatHttpResponsePolicy
    {
        public const int DefaultMaxBytes = 1048576;
        public const int MinimumMaxBytes = 1024;
        public const int MaximumMaxBytes = 4194304;
        public int MaxBytes { get; }

        public ChatHttpResponsePolicy(int maxBytes = DefaultMaxBytes)
        {
            if (maxBytes < MinimumMaxBytes || maxBytes > MaximumMaxBytes)
                throw new ArgumentOutOfRangeException(nameof(maxBytes), "Chat response limit must be between 1024 and 4194304 bytes.");
            MaxBytes = maxBytes;
        }
    }

    public sealed class ChatHttpRequestPolicy
    {
        public const int DefaultMaxBytes = 65536;
        public const int MinimumMaxBytes = 1024;
        public const int MaximumMaxBytes = 1048576;
        public int MaxBytes { get; }

        public ChatHttpRequestPolicy(int maxBytes = DefaultMaxBytes)
        {
            if (maxBytes < MinimumMaxBytes || maxBytes > MaximumMaxBytes)
                throw new ArgumentOutOfRangeException(nameof(maxBytes), "Chat request limit must be between 1024 and 1048576 bytes.");
            MaxBytes = maxBytes;
        }

        public byte[] EncodeJson(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            int byteCount = Encoding.UTF8.GetByteCount(json);
            if (byteCount > MaxBytes)
                throw new RemoteChatTransportException(RemoteChatError.LocalRequestTooLarge, "Chat request exceeds the configured UTF-8 size limit.", 0, "local_request_too_large");
            return Encoding.UTF8.GetBytes(json);
        }
    }

    public sealed class ChatHttpRequestTargetPolicy
    {
        public const int DefaultMaxBytes = 8192;
        public const int MinimumMaxBytes = 1024;
        public const int MaximumMaxBytes = 16384;
        public int MaxBytes { get; }

        public ChatHttpRequestTargetPolicy(int maxBytes = DefaultMaxBytes)
        {
            if (maxBytes < MinimumMaxBytes || maxBytes > MaximumMaxBytes)
                throw new ArgumentOutOfRangeException(nameof(maxBytes), "Chat request-target limit must be between 1024 and 16384 bytes.");
            MaxBytes = maxBytes;
        }

        public void Validate(string requestTarget)
        {
            if (string.IsNullOrEmpty(requestTarget)) throw new ArgumentException("Chat request target is required.", nameof(requestTarget));
            for (int index = 0; index < requestTarget.Length; index++) if (char.IsControl(requestTarget[index])) throw new ArgumentException("Chat request target contains a control character.", nameof(requestTarget));
            if (Encoding.UTF8.GetByteCount(requestTarget) > MaxBytes)
                throw new RemoteChatTransportException(RemoteChatError.LocalRequestTooLarge, "Chat request target exceeds the configured UTF-8 size limit.", 0, "local_request_target_too_large");
        }
    }

    public sealed class BoundedChatResponseBuffer
    {
        private readonly int maxBytes;
        private readonly MemoryStream stream;
        public bool LimitExceeded { get; private set; }
        public int Length => checked((int)stream.Length);
        public string Text => Encoding.UTF8.GetString(stream.ToArray());
        public byte[] ToArray() => stream.ToArray();

        public BoundedChatResponseBuffer(int maxBytes)
        {
            maxBytes = new ChatHttpResponsePolicy(maxBytes).MaxBytes;
            this.maxBytes = maxBytes;
            stream = new MemoryStream(Math.Min(maxBytes, 16384));
        }

        public bool TryAppend(byte[] data, int count)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (count < 0 || count > data.Length) throw new ArgumentOutOfRangeException(nameof(count));
            if (LimitExceeded) return false;
            if (count > maxBytes - stream.Length) { LimitExceeded = true; return false; }
            stream.Write(data, 0, count);
            return true;
        }
    }

    internal sealed class BoundedChatDownloadHandler : DownloadHandlerScript
    {
        private readonly BoundedChatResponseBuffer buffer;
        public bool LimitExceeded => buffer.LimitExceeded;
        public string ResponseText => buffer.Text;
        public BoundedChatDownloadHandler(int maxBytes) { buffer = new BoundedChatResponseBuffer(maxBytes); }
        protected override bool ReceiveData(byte[] data, int dataLength) => data != null && dataLength > 0 && buffer.TryAppend(data, dataLength);
        protected override byte[] GetData() => buffer.ToArray();
        protected override string GetText() => buffer.Text;
    }

    public interface IChatJsonCodec
    {
        string Serialize(object value);
        T Deserialize<T>(string json);
    }

    public sealed class UnityWebRequestChatRestTransport : IChatRestTransport
    {
        private readonly string baseUrl;
        private readonly IChatJsonCodec json;
        private readonly ChatHttpTimeoutPolicy timeoutPolicy;
        private readonly ChatHttpResponsePolicy responsePolicy;
        private readonly ChatHttpRequestPolicy requestPolicy;
        private readonly ChatHttpRequestTargetPolicy requestTargetPolicy;

        public UnityWebRequestChatRestTransport(string baseUrl, IChatJsonCodec json, TimeSpan? requestTimeout = null, int maxResponseBytes = ChatHttpResponsePolicy.DefaultMaxBytes, int maxRequestBytes = ChatHttpRequestPolicy.DefaultMaxBytes, int maxRequestTargetBytes = ChatHttpRequestTargetPolicy.DefaultMaxBytes)
        {
            this.baseUrl = ChatEndpointUrl.NormalizeBaseUrl(baseUrl, allowInsecureLoopback: true);
            this.json = json ?? throw new ArgumentNullException(nameof(json));
            timeoutPolicy = new ChatHttpTimeoutPolicy(requestTimeout);
            responsePolicy = new ChatHttpResponsePolicy(maxResponseBytes);
            requestPolicy = new ChatHttpRequestPolicy(maxRequestBytes);
            requestTargetPolicy = new ChatHttpRequestTargetPolicy(maxRequestTargetBytes);
        }

        public async Task<ChatTransportResponse<T>> SendAsync<T>(ChatTransportRequest request, CancellationToken cancellationToken)
        {
            ChatHttpRequestInvariant.Validate(request);
            cancellationToken.ThrowIfCancellationRequested();
            using (UnityWebRequest webRequest = Build(request))
            {
                var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                UnityWebRequestAsyncOperation operation = webRequest.SendWebRequest();
                operation.completed += _ => completion.TrySetResult(true);
                using (cancellationToken.Register(() => { webRequest.Abort(); completion.TrySetCanceled(cancellationToken); }))
                    await completion.Task;

                BoundedChatDownloadHandler download = webRequest.downloadHandler as BoundedChatDownloadHandler;
                bool transportFailed = download != null && download.LimitExceeded || webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.DataProcessingError;
                string body = transportFailed || download == null ? string.Empty : download.ResponseText;
                int statusCode = checked((int)webRequest.responseCode);
                T parsed = default(T);
                if (!transportFailed && statusCode >= 200 && statusCode < 300 && !string.IsNullOrWhiteSpace(body)) parsed = json.Deserialize<T>(body);
                int retrySeconds;
                int? retryAfter = int.TryParse(webRequest.GetResponseHeader("Retry-After"), out retrySeconds) ? Math.Max(0, retrySeconds) : (int?)null;
                int ageSeconds;
                int? age = int.TryParse(webRequest.GetResponseHeader("Age"), out ageSeconds) ? Math.Max(0, ageSeconds) : (int?)null;
                string transportError = download != null && download.LimitExceeded ? "Chat response exceeded the configured size limit." : transportFailed ? webRequest.error : null;
                return new ChatTransportResponse<T> { StatusCode = statusCode, Body = parsed, RawBody = body, RetryAfterSeconds = retryAfter, CacheControl = webRequest.GetResponseHeader("Cache-Control"), AgeSeconds = age, TransportError = transportError };
            }
        }

        private UnityWebRequest Build(ChatTransportRequest request)
        {
            requestTargetPolicy.Validate(request.Path);
            string url = ChatEndpointUrl.Compose(baseUrl, request.Path);
            byte[] payload = request.Body == null ? null : requestPolicy.EncodeJson(json.Serialize(request.Body));
            var webRequest = new UnityWebRequest(url, request.Method ?? "GET")
            {
                downloadHandler = new BoundedChatDownloadHandler(responsePolicy.MaxBytes),
                redirectLimit = ChatHttpSecurityPolicy.RedirectLimit,
                timeout = timeoutPolicy.TimeoutSeconds
            };
            if (payload != null)
            {
                webRequest.uploadHandler = new UploadHandlerRaw(payload);
                webRequest.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
            }
            webRequest.SetRequestHeader("Accept", "application/json");
            if (request.BypassCache)
            {
                webRequest.SetRequestHeader("Cache-Control", "no-cache, no-store, max-age=0");
                webRequest.SetRequestHeader("Pragma", "no-cache");
            }
            if (!string.IsNullOrEmpty(request.BearerToken))
            {
                if (!ChatSessionSecurity.IsValidBearerToken(request.BearerToken)) throw new ArgumentException("Bearer token is malformed.", nameof(request));
                webRequest.SetRequestHeader("Authorization", "Bearer " + request.BearerToken);
            }
            return webRequest;
        }
    }
}
