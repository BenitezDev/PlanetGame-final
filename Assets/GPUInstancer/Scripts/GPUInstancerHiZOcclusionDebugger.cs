using UnityEngine;

namespace GPUInstancer
{

    public class GPUInstancerHiZOcclusionDebugger : MonoBehaviour
    {
        private Shader debugShader = null;
        private Material debugMaterial = null;
        private GPUInstancerHiZOcclusionGenerator hiZOcclusionGenerator = null;

        [HideInInspector] public int debuggerHiZMipLevel = 0;

        private void OnEnable()
        {
            debugShader = Shader.Find(GPUInstancerConstants.SHADER_GPUI_HIZ_OCCLUSION_DEBUGGER);
            debugMaterial = new Material(debugShader);
            hiZOcclusionGenerator = FindObjectOfType<GPUInstancerHiZOcclusionGenerator>();
        }

        private void OnDisable()
        {
            debugShader = null;
            debugMaterial = null;
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            debugMaterial.SetInt("_HiZMipLevel", debuggerHiZMipLevel);
            Graphics.Blit(hiZOcclusionGenerator.hiZDepthTexture, destination, debugMaterial);
        }
    }

}