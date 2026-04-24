using MHServerEmu.Core.Config;

namespace MHServerEmu.Games.Common
{
    public class GodModeConfig : ConfigContainer
    {
        /// <summary>
        /// If true, god mode properties are automatically applied to all avatars on every region entry.
        /// Default: false
        /// </summary>
        public bool Enabled { get; private set; } = false;

        /// <summary>
        /// DamagePctBonus multiplier to apply. 1000 = 1000x damage.
        /// Default: 1000
        /// </summary>
        public int DamageMultiplier { get; private set; } = 1000;

        /// <summary>
        /// DamagePctBonusVsBosses multiplier to apply. 1000 = 1000x damage vs bosses.
        /// Default: 1000
        /// </summary>
        public int BossMultiplier { get; private set; } = 1000;

        /// <summary>
        /// If true, invulnerability is applied along with the damage boost.
        /// Default: true
        /// </summary>
        public bool Invulnerable { get; private set; } = true;

        /// <summary>
        /// If true, resource costs are disabled along with damage boosts and invulnerability.
        /// Default: true
        /// <summary>
        public bool Mana { get; private set; } = true;
    }
}
