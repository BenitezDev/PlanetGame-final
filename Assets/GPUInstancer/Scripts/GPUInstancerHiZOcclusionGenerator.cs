using UnityEngine;
using UnityEngine.Rendering;

namespace GPUInstancer
{
    public class GPUInstancerHiZOcclusionGenerator : MonoBehaviour
    {
        public bool debuggerEnabled = false;
        [Range(0, 16)]
        public int debuggerHiZMipLevel = 0;
        public Camera mainCamera { get; set; }
        public Vector2 hiZTextureSize;
        public RenderTexture hiZDepthTexture { get; private set; }

        private int hiZMipLevels = 0;
        private int[] hiZMipLevelIDs = null;
        private Shader hiZShader = null;
        private Material hiZMaterial = null;
        private CommandBuffer hiZBuffer = null;
        private CameraEvent cameraEvent = CameraEvent.AfterEverything;
        private GPUInstancerHiZOcclusionDebugger hiZOcclusionDebugger = null;
        private enum ShaderPass { SampleDepth, Reduce }
        
        #region MonoBehaviour Methods

        private void Awake()
        {
            hiZShader = Shader.Find(GPUInstancerConstants.SHADER_GPUI_HIZ_OCCLUSION_GENERATOR);
            hiZMaterial = new Material(hiZShader);
            hiZTextureSize = Vector2.zero;
        }

        private void OnDisable()
        {
            if (mainCamera != null)
            {
                if (hiZBuffer != null)
                {
                    mainCamera.RemoveCommandBuffer(cameraEvent, hiZBuffer);
                    hiZBuffer = null;
                }
            }

            if (hiZDepthTexture != null)
            {
                hiZDepthTexture.Release();
                hiZDepthTexture = null;
            }
        }

        private void LateUpdate()
        {
            if (debuggerEnabled)
            {
                if (hiZOcclusionDebugger == null)
                {
                    hiZOcclusionDebugger = mainCamera.gameObject.AddComponent<GPUInstancerHiZOcclusionDebugger>();
                }

                hiZOcclusionDebugger.debuggerHiZMipLevel = debuggerHiZMipLevel;
            }

            if (!debuggerEnabled && hiZOcclusionDebugger != null)
            {
                DestroyImmediate(hiZOcclusionDebugger);
            }
        }

        public void OnPreRender()
        {
            hiZTextureSize.x = Mathf.NextPowerOfTwo(mainCamera.pixelWidth);
            hiZTextureSize.y = Mathf.NextPowerOfTwo(mainCamera.pixelHeight);
            hiZMipLevels = (int)Mathf.Floor(Mathf.Log(hiZTextureSize.x, 2f));

            bool isCommandBufferInvalid = false;
            if (hiZMipLevels == 0)
            {
                return;
            }

            if (hiZDepthTexture == null || (hiZDepthTexture.width != (int)hiZTextureSize.x || hiZDepthTexture.height != (int)hiZTextureSize.y))
            {
                if (hiZDepthTexture != null)
                    hiZDepthTexture.Release();

                hiZDepthTexture = new RenderTexture((int)hiZTextureSize.x, (int)hiZTextureSize.y, 0, RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear);
                hiZDepthTexture.filterMode = FilterMode.Point;
                hiZDepthTexture.useMipMap = true;
                hiZDepthTexture.autoGenerateMips = false;
                hiZDepthTexture.Create();
                hiZDepthTexture.hideFlags = HideFlags.HideAndDontSave;

                isCommandBufferInvalid = true;
            }

            if (hiZBuffer == null || isCommandBufferInvalid == true)
            {
                hiZMipLevelIDs = new int[hiZMipLevels];

                if (hiZBuffer != null)
                    mainCamera.RemoveCommandBuffer(cameraEvent, hiZBuffer);

                hiZBuffer = new CommandBuffer();
                hiZBuffer.name = "GPU Instancer Hi-Z Buffer";

                RenderTargetIdentifier id = new RenderTargetIdentifier(hiZDepthTexture);

                hiZBuffer.Blit(null, id, hiZMaterial, (int)ShaderPass.SampleDepth);

                for (int i = 0; i < hiZMipLevels; ++i)
                {
                    hiZMipLevelIDs[i] = Shader.PropertyToID("GPU_Instancer_HiZ_Mip_Level_" + i.ToString());

                    int width = (int)hiZTextureSize.x;
                    width = width >> 1;
                    int height = (int)hiZTextureSize.y;
                    height = height >> 1;

                    if (width == 0)
                        width = 1;

                    if (height == 0)
                        height = 1;

                    hiZTextureSize = new Vector2(width, height);

                    hiZBuffer.GetTemporaryRT(hiZMipLevelIDs[i], width, height, 0, FilterMode.Point, RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear);

                    if (i == 0)
                        hiZBuffer.Blit(id, hiZMipLevelIDs[0], hiZMaterial, (int)ShaderPass.Reduce);
                    else
                        hiZBuffer.Blit(hiZMipLevelIDs[i - 1], hiZMipLevelIDs[i], hiZMaterial, (int)ShaderPass.Reduce);

                    hiZBuffer.CopyTexture(hiZMipLevelIDs[i], 0, 0, id, 0, i + 1);

                    if (i >= 1)
                        hiZBuffer.ReleaseTemporaryRT(hiZMipLevelIDs[i - 1]);
                }

                hiZBuffer.ReleaseTemporaryRT(hiZMipLevelIDs[hiZMipLevels - 1]);

                mainCamera.AddCommandBuffer(cameraEvent, hiZBuffer);
            }
        }

        #endregion
    }

}