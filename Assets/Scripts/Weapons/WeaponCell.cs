using Cellstorm.Combat;
using Cellstorm.Swarm;
using UnityEngine;

namespace Cellstorm.Weapons
{
    /// <summary>
    /// One physical cell of a weapon shape. A hostile swarm with higher DEF touching it (or a hostile bullet /
    /// bite with higher DEF) breaks it, which interrupts the weapon being formed.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class WeaponCell : MonoBehaviour
    {
        public ShapedAbility Ability { get; private set; }
        public Faction Faction => Ability.Faction;
        public float Defense => Ability.Defense;

        SpriteRenderer _sr;
        float _baseScale = 1f;
        float _graceUntil;

        public static WeaponCell Create(ShapedAbility ability, Vector2 position, CellStyle style, float radius)
        {
            var go = new GameObject("WeaponCell");
            go.transform.position = position;
            if (style.metaball) go.layer = style.layer;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = radius;

            var cell = go.AddComponent<WeaponCell>();
            cell.Init(ability, style, radius);
            return cell;
        }

        void Init(ShapedAbility ability, CellStyle style, float radius)
        {
            Ability = ability;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(transform, false);
            if (style.metaball) visual.layer = style.layer;

            _sr = visual.AddComponent<SpriteRenderer>();
            _sr.sharedMaterial = style.material;
            _sr.color = style.color;

            if (style.metaball)
            {
                _sr.sprite = ProceduralSprites.Soft;
                _baseScale = 2f * radius / ProceduralSprites.SoftVisibleRadiusFactor;
            }
            else
            {
                _sr.sprite = ProceduralSprites.Hard;
                _sr.sortingOrder = 6;
                _baseScale = 2f * radius;
            }
            visual.transform.localScale = Vector3.one * _baseScale;

            _graceUntil = Time.time + 0.3f;
        }

        public void MoveToward(Vector2 target, float follow, bool primed)
        {
            Vector2 p = Vector2.Lerp(transform.position, target, follow);
            transform.position = new Vector3(p.x, p.y, 0f);

            float pulse = primed ? 1f + 0.15f * Mathf.Sin(Time.time * 14f) : 1f;
            _sr.transform.localScale = Vector3.one * (_baseScale * pulse);
        }

        public void ResetToBody(Vector2 center)
        {
            transform.position = new Vector3(center.x, center.y, 0f);
            _graceUntil = Time.time + 0.6f;
        }

        public void Break()
        {
            if (Ability == null || Time.time < _graceUntil) return;
            _graceUntil = Time.time + 0.6f;
            Ability.NotifyCellBroken(this);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            var rb = other.attachedRigidbody;
            if (rb == null) return;
            if (!rb.TryGetComponent<SwarmCombat>(out var c)) return;
            if (c.Faction == Faction || c.Stats.IsDead) return;

            if (c.Stats.Defense > Defense) Break();
        }
    }
}
