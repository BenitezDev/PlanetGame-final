using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using System.Threading;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancer
{
    public abstract class GPUInstancerManager : MonoBehaviour
    {
        public GPUInstancerShaderBindings shaderBindings;
        public GPUInstancerBillboardAtlasBindings billboardAtlasBindings;

        public List<GPUInstancerPrototype> prototypeList;

        public bool autoSelectCamera = true;
        public GPUInstancerCameraData cameraData = new GPUInstancerCameraData(null);

        public List<GPUInstancerRuntimeData> runtimeDataList;
        public Bounds instancingBounds;

        public bool isFrustumCulling = true;
        public bool isOcclusionCulling = true;
        public float minCullingDistance = 0;

        protected GPUInstancerSpatialPartitioningData<GPUInstancerCell> spData;

        public static List<GPUInstancerManager> activeManagerList;
        public static bool showRenderedAmount;

        protected static ComputeShader _visibilityComputeShader;
        protected static int[] _instanceVisibilityComputeKernelIDs;
#if !UNITY_EDITOR && UNITY_ANDROID
        protected static ComputeShader _bufferToTextureComputeShader;
        protected static int _bufferToTextureComputeKernelID;
#endif

#if UNITY_EDITOR
        [HideInInspector]
        public GPUInstancerPrototype selectedPrototype;
        [HideInInspector]
        public int pickerControlID = -1;
        [HideInInspector]
        public bool editorDataChanged = false;
        [HideInInspector]
        public int pickerMode = 0;

        public GPUInstancerEditorSimulator gpuiSimulator;
#endif

        public class GPUIThreadData
        {
            public Thread thread;
            public object parameter;
        }
        public int maxThreads = 3;
        public readonly List<Thread> activeThreads = new List<Thread>();
        public readonly Queue<GPUIThreadData> threadStartQueue = new Queue<GPUIThreadData>();
        public readonly Queue<System.Action> threadQueue = new Queue<System.Action>();

        // Tree variables
        public static int lastTreePositionUpdate;
        public static GameObject treeProxyParent;
        public static Dictionary<GameObject, Transform> treeProxyList; // Dict[TreePrefab, TreeProxyGO]

        // Time management
        public static int lastDrawCallFrame;
        public static float lastDrawCallTime;
        public static float timeSinceLastDrawCall;

        // Global Wind
        protected static Vector4 _windVector = Vector4.zero;

        protected bool isInitial = true;
        
        public bool isInitialized = false;

        #region MonoBehaviour Methods

        public virtual void Awake()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                CheckPrototypeChanges();
#endif
            if (activeManagerList == null)
                activeManagerList = new List<GPUInstancerManager>();

            if (SystemInfo.supportsComputeShaders)
            {
                if (_visibilityComputeShader == null)
                {
#if !UNITY_EDITOR && UNITY_ANDROID
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
                {
                    _visibilityComputeShader = (ComputeShader)Resources.Load(GPUInstancerConstants.VISIBILITY_COMPUTE_RESOURCE_PATH_VULKAN);
                    GPUInstancerConstants.DETAIL_STORE_INSTANCE_DATA = true;
                    GPUInstancerConstants.COMPUTE_MAX_LOD_BUFFER = 3;
                }
                else
                    _visibilityComputeShader = (ComputeShader)Resources.Load(GPUInstancerConstants.VISIBILITY_COMPUTE_RESOURCE_PATH);
                _bufferToTextureComputeShader = (ComputeShader)Resources.Load(GPUInstancerConstants.BUFFER_TO_TEXTURE_COMPUTE_RESOURCE_PATH);
                _bufferToTextureComputeKernelID = _bufferToTextureComputeShader.FindKernel(GPUInstancerConstants.BUFFER_TO_TEXTURE_KERNEL);
#else
                    _visibilityComputeShader = (ComputeShader)Resources.Load(GPUInstancerConstants.VISIBILITY_COMPUTE_RESOURCE_PATH);
#endif
                    _instanceVisibilityComputeKernelIDs = new int[GPUInstancerConstants.VISIBILITY_COMPUTE_KERNELS.Length];
                    for (int i = 0; i < _instanceVisibilityComputeKernelIDs.Length; i++)
                        _instanceVisibilityComputeKernelIDs[i] = _visibilityComputeShader.FindKernel(GPUInstancerConstants.VISIBILITY_COMPUTE_KERNELS[i]);
                    GPUInstancerConstants.TEXTURE_MAX_SIZE = SystemInfo.maxTextureSize;
                }

                GPUInstancerConstants.SetupComputeRuntimeModification();
                GPUInstancerConstants.SetupComputeSetDataPartial();
            }
            else if (Application.isPlaying)
            {
                Debug.LogError("Target Graphics API does not support Compute Shaders. Please refer to Minimum Requirements on GPUInstancer/ReadMe.txt for detailed information.");
                this.enabled = false;
            }

            showRenderedAmount = false;

            InitializeCameraData();

            SetDefaultGPUInstancerShaderBindings();
            SetDefaultGPUInstancerBillboardAtlasBindings();
        }

        public virtual void Start()
        {
            if (Application.isPlaying && SystemInfo.supportsComputeShaders)
            {
                SetupOcclusionCulling(cameraData);
            }
        }

        public virtual void OnEnable()
        {
#if UNITY_EDITOR
            if (gpuiSimulator == null)
                gpuiSimulator = new GPUInstancerEditorSimulator(this);
#endif

            if (Application.isPlaying && cameraData.mainCamera == null)
            {
                InitializeCameraData();
                if (cameraData.mainCamera == null)
                    Debug.LogWarning(GPUInstancerConstants.ERRORTEXT_cameraNotFound);
            }

            if (activeManagerList != null && !activeManagerList.Contains(this))
                activeManagerList.Add(this);

            if (Application.isPlaying && SystemInfo.supportsComputeShaders)
            {
                if (shaderBindings == null)
                    Debug.LogWarning("No shader bindings file was supplied. Instancing will terminate!");

                if (runtimeDataList == null || runtimeDataList.Count == 0)
                    InitializeRuntimeDataAndBuffers();
                isInitial = true;
            }
        }

        public virtual void Update()
        {
            if (activeThreads.Count > 0)
            {
                for (int i = activeThreads.Count - 1; i >= 0; i--)
                {
                    if (!activeThreads[i].IsAlive)
                        activeThreads.RemoveAt(i);
                }
            }
            while (threadStartQueue.Count > 0 && activeThreads.Count < maxThreads)
            {
                GPUIThreadData threadData = threadStartQueue.Dequeue();
                threadData.thread.Start(threadData.parameter);
                activeThreads.Add(threadData.thread);
            }
            if (threadQueue.Count > 0)
            {
                System.Action action = this.threadQueue.Dequeue();
                if (action != null)
                    action.Invoke();
            }

            if (Application.isPlaying && treeProxyParent && lastTreePositionUpdate != Time.frameCount && cameraData.mainCamera != null)
            {
                treeProxyParent.transform.position = cameraData.mainCamera.transform.position;
                treeProxyParent.transform.rotation = cameraData.mainCamera.transform.rotation;
                lastTreePositionUpdate = Time.frameCount;
            }
        }

        public virtual void LateUpdate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                CheckPrototypeChanges();
            else
            {
#endif
                if (cameraData.mainCamera != null)
                {
                    UpdateTreeMPB();
                    UpdateBuffers(cameraData);
                }
#if UNITY_EDITOR
            }
#endif
        }

        public virtual void OnDestroy()
        {
        }

        public virtual void Reset()
        {
            SetDefaultGPUInstancerShaderBindings();
            SetDefaultGPUInstancerBillboardAtlasBindings();
#if UNITY_EDITOR
            CheckPrototypeChanges();
#endif
        }

        public virtual void OnDisable() // could also be OnDestroy, but OnDestroy seems to be too late to prevent buffer leaks.
        {
            if (activeManagerList != null)
                activeManagerList.Remove(this);

            ClearInstancingData();
#if UNITY_EDITOR
            if (gpuiSimulator != null)
            {
                gpuiSimulator.ClearEditorUpdates();
                gpuiSimulator = null;
            }
#endif
        }

        #endregion MonoBehaviour Methods

        #region Virtual Methods

        public virtual void ClearInstancingData()
        {
            GPUInstancerUtility.ReleaseInstanceBuffers(runtimeDataList);
            GPUInstancerUtility.ReleaseSPBuffers(spData);
            if (runtimeDataList != null)
                runtimeDataList.Clear();
            spData = null;
            threadStartQueue.Clear();
            threadQueue.Clear();
            isInitialized = false;
        }

        public virtual void GeneratePrototypes(bool forceNew = false)
        {
            ClearInstancingData();

            if (forceNew || prototypeList == null)
                prototypeList = new List<GPUInstancerPrototype>();
            else
                prototypeList.RemoveAll(p => p == null);

            SetDefaultGPUInstancerShaderBindings();
            SetDefaultGPUInstancerBillboardAtlasBindings();
        }

#if UNITY_EDITOR
        public virtual void CheckPrototypeChanges()
        {
            if (prototypeList == null)
                GeneratePrototypes();
            else
                prototypeList.RemoveAll(p => p == null);

            if (shaderBindings != null)
            {
                shaderBindings.ClearEmptyShaderInstances();
                foreach (GPUInstancerPrototype prototype in prototypeList)
                {
                    if (prototype.prefabObject != null)
                    {
                        GPUInstancerUtility.GenerateInstancedShadersForGameObject(prototype, shaderBindings);
                        if (string.IsNullOrEmpty(prototype.warningText))
                        {
                            if (prototype.prefabObject.GetComponentInChildren<MeshRenderer>() == null)
                            {
                                prototype.warningText = "Prefab object does not contain any Mesh Renderers.";
                            }
                        }
                    }
                }
            }
            if (billboardAtlasBindings != null)
            {
                billboardAtlasBindings.ClearEmptyBillboardAtlases();
                //foreach (GPUInstancerPrototype prototype in prototypeList)
                //{
                //    if (prototype.prefabObject != null && prototype.useGeneratedBillboard && 
                //        (prototype.billboard == null || prototype.billboard.albedoAtlasTexture == null || prototype.billboard.normalAtlasTexture == null))
                //        GPUInstancerUtility.GeneratePrototypeBillboard(prototype, billboardAtlasBindings);
                //}
            }
        }

        public virtual void ShowObjectPicker()
        {

        }

        public virtual void AddPickerObject(UnityEngine.Object pickerObject)
        {

        }

        public virtual void OnEditorDataChanged()
        {
            editorDataChanged = true;
        }

        public virtual void ApplyEditorDataChanges()
        {

        }
#endif
        public virtual void InitializeRuntimeDataAndBuffers(bool forceNew = true)
        {
            if (forceNew || !isInitialized)
            {
                instancingBounds = new Bounds(Vector3.zero, Vector3.one * 10000f);

                GPUInstancerUtility.ReleaseInstanceBuffers(runtimeDataList);
                GPUInstancerUtility.ReleaseSPBuffers(spData);
                if (runtimeDataList != null)
                    runtimeDataList.Clear();
                else
                    runtimeDataList = new List<GPUInstancerRuntimeData>();

                if (prototypeList == null)
                    prototypeList = new List<GPUInstancerPrototype>();
            }
        }

        public virtual void InitializeSpatialPartitioning()
        {

        }

        public virtual void UpdateSpatialPartitioningCells(GPUInstancerCameraData renderingCameraData)
        {

        }

        public virtual void DeletePrototype(GPUInstancerPrototype prototype, bool removeSO = true)
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Delete prototype");
#endif
            prototypeList.Remove(prototype);

            if (removeSO && prototype.useGeneratedBillboard && prototype.billboard != null)
            {
                if (billboardAtlasBindings.DeleteBillboardTextures(prototype))
                    prototype.billboard = null;
            }
        }

        public virtual void RemoveInstancesInsideBounds(Bounds bounds, float offset, List<GPUInstancerPrototype> prototypeFilter = null)
        {
            if (runtimeDataList != null)
            {
                foreach (GPUInstancerRuntimeData rd in runtimeDataList)
                {
                    if (prototypeFilter != null && !prototypeFilter.Contains(rd.prototype))
                        continue;
                    GPUInstancerUtility.RemoveInstancesInsideBounds(rd.transformationMatrixVisibilityBuffer, bounds.center, bounds.extents, offset);
                }
            }
        }
        public virtual void RemoveInstancesInsideCollider(Collider collider, float offset, List<GPUInstancerPrototype> prototypeFilter = null)
        {
            if (runtimeDataList != null)
            {
                foreach (GPUInstancerRuntimeData rd in runtimeDataList)
                {
                    if (prototypeFilter != null && !prototypeFilter.Contains(rd.prototype))
                        continue;
                    if (collider is BoxCollider)
                        GPUInstancerUtility.RemoveInstancesInsideBoxCollider(rd.transformationMatrixVisibilityBuffer, (BoxCollider)collider, offset);
                    else if (collider is SphereCollider)
                        GPUInstancerUtility.RemoveInstancesInsideSphereCollider(rd.transformationMatrixVisibilityBuffer, (SphereCollider)collider, offset);
                    else if (collider is CapsuleCollider)
                        GPUInstancerUtility.RemoveInstancesInsideCapsuleCollider(rd.transformationMatrixVisibilityBuffer, (CapsuleCollider)collider, offset);
                    else
                        GPUInstancerUtility.RemoveInstancesInsideBounds(rd.transformationMatrixVisibilityBuffer, collider.bounds.center, collider.bounds.extents, offset);
                }
            }
        }

        public virtual void SetGlobalPositionOffset(Vector3 offsetPosition)
        {
        }
        #endregion Virtual Methods

        #region Public Methods

        public void SetDefaultGPUInstancerShaderBindings()
        {
            if (shaderBindings == null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Undo.RecordObject(this, "GPUInstancerShaderBindings instance generated");
#endif
                shaderBindings = GetDefaultGPUInstancerShaderBindings();
            }
        }

        public void SetDefaultGPUInstancerBillboardAtlasBindings()
        {
            if (billboardAtlasBindings == null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Undo.RecordObject(this, "GPUInstancerBillboardAtlasBindings instance generated");
#endif
                billboardAtlasBindings = GetDefaultGPUInstancerBillboardAtlasBindings();
            }
        }

        public static GPUInstancerShaderBindings GetDefaultGPUInstancerShaderBindings()
        {
            GPUInstancerShaderBindings shaderBindings = Resources.Load<GPUInstancerShaderBindings>(GPUInstancerConstants.SETTINGS_PATH + GPUInstancerConstants.SHADER_BINDINGS_DEFAULT_NAME);

            if (shaderBindings == null)
            {
                shaderBindings = ScriptableObject.CreateInstance<GPUInstancerShaderBindings>();
                shaderBindings.ResetShaderInstances();
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (!System.IO.Directory.Exists(GPUInstancerConstants.GetDefaultPath() + GPUInstancerConstants.RESOURCES_PATH + GPUInstancerConstants.SETTINGS_PATH))
                    {
                        System.IO.Directory.CreateDirectory(GPUInstancerConstants.GetDefaultPath() + GPUInstancerConstants.RESOURCES_PATH + GPUInstancerConstants.SETTINGS_PATH);
                    }

                    AssetDatabase.CreateAsset(shaderBindings, GPUInstancerConstants.GetDefaultPath() + GPUInstancerConstants.RESOURCES_PATH + GPUInstancerConstants.SETTINGS_PATH + GPUInstancerConstants.SHADER_BINDINGS_DEFAULT_NAME + ".asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
#endif
            }

            return shaderBindings;
        }

        public static GPUInstancerBillboardAtlasBindings GetDefaultGPUInstancerBillboardAtlasBindings()
        {
            GPUInstancerBillboardAtlasBindings billboardAtlasBindings = Resources.Load<GPUInstancerBillboardAtlasBindings>(GPUInstancerConstants.SETTINGS_PATH + GPUInstancerConstants.BILLBOARD_ATLAS_BINDINGS_DEFAULT_NAME);

            if (billboardAtlasBindings == null)
            {
                billboardAtlasBindings = ScriptableObject.CreateInstance<GPUInstancerBillboardAtlasBindings>();
                billboardAtlasBindings.ResetBillboardAtlases();
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    if (!System.IO.Directory.Exists(GPUInstancerConstants.GetDefaultPath() + GPUInstancerConstants.RESOURCES_PATH + GPUInstancerConstants.SETTINGS_PATH))
                    {
                        System.IO.Directory.CreateDirectory(GPUInstancerConstants.GetDefaultPath() + GPUInstancerConstants.RESOURCES_PATH + GPUInstancerConstants.SETTINGS_PATH);
                    }

                    AssetDatabase.CreateAsset(billboardAtlasBindings, GPUInstancerConstants.GetDefaultPath() + GPUInstancerConstants.RESOURCES_PATH + GPUInstancerConstants.SETTINGS_PATH + GPUInstancerConstants.BILLBOARD_ATLAS_BINDINGS_DEFAULT_NAME + ".asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
#endif
            }

            return billboardAtlasBindings;
        }

        public void InitializeCameraData()
        {
            if (autoSelectCamera || cameraData.mainCamera == null)
            {
                cameraData.SetCamera(Camera.main);
            }
        }

        public void SetupOcclusionCulling(GPUInstancerCameraData renderingCameraData)
        {
            if (renderingCameraData == null || renderingCameraData.mainCamera == null)
                return;

#if UNITY_EDITOR
            if (renderingCameraData.mainCamera.name == GPUInstancerEditorSimulator.sceneViewCameraName)
                return;
#endif

            // Occlusion Culling with VR is not implemented yet.
#if UNITY_2017_2_OR_NEWER
            if (isOcclusionCulling && UnityEngine.XR.XRSettings.enabled)
#else
            if (isOcclusionCulling && UnityEngine.VR.VRSettings.enabled)
#endif
            {
                isOcclusionCulling = false;
                Debug.LogWarning("Currently GPU Instancer doesn't support Occlusion Culling for VR. Disabling the Occlusion Culling feature.");
            }

            if (isOcclusionCulling)
            {
                if (renderingCameraData.hiZOcclusionGenerator == null)
                {
                    GPUInstancerHiZOcclusionGenerator hiZOcclusionGenerator =
                        renderingCameraData.mainCamera.GetComponent<GPUInstancerHiZOcclusionGenerator>();

                    if (hiZOcclusionGenerator == null)
                        hiZOcclusionGenerator = renderingCameraData.mainCamera.gameObject.AddComponent<GPUInstancerHiZOcclusionGenerator>();

                    renderingCameraData.hiZOcclusionGenerator = hiZOcclusionGenerator;

                    renderingCameraData.mainCamera.depthTextureMode = DepthTextureMode.Depth;
                    renderingCameraData.hiZOcclusionGenerator.mainCamera = renderingCameraData.mainCamera;
                    renderingCameraData.hiZOcclusionGenerator.OnPreRender();
                }
            }
        }

        public void UpdateBuffers()
        {
            UpdateBuffers(cameraData);
        }

        public void UpdateBuffers(GPUInstancerCameraData renderingCameraData)
        {
            if (renderingCameraData != null && renderingCameraData.mainCamera != null && SystemInfo.supportsComputeShaders)
            {
                if (isOcclusionCulling && renderingCameraData.hiZOcclusionGenerator == null)
                    SetupOcclusionCulling(renderingCameraData);

                renderingCameraData.CalculateCameraData();

                instancingBounds.center = renderingCameraData.mainCamera.transform.position;

                if (lastDrawCallFrame != Time.frameCount)
                {
                    lastDrawCallFrame = Time.frameCount;
                    timeSinceLastDrawCall = Time.realtimeSinceStartup - lastDrawCallTime;
                    lastDrawCallTime = Time.realtimeSinceStartup;
                }

                UpdateSpatialPartitioningCells(renderingCameraData);

                GPUInstancerUtility.UpdateGPUBuffers(_visibilityComputeShader, _instanceVisibilityComputeKernelIDs, runtimeDataList, renderingCameraData, isFrustumCulling,
                    isOcclusionCulling, showRenderedAmount, isInitial);
                isInitial = false;
#if !UNITY_EDITOR && UNITY_ANDROID
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan)
                    GPUInstancerUtility.DispatchBufferToTexture(runtimeDataList, _bufferToTextureComputeShader, _bufferToTextureComputeKernelID);
#endif
                GPUInstancerUtility.GPUIDrawMeshInstancedIndirect(runtimeDataList, instancingBounds, renderingCameraData);
            }
        }

        public void SetCamera(Camera camera)
        {
            if (cameraData == null)
                cameraData = new GPUInstancerCameraData(camera);
            else
                cameraData.SetCamera(camera);

            if (cameraData.hiZOcclusionGenerator != null)
                DestroyImmediate(cameraData.hiZOcclusionGenerator);

            if (isOcclusionCulling)
            {
                cameraData.hiZOcclusionGenerator = cameraData.mainCamera.GetComponent<GPUInstancerHiZOcclusionGenerator>();
                if (cameraData.hiZOcclusionGenerator == null)
                {
                    cameraData.hiZOcclusionGenerator = cameraData.mainCamera.gameObject.AddComponent<GPUInstancerHiZOcclusionGenerator>();
                }
                cameraData.mainCamera.depthTextureMode = DepthTextureMode.Depth;
                cameraData.hiZOcclusionGenerator.mainCamera = cameraData.mainCamera;
            }
        }

        public ComputeBuffer GetTransformDataBuffer(GPUInstancerPrototype prototype)
        {
            if (runtimeDataList != null && runtimeDataList.Count > 0)
            {
                GPUInstancerRuntimeData runtimeData = runtimeDataList.Find(rd => rd != null && rd.prototype == prototype);
                if (runtimeData != null)
                    return runtimeData.transformationMatrixVisibilityBuffer;
            }
            return null;
        }

        #region Tree Instancing Methods
        public void UpdateTreeMPB()
        {
            if (treeProxyList != null && treeProxyList.Count > 0)
            {
                GPUInstancerPrototypeLOD rdLOD;
                GPUInstancerRenderer rdRenderer;
                MeshRenderer meshRenderer;
                Transform proxyTransform;
                foreach (GPUInstancerRuntimeData runtimeData in runtimeDataList)
                {
                    // Do not set append buffers if there is no instance of this tree prototype on the terrain
                    if (runtimeData.bufferSize == 0)
                        continue;

                    if (runtimeData.prototype.treeType != GPUInstancerTreeType.SpeedTree)
                        continue;

                    proxyTransform = treeProxyList[runtimeData.prototype.prefabObject];

                    if (!proxyTransform)
                        continue;

                    for (int lod = 0; lod < runtimeData.instanceLODs.Count; lod++)
                    {
                        if (proxyTransform.childCount <= lod)
                            continue;

                        rdLOD = runtimeData.instanceLODs[lod];
                        meshRenderer = proxyTransform.GetChild(lod).GetComponent<MeshRenderer>();

                        for (int r = 0; r < rdLOD.renderers.Count; r++)
                        {
                            rdRenderer = rdLOD.renderers[r];
                            //if (treeType == GPUInstancerTreeType.SoftOcclusionTree)
                            //{
                            //    // Soft occlusion shader wind parameters here.
                            //    // rdRenderer.mpb.SetFloat("_ShakeDisplacement", 0.8f);
                            //    continue;
                            //}

                            meshRenderer.GetPropertyBlock(rdRenderer.mpb);
                            if (rdRenderer.shadowMPB != null)
                                meshRenderer.GetPropertyBlock(rdRenderer.shadowMPB);
                        }
                    }

                    GPUInstancerUtility.SetAppendBuffers(runtimeData);
                }
            }
        }

        // Wind workaround:
        public static void AddTreeProxy(GPUInstancerPrototype treePrototype, GPUInstancerRuntimeData runtimeData)
        {
            switch (treePrototype.treeType)
            {
                case GPUInstancerTreeType.SpeedTree:

                    if (treeProxyParent == null)
                    {
                        treeProxyParent = new GameObject("GPUI Tree Manager Proxy");
                        if (treeProxyList != null)
                            treeProxyList.Clear();
                    }

                    if (treeProxyList == null)
                    {
                        treeProxyList = new Dictionary<GameObject, Transform>(); // Dict[TreePrefab, TreeProxyGO]
                    }
                    else if (treeProxyList.ContainsKey(treePrototype.prefabObject))
                    {
                        if (treeProxyList[treePrototype.prefabObject] == null)
                            treeProxyList.Remove(treePrototype.prefabObject);
                        else
                            return;
                    }

                    Mesh treeProxyMesh = new Mesh();
                    treeProxyMesh.name = "TreeProxyMesh";

                    GameObject treeProxyObjectParent = new GameObject(treeProxyList.Count + "_" + treePrototype.name);
                    treeProxyObjectParent.transform.SetParent(treeProxyParent.transform);
                    treeProxyObjectParent.transform.localPosition = Vector3.zero;
                    treeProxyObjectParent.transform.localRotation = Quaternion.identity;
                    treeProxyList.Add(treePrototype.prefabObject, treeProxyObjectParent.transform);

                    LOD[] speedTreeLODs = treePrototype.prefabObject.GetComponent<LODGroup>().GetLODs();
                    for (int lod = 0; lod < speedTreeLODs.Length; lod++)
                    {
                        if (speedTreeLODs[lod].renderers[0].GetComponent<BillboardRenderer>())
                            continue;

                        Material[] treeProxyMaterial = new Material[1] { new Material((Shader.Find(GPUInstancerConstants.SHADER_GPUI_TREE_PROXY))) };

                        InstantiateTreeProxyObject(speedTreeLODs[lod].renderers[0].gameObject, treeProxyObjectParent, treeProxyMaterial, treeProxyMesh, lod == 0);
                    }
                    break;

                case GPUInstancerTreeType.TreeCreatorTree:

                    // no need to create a TreeCreator proxy - setting the global wind vector instead
                    Shader.SetGlobalVector("_Wind", GetWindVector());

                    //Material[] treeCreatorProxyMaterials = new Material[2];
                    //treeCreatorProxyMaterials[0] = new Material(Shader.Find(GPUInstancerConstants.SHADER_GPUI_TREE_PROXY));
                    //treeCreatorProxyMaterials[1] = new Material(Shader.Find(GPUInstancerConstants.SHADER_GPUI_TREE_PROXY));
                    //InstantiateTreeProxyObject(treePrototype.prefabObject, treeProxyObjectParent, treeCreatorProxyMaterials, treeProxyMesh, true);
                    break;

            }
        }

        public static void InstantiateTreeProxyObject(GameObject treePrefab, GameObject proxyObjectParent, Material[] proxyMaterials, Mesh proxyMesh, bool setBounds)
        {
            GameObject treeProxyObject = Instantiate(treePrefab, proxyObjectParent.transform);
            treeProxyObject.name = treePrefab.name;

            if (setBounds)
                proxyMesh.bounds = treeProxyObject.GetComponent<MeshFilter>().sharedMesh.bounds;

            // Setup Tree Proxy object mesh renderer.
            MeshRenderer treeProxyObjectMR = treeProxyObject.GetComponent<MeshRenderer>();
            treeProxyObjectMR.shadowCastingMode = ShadowCastingMode.Off;
            treeProxyObjectMR.receiveShadows = false;
            treeProxyObjectMR.lightProbeUsage = LightProbeUsage.Off;

            for (int i = 0; i < proxyMaterials.Length; i++)
            {
                proxyMaterials[i].CopyPropertiesFromMaterial(treeProxyObjectMR.materials[i]);
                proxyMaterials[i].enableInstancing = true;
            }

            treeProxyObjectMR.sharedMaterials = proxyMaterials;
            treeProxyObjectMR.GetComponent<MeshFilter>().sharedMesh = proxyMesh;

            // Strip all unwanted components potentially on the tree prefab:
            Component[] allComponents = treeProxyObject.GetComponents(typeof(Component));
            for (int i = 0; i < allComponents.Length; i++)
            {
                if (allComponents[i] is Transform || allComponents[i] is MeshFilter ||
                    allComponents[i] is MeshRenderer || allComponents[i] is Tree)
                    continue;

                Destroy(allComponents[i]);
            }
        }
        #endregion Tree Instancing Methods

        #region Global Wind Methods

        public static Vector4 GetWindVector()
        {
            if (_windVector != Vector4.zero)
                return _windVector;

            UpdateSceneWind();

            return _windVector;
        }

        public static void UpdateSceneWind()
        {
            WindZone[] sceneWindZones = FindObjectsOfType<WindZone>();

            for (int i = 0; i < sceneWindZones.Length; i++)
            {
                if (sceneWindZones[i].mode == WindZoneMode.Directional)
                {
                    _windVector = new Vector4(sceneWindZones[i].windTurbulence, sceneWindZones[i].windPulseMagnitude, sceneWindZones[i].windPulseFrequency, sceneWindZones[i].windMain);
                    break;
                }
            }
        }

        #endregion Wind Methods

        public Exception threadException;
        public void LogThreadException()
        {
            Debug.LogException(threadException);
        }
        #endregion Public Methods
    }

}