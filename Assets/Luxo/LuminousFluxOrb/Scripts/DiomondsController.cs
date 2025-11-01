using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LuminousFluxOrb
{
    [ExecuteAlways]
    public class DiomondsController : MonoBehaviour
    {
        #region ComputeShader Properties

        public ComputeShader shader;
        public int texResolution = 1024;
        public Color clearColor = Color.black;
        public Color diomondsColor = Color.white;

        [Range(1, 20)]
        public int count = 10;

        struct Diomonds
        {
            public Vector2 origin;
            public Vector2 velocity;
            public float scale;
        }

        Diomonds[] diomondsData;

        #endregion

        #region Runtime Properties

        Renderer rend;
        RenderTexture outputTexture;

        int diomondsHandle;
        int clearHandle;

        ComputeBuffer buffer;

        #endregion

        #region Core Methods

        void OnEnable() {
#if UNITY_EDITOR
            EditorApplication.update += EditorUpdate;
#endif

            SetupOutputTexture();
            rend = GetComponent<Renderer>();
            if (rend != null) rend.enabled = true;

            InitData();
            InitShader();
        }

        void OnDisable() {
#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
#endif

            ReleaseResources();
        }

        void Start() {
            if (!Application.isPlaying) return;

            SetupOutputTexture();
            InitData();
            InitShader();
        }

        void Update() {
            if (Application.isPlaying) {
                DispatchKernel(count);
            }
        }

        private void OnDestroy() {
            ReleaseResources();
        }

        private void ReleaseResources() {
            if (buffer != null) {
                buffer.Dispose();
                buffer = null;
            }

            if (outputTexture != null) {
                outputTexture.Release();
                outputTexture = null;
            }
        }

        #endregion

        #region Compute Shader Methods

        private void SetupOutputTexture() {
            if (outputTexture != null) {
                outputTexture.Release();
            }

            outputTexture = new RenderTexture(texResolution, texResolution, 0);
            outputTexture.enableRandomWrite = true;
            outputTexture.wrapMode = TextureWrapMode.Repeat;
            outputTexture.Create();
        }

        private void InitData() {
            if (shader == null) return;

            diomondsHandle = shader.FindKernel("Diomonds");
            shader.GetKernelThreadGroupSizes(diomondsHandle, out uint threadGroupSizeX, out _, out _);

            int total = (int)threadGroupSizeX * count;
            diomondsData = new Diomonds[total];

            float speed = 100f;
            float halfSpeed = speed * 0.5f;
            float minRadius = 5f;
            float maxRadius = 10f;
            float radiusRange = maxRadius - minRadius;

            for (int i = 0; i < total; i++) {
                Diomonds d = new Diomonds {
                    origin = new Vector2(Random.value * texResolution, Random.value * texResolution),
                    velocity = new Vector2((Random.value * speed) - halfSpeed, (Random.value * speed) - halfSpeed),
                    scale = Random.value * radiusRange + minRadius
                };
                diomondsData[i] = d;
            }
        }

        private void InitShader() {
            if (shader == null || outputTexture == null) return;

            clearHandle = shader.FindKernel("Clear");

            shader.SetVector("clearColor", clearColor);
            shader.SetVector("diomondsColor", diomondsColor);
            shader.SetInt("texResolution", texResolution);

            int stride = (2 + 2 + 1) * 4;
            if (buffer != null) buffer.Dispose();
            buffer = new ComputeBuffer(diomondsData.Length, stride);
            buffer.SetData(diomondsData);

            shader.SetBuffer(diomondsHandle, "diomondsBuffer", buffer);
            shader.SetTexture(diomondsHandle, "Result", outputTexture);
            shader.SetTexture(clearHandle, "Result", outputTexture);

            if (rend == null)
                rend = GetComponent<Renderer>();

            if (rend != null && rend.sharedMaterial != null)
                rend.sharedMaterial.SetTexture("_MainTex", outputTexture);
        }

        private void DispatchKernel(int count) {
            if (shader == null || outputTexture == null) return;

            shader.Dispatch(clearHandle, texResolution / 8, texResolution / 8, 1);
            shader.SetFloat("time", Time.time);
            shader.Dispatch(diomondsHandle, count, 1, 1);
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        void EditorUpdate() {
            if (!Application.isPlaying) {
                DispatchKernel(count);
            }
        }

        private void OnValidate() {
            if (!enabled || shader == null) return;

            Reinitialize();
        }

        [ContextMenu("Reinitialize")]
        public void Reinitialize() {
            ReleaseResources();
            SetupOutputTexture();
            InitData();
            InitShader();
        }
#endif

        #endregion
    }
}