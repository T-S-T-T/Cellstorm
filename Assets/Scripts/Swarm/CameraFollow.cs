using UnityEngine;

namespace Cellstorm.Swarm
{
    /// <summary>Put on the main camera. Follows the swarm and zooms out as it grows.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] SwarmBody target;
        [SerializeField] float positionSmoothTime = 0.12f;
        [SerializeField] float minOrthoSize = 5f;
        [Tooltip("Orthographic size = swarm radius * this (when larger than the minimum).")]
        [SerializeField] float orthoPerRadius = 2.6f;
        [SerializeField] float zoomSharpness = 3f;

        Camera _cam;
        Vector3 _velocity;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.orthographic = true;
        }

        void Start()
        {
            if (target == null) target = FindAnyObjectByType<SwarmBody>();
            if (target == null) return;

            var p = target.transform.position;
            transform.position = new Vector3(p.x, p.y, transform.position.z);
            _cam.orthographicSize = DesiredSize();
        }

        void LateUpdate()
        {
            if (target == null) return;

            var p = target.transform.position;
            var goal = new Vector3(p.x, p.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref _velocity, positionSmoothTime);

            float k = 1f - Mathf.Exp(-zoomSharpness * Time.deltaTime);
            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, DesiredSize(), k);
        }

        float DesiredSize()
        {
            return Mathf.Max(minOrthoSize, target.SmoothedRadius * orthoPerRadius);
        }
    }
}
