using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeeKingdom.Networking
{
    public enum AuthenticatedGameRestError
    {
        NetworkFailure = 0,
        Unauthorized = 1,
        RemoteRejected = 2,
        InvalidResponse = 3
    }

    public sealed class AuthenticatedGameRestException : Exception
    {
        public AuthenticatedGameRestException(
            AuthenticatedGameRestError error,
            string safeCode,
            int statusCode = 0)
            : base(safeCode ?? string.Empty)
        {
            Error = error;
            SafeCode = safeCode ?? string.Empty;
            StatusCode = statusCode;
        }

        public AuthenticatedGameRestError Error { get; }
        public string SafeCode { get; }
        public int StatusCode { get; }
    }

    public interface IGameJsonCodec
    {
        string Serialize<T>(T value);
        T Deserialize<T>(string json);
    }

    public sealed class SystemTextGameJsonCodec : IGameJsonCodec
    {
        private readonly JsonSerializerOptions options;

        public SystemTextGameJsonCodec()
        {
            options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = false,
                ReadCommentHandling = JsonCommentHandling.Disallow,
                MaxDepth = 32,
                // M043L-CL: several wire request DTOs (CreateAllianceWireRequest,
                // SubmitApplicationWireRequest, CreateInvitationWireRequest, UpdateProfileWireRequest)
                // declare their data as public fields, not properties - System.Text.Json ignores
                // fields entirely unless told otherwise, so every one of them was silently
                // serializing to "{}" and the server was rejecting the resulting all-null/all-default
                // request body (e.g. alliance.invalid_request from a null ClientRequestId).
                IncludeFields = true
            };
            options.Converters.Add(new BeeGuidJsonConverter());
            // M043M-CL: the server serializes every enum as a string (Program.cs registers
            // builder.Services.ConfigureHttpJsonOptions with a plain JsonStringEnumConverter()), but
            // this codec never mirrored that - every enum response field (AllianceJoinMode,
            // AllianceStatus, AllianceRole, etc.) failed to deserialize the moment a real value other
            // than the JSON default ever came back. Invisible until now because no successful
            // AllianceEntity had ever round-tripped through this codec before M043L fixed Create.
            options.Converters.Add(new JsonStringEnumConverter());
        }

        public string Serialize<T>(T value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return JsonSerializer.Serialize(value, options);
        }

        public T Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new JsonException("The game JSON payload is empty.");
            T value = JsonSerializer.Deserialize<T>(json, options);
            if (value == null) throw new JsonException("The game JSON payload is null.");
            return value;
        }

        private sealed class BeeGuidJsonConverter : JsonConverter<Guid>
        {
            public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions serializerOptions)
            {
                string text = null;
                if (reader.TokenType == JsonTokenType.String)
                {
                    text = reader.GetString();
                }
                else if (reader.TokenType == JsonTokenType.StartObject)
                {
                    using (JsonDocument document = JsonDocument.ParseValue(ref reader))
                    {
                        JsonElement value;
                        if ((document.RootElement.TryGetProperty("value", out value) ||
                             document.RootElement.TryGetProperty("Value", out value)) &&
                            value.ValueKind == JsonValueKind.String)
                            text = value.GetString();
                    }
                }

                Guid parsed;
                if (!Guid.TryParseExact(text, "D", out parsed))
                    throw new JsonException("A game identifier is malformed.");
                return parsed;
            }

            public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions serializerOptions)
            {
                writer.WriteStringValue(value.ToString("D"));
            }
        }
    }
}
