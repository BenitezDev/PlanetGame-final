using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancer
{
    [ExecuteInEditMode]
    public class GPUInstancerPrefabManager : GPUInstancerManager
    {
        [SerializeField]
        public List<RegisteredPrefabsData> registeredPrefabs = new List<RegisteredPrefabsData>();
        [SerializeField]
        public List<GameObject> prefabList;
        public bool enableMROnManagerDisable = true;
        public bool enableMROnRemoveInstance = true;

        private List<GPUInstancerModificationCollider> _modificationColliders;
        private Dictionary<GPUInstancerPrototype, List<GPUInstancerPrefab>> _registeredPrefabsRuntimeData;
        private List<IPrefabVariationData> _variationDataList;
        private bool _addRemoveInProgress;

        #region MonoBehavior Methods

        public override void Awake()
        {
            base.Awake();
        }

        public override void Reset()
        {
            base.Reset();

            RegisterPrefabsInScene();
        }

        public override void Update()
        {
            base.Update();

            if (runtimeDataList != null)
            {
                foreach (GPUInstancerRuntimeData runtimeData in runtimeDataList)
                {
                    if (runtimeData.prototype.autoUpdateTransformData
                        //#if UNITY_EDITOR
                        //                        || EditorApplication.isPaused
                        //#endif
                        )
                    {
                        List<GPUInstancerPrefab> prefabInstanceList = _registeredPrefabsRuntimeData[runtimeData.prototype];
                        Transform instanceTransform;
                        foreach (GPUInstancerPrefab prefabInstance in prefabInstanceList)
                        {
                            instanceTransform = prefabInstance.GetInstanceTransform();
                            if (instanceTransform.hasChanged && prefabInstance.state == PrefabInstancingState.Instanced)
                            {
                                instanceTransform.hasChanged = false;
                                runtimeData.instanceDataArray[prefabInstance.gpuInstancerID - 1] = instanceTransform.localToWorldMatrix;
                                runtimeData.transformDataModified = true;
                            }
                        }
                    }

                    if (runtimeData.transformDataModified)
                    {
                        runtimeData.transformationMatrixVisibilityBuffer.SetData(runtimeData.instanceDataArray);
                        runtimeData.transformDataModified = false;
                    }
                }
            }
        }
        #endregion MonoBehavior Methods

        public override void ClearInstancingData()
        {
            base.ClearInstancingData();

            if (Application.isPlaying && _registeredPrefabsRuntimeData != null && enableMROnManagerDisable)
            {
                foreach (GPUInstancerPrefabPrototype p in _registeredPrefabsRuntimeData.Keys)
                {
                    if (p.meshRenderersDisabled)
                        continue;
                    foreach (GPUInstancerPrefab prefabInstance in _registeredPrefabsRuntimeData[p])
                    {
                        if (!prefabInstance)
                            continue;

                        SetRenderersEnabled(prefabInstance, true);
                    }
                }
            }

            if (_variationDataList != null)
            {
                foreach (IPrefabVariationData pvd in _variationDataList)
                    pvd.ReleaseBuffer();
            }
        }

        public override void GeneratePrototypes(bool forceNew = false)
        {
            base.GeneratePrototypes();

            GPUInstancerUtility.SetPrefabInstancePrototypes(gameObject, prototypeList, prefabList, shaderBindings, billboardAtlasBindings, forceNew);
        }

#if UNITY_EDITOR
        public override void CheckPrototypeChanges()
        {
            base.CheckPrototypeChanges();

            if (prefabList == null)
                prefabList = new List<GameObject>();

            prefabList.RemoveAll(p => p == null);
            prefabList.RemoveAll(p => p.GetComponent<GPUInstancerPrefab>() == null);
            prototypeList.RemoveAll(p => p == null);
            prototypeList.RemoveAll(p => !prefabList.Contains(p.prefabObject));

            if (prefabList.Count != prototypeList.Count)
                GeneratePrototypes();

            registeredPrefabs.RemoveAll(rpd => !prototypeList.Contains(rpd.prefabPrototype));
            foreach (GPUInstancerPrefabPrototype prototype in prototypeList)
            {
                if (!registeredPrefabs.Exists(rpd => rpd.prefabPrototype == prototype))
                    registeredPrefabs.Add(new RegisteredPrefabsData(prototype));
            }
        }

        public override void ShowObjectPicker()
        {
            base.ShowObjectPicker();

            EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, "t:prefab", pickerControlID);
        }

        public override void AddPickerObject(UnityEngine.Object pickerObject)
        {
            base.AddPickerObject(pickerObject);

            if (pickerObject == null)
                return;

            if (pickerObject is GPUInstancerPrefabPrototype)
            {
                GPUInstancerPrefabPrototype prefabPrototype = (GPUInstancerPrefabPrototype)pickerObject;
                if (prefabPrototype.prefabObject != null)
                {
                    pickerObject = prefabPrototype.prefabObject;
                }
            }

            if (!(pickerObject is GameObject))
            {
#if UNITY_2018_3_OR_NEWER
                if(PrefabUtility.GetPrefabAssetType(pickerObject) == PrefabAssetType.Model)
#else
                if (PrefabUtility.GetPrefabType(pickerObject) == PrefabType.ModelPrefab)
#endif
                    EditorUtility.DisplayDialog(GPUInstancerConstants.TEXT_PREFAB_TYPE_WARNING_TITLE, GPUInstancerConstants.TEXT_PREFAB_TYPE_WARNING_3D, GPUInstancerConstants.TEXT_OK);
                else
                    EditorUtility.DisplayDialog(GPUInstancerConstants.TEXT_PREFAB_TYPE_WARNING_TITLE, GPUInstancerConstants.TEXT_PREFAB_TYPE_WARNING, GPUInstancerConstants.TEXT_OK);
                return;
            }

            GameObject prefabObject = (GameObject)pickerObject;

#if UNITY_2018_3_OR_NEWER
            PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(pickerObject);

            if (prefabType == PrefabAssetType.Regular || prefabType == PrefabAssetType.Variant)
            {
                if (PrefabUtility.IsPartOfNonAssetPrefabInstance(prefabObject))
                    prefabObject = GPUInstancerUtility.GetOutermostPrefabAssetRoot(prefabObject);
            }
            else
            {
                if (prefabType == PrefabAssetType.Model)
                    EditorUtility.DisplayDialog(GPUInstancerConstants.TEXT_PREFAB_TYPE_WARNING_TITLE, GPUInstancerConstants.TEXT_PREFAB_TYPE_WARNING_3D, GPUInstancerConstants.TEXT_OK);
                else
                    EditorUtility.DisplayDialog(GPUInstancerConstants.TEXT_PREFAB_TYPE_WARNING_TITLE, GPUInstancerConstants.TEXT_PREFAB_TYPE_WARNING, GPUInstancerConstants.TEXT_OK);
                return;
            }
            if (prefabType == PrefabAssetType.Variant)
            {
                if (prefabObject.GetComponent<GPUInstancerPrefab>() == null && 
                    !EditorUtility.DisplayDialog("Variant Prefab Warning",
                        "Prefab is a Variant. Do you wish to add the Variant as a prototype or the corresponding Prefab?" +
                        "\n\nIt is recommended to add the Prefab, if you do not have different renderers for the Variant.",
                        "Add Variant",
                        "Add Prefab"))
                {
                    prefabObject = GPUInstancerUtility.GetCorrespongingPrefabOfVariant(prefabObject);
                }
            }
#else
            PrefabType prefabType = PrefabUtility.GetPrefabType(pickerObject);

            if (prefabType != PrefabType.Prefab)
            {
                bool instanceFound = false;
                if (prefabType == PrefabType.PrefabInstance)
                {
#if UNITY_2018_2_OR_NEWER
                    GameObject newPrefabObject = (GameObject)PrefabUtility.GetCorrespondingObjectFromSource(prefabObject);
#else
                    GameObject newPrefabObject = (GameObject)PrefabUtility.GetPrefabParent(prefabObject);
#endif
                    if (PrefabUtility.GetPrefabType(newPrefabObject) == PrefabType.Prefab)
                    {
                        while (newPrefabObject.transform.parent != null)
                            newPrefabObject = newPrefabObject.transform.parent.gameObject;
                        prefabObject = newPrefabObject;
                        instanceFound = true;
                    }
                }
                if (!instanceFound)
                {
                    if (prefabType == PrefabType.ModelPrefab)
                        EditorUtility.DisplayDialog(GPUInstancerConstants.TEXT_PREFAB_TYPE_WARNING_TITLE, GPUInstancerConstants.TEXT_PREFAB_TYPE_WARNING_3D, GPUInstancerConstants.TEXT_OK);
                    else
                        EditorUtility.DisplayDialog(GPUInstancerConstants.TEXT_PREFAB_TYPE_WARNING_TITLE, GPUInstancerConstants.TEXT_PREFAB_TYPE_WARNING, GPUInstancerConstants.TEXT_OK);
                    return;
                }
            }
#endif

            if (prefabList.Contains(prefabObject))
            {
                selectedPrototype = prefabObject.GetComponent<GPUInstancerPrefab>().prefabPrototype;
                return;
            }

            GPUInstancerPrefab prefabScript = prefabObject.GetComponent<GPUInstancerPrefab>();
            if (prefabScript == null)
            {
#if UNITY_2018_3_OR_NEWER
                prefabScript = GPUInstancerUtility.AddComponentToPrefab<GPUInstancerPrefab>(prefabObject);
#else
                prefabScript = prefabObject.AddComponent<GPUInstancerPrefab>();
#endif
            }
            if (prefabScript == null)
                return;

            if (prefabScript.prefabPrototype != null && prototypeList.Contains(prefabScript.prefabPrototype))
            {
                selectedPrototype = prefabScript.prefabPrototype;
                return;
            }

            Undo.RecordObject(this, "Add prototype");

            if (!prefabList.Contains(prefabObject))
            {
                prefabList.Add(prefabObject);
                GeneratePrototypes();
            }

            if (prefabScript.prefabPrototype != null)
            {
                if (registeredPrefabs == null)
                    registeredPrefabs = new List<RegisteredPrefabsData>();

                RegisteredPrefabsData data = registeredPrefabs.Find(d => d.prefabPrototype == prefabScript.prefabPrototype);
                if (data == null)
                {
                    data = new RegisteredPrefabsData(prefabScript.prefabPrototype);
                    registeredPrefabs.Add(data);
                }

                GPUInstancerPrefab[] scenePrefabInstances = FindObjectsOfType<GPUInstancerPrefab>();
                foreach (GPUInstancerPrefab prefabInstance in scenePrefabInstances)
                    if (prefabInstance.prefabPrototype == prefabScript.prefabPrototype)
                        data.registeredPrefabs.Add(prefabInstance);
            }
        }
#endif
        public override void InitializeRuntimeDataAndBuffers(bool forceNew = true)
        {
            base.InitializeRuntimeDataAndBuffers(forceNew);

            if (!forceNew && isInitialized)
                return;

            if (_registeredPrefabsRuntimeData == null)
                _registeredPrefabsRuntimeData = new Dictionary<GPUInstancerPrototype, List<GPUInstancerPrefab>>();

            if (registeredPrefabs != null && registeredPrefabs.Count > 0)
            {
                foreach (RegisteredPrefabsData rpd in registeredPrefabs)
                {
                    if (!_registeredPrefabsRuntimeData.ContainsKey(rpd.prefabPrototype))
                        _registeredPrefabsRuntimeData.Add(rpd.prefabPrototype, rpd.registeredPrefabs);
                    else
                    {
                        _registeredPrefabsRuntimeData[rpd.prefabPrototype].AddRange(rpd.registeredPrefabs);
                        _registeredPrefabsRuntimeData[rpd.prefabPrototype] = new List<GPUInstancerPrefab>(_registeredPrefabsRuntimeData[rpd.prefabPrototype].Distinct());
                    }
                }
                registeredPrefabs.Clear();
            }

            if (_registeredPrefabsRuntimeData.Count != prototypeList.Count)
            {
                foreach (GPUInstancerPrototype p in prototypeList)
                {
                    if (!_registeredPrefabsRuntimeData.ContainsKey(p))
                        _registeredPrefabsRuntimeData.Add(p, new List<GPUInstancerPrefab>());
                }
            }

            InitializeRuntimeDataRegisteredPrefabs();
            GPUInstancerUtility.InitializeGPUBuffers(runtimeDataList);
            isInitial = true;
            isInitialized = true;
        }

        public override void DeletePrototype(GPUInstancerPrototype prototype, bool removeSO = true)
        {
            base.DeletePrototype(prototype, removeSO);

            prefabList.Remove(prototype.prefabObject);
            if (removeSO)
            {
#if UNITY_2018_3_OR_NEWER && UNITY_EDITOR
                GPUInstancerUtility.RemoveComponentFromPrefab<GPUInstancerPrefab>(prototype.prefabObject);
                GPUInstancerUtility.RemoveComponentFromPrefab<GPUInstancerPrefabRuntimeHandler>(prototype.prefabObject);
#else
                DestroyImmediate(prototype.prefabObject.GetComponent<GPUInstancerPrefab>(), true);
                if (prototype.prefabObject.GetComponent<GPUInstancerPrefabRuntimeHandler>() != null)
                    DestroyImmediate(prototype.prefabObject.GetComponent<GPUInstancerPrefabRuntimeHandler>(), true);
#endif
#if UNITY_EDITOR
                EditorUtility.SetDirty(prototype.prefabObject);
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(prototype));
#endif
            }
            GeneratePrototypes(false);
        }

        private void InitializeRuntimeDataRegisteredPrefabs(int additionalBufferSize = 0)
        {
            if (runtimeDataList == null)
                runtimeDataList = new List<GPUInstancerRuntimeData>();
            else
                GPUInstancerUtility.ClearInstanceData(runtimeDataList);

            foreach (GPUInstancerPrefabPrototype p in prototypeList)
            {
                InitializeRuntimeDataForPrefabPrototype(p, additionalBufferSize);
            }
        }

        private GPUInstancerRuntimeData InitializeRuntimeDataForPrefabPrototype(GPUInstancerPrefabPrototype p, int additionalBufferSize = 0)
        {
            GPUInstancerRuntimeData runtimeData = runtimeDataList.Find(rd => rd.prototype == p);
            if (runtimeData == null)
            {
                runtimeData = new GPUInstancerRuntimeData(p);
                if (!runtimeData.CreateRenderersFromGameObject(p, shaderBindings))
                    return null;
                runtimeDataList.Add(runtimeData);
                if (p.isShadowCasting)
                {
                    runtimeData.hasShadowCasterBuffer = true;
                    if (!p.useOriginalShaderForShadow)
                    {
                        runtimeData.shadowCasterMaterial = new Material(Shader.Find(GPUInstancerConstants.SHADER_GPUI_SHADOWS_ONLY));
                    }
                }

                GPUInstancerUtility.AddBillboardToRuntimeData(runtimeData, shaderBindings);

                if (p.treeType == GPUInstancerTreeType.SpeedTree || p.treeType == GPUInstancerTreeType.TreeCreatorTree)
                    AddTreeProxy(p, runtimeData);
            }

            int instanceCount = 0;
            List<GPUInstancerPrefab> registeredPrefabsList;
            if (_registeredPrefabsRuntimeData.TryGetValue(p, out registeredPrefabsList))
            {
                runtimeData.instanceDataArray = new Matrix4x4[registeredPrefabsList.Count + (p.enableRuntimeModifications && p.addRemoveInstancesAtRuntime ? p.extraBufferSize : 0) + additionalBufferSize];
                runtimeData.bufferSize = runtimeData.instanceDataArray.Length;

                Matrix4x4 instanceData;
                foreach (GPUInstancerPrefab prefabInstance in registeredPrefabsList)
                {
                    if (!prefabInstance)
                        continue;

                    instanceData = prefabInstance.GetInstanceTransform().localToWorldMatrix;
                    prefabInstance.GetInstanceTransform().hasChanged = false;
                    prefabInstance.state = PrefabInstancingState.Instanced;

                    bool disableRenderers = true;

                    if (prefabInstance.prefabPrototype.enableRuntimeModifications)
                    {
                        if (_modificationColliders != null && _modificationColliders.Count > 0)
                        {
                            bool isInsideCollider = false;
                            foreach (GPUInstancerModificationCollider mc in _modificationColliders)
                            {
                                if (mc.IsInsideCollider(prefabInstance))
                                {
                                    isInsideCollider = true;
                                    mc.AddEnteredInstance(prefabInstance);
                                    instanceData = Matrix4x4.zero;
                                    prefabInstance.state = PrefabInstancingState.Disabled;
                                    disableRenderers = false;
                                    break;
                                }
                            }
                            if (!isInsideCollider)
                            {
                                if (prefabInstance.prefabPrototype.startWithRigidBody && prefabInstance.GetComponent<Rigidbody>() != null)
                                {
                                    isInsideCollider = true;
                                    _modificationColliders[0].AddEnteredInstance(prefabInstance);
                                    instanceData = Matrix4x4.zero;
                                    prefabInstance.state = PrefabInstancingState.Disabled;
                                    disableRenderers = false;
                                }
                            }
                        }
                    }

                    if (disableRenderers && !prefabInstance.prefabPrototype.meshRenderersDisabled)
                        SetRenderersEnabled(prefabInstance, false);

                    runtimeData.instanceDataArray[instanceCount] = instanceData;
                    instanceCount++;
                    prefabInstance.gpuInstancerID = instanceCount;
                }
            }

            // set instanceCount
            runtimeData.instanceCount = instanceCount;

            // variations
            if (_variationDataList != null)
            {
                foreach (IPrefabVariationData pvd in _variationDataList)
                {
                    if (pvd.GetPrototype() == p)
                    {
                        pvd.InitializeBufferAndArray(runtimeData.bufferSize);
                        if (registeredPrefabsList != null)
                        {
                            foreach (GPUInstancerPrefab prefabInstance in registeredPrefabsList)
                            {
                                pvd.SetInstanceData(prefabInstance);
                            }
                        }
                        pvd.SetBufferData(0, 0, runtimeData.bufferSize);

                        for (int i = 0; i < runtimeData.instanceLODs.Count; i++)
                        {
                            for (int j = 0; j < runtimeData.instanceLODs[i].renderers.Count; j++)
                            {
                                pvd.SetVariation(runtimeData.instanceLODs[i].renderers[j].mpb);
                            }
                        }
                    }
                }
            }
            return runtimeData;
        }

        public static void SetRenderersEnabled(GPUInstancerPrefab prefabInstance, bool enabled)
        {
            if (!prefabInstance || !prefabInstance.prefabPrototype || !prefabInstance.prefabPrototype.prefabObject)
                return;

            MeshRenderer[] meshRenderers = prefabInstance.GetComponentsInChildren<MeshRenderer>(true);
            if (meshRenderers != null && meshRenderers.Length > 0)
                for (int mr = 0; mr < meshRenderers.Length; mr++)
                    meshRenderers[mr].enabled = enabled;

            BillboardRenderer[] billboardRenderers = prefabInstance.GetComponentsInChildren<BillboardRenderer>(true);
            if (billboardRenderers != null && billboardRenderers.Length > 0)
                for (int mr = 0; mr < billboardRenderers.Length; mr++)
                    billboardRenderers[mr].enabled = enabled;

            LODGroup lodGroup = prefabInstance.GetComponent<LODGroup>();
            if (lodGroup != null)
                lodGroup.enabled = enabled;

            Rigidbody rigidbody = prefabInstance.GetComponent<Rigidbody>();

            if (enabled)
            {
                if (rigidbody == null)
                {
                    GPUInstancerPrefabPrototype.RigidbodyData rigidbodyData = prefabInstance.prefabPrototype.rigidbodyData;
                    if (rigidbodyData != null)
                    {
                        rigidbody = prefabInstance.gameObject.AddComponent<Rigidbody>();
                        rigidbody.useGravity = rigidbodyData.useGravity;
                        rigidbody.angularDrag = rigidbodyData.angularDrag;
                        rigidbody.mass = rigidbodyData.mass;
                        rigidbody.constraints = rigidbodyData.constraints;
                        rigidbody.detectCollisions = true;
                        rigidbody.drag = rigidbodyData.drag;
                        rigidbody.isKinematic = rigidbodyData.isKinematic;
                        rigidbody.interpolation = rigidbodyData.interpolation;
                    }
                }
            }
            else if (rigidbody != null && !prefabInstance.prefabPrototype.autoUpdateTransformData)
                Destroy(rigidbody);
        }

        #region API Methods

        public void DisableIntancingForInstance(GPUInstancerPrefab prefabInstance)
        {
            if (!prefabInstance)
                return;

            GPUInstancerRuntimeData runtimeData = runtimeDataList.Find(rd => rd.prototype == prefabInstance.prefabPrototype);
            if (runtimeData != null && prefabInstance.gpuInstancerID > 0 && prefabInstance.gpuInstancerID <= runtimeData.instanceDataArray.Length)
            {
                prefabInstance.state = PrefabInstancingState.Disabled;
                runtimeData.instanceDataArray[prefabInstance.gpuInstancerID - 1] = Matrix4x4.zero;

                runtimeData.transformationMatrixVisibilityBuffer.SetDataPartial(runtimeData.instanceDataArray, prefabInstance.gpuInstancerID - 1, prefabInstance.gpuInstancerID - 1, 1);
                SetRenderersEnabled(prefabInstance, true);
            }
            else
            {
                Debug.LogWarning("Can not disable instancing for instance with id: " + prefabInstance.gpuInstancerID);
            }
        }

        public void EnableInstancingForInstance(GPUInstancerPrefab prefabInstance)
        {
            if (!prefabInstance)
                return;

            GPUInstancerRuntimeData runtimeData = runtimeDataList.Find(rd => rd.prototype == prefabInstance.prefabPrototype);
            if (runtimeData != null && prefabInstance.gpuInstancerID > 0 && prefabInstance.gpuInstancerID <= runtimeData.instanceDataArray.Length)
            {
                prefabInstance.state = PrefabInstancingState.Instanced;
                runtimeData.instanceDataArray[prefabInstance.gpuInstancerID - 1] = prefabInstance.GetInstanceTransform().localToWorldMatrix;

                runtimeData.transformationMatrixVisibilityBuffer.SetDataPartial(runtimeData.instanceDataArray, prefabInstance.gpuInstancerID - 1, prefabInstance.gpuInstancerID - 1, 1);
                SetRenderersEnabled(prefabInstance, false);
            }
            else
            {
                Debug.LogWarning("Can not enable instancing for instance with id: " + prefabInstance.gpuInstancerID);
            }
        }

        public void UpdateTransformDataForInstance(GPUInstancerPrefab prefabInstance)
        {
            if (!prefabInstance)
                return;

            GPUInstancerRuntimeData runtimeData = runtimeDataList.Find(rd => rd.prototype == prefabInstance.prefabPrototype);
            if (runtimeData != null && prefabInstance.gpuInstancerID > 0 && prefabInstance.gpuInstancerID <= runtimeData.instanceDataArray.Length)
            {
                runtimeData.instanceDataArray[prefabInstance.gpuInstancerID - 1] = prefabInstance.GetInstanceTransform().localToWorldMatrix;

                // automatically set in Update method
                runtimeData.transformDataModified = true;
                //runtimeData.transformationMatrixVisibilityBuffer.SetDataPartial(runtimeData.instanceDataArray, prefabInstance.gpuInstancerID - 1, prefabInstance.gpuInstancerID - 1, 1);
            }
            else
            {
                Debug.LogWarning("Can not update transform for instance with id: " + prefabInstance.gpuInstancerID);
            }
        }

        public void AddPrefabInstance(GPUInstancerPrefab prefabInstance, bool automaticallyIncreaseBufferSize = false)
        {
            if (!prefabInstance || prefabInstance.state == PrefabInstancingState.Instanced)
                return;

            GPUInstancerRuntimeData runtimeData = runtimeDataList.Find(rd => rd.prototype == prefabInstance.prefabPrototype);
            if (runtimeData != null)
            {
                if (runtimeData.instanceDataArray.Length == runtimeData.instanceCount)
                {
                    if (automaticallyIncreaseBufferSize)
                    {
                        runtimeData.bufferSize += 1024;
                        Matrix4x4[] oldInstanceDataArray = runtimeData.instanceDataArray;
                        runtimeData.instanceDataArray = new Matrix4x4[runtimeData.bufferSize];
                        Array.Copy(oldInstanceDataArray, runtimeData.instanceDataArray, oldInstanceDataArray.Length);
                        runtimeData.instanceDataArray[runtimeData.instanceCount] = prefabInstance.GetInstanceTransform().localToWorldMatrix;
                        runtimeData.instanceCount++;
                        prefabInstance.gpuInstancerID = runtimeData.instanceCount;
                        _registeredPrefabsRuntimeData[prefabInstance.prefabPrototype].Add(prefabInstance);
                        if (!prefabInstance.prefabPrototype.meshRenderersDisabled)
                            SetRenderersEnabled(prefabInstance, false);
                        prefabInstance.GetInstanceTransform().hasChanged = false;
                        prefabInstance.state = PrefabInstancingState.Instanced;
                        GPUInstancerUtility.InitializeGPUBuffer(runtimeData);
                        return;
                    }
                    else
                    {
                        Debug.LogWarning("Can not add instance. Buffer is full.");
                        return;
                    }
                }
                prefabInstance.state = PrefabInstancingState.Instanced;
                runtimeData.instanceDataArray[runtimeData.instanceCount] = prefabInstance.GetInstanceTransform().localToWorldMatrix;
                runtimeData.instanceCount++;
                prefabInstance.gpuInstancerID = runtimeData.instanceCount;

                runtimeData.transformationMatrixVisibilityBuffer.SetDataPartial(runtimeData.instanceDataArray, prefabInstance.gpuInstancerID - 1, prefabInstance.gpuInstancerID - 1, 1);
                if (!prefabInstance.prefabPrototype.meshRenderersDisabled)
                    SetRenderersEnabled(prefabInstance, false);

                if (!_registeredPrefabsRuntimeData.ContainsKey(prefabInstance.prefabPrototype))
                    _registeredPrefabsRuntimeData.Add(prefabInstance.prefabPrototype, new List<GPUInstancerPrefab>());
                _registeredPrefabsRuntimeData[prefabInstance.prefabPrototype].Add(prefabInstance);
                prefabInstance.GetInstanceTransform().hasChanged = false;

                // variations
                if (_variationDataList != null)
                {
                    foreach (IPrefabVariationData pvd in _variationDataList)
                    {
                        if (pvd.GetPrototype() == prefabInstance.prefabPrototype)
                        {
                            pvd.SetInstanceData(prefabInstance);
                            pvd.SetBufferData(prefabInstance.gpuInstancerID - 1, prefabInstance.gpuInstancerID - 1, 1);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds prefab instances for multiple prototypes
        /// </summary>
        public void AddPrefabInstances(IEnumerable<GPUInstancerPrefab> prefabInstances, bool isThreading = false)
        {
            while (isThreading && _addRemoveInProgress)
                Thread.Sleep(100);
            _addRemoveInProgress = true;
            List<GPUInstancerPrefab>[] instanceLists = new List<GPUInstancerPrefab>[prototypeList.Count];
            Dictionary<GPUInstancerPrototype, int> indexDict = new Dictionary<GPUInstancerPrototype, int>();
            for (int i = 0; i < instanceLists.Length; i++)
            {
                instanceLists[i] = new List<GPUInstancerPrefab>();
                indexDict.Add(prototypeList[i], i);
            }

            foreach (GPUInstancerPrefab prefabInstance in prefabInstances)
            {
                instanceLists[indexDict[prefabInstance.prefabPrototype]].Add(prefabInstance);
            }

            for (int i = 0; i < instanceLists.Length; i++)
            {
                AddPrefabInstances((GPUInstancerPrefabPrototype)prototypeList[i], instanceLists[i], isThreading);
            }
            if (isThreading)
                threadQueue.Enqueue(() => _addRemoveInProgress = false);
            else
                _addRemoveInProgress = false;
        }

        /// <summary>
        /// Adds prefab instances for single prototye
        /// </summary>
        public void AddPrefabInstances(GPUInstancerPrefabPrototype prototype, IEnumerable<GPUInstancerPrefab> prefabInstances, bool isThreading = false)
        {
            if (prefabInstances == null || prefabInstances.Count() == 0)
                return;

            GPUInstancerRuntimeData runtimeData = runtimeDataList.Find(rd => rd.prototype == prototype);

            if (runtimeData == null)
            {
                Debug.LogError("Can not find RuntimeData for prototype: " + prototype.name);
                return;
            }

            int count = prefabInstances.Count();

            GPUInstancerPrefab prefabInstance;
            if (runtimeData.instanceCount + count > runtimeData.bufferSize)
            {
                runtimeData.bufferSize = runtimeData.instanceCount + count;
                Matrix4x4[] oldInstanceDataArray = runtimeData.instanceDataArray;
                runtimeData.instanceDataArray = new Matrix4x4[runtimeData.bufferSize];
                Array.Copy(oldInstanceDataArray, runtimeData.instanceDataArray, oldInstanceDataArray.Length);

                for (int i = 0; i < count; i++)
                {
                    prefabInstance = prefabInstances.ElementAt(i);
                    runtimeData.instanceDataArray[runtimeData.instanceCount + i] = prefabInstance.GetLocalToWorldMatrix();
                    prefabInstance.gpuInstancerID = runtimeData.instanceCount + i + 1;
                    if (!prototype.meshRenderersDisabled)
                        SetRenderersEnabled(prefabInstance, false);
                    prefabInstance.state = PrefabInstancingState.Instanced;
                }
                _registeredPrefabsRuntimeData[prototype].AddRange(prefabInstances);
                runtimeData.instanceCount = runtimeData.bufferSize;

                if (isThreading)
                    threadQueue.Enqueue(() => GPUInstancerUtility.InitializeGPUBuffer(runtimeData));
                else
                    GPUInstancerUtility.InitializeGPUBuffer(runtimeData);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                prefabInstance = prefabInstances.ElementAt(i);
                runtimeData.instanceDataArray[runtimeData.instanceCount + i] = prefabInstance.GetLocalToWorldMatrix();
                prefabInstance.gpuInstancerID = runtimeData.instanceCount + i + 1;
                if (!prototype.meshRenderersDisabled)
                    SetRenderersEnabled(prefabInstance, false);
                prefabInstance.state = PrefabInstancingState.Instanced;
            }
            _registeredPrefabsRuntimeData[prototype].AddRange(prefabInstances);
            if (isThreading)
                threadQueue.Enqueue(() => runtimeData.transformationMatrixVisibilityBuffer.SetData(runtimeData.instanceDataArray));
            else
                runtimeData.transformationMatrixVisibilityBuffer.SetData(runtimeData.instanceDataArray);
            runtimeData.instanceCount += count;
        }

        public void UpdateInstanceDataArray(GPUInstancerRuntimeData runtimeData, List<GPUInstancerPrefab> prefabList, bool isThreading = false)
        {
            if (runtimeData.instanceDataArray.Length != prefabList.Count)
                runtimeData.instanceDataArray = new Matrix4x4[prefabList.Count];

            for (int i = 0; i < prefabList.Count;)
            {
                runtimeData.instanceDataArray[i] = prefabList[i].GetLocalToWorldMatrix();
                prefabList[i].gpuInstancerID = ++i;
            }
            int instanceCount = prefabList.Count;
            int bufferSize = instanceCount + ((GPUInstancerPrefabPrototype)runtimeData.prototype).extraBufferSize;
            if (isThreading)
                threadQueue.Enqueue(() =>
                {
                    runtimeData.instanceCount = instanceCount;
                    runtimeData.bufferSize = bufferSize;
                    GPUInstancerUtility.InitializeGPUBuffer(runtimeData);
                });
            else
            {
                runtimeData.instanceCount = instanceCount;
                runtimeData.bufferSize = bufferSize;
                GPUInstancerUtility.InitializeGPUBuffer(runtimeData);
            }
            return;
        }

        public void RemovePrefabInstance(GPUInstancerPrefab prefabInstance)
        {
            if (!prefabInstance || prefabInstance.state == PrefabInstancingState.None)
                return;

            GPUInstancerRuntimeData runtimeData = runtimeDataList.Find(rd => rd.prototype == prefabInstance.prefabPrototype);
            if (runtimeData != null)
            {
                if (prefabInstance.gpuInstancerID > runtimeData.instanceDataArray.Length)
                {
                    Debug.LogWarning("Instance can not be removed.");
                    return;
                }
                List<GPUInstancerPrefab> prefabInstanceList = _registeredPrefabsRuntimeData[prefabInstance.prefabPrototype];
                GPUInstancerPrefab lastIndexPrefabInstance = prefabInstanceList.Find(pi => pi != null && pi.gpuInstancerID == runtimeData.instanceCount);
                if (!lastIndexPrefabInstance)
                {
                    prefabInstanceList.RemoveAll(pi => pi == null);
                    Debug.LogWarning("Prefab instance was destoyed before being removed from instance list in GPUI Prefab Manager!");
                    return;
                }

                prefabInstance.state = PrefabInstancingState.None;

                // exchange last index with this one
                runtimeData.instanceDataArray[prefabInstance.gpuInstancerID - 1] = runtimeData.instanceDataArray[lastIndexPrefabInstance.gpuInstancerID - 1];
                // set last index data to Matrix4x4.zero
                runtimeData.instanceDataArray[lastIndexPrefabInstance.gpuInstancerID - 1] = Matrix4x4.zero;
                runtimeData.instanceCount--;

                prefabInstanceList.Remove(prefabInstance);

                runtimeData.transformationMatrixVisibilityBuffer.SetDataPartial(runtimeData.instanceDataArray, prefabInstance.gpuInstancerID - 1, prefabInstance.gpuInstancerID - 1, 1);
                runtimeData.transformationMatrixVisibilityBuffer.SetDataPartial(runtimeData.instanceDataArray, lastIndexPrefabInstance.gpuInstancerID - 1, lastIndexPrefabInstance.gpuInstancerID - 1, 1);

                lastIndexPrefabInstance.gpuInstancerID = prefabInstance.gpuInstancerID;

                if (enableMROnRemoveInstance && !prefabInstance.prefabPrototype.meshRenderersDisabled)
                    SetRenderersEnabled(prefabInstance, true);
                //Destroy(prefabInstance);

                // variations
                if (_variationDataList != null)
                {
                    foreach (IPrefabVariationData pvd in _variationDataList)
                    {
                        if (pvd.GetPrototype() == lastIndexPrefabInstance.prefabPrototype)
                        {
                            pvd.SetInstanceData(lastIndexPrefabInstance);
                            pvd.SetBufferData(lastIndexPrefabInstance.gpuInstancerID - 1, lastIndexPrefabInstance.gpuInstancerID - 1, 1);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Removes prefab instances for multiple prototypes
        /// </summary>
        public void RemovePrefabInstances(IEnumerable<GPUInstancerPrefab> prefabInstances, bool isThreading = false)
        {
            while (isThreading && _addRemoveInProgress)
                Thread.Sleep(100);
            _addRemoveInProgress = true;
            List<GPUInstancerPrefab>[] instanceLists = new List<GPUInstancerPrefab>[prototypeList.Count];
            Dictionary<GPUInstancerPrototype, int> indexDict = new Dictionary<GPUInstancerPrototype, int>();
            for (int i = 0; i < instanceLists.Length; i++)
            {
                instanceLists[i] = new List<GPUInstancerPrefab>();
                indexDict.Add(prototypeList[i], i);
            }

            foreach (GPUInstancerPrefab prefabInstance in prefabInstances)
            {
                instanceLists[indexDict[prefabInstance.prefabPrototype]].Add(prefabInstance);
            }
            for (int i = 0; i < instanceLists.Length; i++)
            {
                RemovePrefabInstances((GPUInstancerPrefabPrototype)prototypeList[i], instanceLists[i], isThreading);
            }
            if (isThreading)
                threadQueue.Enqueue(() => _addRemoveInProgress = false);
            else
                _addRemoveInProgress = false;
        }

        /// <summary>
        /// Removes prefab instances for single prototye
        /// </summary>
        public void RemovePrefabInstances(GPUInstancerPrefabPrototype prototype, IEnumerable<GPUInstancerPrefab> prefabInstances, bool isThreading = false)
        {
            if (prefabInstances == null || prefabInstances.Count() == 0)
                return;

            int count = prefabInstances.Count();

            GPUInstancerRuntimeData runtimeData = runtimeDataList.Find(rd => rd.prototype == prototype);

            if (runtimeData == null)
            {
                Debug.LogError("Can not find RuntimeData for prototype: " + prototype.name);
                return;
            }

            List<GPUInstancerPrefab> prefabInstanceList = _registeredPrefabsRuntimeData[prototype];
            prefabInstanceList.RemoveRange(prefabInstances.ElementAt(0).gpuInstancerID - 1, count);
            foreach (GPUInstancerPrefab pi in prefabInstances)
            {
                if (enableMROnRemoveInstance && !prototype.meshRenderersDisabled)
                    SetRenderersEnabled(pi, true);
                pi.state = PrefabInstancingState.None;
                pi.gpuInstancerID = 0;
            }

            UpdateInstanceDataArray(runtimeData, prefabInstanceList, isThreading);
        }

        public void RegisterPrefabsInScene()
        {
#if UNITY_EDITOR
            Undo.RecordObject(this, "Registered prefabs changed");
#endif
            registeredPrefabs.Clear();
            foreach (GPUInstancerPrefabPrototype pp in prototypeList)
                registeredPrefabs.Add(new RegisteredPrefabsData(pp));

            GPUInstancerPrefab[] scenePrefabInstances = FindObjectsOfType<GPUInstancerPrefab>();
            foreach (GPUInstancerPrefab prefabInstance in scenePrefabInstances)
                AddRegisteredPrefab(prefabInstance);
        }

        public void RegisterPrefabInstanceList(IEnumerable<GPUInstancerPrefab> prefabInstanceList)
        {
            if (_registeredPrefabsRuntimeData == null)
                _registeredPrefabsRuntimeData = new Dictionary<GPUInstancerPrototype, List<GPUInstancerPrefab>>();
            if (_registeredPrefabsRuntimeData.Keys.Count != prototypeList.Count)
            {
                foreach (GPUInstancerPrototype prototype in prototypeList)
                    if (!_registeredPrefabsRuntimeData.ContainsKey(prototype))
                        _registeredPrefabsRuntimeData.Add(prototype, new List<GPUInstancerPrefab>());
            }

            foreach (GPUInstancerPrefab prefabInstance in prefabInstanceList)
            {
                _registeredPrefabsRuntimeData[prefabInstance.prefabPrototype].Add(prefabInstance);
            }
        }

        public void UnregisterPrefabInstanceList(IEnumerable<GPUInstancerPrefab> prefabInstanceList)
        {
            if (_registeredPrefabsRuntimeData == null)
                _registeredPrefabsRuntimeData = new Dictionary<GPUInstancerPrototype, List<GPUInstancerPrefab>>();
            if (_registeredPrefabsRuntimeData.Keys.Count != prototypeList.Count)
            {
                foreach (GPUInstancerPrototype prototype in prototypeList)
                    if (!_registeredPrefabsRuntimeData.ContainsKey(prototype))
                        _registeredPrefabsRuntimeData.Add(prototype, new List<GPUInstancerPrefab>());
            }

            foreach (GPUInstancerPrefab prefabInstance in prefabInstanceList)
            {
                _registeredPrefabsRuntimeData[prefabInstance.prefabPrototype].Remove(prefabInstance);
            }
        }

        public void ClearRegisteredPrefabInstances()
        {
            foreach (GPUInstancerPrototype p in _registeredPrefabsRuntimeData.Keys)
            {
                _registeredPrefabsRuntimeData[p].Clear();
            }
        }

        public void ClearRegisteredPrefabInstances(GPUInstancerPrototype p)
        {
            if (_registeredPrefabsRuntimeData.ContainsKey(p))
                _registeredPrefabsRuntimeData[p].Clear();
        }

        public PrefabVariationData<T> DefinePrototypeVariationBuffer<T>(GPUInstancerPrefabPrototype prototype, string bufferName)
        {
            if (_variationDataList == null)
                _variationDataList = new List<IPrefabVariationData>();
            PrefabVariationData<T> result = new PrefabVariationData<T>(prototype, bufferName);
            _variationDataList.Add(result);
            return result;
        }

        public void UpdateVariationData<T>(GPUInstancerPrefab prefabInstance, string bufferName, T value)
        {
            if (!prefabInstance || !prefabInstance.prefabPrototype)
                return;
            PrefabVariationData<T> variationData = (PrefabVariationData<T>)_variationDataList.Find(v => v is PrefabVariationData<T> &&
                ((PrefabVariationData<T>)v).prototype == prefabInstance.prefabPrototype && ((PrefabVariationData<T>)v).bufferName == bufferName);
            if (variationData != null && variationData.dataArray != null)
            {
                int index = prefabInstance.gpuInstancerID - 1;
                if (index >= 0 && index < variationData.dataArray.Length)
                {
                    variationData.dataArray[index] = value;
#if UNITY_2017_1_OR_NEWER
                    variationData.variationBuffer.SetData(variationData.dataArray, index, index, 1);
#else
                    variationData.variationBuffer.SetData(variationData.dataArray);
#endif
                }
            }
        }

        public PrefabVariationData<T> DefineAndAddVariationFromArray<T>(GPUInstancerPrefabPrototype prototype, string bufferName, T[] variationArray)
        {
            PrefabVariationData<T> variationData = DefinePrototypeVariationBuffer<T>(prototype, bufferName);
            GPUInstancerRuntimeData runtimeData = runtimeDataList.Find(rd => rd.prototype == prototype);
            if (runtimeData != null)
            {
                variationData.SetArrayAndInitializeBuffer(variationArray);
                variationData.SetBufferData(0, 0, runtimeData.bufferSize);
                for (int l = 0; l < runtimeData.instanceLODs.Count; l++)
                {
                    for (int r = 0; r < runtimeData.instanceLODs[l].renderers.Count; r++)
                    {
                        variationData.SetVariation(runtimeData.instanceLODs[l].renderers[r].mpb);
                    }
                }
            }

            return variationData;
        }

        public PrefabVariationData<T> UpdateVariationsFromArray<T>(GPUInstancerPrefabPrototype prototype, string bufferName, T[] variationArray)
        {
            PrefabVariationData<T> variationData = (PrefabVariationData<T>)_variationDataList.Find(v => v is PrefabVariationData<T> &&
                ((PrefabVariationData<T>)v).prototype == prototype && ((PrefabVariationData<T>)v).bufferName == bufferName);
            if (variationData != null)
            {
                GPUInstancerRuntimeData runtimeData = runtimeDataList.Find(rd => rd.prototype == prototype);
                if (runtimeData != null)
                {
                    variationData.dataArray = variationArray;
                    variationData.SetBufferData(0, 0, runtimeData.bufferSize);
                    for (int l = 0; l < runtimeData.instanceLODs.Count; l++)
                    {
                        for (int r = 0; r < runtimeData.instanceLODs[l].renderers.Count; r++)
                        {
                            variationData.SetVariation(runtimeData.instanceLODs[l].renderers[r].mpb);
                        }
                    }
                }
            }

            return variationData;
        }

        public GPUInstancerPrefabPrototype DefineGameObjectAsPrefabPrototypeAtRuntime(GameObject prototypeGameObject)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("DefineGameObjectAsPrefabPrototypeAtRuntime method is designed to use at runtime. Prototype generation canceled.");
                return null;
            }

            GPUInstancerPrefabPrototype prefabPrototype = GPUInstancerUtility.GeneratePrefabPrototype(prototypeGameObject, shaderBindings, billboardAtlasBindings, false);
            if (!prototypeList.Contains(prefabPrototype))
                prototypeList.Add(prefabPrototype);
            if (!prefabList.Contains(prototypeGameObject))
                prefabList.Add(prototypeGameObject);

            return prefabPrototype;
        }

        public void AddInstancesToPrefabPrototypeAtRuntime(GPUInstancerPrefabPrototype prefabPrototype, IEnumerable<GameObject> instances)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("AddInstancesToPrefabPrototypeAtRuntime method is designed to use at runtime. Adding instances canceled.");
                return;
            }

            if (isActiveAndEnabled)
            {
                List<GPUInstancerPrefab> instanceList;
                if (!_registeredPrefabsRuntimeData.TryGetValue(prefabPrototype, out instanceList))
                {
                    instanceList = new List<GPUInstancerPrefab>();
                    _registeredPrefabsRuntimeData.Add(prefabPrototype, instanceList);
                }

                GPUInstancerPrefab prefabInstance;
                foreach (GameObject instance in instances)
                {
                    prefabInstance = instance.GetComponent<GPUInstancerPrefab>();
                    if (prefabInstance == null)
                    {
                        prefabInstance = instance.AddComponent<GPUInstancerPrefab>();
                        prefabInstance.prefabPrototype = prefabPrototype;
                    }
                    if (prefabInstance != null && !instanceList.Contains(prefabInstance))
                        instanceList.Add(prefabInstance);
                }

                GPUInstancerRuntimeData runtimeData = InitializeRuntimeDataForPrefabPrototype(prefabPrototype, 0);
                GPUInstancerUtility.ReleaseInstanceBuffers(runtimeData);
                GPUInstancerUtility.InitializeGPUBuffer(runtimeData);
            }
            else
            {
                if (registeredPrefabs == null)
                    registeredPrefabs = new List<RegisteredPrefabsData>();

                RegisteredPrefabsData data = registeredPrefabs.Find(d => d.prefabPrototype == prefabPrototype);
                if (data == null)
                {
                    data = new RegisteredPrefabsData(prefabPrototype);
                    registeredPrefabs.Add(data);
                }

                GPUInstancerPrefab prefabInstance;
                foreach (GameObject instance in instances)
                {
                    prefabInstance = instance.GetComponent<GPUInstancerPrefab>();
                    if (prefabInstance == null)
                    {
                        prefabInstance = instance.AddComponent<GPUInstancerPrefab>();
                        prefabInstance.prefabPrototype = prefabPrototype;
                    }
                    if (prefabInstance != null && !data.registeredPrefabs.Contains(prefabInstance))
                        data.registeredPrefabs.Add(prefabInstance);
                }
            }
        }

        public void RemovePrototypeAtRuntime(GPUInstancerPrefabPrototype prefabPrototype)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("RemovePrototypeAtRuntime method is designed to use at runtime. Prototype removal canceled.");
                return;
            }

            GPUInstancerRuntimeData runtimeData = runtimeDataList.Find(rd => rd.prototype == prefabPrototype);
            if (runtimeData != null)
            {
                GPUInstancerUtility.ReleaseInstanceBuffers(runtimeData);
                if (runtimeDataList != null)
                    runtimeDataList.Remove(runtimeData);
            }
            if (isActiveAndEnabled)
            {
                _registeredPrefabsRuntimeData.Remove(prefabPrototype);
            }
            else if (registeredPrefabs != null)
            {
                RegisteredPrefabsData data = registeredPrefabs.Find(d => d.prefabPrototype == prefabPrototype);
                if (data != null)
                    registeredPrefabs.Remove(data);
            }
            if (prototypeList.Contains(prefabPrototype))
                prototypeList.Remove(prefabPrototype);
            if (prefabPrototype.prefabObject && prefabList.Contains(prefabPrototype.prefabObject))
                prefabList.Remove(prefabPrototype.prefabObject);
        }
        #endregion API Methods

        public void AddRegisteredPrefab(GPUInstancerPrefab prefabInstance)
        {
            RegisteredPrefabsData data = registeredPrefabs.Find(rpd => rpd.prefabPrototype == prefabInstance.prefabPrototype);
            if (data != null)
                data.registeredPrefabs.Add(prefabInstance);
        }

        public void AddRuntimeRegisteredPrefab(GPUInstancerPrefab prefabInstance)
        {
            List<GPUInstancerPrefab> list;
            if (_registeredPrefabsRuntimeData.ContainsKey(prefabInstance.prefabPrototype))
                list = _registeredPrefabsRuntimeData[prefabInstance.prefabPrototype];
            else
            {
                list = new List<GPUInstancerPrefab>();
                _registeredPrefabsRuntimeData.Add(prefabInstance.prefabPrototype, list);
            }

            if (!list.Contains(prefabInstance))
                list.Add(prefabInstance);
        }

        public void AddModificationCollider(GPUInstancerModificationCollider modificationCollider)
        {
            if (_modificationColliders == null)
                _modificationColliders = new List<GPUInstancerModificationCollider>();

            _modificationColliders.Add(modificationCollider);
        }

        public int GetEnabledPrefabCount()
        {
            int sum = 0;
            if (_modificationColliders != null)
            {
                for (int i = 0; i < _modificationColliders.Count; i++)
                    sum += _modificationColliders[i].GetEnteredInstanceCount();
            }
            return sum;
        }

        public Dictionary<GPUInstancerPrototype, List<GPUInstancerPrefab>> GetRegisteredPrefabsRuntimeData()
        {
            return _registeredPrefabsRuntimeData;
        }
    }

    [Serializable]
    public class RegisteredPrefabsData
    {
        public GPUInstancerPrototype prefabPrototype;
        public List<GPUInstancerPrefab> registeredPrefabs;

        public RegisteredPrefabsData(GPUInstancerPrototype prefabPrototype)
        {
            this.prefabPrototype = prefabPrototype;
            registeredPrefabs = new List<GPUInstancerPrefab>();
        }
    }

    public interface IPrefabVariationData
    {
        void InitializeBufferAndArray(int count, bool setDefaults = true);
        void SetInstanceData(GPUInstancerPrefab prefabInstance);
        void SetBufferData(int managedBufferStartIndex, int computeBufferStartIndex, int count);
        void SetVariation(MaterialPropertyBlock mpb);
        GPUInstancerPrefabPrototype GetPrototype();
        void ReleaseBuffer();
    }

    public class PrefabVariationData<T> : IPrefabVariationData
    {
        public GPUInstancerPrefabPrototype prototype;
        public string bufferName;
        public ComputeBuffer variationBuffer;
        public T[] dataArray;
        public T defaultValue;

        public PrefabVariationData(GPUInstancerPrefabPrototype prototype, string bufferName, T defaultValue = default(T))
        {
            this.prototype = prototype;
            this.bufferName = bufferName;
            this.defaultValue = defaultValue;
        }

        public void InitializeBufferAndArray(int count, bool setDefaults = true)
        {
            dataArray = new T[count];
            if (setDefaults)
            {
                for (int i = 0; i < count; i++)
                {
                    dataArray[i] = defaultValue;
                }
            }
            if (variationBuffer != null)
                variationBuffer.Release();
            variationBuffer = new ComputeBuffer(count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(T)));
        }

        public void SetArrayAndInitializeBuffer(T[] dataArray)
        {
            this.dataArray = dataArray;
            if (variationBuffer != null)
                variationBuffer.Release();
            variationBuffer = new ComputeBuffer(dataArray.Length, System.Runtime.InteropServices.Marshal.SizeOf(typeof(T)));
        }

        public void SetInstanceData(GPUInstancerPrefab prefabInstance)
        {
            if (prefabInstance.variationDataList != null && dataArray != null && prefabInstance.variationDataList.ContainsKey(bufferName) && dataArray.Length > prefabInstance.gpuInstancerID - 1)
                dataArray[prefabInstance.gpuInstancerID - 1] = (T)prefabInstance.variationDataList[bufferName];
        }

        public void SetBufferData(int managedBufferStartIndex, int computeBufferStartIndex, int count)
        {
            if (variationBuffer != null)
            {
#if UNITY_2017_1_OR_NEWER
                variationBuffer.SetData(dataArray, managedBufferStartIndex, computeBufferStartIndex, count);
#else
                variationBuffer.SetData(dataArray);
#endif
            }
        }

        public void SetVariation(MaterialPropertyBlock mpb)
        {
            if (variationBuffer != null)
                mpb.SetBuffer(bufferName, variationBuffer);
        }

        public GPUInstancerPrefabPrototype GetPrototype()
        {
            return prototype;
        }

        public void ReleaseBuffer()
        {
            if (variationBuffer != null)
                variationBuffer.Release();
            variationBuffer = null;
            dataArray = null;
        }
    }
}