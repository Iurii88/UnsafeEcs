namespace UnsafeEcs.Core.Worlds
{
    /// <summary>
    /// Stores the name of a world. Used with WorldData to provide named worlds.
    /// </summary>
    public class WorldName
    {
        public string Value { get; set; }

        public WorldName(string name)
        {
            Value = name;
        }

        public override string ToString() => Value;
    }
}
