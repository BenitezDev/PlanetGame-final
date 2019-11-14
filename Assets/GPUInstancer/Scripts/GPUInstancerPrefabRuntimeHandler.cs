using System.Collections.Generic;
using UnityEngine;

namespace GPUInstancer
{
    public class GPUInstancerPrefabRuntimeHandler : MonoBehaviour
    {
        [HideInInspector]
        public GPUInstancerPrefab gpuiPrefab;

        private static Dictionary<GPUInstancerPrefabPrototype, GPUInstancerPrefabManager> _managerDictionary;

        private void Awake()
        {
            gpuiPrefab = GetComponent<GPUInstancerPrefab>();
            if (_managerDictionary == null)
            {
                _managerDictionary = new Dictionary<GPUInstancerPrefabPrototype, GPUInstancerPrefabManager>();

                GPUInstancerPrefabManager[] prefabManagers = FindObjectsOfType<GPUInstancerPrefabManager>();
                if (prefabManagers != null && prefabManagers.Length > 0)
                {
                    foreach (GPUInstancerPrefabManager pm in prefabManagers)
                    {
                        foreach (GPUInstancerPrefabPrototype prototype in pm.prototypeList)
                        {
                            if (!_managerDictionary.ContainsKey(prototype))
                                _managerDictionary.Add(prototype, pm);
                        }
                    }
                }
            }
        }

        private void Start()
        {
            if(gpuiPrefab.state == PrefabInstancingState.None)
            {
                GPUInstancerPrefabManager prefabManager = GetPrefabManager();
                if (prefabManager != null)
                    prefabManager.AddPrefabInstance(gpuiPrefab, true);
            }
        }

        private void OnDisable()
        {
            if (gpuiPrefab.state == PrefabInstancingState.Instanced)
            {
                GPUInstancerPrefabManager prefabManager = GetPrefabManager();
                if (prefabManager != null)
                    prefabManager.RemovePrefabInstance(gpuiPrefab);
            }                
        }

        private GPUInstancerPrefabManager GetPrefabManager()
        {
            GPUInstancerPrefabManager prefabManager = null;
            if(GPUInstancerManager.activeManagerList != null)
            {
                if (!_managerDictionary.ContainsKey(gpuiPrefab.prefabPrototype))
                {
                    prefabManager = (GPUInstancerPrefabManager)GPUInstancerManager.activeManagerList.Find(manager => manager.prototypeList.Contains(gpuiPrefab.prefabPrototype));
                    if (prefabManager == null)
                    {
                        Debug.LogWarning("Can not find GPUI Prefab Manager for prototype: " + gpuiPrefab.prefabPrototype);
                        return null;
                    }
                    _managerDictionary.Add(gpuiPrefab.prefabPrototype, prefabManager);
                }
                else
                {
                    prefabManager = _managerDictionary[gpuiPrefab.prefabPrototype];
                }
            }
            return prefabManager;
        }
    }
}
