namespace BeeKingdom.Buildings.Interaction
{
    public sealed class LivingHiveHotspotMetadata
    {
        public string HotspotId { get; }
        public string CellId { get; }
        public int ZoneNumber { get; }
        public string Label { get; }
        public string Role { get; }
        public string IconId { get; }
        public string StateIcon { get; }
        public string ActionLabel { get; }
        public string Disclosure { get; }
        public int Priority { get; }

        public LivingHiveHotspotMetadata(
            string hotspotId,
            string cellId,
            int zoneNumber,
            string label,
            string role,
            string iconId,
            string stateIcon,
            string actionLabel,
            string disclosure,
            int priority)
        {
            HotspotId = hotspotId;
            CellId = cellId;
            ZoneNumber = zoneNumber;
            Label = label;
            Role = role;
            IconId = iconId;
            StateIcon = stateIcon;
            ActionLabel = actionLabel;
            Disclosure = disclosure;
            Priority = priority;
        }
    }
}