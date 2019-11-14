using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;

namespace GPUInstancer
{
    public class GPUInstancerRuntimeData
    {
        public GPUInstancerPrototype prototype;

        // Mesh - Material - LOD info
        public List<GPUInstancerPrototypeLOD> instanceLODs;
        public Bounds instanceBounds;
        public float[] lodSizes = new float[] {
            1000, 1000, 1000, 1000,
            1000, 1000, 1000, 1000,
            1000, 1000, 1000, 1000,
            1000, 1000, 1000, 1000 };
        public float[] lodCFSizes = new float[] {
            -1, -1, -1, -1,
            -1, -1, -1, -1,
            -1, -1, -1, -1,
            -1, -1, -1, -1 };

        // Instance Data
        [HideInInspector]
        public Matrix4x4[] instanceDataArray;
        // Currently instanced count
        public int instanceCount;
        // Buffer size
        public int bufferSize;

        // Buffers Data
        public ComputeBuffer transformationMatrixVisibilityBuffer;
        public ComputeBuffer argsBuffer; // for multiple material (submesh) rendering
        public ComputeBuffer instanceLODDataBuffer; // for storing LOD data
        public uint[] args;

        public bool hasShadowCasterBuffer;
        public Material shadowCasterMaterial;
        public ComputeBuffer shadowArgsBuffer;
        public uint[] shadowArgs;

        public bool transformDataModified;

        public GPUInstancerRuntimeData(GPUInstancerPrototype prototype)
        {
            this.prototype = prototype;
        }

        #region AddLodAndRenderer

        /// <summary>
        /// Adds a new LOD and creates a single renderer for it.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="materials"></param>
        /// <param name="lodSize"></param>
        public void AddLodAndRenderer(Mesh mesh, List<Material> materials, MaterialPropertyBlock mpb, bool castShadows, float lodSize = -1, MaterialPropertyBlock shadowMPB = null, bool excludeBounds = false, int layer = 0)
        {
            AddLod(lodSize, excludeBounds);
            AddRenderer(instanceLODs.Count - 1, mesh, materials, Matrix4x4.identity, mpb, castShadows, layer, shadowMPB);
        }

        /// <summary>
        /// Registers an LOD to the prototype. LODs contain the renderers for instance prototypes,
        /// so even if no LOD is being used, the prototype must be registered as LOD0 using this method.
        /// </summary>
        /// <param name="screenRelativeTransitionHeight">if not defined, will default to 0</param>
        public void AddLod(float screenRelativeTransitionHeight = -1, bool excludeBounds = false)
        {
            if (instanceLODs == null)
                instanceLODs = new List<GPUInstancerPrototypeLOD>();

            GPUInstancerPrototypeLOD instanceLOD = new GPUInstancerPrototypeLOD();
            instanceLOD.excludeBounds = excludeBounds;
            instanceLODs.Add(instanceLOD);

            // Ensure the LOD will render if this is the first LOD and lodDistance is not set.
            if (instanceLODs.Count == 1 && screenRelativeTransitionHeight < 0f)
                lodSizes[0] = 0;

            // Do not modify the lodDistances vector if LOD distance is not supplied.
            if (screenRelativeTransitionHeight < 0f)
                return;

            float lodSize = (screenRelativeTransitionHeight / (prototype != null && prototype.lodBiasAdjustment > 0 ? prototype.lodBiasAdjustment : 1)) / QualitySettings.lodBias;
            if (instanceLODs.Count < 5)
                lodSizes[(instanceLODs.Count - 1) * 4] = lodSize;
            else
                lodSizes[(instanceLODs.Count - 5) * 4 + 1] = lodSize;

            if (prototype.isLODCrossFade)
            {
#if !UNITY_EDITOR && UNITY_ANDROID
                prototype.isLODCrossFade = false;
#else
                if (!prototype.isLODCrossFadeAnimate)
                {
                    float previousLodSize = 1;
                    if (instanceLODs.Count > 1)
                    {
                        if (instanceLODs.Count < 5)
                            previousLodSize = lodSizes[(instanceLODs.Count - 2) * 4];
                        else if (instanceLODs.Count == 5)
                            previousLodSize = lodSizes[12];
                        else
                            previousLodSize = lodSizes[(instanceLODs.Count - 6) * 4];
                    }
                    float dif = previousLodSize - lodSize;
                    float cfSize = lodSize + dif * prototype.lodFadeTransitionWidth;

                    if (instanceLODs.Count < 5)
                        lodCFSizes[(instanceLODs.Count - 1) * 4] = cfSize;
                    else
                        lodCFSizes[(instanceLODs.Count - 5) * 4 + 1] = cfSize;
                }
#endif
            }
        }

        /// <summary>
        /// Adds a renderer to an LOD. Renderers define the meshes and materials to render for a given instance prototype LOD.
        /// </summary>
        /// <param name="lod">The LOD to add this renderer to. LOD indices start from 0.</param>
        /// <param name="mesh">The mesh that this renderer will use.</param>
        /// <param name="materials">The list of materials that this renderer will use (must be GPU Instancer compatible materials)</param>
        /// <param name="transformOffset">The transformation matrix that represents a change in position, rotation and scale 
        /// for this renderer as an offset from the instance prototype. This matrix will be applied to the prototype instance 
        /// matrix for final rendering calculations in the shader. Use Matrix4x4.Identity if no offset is desired.</param>
        public void AddRenderer(int lod, Mesh mesh, List<Material> materials, Matrix4x4 transformOffset, MaterialPropertyBlock mpb, bool castShadows, int layer = 0, MaterialPropertyBlock shadowMPB = null)
        {

            if (instanceLODs == null || instanceLODs.Count <= lod || instanceLODs[lod] == null)
            {
                Debug.LogError("Can't add renderer: Invalid LOD");
                return;
            }

            if (mesh == null)
            {
                Debug.LogError("Can't add renderer: mesh is null. Make sure that all the MeshFilters on the objects has a mesh assigned.");
                return;
            }

            if (materials == null || materials.Count == 0)
            {
                Debug.LogError("Can't add renderer: no materials. Make sure that all the MeshRenderers have their materials assigned.");
                return;
            }

            if (instanceLODs[lod].renderers == null)
                instanceLODs[lod].renderers = new List<GPUInstancerRenderer>();

            GPUInstancerRenderer renderer = new GPUInstancerRenderer
            {
                mesh = mesh,
                materials = materials,
                transformOffset = transformOffset,
                mpb = mpb,
                shadowMPB = shadowMPB,
                layer = layer,
                castShadows = castShadows
            };

            instanceLODs[lod].renderers.Add(renderer);
            CalculateBounds();
        }

        public void CalculateBounds()
        {
            if (instanceLODs == null || instanceLODs.Count == 0 || instanceLODs[0].renderers == null ||
                instanceLODs[0].renderers.Count == 0)
                return;

            Bounds rendererBounds;
            for (int lod = 0; lod < instanceLODs.Count; lod++)
            {
                if (instanceLODs[lod].excludeBounds)
                    continue;

                for (int r = 0; r < instanceLODs[lod].renderers.Count; r++)
                {
                    rendererBounds = new Bounds(instanceLODs[lod].renderers[r].mesh.bounds.center + (Vector3)instanceLODs[lod].renderers[r].transformOffset.GetColumn(3),
                        new Vector3(
                        instanceLODs[lod].renderers[r].mesh.bounds.size.x * instanceLODs[lod].renderers[r].transformOffset.GetRow(0).magnitude,
                        instanceLODs[lod].renderers[r].mesh.bounds.size.y * instanceLODs[lod].renderers[r].transformOffset.GetRow(1).magnitude,
                        instanceLODs[lod].renderers[r].mesh.bounds.size.z * instanceLODs[lod].renderers[r].transformOffset.GetRow(2).magnitude));
                    if (lod == 0 && r == 0)
                    {
                        instanceBounds = rendererBounds;
                        continue;
                    }
                    instanceBounds.Encapsulate(rendererBounds);

                    //Vector3[] verts = instanceLODs[lod].renderers[r].mesh.vertices;
                    //for (var v = 0; v < verts.Length; v++)
                    //    instanceBounds.Encapsulate(verts[v]);
                }
            }
        }

        #endregion AddLodAndRenderer

        #region CreateRenderersFromGameObject

        /// <summary>
        /// Generates instancing renderer data for a given GameObject, at the first LOD level.
        /// </summary>
        /// <param name="gameObject">GameObject</param>
        /// <param name="settings">GPU Instancer settings to find appropriate shader for materials</param> 
        /// <param name="includeChildren">if true, renderers for all found children of this gameObject will be created as well</param>
        public bool CreateRenderersFromGameObject(GPUInstancerPrototype prototype, GPUInstancerShaderBindings shaderBindings)
        {
            if (prototype.prefabObject == null)
                return false;

            if (prototype.prefabObject.GetComponent<LODGroup>() != null)
                return GenerateLODsFromLODGroup(prototype.prefabObject.GetComponent<LODGroup>(), shaderBindings, prototype.isShadowCasting);
            else
            {
                if (instanceLODs == null || instanceLODs.Count == 0)
                    AddLod();
                return CreateRenderersFromGameObject(0, prototype.prefabObject, shaderBindings, prototype.isShadowCasting);
            }
        }

        /// <summary>
        /// Generates all LOD and render data from the supplied Unity LODGroup. Deletes all existing LOD data.
        /// </summary>
        /// <param name="lodGroup">Unity LODGroup</param>
        /// <param name="settings">GPU Instancer settings to find appropriate shader for materials</param> 
        private bool GenerateLODsFromLODGroup(LODGroup lodGroup, GPUInstancerShaderBindings shaderBindings, bool createShadowMPB)
        {
            if (instanceLODs == null)
                instanceLODs = new List<GPUInstancerPrototypeLOD>();
            else
                instanceLODs.Clear();

            for (int lod = 0; lod < lodGroup.GetLODs().Length; lod++)
            {
                bool hasBillboardRenderer = false;
                List<Renderer> lodRenderers = new List<Renderer>();
                if (lodGroup.GetLODs()[lod].renderers != null)
                {
                    foreach (Renderer renderer in lodGroup.GetLODs()[lod].renderers)
                    {
                        if (renderer != null && renderer is MeshRenderer && renderer.GetComponent<MeshFilter>() != null)
                            lodRenderers.Add(renderer);
                        else if (renderer != null && renderer is BillboardRenderer)
                            hasBillboardRenderer = true;
                    }
                }

                if (!lodRenderers.Any())
                {
                    if (!hasBillboardRenderer)
                        Debug.LogWarning("LODGroup has no mesh renderers. Prefab: " + lodGroup.gameObject.name + " LODIndex: " + lod);
                    continue;
                }

                AddLod(lodGroup.GetLODs()[lod].screenRelativeTransitionHeight);

                for (int r = 0; r < lodRenderers.Count; r++)
                {
                    List<Material> instanceMaterials = new List<Material>();
                    for (int m = 0; m < lodRenderers[r].sharedMaterials.Length; m++)
                    {
                        instanceMaterials.Add(shaderBindings.GetInstancedMaterial(lodRenderers[r].sharedMaterials[m]));
                    }

                    Matrix4x4 transformOffset = Matrix4x4.identity;
                    Transform currentTransform = lodRenderers[r].gameObject.transform;
                    while (currentTransform != lodGroup.gameObject.transform)
                    {
                        transformOffset = Matrix4x4.TRS(currentTransform.localPosition, currentTransform.localRotation, currentTransform.localScale) * transformOffset;
                        currentTransform = currentTransform.parent;
                    }

                    MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                    lodRenderers[r].GetPropertyBlock(mpb);
                    MaterialPropertyBlock shadowMPB = null;
                    if (createShadowMPB)
                    {
                        shadowMPB = new MaterialPropertyBlock();
                        lodRenderers[r].GetPropertyBlock(shadowMPB);
                    }
                    AddRenderer(lod, lodRenderers[r].GetComponent<MeshFilter>().sharedMesh, instanceMaterials, transformOffset, mpb, lodRenderers[r].shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off, lodRenderers[r].gameObject.layer, shadowMPB);
                }
            }
            return true;
        }

        /// <summary>
        /// Generates instancing renderer data for a given GameObject, at the given LOD level.
        /// </summary>
        /// <param name="lod">Which LOD level to generate renderers in</param>
        /// <param name="gameObject">GameObject</param>
        /// <param name="settings">GPU Instancer settings to find appropriate shader for materials</param> 
        /// <param name="includeChildren">if true, renderers for all found children of this gameObject will be created as well</param>
        private bool CreateRenderersFromGameObject(int lod, GameObject gameObject, GPUInstancerShaderBindings shaderBindings, bool createShadowMPB)
        {
            if (instanceLODs == null || instanceLODs.Count <= lod || instanceLODs[lod] == null)
            {
                Debug.LogError("Can't create renderer(s): Invalid LOD");
                return false;
            }

            if (!gameObject)
            {
                Debug.LogError("Can't create renderer(s): reference GameObject is null");
                return false;
            }

            List<MeshRenderer> meshRenderers = new List<MeshRenderer>();
            GetMeshRenderersOfTransform(gameObject.transform, meshRenderers);

            if (meshRenderers == null || meshRenderers.Count == 0)
            {
                Debug.LogError("Can't create renderer(s): no MeshRenderers found in the reference GameObject <" + gameObject.name +
                        "> or any of its children");
                return false;
            }

            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                if (meshRenderer.GetComponent<MeshFilter>() == null)
                {
                    Debug.LogWarning("MeshRenderer with no MeshFilter found on GameObject <" + gameObject.name +
                        "> (Child: <" + meshRenderer.gameObject + ">). Are you missing a component?");
                    continue;
                }

                List<Material> instanceMaterials = new List<Material>();

                for (int m = 0; m < meshRenderer.sharedMaterials.Length; m++)
                {
                    instanceMaterials.Add(shaderBindings.GetInstancedMaterial(meshRenderer.sharedMaterials[m]));
                }

                Matrix4x4 transformOffset = Matrix4x4.identity;
                Transform currentTransform = meshRenderer.gameObject.transform;
                while (currentTransform != gameObject.transform)
                {
                    transformOffset = Matrix4x4.TRS(currentTransform.localPosition, currentTransform.localRotation, currentTransform.localScale) * transformOffset;
                    currentTransform = currentTransform.parent;
                }

                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                meshRenderer.GetPropertyBlock(mpb);
                MaterialPropertyBlock shadowMPB = null;
                if (createShadowMPB)
                {
                    shadowMPB = new MaterialPropertyBlock();
                    meshRenderer.GetPropertyBlock(shadowMPB);
                }
                AddRenderer(lod, meshRenderer.GetComponent<MeshFilter>().sharedMesh, instanceMaterials, transformOffset, mpb, meshRenderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off, meshRenderer.gameObject.layer, shadowMPB);
            }

            return true;
        }

        public void GetMeshRenderersOfTransform(Transform objectTransform, List<MeshRenderer> meshRenderers)
        {
            MeshRenderer meshRenderer = objectTransform.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                meshRenderers.Add(meshRenderer);

            Transform childTransform;
            for (int i = 0; i < objectTransform.childCount; i++)
            {
                childTransform = objectTransform.GetChild(i);
                if (childTransform.GetComponent<GPUInstancerPrefab>() != null)
                    continue;
                GetMeshRenderersOfTransform(childTransform, meshRenderers);
            }
        }

        #endregion CreateRenderersFromGameObject
    }

    public class GPUInstancerPrototypeLOD
    {
        // Prototype Data
        public List<GPUInstancerRenderer> renderers; // support for multiple mesh renderers
        // Buffers Data
        public ComputeBuffer transformationMatrixAppendBuffer;
        public ComputeBuffer shadowAppendBuffer;
        public bool excludeBounds;
#if !UNITY_EDITOR && UNITY_ANDROID
        public RenderTexture transformationMatrixAppendTexture;
        public RenderTexture shadowAppendTexture;
#endif

        public int argsBufferOffset { get { return renderers == null ? -1 : renderers[0].argsBufferOffset; } }
    }

    public class GPUInstancerRenderer
    {
        public Mesh mesh;
        public List<Material> materials; // support for multiple submeshes.
        public Matrix4x4 transformOffset;
        public int argsBufferOffset;
        public MaterialPropertyBlock mpb;
        public MaterialPropertyBlock shadowMPB;
        public int layer;
        public bool castShadows;
    }
}
