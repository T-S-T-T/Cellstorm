using UnityEngine;

namespace Cellstorm.Swarm
{
    /// <summary>One shared unlit sprite material, so many enemies, effects and cells batch together and allocate nothing.</summary>
    public static class SpriteMaterials
    {
        static Material _default;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _default = null;
        }

        public static Material Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new Material(Shader.Find("Sprites/Default")) { hideFlags = HideFlags.HideAndDontSave };
                }
                return _default;
            }
        }
    }
}
