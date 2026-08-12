namespace BeeKingdom.Hive
{
    public readonly struct QueenHealth
    {
        public int Current { get; }
        public int Maximum { get; }
        public float Ratio => Maximum <= 0 ? 0f : (float)Current / Maximum;
        public bool IsDead => Current <= 0;
        public bool IsInjured => !IsDead && Ratio < 0.35f;

        public QueenHealth(int current, int maximum)
        {
            Maximum = maximum <= 0 ? 1 : maximum;
            Current = current < 0 ? 0 : current > Maximum ? Maximum : current;
        }
    }
}
