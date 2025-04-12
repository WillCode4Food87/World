using System;
using System.Collections.Generic;
using Server;
using Server.Mobiles;

namespace Server.Custom.KoperPets
{
    public static class KoperDruidismGain
    {
        private static readonly Dictionary<Mobile, DateTime> _cooldowns = new Dictionary<Mobile, DateTime>();
        private static readonly TimeSpan CooldownTime = TimeSpan.FromSeconds(MyServerSettings.KoperCooldown()); // 20-second cooldown

        public static void TryGainDruidismSkill(Mobile owner)
        {
            if (owner == null || !owner.Alive || !MyServerSettings.KoperPets())
                return; // No skill gain for dead players/system disabled

            // Check if the player is on cooldown
            if (_cooldowns.ContainsKey(owner) && DateTime.UtcNow < _cooldowns[owner])
            {
                return; // Cooldown is active, exit without giving skill
            }

            double druidismSkill = owner.Skills[SkillName.Druidism].Base;
            double gainChance;
            double minGain;
            double maxGain;
            double druidismMultiplier = MyServerSettings.KoperDruidismChance();


            // Determine gain chance and amount based on skill level
            if (druidismMultiplier <= 0) druidismMultiplier = 1.0; // Ensure valid value
            if (druidismSkill <= 30.0) { gainChance = 0.20 * druidismMultiplier; minGain = 0.1; maxGain = 1.0; }
            else if (druidismSkill <= 50.0) { gainChance = 0.15 * druidismMultiplier; minGain = 0.1; maxGain = 0.5; }
            else if (druidismSkill <= 70.0) { gainChance = 0.10 * druidismMultiplier; minGain = 0.1; maxGain = 0.2; }
            else if (druidismSkill < 100.0) { gainChance = 0.05 * druidismMultiplier; minGain = 0.1; maxGain = 0.1; }
            else return; // No gain if at max skill

            if (Utility.RandomDouble() <= gainChance)
            {
                double skillGain = Utility.RandomDouble() * (maxGain - minGain) + minGain;
                owner.Skills[SkillName.Druidism].Base += skillGain;

                // Start cooldown timer
                _cooldowns[owner] = DateTime.UtcNow + CooldownTime;
            }
        }
    }
}
