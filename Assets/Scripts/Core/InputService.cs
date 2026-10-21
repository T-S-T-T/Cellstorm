using UnityEngine;
using UnityEngine.InputSystem;

namespace Cellstorm.Core
{
    /// <summary>
    /// Single access point for all player input. Loads the CellstormInput asset from Resources,
    /// works on a private runtime copy (so rebinding never edits the asset on disk) and applies
    /// any saved binding overrides. Rebinding UI comes in Phase 6; the hooks are already here.
    /// </summary>
    public static class InputService
    {
        const string ResourcePath = "CellstormInput";
        const string OverridesPrefsKey = "Cellstorm.InputOverrides";

        static InputActionAsset _asset;
        static InputActionMap _gameplay;
        static InputAction _move, _aim, _attack1, _attack2, _skill1, _skill2, _swap, _pause;

        public static InputAction Move    { get { EnsureInitialized(); return _move; } }
        public static InputAction Aim     { get { EnsureInitialized(); return _aim; } }
        public static InputAction Attack1 { get { EnsureInitialized(); return _attack1; } }
        public static InputAction Attack2 { get { EnsureInitialized(); return _attack2; } }
        public static InputAction Skill1  { get { EnsureInitialized(); return _skill1; } }
        public static InputAction Skill2  { get { EnsureInitialized(); return _skill2; } }
        public static InputAction Swap    { get { EnsureInitialized(); return _swap; } }
        public static InputAction Pause   { get { EnsureInitialized(); return _pause; } }

        /// <summary>The runtime copy of the asset. Use this for rebinding UI later.</summary>
        public static InputActionAsset Asset { get { EnsureInitialized(); return _asset; } }

        public static void Initialize() => EnsureInitialized();

        public static void SaveBindingOverrides()
        {
            EnsureInitialized();
            PlayerPrefs.SetString(OverridesPrefsKey, _asset.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();
        }

        public static void ResetBindingOverrides()
        {
            EnsureInitialized();
            _asset.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(OverridesPrefsKey);
        }

        static void EnsureInitialized()
        {
            if (_asset != null) return;

            var source = Resources.Load<InputActionAsset>(ResourcePath);
            if (source == null)
            {
                Debug.LogError($"InputService: could not find '{ResourcePath}' in a Resources folder.");
                return;
            }

            _asset = Object.Instantiate(source);

            if (PlayerPrefs.HasKey(OverridesPrefsKey))
                _asset.LoadBindingOverridesFromJson(PlayerPrefs.GetString(OverridesPrefsKey));

            _gameplay = _asset.FindActionMap("Gameplay", true);
            _move    = _gameplay.FindAction("Move", true);
            _aim     = _gameplay.FindAction("Aim", true);
            _attack1 = _gameplay.FindAction("Attack1", true);
            _attack2 = _gameplay.FindAction("Attack2", true);
            _skill1  = _gameplay.FindAction("Skill1", true);
            _skill2  = _gameplay.FindAction("Skill2", true);
            _swap    = _gameplay.FindAction("Swap", true);
            _pause   = _gameplay.FindAction("Pause", true);

            _gameplay.Enable();
        }

        // Keeps things clean if "Enter Play Mode without domain reload" is enabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _gameplay?.Disable();
            _asset = null;
            _gameplay = null;
            _move = _aim = _attack1 = _attack2 = _skill1 = _skill2 = _swap = _pause = null;
        }
    }
}
