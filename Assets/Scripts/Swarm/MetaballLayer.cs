using UnityEngine;

namespace Cellstorm.Swarm
{
    /// <summary>
    /// Metaball pipeline: a helper camera renders everything on the Metaball layer (the soft dots)
    /// into a RenderTexture; a screen-sized quad in front of the main camera draws that texture
    /// through a threshold shader, which turns the merged soft field into one hard-edged blob.
    /// One instance per faction later (Player / Enemy) with different layers and colours.
    /// </summary>
    [DefaultExecutionOrder(100)] // after CameraFollow
    public class MetaballLayer : MonoBehaviour
    {
        [SerializeField] string layerName = "Metaball";
        [Tooltip("Lower for speed (0.5 = half resolution). Edges stay smooth thanks to filtering.")]
        [SerializeField, Range(0.25f, 1f)] float resolutionScale = 1f;

        [Header("Look")]
        [SerializeField, Range(0.1f, 0.9f)] float threshold = 0.5f;
        [SerializeField, Range(0.001f, 0.2f)] float softness = 0.03f;
        [SerializeField, Range(0f, 0.5f)] float outlineWidth = 0.12f;
        [SerializeField] Color outlineColor = new Color(0.05f, 0.30f, 0.20f, 1f);
        [SerializeField] int sortingOrder = 10;

        Camera _main;
        Camera _cam;
        RenderTexture _rt;
        Transform _quad;
        Material _mat;
        int _layer;
        int _w, _h;

        void Start()
        {
            _layer = LayerMask.NameToLayer(layerName);
            if (_layer < 0)
            {
                Debug.LogError($"MetaballLayer: layer '{layerName}' does not exist. " +
                               "Run the menu Cellstorm > Phase 1 > Add Metaball Layer.");
                enabled = false;
                return;
            }

            _main = Camera.main;
            if (_main == null)
            {
                Debug.LogError("MetaballLayer: no camera tagged MainCamera found.");
                enabled = false;
                return;
            }

            var shader = Resources.Load<Shader>("MetaballThreshold");
            if (shader == null) shader = Shader.Find("Cellstorm/MetaballThreshold");
            if (shader == null)
            {
                Debug.LogError("MetaballLayer: shader 'MetaballThreshold' not found (it must be in a Resources folder).");
                enabled = false;
                return;
            }
            _mat = new Material(shader);

            // The main camera must not draw the raw dots.
            _main.cullingMask &= ~(1 << _layer);

            // Helper camera that renders only the dots into a RenderTexture.
            var camGo = new GameObject("MetaballCamera");
            camGo.transform.SetParent(_main.transform, false);
            _cam = camGo.AddComponent<Camera>();
            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _cam.cullingMask = 1 << _layer;
            _cam.depth = _main.depth - 1f;
            _cam.allowHDR = false;
            _cam.allowMSAA = false;
            _cam.nearClipPlane = _main.nearClipPlane;
            _cam.farClipPlane = _main.farClipPlane;

            // Screen-sized quad, child of the main camera, showing the result.
            var quadGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadGo.name = "MetaballQuad";
            var collider = quadGo.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            quadGo.transform.SetParent(_main.transform, false);
            quadGo.transform.localPosition = new Vector3(0f, 0f, 10f);
            var mr = quadGo.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _mat;
            mr.sortingOrder = sortingOrder;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _quad = quadGo.transform;

            EnsureRenderTexture();
        }

        void LateUpdate()
        {
            if (_cam == null) return;

            EnsureRenderTexture();

            _cam.orthographicSize = _main.orthographicSize;

            float height = 2f * _main.orthographicSize;
            _quad.localScale = new Vector3(height * _main.aspect, height, 1f);

            _mat.SetFloat("_Threshold", threshold);
            _mat.SetFloat("_Softness", softness);
            _mat.SetFloat("_OutlineWidth", outlineWidth);
            _mat.SetColor("_OutlineColor", outlineColor);
        }

        void EnsureRenderTexture()
        {
            int w = Mathf.Max(16, Mathf.RoundToInt(_main.pixelWidth * resolutionScale));
            int h = Mathf.Max(16, Mathf.RoundToInt(_main.pixelHeight * resolutionScale));
            if (_rt != null && w == _w && h == _h) return;

            _cam.targetTexture = null;
            if (_rt != null) _rt.Release();

            _w = w;
            _h = h;
            _rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1
            };
            _rt.Create();

            _cam.targetTexture = _rt;
            _mat.SetTexture("_MainTex", _rt);
        }

        void OnDestroy()
        {
            if (_main != null && _layer >= 0) _main.cullingMask |= 1 << _layer;
            if (_cam != null) _cam.targetTexture = null;
            if (_rt != null) { _rt.Release(); Destroy(_rt); }
            if (_mat != null) Destroy(_mat);
        }
    }
}
