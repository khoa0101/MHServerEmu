using MHServerEmu.Core.Config;

namespace MHServerEmu.Games.Loot
{
    public class LootVaporizerConfig : ConfigContainer
    {
        /// <summary>
        /// If true, Medal items will be subject to loot vaporization.
        /// Uses the Gear01 rarity threshold as a proxy since there is no client UI for configuring it independently.
        /// Default: true
        /// </summary>
        public bool VaporizeMedal { get; private set; } = true;

        /// <summary>
        /// If true, Ring and Insignia items will be subject to loot vaporization.
        /// Uses the Gear01 rarity threshold as a proxy since there is no client UI for configuring them independently.
        /// Default: true
        /// </summary>
        public bool VaporizeRingInsignia { get; private set; } = true;

        /// <summary>
        /// If true, Team-Up gear items will be subject to loot vaporization.
        /// Uses the current team-up agent's equipment inventory assignments to determine the slot and threshold.
        /// Has no effect if no team-up agent is active.
        /// Default: true
        /// </summary>
        public bool VaporizeTeamUpGear { get; private set; } = true;
    }
}
