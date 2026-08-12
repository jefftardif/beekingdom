using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Population
{
    public enum CommunicationKind { Pheromone, Dance, DirectContact, Vibration, ColonyBroadcast, QueenSignal, EmergencySignal, RecruitmentSignal }
    public enum CommunicationSignalType { FoodFound, DangerDetected, ConstructionNeeded, DefenseRequired, QueenNeedsHelp, SwarmPreparation, ResourceShortage, PathBlocked, NewNursery, ColonyEmergency, Custom }

    public sealed class CommunicationChannel
    {
        public string ChannelId { get; }
        public CommunicationKind Kind { get; }
        public double SaturationLimit { get; }
        public CommunicationChannel(string channelId, CommunicationKind kind, double saturationLimit)
        {
            ChannelId = string.IsNullOrWhiteSpace(channelId) ? throw new ArgumentException("Channel id is required.", nameof(channelId)) : channelId;
            Kind = kind;
            SaturationLimit = Math.Max(1d, saturationLimit);
        }
    }

    public sealed class CommunicationSignal
    {
        public string SignalId { get; }
        public CommunicationSignalType Type { get; }
        public string Origin { get; }
        public string ChannelId { get; }
        public double Radius { get; private set; }
        public double Intensity { get; private set; }
        public double Decay { get; }
        public double Lifetime { get; }
        public double Priority { get; }
        public double Age { get; private set; }
        public CommunicationSignal(string signalId, CommunicationSignalType type, string origin, string channelId, double radius, double intensity, double decay, double lifetime, double priority)
        {
            SignalId = signalId;
            Type = type;
            Origin = origin ?? string.Empty;
            ChannelId = channelId ?? string.Empty;
            Radius = Math.Max(0d, radius);
            Intensity = Clamp01(intensity);
            Decay = Clamp01(decay);
            Lifetime = Math.Max(0d, lifetime);
            Priority = Clamp01(priority);
        }
        public void Propagate(double delta) { Age += Math.Max(0d, delta); Intensity = Clamp01(Intensity - Decay * Math.Max(0d, delta)); Radius = Math.Max(0d, Radius + delta); }
        public bool Expired => Age >= Lifetime || Intensity <= 0d;
        private static double Clamp01(double value) => value < 0d ? 0d : value > 1d ? 1d : value;
    }

    public sealed class SignalPropagationEngine
    {
        public void Propagate(CommunicationSignal signal, double delta) => signal.Propagate(delta);
        public bool CanReceive(CommunicationSignal signal, double perceptionRange, double sensitivity) => signal.Radius <= perceptionRange && signal.Intensity * sensitivity > 0.01d;
    }

    public sealed class SwarmCommunicationEngine
    {
        private readonly SignalPropagationEngine propagation = new SignalPropagationEngine();
        public void PropagateSignal(CommunicationSignal signal, double delta) => propagation.Propagate(signal, delta);
        public bool ReceiveSignal(CommunicationSignal signal, double perceptionRange, double sensitivity) => propagation.CanReceive(signal, perceptionRange, sensitivity);
    }

    public sealed class CommunicationDiagnostics
    {
        public int Broadcast { get; private set; }
        public int Received { get; private set; }
        public int Expired { get; private set; }
        public int Blocked { get; private set; }
        public void RecordBroadcast() => Broadcast++;
        public void RecordReceived() => Received++;
        public void RecordExpired() => Expired++;
        public void RecordBlocked() => Blocked++;
    }

    public sealed class SwarmCommunicationManager
    {
        private readonly Dictionary<string, CommunicationChannel> channels = new Dictionary<string, CommunicationChannel>();
        private readonly List<CommunicationSignal> signals = new List<CommunicationSignal>();
        private readonly SwarmCommunicationEngine engine = new SwarmCommunicationEngine();
        private readonly IEventBus eventBus;
        private int sequence;
        public CommunicationDiagnostics Diagnostics { get; } = new CommunicationDiagnostics();
        public SwarmCommunicationManager(IEventBus eventBus = null) { this.eventBus = eventBus; }
        public bool RegisterCommunicationChannel(CommunicationChannel channel) { if (channel == null || channels.ContainsKey(channel.ChannelId)) return false; channels.Add(channel.ChannelId, channel); return true; }
        public CommunicationSignal BroadcastSignal(string channelId, CommunicationSignalType type, string origin, double radius, double intensity, double decay, double lifetime, double priority)
        {
            if (!channels.TryGetValue(channelId, out CommunicationChannel channel)) return null;
            if (CountChannelSignals(channelId) >= channel.SaturationLimit) { Diagnostics.RecordBlocked(); eventBus?.Publish(new CommunicationBlocked(channelId)); return null; }
            CommunicationSignal signal = new CommunicationSignal("signal-" + (++sequence).ToString("D6"), type, origin, channelId, radius, intensity, decay, lifetime, priority);
            signals.Add(signal);
            Diagnostics.RecordBroadcast();
            eventBus?.Publish(new SignalBroadcast(signal.SignalId));
            return signal;
        }
        public bool ReceiveSignal(string signalId, double perceptionRange, double sensitivity)
        {
            CommunicationSignal signal = Find(signalId);
            bool received = signal != null && engine.ReceiveSignal(signal, perceptionRange, sensitivity);
            if (received) { Diagnostics.RecordReceived(); eventBus?.Publish(new SignalReceived(signalId)); }
            return received;
        }
        public void PropagateSignal(double delta) { for (int i = 0; i < signals.Count; i++) engine.PropagateSignal(signals[i], delta); }
        public void ExpireSignal()
        {
            for (int i = signals.Count - 1; i >= 0; i--)
            {
                if (!signals[i].Expired) continue;
                string id = signals[i].SignalId;
                signals.RemoveAt(i);
                Diagnostics.RecordExpired();
                eventBus?.Publish(new SignalExpired(id));
            }
        }
        public IReadOnlyList<CommunicationSignal> QuerySignals() => signals;
        private int CountChannelSignals(string channelId) { int count = 0; for (int i = 0; i < signals.Count; i++) if (signals[i].ChannelId == channelId) count++; return count; }
        private CommunicationSignal Find(string signalId) { for (int i = 0; i < signals.Count; i++) if (signals[i].SignalId == signalId) return signals[i]; return null; }
    }

    public readonly struct SignalBroadcast : IGameplayEvent, IBeeEvent { public string SignalId { get; } public SignalBroadcast(string signalId) { SignalId = signalId; } }
    public readonly struct SignalReceived : IGameplayEvent, IBeeEvent { public string SignalId { get; } public SignalReceived(string signalId) { SignalId = signalId; } }
    public readonly struct SignalExpired : IGameplayEvent, IBeeEvent { public string SignalId { get; } public SignalExpired(string signalId) { SignalId = signalId; } }
    public readonly struct CommunicationBlocked : IGameplayEvent, IBeeEvent { public string ChannelId { get; } public CommunicationBlocked(string channelId) { ChannelId = channelId; } }
    public readonly struct ColonyBroadcastCompleted : IGameplayEvent, IBeeEvent { }
}
