using Cellstorm.Combat;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>What an ability needs from the swarm that owns it (player body or enemy).</summary>
    public interface IAbilityHost : IKnockbackable
    {
        Vector2 AimDirection { get; }
        /// <summary>Abilities lower this while shaping or executing.</summary>
        float TurnRateMultiplier { get; set; }

        /// <summary>Ram: overrides normal movement with a straight dash for 'duration' seconds.</summary>
        void BeginDash(Vector2 direction, float speed, float duration);
        /// <summary>Ram cancelled early (death, unequip).</summary>
        void EndDash();
    }
}
