using MHServerEmu.Core.Config;

namespace MHServerEmu.Games.Loot
{
    public class LootConfig : ConfigContainer
    {
        /// <summary>
        /// If true, all currencies (Eternity Splinters, Odin Marks, Cosmic Worldstones, etc.)
        /// are automatically collected instead of spawning as physical world pickups.
        /// Default: true
        /// </summary>
        public bool AutoCollectCurrencies { get; private set; } = true;

        /// <summary>
        /// Overrides the pickup radius for all orbs (XP, heal, endurance, etc.).
        /// Set to 0 to use the default values from the game data.
        /// Default: 0
        /// </summary>
        public float OrbPickupRadius { get; private set; } = 0f;
    }
}
