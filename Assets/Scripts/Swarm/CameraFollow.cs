using UnityEngine;

namespace Cellstorm.Swarm
{
    /// <summary>Put on the main camera. Follows the swarm, zooms out as it grows, and shakes on impacts.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public static CameraFollow Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
        }

        /// <summary>Screen shake. 0.1 = tiny, 0.5 = strong. Does nothing when there is no camera follow.</summary>
        public static void Shake(float amount)
        {
            if (Instance != null) Instance.AddTrauma(amount);
        }

        [SerializeField] SwarmBody target;
        [SerializeField] float positionSmoothTime = 0.12f;
        [SerializeField] float minOrthoSize = 5f;
        [Tooltip("Orthographic size = swarm radius * this (when larger than the minimum).")]
        [SerializeField] float orthoPerRadius = 2.6f;
        [SerializeField] float zoomSharpness = 3f;

        [Header("Shake")]
        [SerializeField] float maxShakeOffset = 0.6f;
        [SerializeField] float shakeDecayPerSecond = 1.8f;

        Camera _cam;
        Vector3 _velocity;
        Vector3 _basePosition;
        float _trauma;

        void Awake()
        {
            Instance = this;
            _cam = GetComponent<Camera>();
            _cam.orthographic = true;
            _basePosition = transform.position;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void AddTrauma(float amount)
        {
            _trauma = Mathf.Clamp01(_trauma + Mathf.Max(0f, amount));
        }

        void Start()
        {
            if (target == null) target = FindAnyObjectByType<SwarmBody>();
            if (target == null) return;

            var p = target.transform.position;
            _basePosition = new Vector3(p.x, p.y, _basePosition.z);
            transform.position = _basePosition;
            _cam.orthographicSize = DesiredSize();
        }

        void LateUpdate()
        {
            if (target == null) return;

            var p = target.transform.position;
            var goal = new Vector3(p.x, p.y, _basePosition.z);
            _basePosition = Vector3.SmoothDamp(_basePosition, goal, ref _velocity, positionSmoothTime);

            float dt = Time.deltaTime;
            _trauma = Mathf.Max(0f, _trauma - shakeDecayPerSecond * dt);

            Vector3 offset = Vector3.zero;
            if (_trauma > 0f)
            {
                float t = Time.time * 40f;
                float s = _trauma * _trauma;
                offset = new Vector3(
                    (Mathf.PerlinNoise(t, 0.1f) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(0.3f, t) - 0.5f) * 2f,
                    0f) * (maxShakeOffset * s);
            }
            transform.position = _basePosition + offset;

            float k = 1f - Mathf.Exp(-zoomSharpness * dt);
            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, DesiredSize(), k);
        }

        float DesiredSize()
        {
            return Mathf.Max(minOrthoSize, target.SmoothedRadius * orthoPerRadius);
        }
    }
}
