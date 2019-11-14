using System.Collections.Generic;
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancer
{
    /// <summary>
    /// Add this to a Unity terrain for GPU Instancing terrain trees at runtime.
    /// </summary>
    [ExecuteInEditMode]
    public class GPUInstancerTreeManager : GPUInstancerTerrainManager
    {
        private static ComputeShader _treeInstantiationComputeShader;

        #region Monobehavior Methods
        public override void Awake()
        {
            base.Awake();

            if (_treeInstantiationComputeShader == null)
                _treeInstantiationComputeShader = Resources.Load<ComputeShader>(GPUInstancerConstants.TREE_INSTANTIATION_RESOURCE_PATH);
        }

        #endregion Monobehavior Methods

        #region Override Methods

        public override void ClearInstancingData()
        {
            base.ClearInstancingData();

            if (terrain != null && terrain.treeDistance == 0)
            {
                terrain.treeDistance = terrainSettings.maxTreeDistance;
            }
        }

        public override void GeneratePrototypes(bool forceNew = false)
        {
            base.GeneratePrototypes(forceNew);

            if (terrainSettings != null && terrain != null && terrain.terrainData != null)
            {
                GPUInstancerUtility.SetTreeInstancePrototypes(gameObject, prototypeList, terrain.terrainData.treePrototypes, shaderBindings, billboardAtlasBindings, terrainSettings, forceNew);
            }
        }

#if UNITY_EDITOR
        public override void CheckPrototypeChanges()
        {
            base.CheckPrototypeChanges();

            if (!Application.isPlaying && Terrain.activeTerrain != null)
            {
                if (prototypeList.Count != Terrain.activeTerrain.terrainData.treePrototypes.Length)
                {
                    GeneratePrototypes();
                }
            }
        }

        public override void ShowObjectPicker()
        {
            base.ShowObjectPicker();

            EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, "", pickerControlID);
        }

        public override void AddPickerObject(UnityEngine.Object pickerObject)
        {
            base.AddPickerObject(pickerObject);

            if (pickerObject == null)
                return;

            if (!(pickerObject is GameObject))
            {
                EditorUtility.DisplayDialog(GPUInstancerConstants.TEXT_PREFAB_TYPE_WARNING_TITLE, GPUInstancerConstants.TEXT_TREE_PREFAB_TYPE_WARNING, GPUInstancerConstants.TEXT_OK);
                return;
            }

            GameObject prefabObject = (GameObject)pickerObject;

#if UNITY_2018_3_OR_NEWER
            PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(pickerObject);

            if (prefabType == PrefabAssetType.Regular || prefabType == PrefabAssetType.Variant || prefabType == PrefabAssetType.Model)
            {
                GameObject newPrefabObject = (GameObject)PrefabUtility.GetCorrespondingObjectFromSource(prefabObject);
                if (newPrefabObject != null)
                {
                    while (newPrefabObject.transform.parent != null)
                        newPrefabObject = newPrefabObject.transform.parent.gameObject;
                    prefabObject = newPrefabObject;
                }
            }
            else
            {
                EditorUtility.DisplayDialog(GPUInstancerConstants.TEXT_PREFAB_TYPE_WARNING_TITLE, GPUInstancerConstants.TEXT_TREE_PREFAB_TYPE_WARNING, GPUInstancerConstants.TEXT_OK);
                return;
            }
#else
            PrefabType prefabType = PrefabUtility.GetPrefabType(pickerObject);

            if (prefabType != PrefabType.Prefab && prefabType != PrefabType.ModelPrefab)
            {
                bool instanceFound = false;
                if (prefabType == PrefabType.PrefabInstance || prefabType == PrefabType.ModelPrefabInstance)
                {
#if UNITY_2018_2_OR_NEWER
                    GameObject newPrefabObject = (GameObject)PrefabUtility.GetCorrespondingObjectFromSource(prefabObject);
#else
                    GameObject newPrefabObject = (GameObject)PrefabUtility.GetPrefabParent(prefabObject);
#endif
                    if (PrefabUtility.GetPrefabType(newPrefabObject) == PrefabType.Prefab || PrefabUtility.GetPrefabType(newPrefabObject) == PrefabType.ModelPrefab)
                    {
                        while (newPrefabObject.transform.parent != null)
                            newPrefabObject = newPrefabObject.transform.parent.gameObject;
                        prefabObject = newPrefabObject;
                        instanceFound = true;
                    }
                }
                if (!instanceFound)
                {
                    EditorUtility.DisplayDialog(GPUInstancerConstants.TEXT_PREFAB_TYPE_WARNING_TITLE, GPUInstancerConstants.TEXT_TREE_PREFAB_TYPE_WARNING, GPUInstancerConstants.TEXT_OK);
                    return;
                }
            }
#endif

            Undo.RecordObject(this, "Add tree prototype");

            if (terrainSettings != null && terrain != null && terrain.terrainData != null)
            {
                List<TreePrototype> newTreePrototypes = new List<TreePrototype>(terrain.terrainData.treePrototypes);

                TreePrototype terrainTreePrototype = new TreePrototype()
                {
                    prefab = prefabObject,
                    bendFactor = 0
                };
                newTreePrototypes.Add(terrainTreePrototype);

                terrain.terrainData.treePrototypes = newTreePrototypes.ToArray();
                terrain.terrainData.RefreshPrototypes();
                GPUInstancerUtility.AddTreeInstancePrototypeFromTerrainPrototype(gameObject, prototypeList, terrainTreePrototype, newTreePrototypes.Count - 1, shaderBindings, billboardAtlasBindings, terrainSettings);
            }
        }
#endif

        public override void InitializeRuntimeDataAndBuffers(bool forceNew = true)
        {
            base.InitializeRuntimeDataAndBuffers(forceNew);

            if (!forceNew && isInitialized)
                return;

            if (terrainSettings == null)
                return;

            if (prototypeList != null && prototypeList.Count > 0)
            {
                GPUInstancerUtility.AddTreeInstanceRuntimeDataToList(runtimeDataList, prototypeList, shaderBindings, terrainSettings);
            }

            StartCoroutine(ReplaceUnityTrees());

            isInitialized = true;
        }

        public override void DeletePrototype(GPUInstancerPrototype prototype, bool removeSO = true)
        {
            if (terrainSettings != null && terrain != null && terrain.terrainData != null)
            {
                int treePrototypeIndex = prototypeList.IndexOf(prototype);

                TreePrototype[] treePrototypes = terrain.terrainData.treePrototypes;
                List<TreePrototype> newTreePrototypes = new List<TreePrototype>(treePrototypes);
                List<TreeInstance> newTreeInstanceList = new List<TreeInstance>();
                TreeInstance treeInstance;

                for (int i = 0; i < terrain.terrainData.treeInstances.Length; i++)
                {
                    treeInstance = terrain.terrainData.treeInstances[i];
                    if (treeInstance.prototypeIndex < treePrototypeIndex)
                    {
                        newTreeInstanceList.Add(treeInstance);
                    }
                    else if (treeInstance.prototypeIndex > treePrototypeIndex)
                    {
                        treeInstance.prototypeIndex = treeInstance.prototypeIndex - 1;
                        newTreeInstanceList.Add(treeInstance);
                    }
                }

                if (newTreePrototypes.Count > treePrototypeIndex)
                    newTreePrototypes.RemoveAt(treePrototypeIndex);

                terrain.terrainData.treeInstances = newTreeInstanceList.ToArray();
                terrain.terrainData.treePrototypes = newTreePrototypes.ToArray();

                terrain.terrainData.RefreshPrototypes();

                if (removeSO)
                    base.DeletePrototype(prototype, removeSO);
                GeneratePrototypes(false);
                if (!removeSO)
                    base.DeletePrototype(prototype, removeSO);
            }
            else
                base.DeletePrototype(prototype, removeSO);
        }

        #endregion Override Methods

        public IEnumerator ReplaceUnityTrees()
        {
            TreeInstance[] treeInstances = terrain.terrainData.treeInstances;
            int instanceTotal = treeInstances.Length;

            if (instanceTotal > 0)
            {
                Vector3 terrainSize = terrain.terrainData.size;
                Vector3 terrainPosition = terrain.GetPosition();

                Vector3 treePos = Vector3.zero;
                TreeInstance treeInstance;

                Vector4[] treeScales = new Vector4[prototypeList.Count];
                int count = 0;
                foreach (GPUInstancerTreePrototype tp in prototypeList)
                {
                    treeScales[count] = tp.prefabObject.transform.localScale;
                    count++;
                }

                terrain.treeDistance = 0f; // will not persist if called at runtime.

                Vector4[] treeDataArray = new Vector4[instanceTotal * 2]; // prototypeIndex - positionx3 - rotation - scalex2
                int[] instanceCounts = new int[terrain.terrainData.treePrototypes.Length];

                int index = 0;
                for (int i = 0; i < instanceTotal; i++)
                {
                    treeInstance = treeInstances[i];
                    treePos = treeInstance.position;

                    treeDataArray[index].x = treeInstance.prototypeIndex;
                    treeDataArray[index].y = treePos.x;
                    treeDataArray[index].z = treePos.y;
                    treeDataArray[index].w = treePos.z;
                    index++;
                    treeDataArray[index].x = treeInstance.rotation; 
                    treeDataArray[index].y = treeInstance.widthScale;
                    treeDataArray[index].z = treeInstance.heightScale;
                    index++;

                    instanceCounts[treeInstance.prototypeIndex]++;
                }
                yield return null;

                ComputeBuffer treeDataBuffer = new ComputeBuffer(treeDataArray.Length, GPUInstancerConstants.STRIDE_SIZE_FLOAT4);
                treeDataBuffer.SetData(treeDataArray);
                ComputeBuffer treeScalesBuffer = new ComputeBuffer(treeScales.Length, GPUInstancerConstants.STRIDE_SIZE_FLOAT4);
                treeScalesBuffer.SetData(treeScales);
                ComputeBuffer counterBuffer = new ComputeBuffer(1, GPUInstancerConstants.STRIDE_SIZE_INT);
                uint[] emptyCounterData = new uint[1];

                GPUInstancerRuntimeData runtimeData;
                for (int i = 0; i < runtimeDataList.Count; i++)
                {
                    if (instanceCounts[i] == 0)
                        continue;

                    runtimeData = runtimeDataList[i];

                    counterBuffer.SetData(emptyCounterData);
                    runtimeData.transformationMatrixVisibilityBuffer = new ComputeBuffer(instanceCounts[i], GPUInstancerConstants.STRIDE_SIZE_MATRIX4X4);

                    _treeInstantiationComputeShader.SetBuffer(0,
                        GPUInstancerConstants.VisibilityKernelPoperties.INSTANCE_DATA_BUFFER, runtimeData.transformationMatrixVisibilityBuffer);
                    _treeInstantiationComputeShader.SetBuffer(0,
                        GPUInstancerConstants.TreeKernelProperties.TREE_DATA, treeDataBuffer);
                    _treeInstantiationComputeShader.SetBuffer(0,
                        GPUInstancerConstants.TreeKernelProperties.TREE_SCALES, treeScalesBuffer);
                    _treeInstantiationComputeShader.SetBuffer(0,
                        GPUInstancerConstants.GrassKernelProperties.COUNTER_BUFFER, counterBuffer);
                    _treeInstantiationComputeShader.SetInt(
                        GPUInstancerConstants.VisibilityKernelPoperties.BUFFER_PARAMETER_BUFFER_SIZE, instanceTotal);
                    _treeInstantiationComputeShader.SetVector(
                        GPUInstancerConstants.GrassKernelProperties.TERRAIN_SIZE_DATA, terrainSize);
                    _treeInstantiationComputeShader.SetVector(
                        GPUInstancerConstants.TreeKernelProperties.TERRAIN_POSITION, terrainPosition);
                    _treeInstantiationComputeShader.SetBool(
                        GPUInstancerConstants.TreeKernelProperties.IS_APPLY_ROTATION, ((GPUInstancerTreePrototype)runtimeData.prototype).isApplyRotation);
                    _treeInstantiationComputeShader.SetInt(
                        GPUInstancerConstants.TreeKernelProperties.PROTOTYPE_INDEX, i);

                    _treeInstantiationComputeShader.Dispatch(0,
                        Mathf.CeilToInt(instanceTotal / GPUInstancerConstants.VISIBILITY_SHADER_THREAD_COUNT), 1, 1);

                    runtimeData.bufferSize = instanceCounts[i];
                    runtimeData.instanceCount = instanceCounts[i];
                    GPUInstancerUtility.InitializeGPUBuffer(runtimeData);

                    yield return null;
                }

                treeDataBuffer.Release();
                treeScalesBuffer.Release();
                counterBuffer.Release();
            }

            isInitial = true;
            GPUInstancerUtility.TriggerEvent(GPUInstancerEventType.TreeInitializationFinished);
        }
    }
}