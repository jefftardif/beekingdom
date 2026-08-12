using BeeKingdom.Protocol.Errors;
using BeeKingdom.Protocol.Messages;

namespace BeeKingdom.Protocol.Diagnostics;

public sealed class ProtocolDiagnostics
{
    private readonly Dictionary<ProtocolMessageType, long> messagesByType = new();
    private readonly Dictionary<ProtocolErrorCode, long> errorsByCode = new();

    public long MessageCount { get; private set; }
    public long BytesSerialized { get; private set; }
    public long BytesDeserialized { get; private set; }
    public long ErrorCount { get; private set; }
    public long ProcessingTicks { get; private set; }

    public IReadOnlyDictionary<ProtocolMessageType, long> MessagesByType => messagesByType;
    public IReadOnlyDictionary<ProtocolErrorCode, long> ErrorsByCode => errorsByCode;

    public void RecordMessage(ProtocolMessageType messageType, int byteCount, long processingTicks)
    {
        MessageCount++;
        BytesSerialized += byteCount;
        ProcessingTicks += processingTicks;
        messagesByType[messageType] = messagesByType.TryGetValue(messageType, out long current) ? current + 1 : 1;
    }

    public void RecordDeserialize(int byteCount)
    {
        BytesDeserialized += byteCount;
    }

    public void RecordError(ProtocolErrorCode errorCode)
    {
        ErrorCount++;
        errorsByCode[errorCode] = errorsByCode.TryGetValue(errorCode, out long current) ? current + 1 : 1;
    }
}
