using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancer
{
    public class GPUInstancerShaderBindings : ScriptableObject
    {
        public List<ShaderInstance> shaderInstances;

        private static readonly List<string> _standartUnityShaders = new List<string> {
            GPUInstancerConstants.SHADER_UNITY_STANDARD, GPUInstancerConstants.SHADER_UNITY_STANDARD_SPECULAR,
            GPUInstancerConstants.SHADER_UNITY_STANDARD_ROUGHNESS, GPUInstancerConstants.SHADER_UNITY_VERTEXLIT,
            GPUInstancerConstants.SHADER_UNITY_SPEED_TREE, GPUInstancerConstants.SHADER_UNITY_TREE_CREATOR_BARK, GPUInstancerConstants.SHADER_UNITY_TREE_CREATOR_BARK_OPTIMIZED,
            GPUInstancerConstants.SHADER_UNITY_TREE_CREATOR_LEAVES, GPUInstancerConstants.SHADER_UNITY_TREE_CREATOR_LEAVES_OPTIMIZED,
            GPUInstancerConstants.SHADER_UNITY_TREE_CREATOR_LEAVES_FAST, GPUInstancerConstants.SHADER_UNITY_TREE_CREATOR_LEAVES_FAST_OPTIMIZED,
            GPUInstancerConstants.SHADER_UNITY_TREE_SOFT_OCCLUSION_BARK, GPUInstancerConstants.SHADER_UNITY_TREE_SOFT_OCCLUSION_LEAVES
        };
        private static readonly List<string> _standartUnityShadersGPUI = new List<string> {
            GPUInstancerConstants.SHADER_GPUI_STANDARD, GPUInstancerConstants.SHADER_GPUI_STANDARD_SPECULAR,
            GPUInstancerConstants.SHADER_GPUI_STANDARD_ROUGHNESS, GPUInstancerConstants.SHADER_GPUI_VERTEXLIT,
            GPUInstancerConstants.SHADER_GPUI_SPEED_TREE, GPUInstancerConstants.SHADER_GPUI_TREE_CREATOR_BARK, GPUInstancerConstants.SHADER_GPUI_TREE_CREATOR_BARK_OPTIMIZED,
            GPUInstancerConstants.SHADER_GPUI_TREE_CREATOR_LEAVES, GPUInstancerConstants.SHADER_GPUI_TREE_CREATOR_LEAVES_OPTIMIZED,
            GPUInstancerConstants.SHADER_GPUI_TREE_CREATOR_LEAVES_FAST, GPUInstancerConstants.SHADER_GPUI_TREE_CREATOR_LEAVES_FAST_OPTIMIZED,
            GPUInstancerConstants.SHADER_GPUI_TREE_SOFT_OCCLUSION_BARK, GPUInstancerConstants.SHADER_GPUI_TREE_SOFT_OCCLUSION_LEAVES
        };
        private static readonly List<string> _extraGPUIShaders = new List<string> {
            GPUInstancerConstants.SHADER_GPUI_FOLIAGE, GPUInstancerConstants.SHADER_GPUI_SHADOWS_ONLY
        };

        public Shader GetInstancedShader(string shaderName)
        {
            if (_standartUnityShaders.Contains(shaderName))
                return Shader.Find(_standartUnityShadersGPUI[_standartUnityShaders.IndexOf(shaderName)]);

            if (_standartUnityShadersGPUI.Contains(shaderName))
                return Shader.Find(shaderName);

            if (_extraGPUIShaders.Contains(shaderName))
                return Shader.Find(shaderName);

            foreach (ShaderInstance si in shaderInstances)
            {
                if (si.name.Equals(shaderName))
                    return si.instancedShader;
            }
            if (!shaderName.Equals(GPUInstancerConstants.SHADER_UNITY_STANDARD))
            {
                if (Application.isPlaying)
                    Debug.LogWarning("Can not find instanced shader for : " + shaderName + ". Using Standard shader instead.");
                return GetInstancedShader(GPUInstancerConstants.SHADER_UNITY_STANDARD);
            }
            Debug.LogWarning("Can not find instanced shader for : " + shaderName);
            return null;
        }

        public Material GetInstancedMaterial(Material originalMaterial)
        {
            if (originalMaterial == null || originalMaterial.shader == null)
            {
                Debug.LogWarning("One of the GPU Instancer prototypes is missing material reference! Check the Material references in MeshRenderer.");
                return new Material(GetInstancedShader(GPUInstancerConstants.SHADER_UNITY_STANDARD));
            }
            Material instancedMaterial = new Material(GetInstancedShader(originalMaterial.shader.name));
            instancedMaterial.CopyPropertiesFromMaterial(originalMaterial);
            instancedMaterial.name = originalMaterial.name + "_GPUI";

            return instancedMaterial;
        }

        public void ResetShaderInstances()
        {
            if (shaderInstances == null)
                shaderInstances = new List<ShaderInstance>();
            else
                shaderInstances.Clear();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public void ClearEmptyShaderInstances()
        {
            if (shaderInstances != null)
            {
#if UNITY_EDITOR             
                bool modified = shaderInstances.RemoveAll(si => si == null || si.instancedShader == null || string.IsNullOrEmpty(si.name)) > 0;
                for (int i = 0; i < shaderInstances.Count; i++)
                {
                    if (shaderInstances[i].isOriginalInstanced)
                        continue;

                    Shader originalShader = Shader.Find(shaderInstances[i].name);
                    if (!GPUInstancerUtility.IsShaderInstanced(originalShader))
                    {
                        string originalAssetPath = UnityEditor.AssetDatabase.GetAssetPath(originalShader);
                        DateTime lastWriteTime = System.IO.File.GetLastWriteTime(originalAssetPath);
                        if (lastWriteTime >= DateTime.Now)
                            continue;

                        DateTime instancedTime = DateTime.MinValue;
                        bool isValidDate = false;
                        if (!string.IsNullOrEmpty(shaderInstances[i].modifiedDate))
                            isValidDate = DateTime.TryParseExact(shaderInstances[i].modifiedDate, "MM/dd/yyyy HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.None, out instancedTime);
                        if (!isValidDate || lastWriteTime > Convert.ToDateTime(shaderInstances[i].modifiedDate, System.Globalization.CultureInfo.InvariantCulture))
                        {
                            shaderInstances[i].instancedShader = GPUInstancerUtility.CreateInstancedShader(originalShader, this);
                            shaderInstances[i].modifiedDate = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff",
                                System.Globalization.CultureInfo.InvariantCulture);
                            modified = true;
                        }
                    }
                    else
                    {
                        shaderInstances[i].isOriginalInstanced = true;
                        modified = true;
                    }
                }

                // remove non unique instances
                List<string> shaderNames = new List<string>();
                foreach (ShaderInstance si in shaderInstances.ToArray())
                {
                    if (shaderNames.Contains(si.name))
                    {
                        shaderInstances.Remove(si);
                        modified = true;
                    }
                    else
                        shaderNames.Add(si.name);
                }

                if (modified)
                    UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        public void AddShaderInstance(string name, Shader instancedShader, bool isOriginalInstanced = false)
        {
            shaderInstances.Add(new ShaderInstance(name, instancedShader, isOriginalInstanced));
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        public bool IsShadersInstancedVersionExists(string shaderName)
        {
            if (_standartUnityShaders.Contains(shaderName) || _standartUnityShadersGPUI.Contains(shaderName) || _extraGPUIShaders.Contains(shaderName))
                return true;

            foreach (ShaderInstance si in shaderInstances)
            {
                if (si.name.Equals(shaderName))
                    return true;
            }
            return false;
        }
    }

    [Serializable]
    public class ShaderInstance
    {
        public string name;
        public Shader instancedShader;
        public string modifiedDate;
        public bool isOriginalInstanced;

        public ShaderInstance(string name, Shader instancedShader, bool isOriginalInstanced)
        {
            this.name = name;
            this.instancedShader = instancedShader;
            this.modifiedDate = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff",
                                System.Globalization.CultureInfo.InvariantCulture);
            this.isOriginalInstanced = isOriginalInstanced;
        }
    }

}