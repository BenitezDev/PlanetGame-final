using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace GPUInstancer
{
    public static class GPUInstancerAPI
    {
        #region Global

        /// <summary>
        ///     <para>Main GPU Instancer initialization Method. Generates the necessary GPUInstancer runtime data from predifined 
        ///     GPU Instancer prototypes that are registered in the manager, and generates all necessary GPU buffers for instancing.</para>
        ///     <para>Use this as the final step after you setup a GPU Instancer manager and all its prototypes.</para>
        ///     <para>Note that you can also use this to re-initialize the GPU Instancer prototypes that are registered in the manager at runtime.</para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="forceNew">If set to false will not regenerate buffers if already initialized before</param>
        public static void InitializeGPUInstancer(GPUInstancerManager manager, bool forceNew = true)
        {
            manager.InitializeRuntimeDataAndBuffers(forceNew);
        }

        /// <summary>
        ///     <para>Sets the active camera for a specific manager. This camera is used by GPU Instancer for various calculations (including culling operations). </para>
        ///     <para>Use this right after you add or change your camera at runtime. </para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="camera">The camera that GPU Instancer will use.</param>
        public static void SetCamera(GPUInstancerManager manager, Camera camera)
        {
            manager.SetCamera(camera);
        }

        /// <summary>
        ///     <para>Sets the active camera for all managers. This camera is used by GPU Instancer for various calculations (including culling operations). </para>
        ///     <para>Use this right after you add or change your camera at runtime. </para>
        /// </summary>
        /// <param name="camera">The camera that GPU Instancer will use.</param>
        public static void SetCamera(Camera camera)
        {
            if (GPUInstancerManager.activeManagerList != null)
                GPUInstancerManager.activeManagerList.ForEach(m => m.SetCamera(camera));
        }

        /// <summary>
        ///     <para>Returns a list of active managers. Use this if you want to access the managers at runtime.</para>
        /// </summary>
        /// <returns>The List of active managers. Null if no active managers present.</returns>
        public static List<GPUInstancerManager> GetActiveManagers()
        {
            return GPUInstancerManager.activeManagerList == null ? null : GPUInstancerManager.activeManagerList.ToList();
        }

        /// <summary>
        ///     <para>Starts listening the specified process and runs the given callback function when it finishes.</para>
        ///     <para>GPU Instancer does not lock Unity updates when initializing instances and instead, does this in a background process. 
        ///     Each prototype will show on the terrain upon its own initialization. Use this method to get notified when all prototypes are initialized.</para>
        ///     <para>The most common usage for this is to show a loading bar. For an example, see: <seealso cref="DetailDemoSceneController"/></para>
        /// </summary>
        /// <param name="eventType">The event type that will be listened for callback</param>
        /// <param name="callback">The callback function to run upon initialization completion. Can be any function that doesn't take any parameters.</param>
        public static void StartListeningGPUIEvent(GPUInstancerEventType eventType, UnityAction callback)
        {
            GPUInstancerUtility.StartListening(eventType, callback);
        }

        /// <summary>
        ///     <para>Stops listening the specified process and unregisters the given callback function that was registered with <see cref="StartListeningGPUIEvent"/>.</para>
        ///     <para>Use this in your callback function to unregister it (e.g. after hiding the loading bar).</para>
        ///     <para>For an example, see: <seealso cref="DetailDemoSceneController"/></para>
        /// </summary>
        /// <param name="eventType">The event type that was registered with <see cref="StartListeningGPUIEvent"/></param>
        /// <param name="callback">The callback function that was registered with <see cref="StartListeningGPUIEvent"/></param>
        public static void StopListeningGPUIEvent(GPUInstancerEventType eventType, UnityAction callback)
        {
            GPUInstancerUtility.StopListening(eventType, callback);
        }

        /// <summary>
        /// Updates all transform values with the given offset position.
        /// </summary>
        /// <param name="manager">GPUI Manager to apply the offset</param>
        /// <param name="offsetPosition">Offset Position</param>
        public static void SetGlobalPositionOffset(GPUInstancerManager manager, Vector3 offsetPosition)
        {
            GPUInstancerUtility.SetGlobalPositionOffset(manager, offsetPosition);
        }

        /// <summary>
        /// Removes the instances that are inside bounds.
        /// </summary>
        /// <param name="manager">GPUI Manager to remove the instances from</param>
        /// <param name="bounds">Bounds that define the area that the instances will be removed</param>
        public static void RemoveInstancesInsideBounds(GPUInstancerManager manager, Bounds bounds, float offset = 0, List<GPUInstancerPrototype> prototypeFilter = null)
        {
            manager.RemoveInstancesInsideBounds(bounds, offset, prototypeFilter);
        }

        /// <summary>
        /// Removes the instances that are inside collider.
        /// </summary>
        /// <param name="manager">GPUI Manager to remove the instances from</param>
        /// <param name="collider">Collider that define the area that the instances will be removed</param>
        public static void RemoveInstancesInsideCollider(GPUInstancerManager manager, Collider collider, float offset = 0, List<GPUInstancerPrototype> prototypeFilter = null)
        {
            manager.RemoveInstancesInsideCollider(collider, offset, prototypeFilter);
        }

        /// <summary>
        /// [For Advanced Users Only] Returns the float4x4 ComputeBuffer that store the localToWorldMatrix for each instance in GPU memory. This buffer can be used to make
        /// modifications in GPU memory before the rendering process. 
        /// </summary>
        /// <param name="manager">GPUI Manager to get the buffer from</param>
        /// <param name="prototype">Prototype that the buffer belongs to</param>
        /// <returns></returns>
        public static ComputeBuffer GetTransformDataBuffer(GPUInstancerManager manager, GPUInstancerPrototype prototype)
        {
            return manager.GetTransformDataBuffer(prototype);
        }
        #endregion Global

        #region Prefab Instancing

        /// <summary>
        ///     <para>Registers a list of prefab instances with GPU Instancer. You must use <see cref="InitializeGPUInstancer"/> after registering these prefabs for final initialization.</para>
        ///     <para>The prefabs of the instances in this list must be previously defined in the given manager (either at runtime or editor time).</para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="prefabInstanceList">The list of prefabs instances to GPU instance.</param>
        public static void RegisterPrefabInstanceList(GPUInstancerPrefabManager manager, IEnumerable<GPUInstancerPrefab> prefabInstanceList)
        {
            manager.RegisterPrefabInstanceList(prefabInstanceList);
        }

        /// <summary>
        ///     <para>Unregisters a list of prefab instances from GPU Instancer. You must use <see cref="InitializeGPUInstancer"/> after unregistering these prefabs for final initialization.</para>
        ///     <para>The prefabs of the instances in this list must be previously defined in the given manager (either at runtime or editor time).</para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="prefabInstanceList">The list of prefabs instances to be removed from  GPU instancer.</param>
        public static void UnregisterPrefabInstanceList(GPUInstancerPrefabManager manager, IEnumerable<GPUInstancerPrefab> prefabInstanceList)
        {
            manager.UnregisterPrefabInstanceList(prefabInstanceList);
        }

        /// <summary>
        ///     <para>Clears the registered prefab instances from the prefab manager.</para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        public static void ClearRegisteredPrefabInstances(GPUInstancerPrefabManager manager)
        {
            manager.ClearRegisteredPrefabInstances();
        }

        /// <summary>
        ///     <para>Clears the registered prefab instances from the prefab manager for a specific prototype.</para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="prototype">The prototype to clear registered instances for.</param>
        public static void ClearRegisteredPrefabInstances(GPUInstancerPrefabManager manager, GPUInstancerPrototype prototype)
        {
            manager.ClearRegisteredPrefabInstances(prototype);
        }

        /// <summary>
        ///     <para>Adds a new prefab instance for GPU instancing to an already initialized list of registered instances. </para>
        ///     <para>Use this if you want to add another instance of a prefab after you have initialized a list of prefabs with <see cref="InitializeGPUInstancer"/>.</para>
        ///     <para>The prefab of this instance must be previously defined in the given manager (either at runtime or editor time).</para>
        ///     <para>Note that the prefab must be enabled for adding and removal in the manager in order for this to work (for performance reasons).</para>
        ///     <para>Also note that the number of total instances is limited by the count of already initialized instances plus the extra amount you define in the manager.</para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="prefabInstance">The prefab instance to add.</param>
        public static void AddPrefabInstance(GPUInstancerPrefabManager manager, GPUInstancerPrefab prefabInstance)
        {
            manager.AddPrefabInstance(prefabInstance);
        }

        /// <summary>
        ///     <para>Removes a prefab instance from an already initialized list of registered instances. </para>
        ///     <para>Use this if you want to remove a prefab instance after you have initialized a list of prefabs with <see cref="InitializeGPUInstancer"/> 
        ///     (usually before destroying the GameObject).</para>
        ///     <para>The prefab of this instance must be previously defined in the given manager (either at runtime or editor time).</para>
        ///     <para>Note that the prefab must be enabled for adding and removal in the manager in order for this to work (for performance reasons).</para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="prefabInstance">The prefab instance to remove.</param>
        public static void RemovePrefabInstance(GPUInstancerPrefabManager manager, GPUInstancerPrefab prefabInstance)
        {
            manager.RemovePrefabInstance(prefabInstance);
        }

        /// <summary>
        ///     <para>Disables GPU instancing and enables Unity renderers for the given prefab instance without removing it from the list of registerd prefabs.</para>
        ///     <para>Use this if you want to pause GPU Instancing for a prefab (e.g. to enable physics).</para>
        ///     <para>Note that the prefab must be enabled for runtime modifications in the manager in order for this to work (for performance reasons).</para>
        ///     <para>Also note that you can also add <seealso cref="GPUInstancerModificationCollider"/> to a game object to use its collider to automatically 
        ///     enable/disable instancing when a prefab instance enters/exits its collider.</para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="prefabInstance">The prefab instance to disable the GPU Instancing of.</param>
        public static void DisableIntancingForInstance(GPUInstancerPrefabManager manager, GPUInstancerPrefab prefabInstance)
        {
            manager.DisableIntancingForInstance(prefabInstance);
        }

        /// <summary>
        ///     <para>Enables GPU instancing and disables Unity renderers for the given prefab instance without re-adding it to the list of registerd prefabs.</para>
        ///     <para>Use this if you want to unpause GPU Instancing for a prefab.</para>
        ///     <para>Note that the prefab must be enabled for runtime modifications in the manager in order for this to work (for performance reasons).</para>
        ///     <para>Also note that you can also add <seealso cref="GPUInstancerModificationCollider"/> to a game object to use its collider to automatically 
        ///     enable/disable instancing when a prefab instance enters/exits its collider.</para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="prefabInstance">The prefab instance to enable the GPU Instancing of.</param>
        public static void EnableInstancingForInstance(GPUInstancerPrefabManager manager, GPUInstancerPrefab prefabInstance)
        {
            manager.EnableInstancingForInstance(prefabInstance);
        }

        /// <summary>
        ///     <para>Updates and synchronizes the GPU Instancer transform data (position, rotation and scale) for the given prefab instance.</para>
        ///     <para>Use this if you want to update, rotate, and/or scale prefab instances after initialization.</para>
        ///     <para>The updated values are taken directly from the transformation operations made beforehand on the instance's Unity transform component. 
        ///     (These operations will not reflect on the GPU Instanced prefab automatically unless you use this method).</para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="prefabInstance">The prefab instance to update the transform values of. The instance's Unity transform component must be updated beforehand.</param>
        public static void UpdateTransformDataForInstance(GPUInstancerPrefabManager manager, GPUInstancerPrefab prefabInstance)
        {
            manager.UpdateTransformDataForInstance(prefabInstance);
        }

        /// <summary>
        ///     <para>Specifies a variation buffer for a GPU Instancer prototype that is defined in the prefab's shader. Required to use <see cref="AddVariation{T}"/></para>
        ///     <prara>Use this if you want any type of variation between this prototype's instances.</prara>
        ///     <para>To define the buffer necessary for this variation in your shader, you need to create a StructuredBuffer field of the relevant type in that shader. 
        ///     You can then access this buffer with "gpuiTransformationMatrix[unity_InstanceID]"</para>
        ///     <para>see <seealso cref="ColorVariations"/> and its demo scene for an example</para>
        /// </summary>
        /// 
        /// <example> 
        ///     This sample shows how to use the variation buffer in your shader:
        /// 
        ///     <code><![CDATA[
        ///     ...
        ///     fixed4 _Color;
        /// 
        ///     #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        ///         StructuredBuffer<float4> colorBuffer;
        ///     #endif
        ///     ...
        ///     void surf (Input IN, inout SurfaceOutputStandard o) {
        ///     ...
        ///         #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        ///             uint index = gpuiTransformationMatrix[unity_InstanceID];
        ///             col = colorBuffer[index];
        ///         #else
        ///             col = _Color;
        ///         #endif
        ///     ...
        ///     }
        ///     ]]></code>
        /// 
        ///     See "GPUInstancer/ColorVariationShader" for the full example.
        /// 
        /// </example>
        /// 
        /// <typeparam name="T">The type of variation buffer. Must be defined in the instance prototype's shader</typeparam>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="prototype">The GPU Instancer prototype to define variations.</param>
        /// <param name="bufferName">The name of the variation buffer in the prototype's shader.</param>
        public static void DefinePrototypeVariationBuffer<T>(GPUInstancerPrefabManager manager, GPUInstancerPrefabPrototype prototype, string bufferName)
        {
            manager.DefinePrototypeVariationBuffer<T>(prototype, bufferName);
        }

        /// <summary>
        ///     <para>Sets the variation value for this prefab instance. The variation buffer for the prototype must be defined 
        ///     with <see cref="DefinePrototypeVariationBuffer{T}"/> before using this.</para>
        /// </summary>
        /// <typeparam name="T">The type of variation buffer. Must be defined in the instance prototype's shader.</typeparam>
        /// <param name="prefabInstance">The prefab instance to add the variation to.</param>
        /// <param name="bufferName">The name of the variation buffer in the prototype's shader.</param>
        /// <param name="value">The value of the variation.</param>
        public static void AddVariation<T>(GPUInstancerPrefab prefabInstance, string bufferName, T value)
        {
            prefabInstance.AddVariation(bufferName, value);
        }

        /// <summary>
        ///     <para>Updates the variation value for this prefab instance. The variation buffer for the prototype must be defined 
        ///     with <see cref="DefinePrototypeVariationBuffer{T}"/> before using this.</para>
        /// </summary>
        /// <typeparam name="T">The type of variation buffer. Must be defined in the instance prototype's shader.</typeparam>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="prefabInstance">The prefab instance to update the variation at.</param>
        /// <param name="bufferName">The name of the variation buffer in the prototype's shader.</param>
        /// <param name="value">The value of the variation.</param>
        public static void UpdateVariation<T>(GPUInstancerPrefabManager manager, GPUInstancerPrefab prefabInstance, string bufferName, T value)
        {
            prefabInstance.AddVariation(bufferName, value);
            manager.UpdateVariationData(prefabInstance, bufferName, value);
        }

        /// <summary>
        /// Specifies a variation buffer for a GPU Instancer prototype that is defined in the prefab's shader. And sets the variation values for the given array.
        /// </summary>
        /// <typeparam name="T">The type of variation buffer. Must be defined in the instance prototype's shader</typeparam>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="prototype">The GPU Instancer prototype to define variations.</param>
        /// <param name="bufferName">The name of the variation buffer in the prototype's shader.</param>
        /// <param name="variationArray">The array that stores the variation information.</param>
        public static void DefineAndAddVariationFromArray<T>(GPUInstancerPrefabManager manager, GPUInstancerPrefabPrototype prototype, string bufferName, T[] variationArray)
        {
            manager.DefineAndAddVariationFromArray<T>(prototype, bufferName, variationArray);
        }

        /// <summary>
        /// Updates the variation values for the given array for the specified prototype and buffer.
        /// </summary>
        /// <typeparam name="T">The type of variation buffer. Must be defined in the instance prototype's shader</typeparam>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="prototype">The GPU Instancer prototype to define variations.</param>
        /// <param name="bufferName">The name of the variation buffer in the prototype's shader.</param>
        /// <param name="variationArray">The array that stores the variation information.</param>
        public static void UpdateVariationFromArray<T>(GPUInstancerPrefabManager manager, GPUInstancerPrefabPrototype prototype, string bufferName, T[] variationArray)
        {
            manager.UpdateVariationsFromArray<T>(prototype, bufferName, variationArray);
        }

        /// <summary>
        /// Use this method to create prefab instances with the given transform information without creating GameObjects.
        /// </summary>
        /// <param name="prefabManager">The GPUI Prefab Manager that the prefab prototype is defined on</param>
        /// <param name="prototype">GPUI Prefab Prototype</param>
        /// <param name="matrix4x4Array">Array of Matrix4x4 that store the transform data of prefab instances</param>
        public static void InitializeWithMatrix4x4Array(GPUInstancerPrefabManager prefabManager, GPUInstancerPrefabPrototype prototype, Matrix4x4[] matrix4x4Array)
        {
            GPUInstancerUtility.InitializeWithMatrix4x4Array(prefabManager, prototype, matrix4x4Array);
        }

        /// <summary>
        /// Use this method to update transform data of all prefab instances with a Matrix4x4 array
        /// </summary>
        /// <param name="prefabManager">The GPUI Prefab Manager that the prefab prototype is defined on</param>
        /// <param name="prototype">GPUI Prefab Prototype</param>
        /// <param name="matrix4x4Array">Array of Matrix4x4 that store the transform data of prefab instances</param>
        public static void UpdateVisibilityBufferWithMatrix4x4Array(GPUInstancerPrefabManager prefabManager, GPUInstancerPrefabPrototype prototype, Matrix4x4[] matrix4x4Array)
        {
            GPUInstancerUtility.UpdateVisibilityBufferWithMatrix4x4Array(prefabManager, prototype, matrix4x4Array);
        }

        /// <summary>
        /// Use this method to define Prefab Prototypes at runtime for procedurally generated GameObjects
        /// </summary>
        /// <param name="prefabManager">The GPUI Prefab Manager that the prefab prototype will be defined on</param>
        /// <param name="prototypeGameObject">GameObject to use as reference for the prototype</param>
        /// <returns></returns>
        public static GPUInstancerPrefabPrototype DefineGameObjectAsPrefabPrototypeAtRuntime(GPUInstancerPrefabManager prefabManager, GameObject prototypeGameObject)
        {
            return prefabManager.DefineGameObjectAsPrefabPrototypeAtRuntime(prototypeGameObject);
        }

        /// <summary>
        /// Use this method to add new instances to prototype when you do not use prefabs (Ex: when you create a prototype with DefineGameObjectAsPrefabPrototypeAtRuntime API method)
        /// </summary>
        /// <param name="prefabManager">The GPUI Prefab Manager that the prefab prototype is defined on</param>
        /// <param name="prefabPrototype">GPUI Prefab Prototype</param>
        /// <param name="instances">List of GameObjects to register on the manager</param>
        public static void AddInstancesToPrefabPrototypeAtRuntime(GPUInstancerPrefabManager prefabManager, GPUInstancerPrefabPrototype prefabPrototype, IEnumerable<GameObject> instances)
        {
            prefabManager.AddInstancesToPrefabPrototypeAtRuntime(prefabPrototype, instances);
        }

        /// <summary>
        /// Use this method to remove a prototype definition at runtime
        /// </summary>
        /// <param name="prefabManager">The GPUI Prefab Manager that the prefab prototype is defined on</param>
        /// <param name="prefabPrototype">GPUI Prefab Prototype ro remove from the manager</param>
        public static void RemovePrototypeAtRuntime(GPUInstancerPrefabManager prefabManager, GPUInstancerPrefabPrototype prefabPrototype)
        {
            prefabManager.RemovePrototypeAtRuntime(prefabPrototype);
        }
        #endregion Prefab Instancing

        #region Detail & Tree Instancing

        /// <summary>
        ///     <para>Sets the Unity terrain to the GPU Instancer manager and generates the instance prototypes from Unity detail 
        ///     prototypes that are defined on the given Unity terrain component.</para>
        ///     <para>Use this to initialize the GPU Instancer detail manager if you want to generate your terrain at runtime. 
        ///     See <seealso cref="TerrainGenerator"/> and its demo scene for an example.</para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="terrain"></param>
        public static void SetupManagerWithTerrain(GPUInstancerTerrainManager manager, Terrain terrain)
        {
            manager.SetupManagerWithTerrain(terrain);
        }

        #endregion Detail & Tree Instancing

        #region Detail Instancing

        /// <summary>
        ///     <para>Updates and synchronizes the GPU Instancer detail prototypes with the modifications made in the manager at runtime.</para>
        ///     <para>Use this if you want to make changes to the detail prototypes at runtime. Prototypes in the manager must be modified before using this.</para>
        ///     <para>For example usages, see: <see cref="DetailDemoSceneController"/> and <seealso cref="TerrainGenerator"/></para>
        /// </summary>
        /// <param name="manager">The manager that defines the prototypes you want to GPU instance.</param>
        /// <param name="updateMeshes">Whether GPU Instancer should also update meshes. Send this value as "true" if you change properties 
        /// related to cross quadding, noise spread and/or detail scales</param>
        public static void UpdateDetailInstances(GPUInstancerDetailManager manager, bool updateMeshes = false)
        {
            GPUInstancerUtility.UpdateDetailInstanceRuntimeDataList(manager.runtimeDataList, manager.terrainSettings, updateMeshes, manager.detailLayer);
        }

        /// <summary>
        /// Returns a list of 2D array of detail object density for the all the prototypes of the manager.
        /// </summary>
        /// <param name="manager"></param>
        /// <returns></returns>
        public static List<int[,]> GetDetailMapData(GPUInstancerDetailManager manager)
        {
            return manager.GetDetailMapData();
        }

        /// <summary>
        /// Returns a 2D array of detail object density for the given layer.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="layerIndex"></param>
        /// <returns></returns>
        public static int[,] GetDetailLayer(GPUInstancerDetailManager manager, int layerIndex)
        {
            return manager.GetDetailLayer(layerIndex);
        }

        /// <summary>
        /// Can be used to set the Detail Map Data to the Detail Manager before initialization.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="detailMapData"></param>
        public static void SetDetailMapData(GPUInstancerDetailManager manager, List<int[,]> detailMapData)
        {
            manager.SetDetailMapData(detailMapData);
        }
        #endregion
    }
}
