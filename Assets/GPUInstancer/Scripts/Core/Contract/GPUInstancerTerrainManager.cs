using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GPUInstancer
{

    public abstract class GPUInstancerTerrainManager : GPUInstancerManager
    {
        [SerializeField]
        private Terrain _terrain;
        public Terrain terrain { get { return _terrain; } }
        public GPUInstancerTerrainSettings terrainSettings;
        protected bool replacingInstances;
        protected bool initalizingInstances;

        public override void OnDestroy()
        {
            base.OnDestroy();
#if UNITY_EDITOR
            if (!Application.isPlaying && terrain != null && terrain.gameObject != null && terrain.GetComponent<GPUInstancerTerrainProxy>() != null && !terrain.GetComponent<GPUInstancerTerrainProxy>().beingDestroyed)
            {
                Undo.RecordObject(terrain.gameObject, "Remove GPUInstancerTerrainProxy");
                DestroyImmediate(terrain.GetComponent<GPUInstancerTerrainProxy>());
            }
#endif
        }

        public override void Reset()
        {
            base.Reset();

            if(terrain == null && gameObject.GetComponent<Terrain>() != null)
            {
                SetupManagerWithTerrain(gameObject.GetComponent<Terrain>());
            }
        }

        // Remove comment-out status to see partitioning bound gizmos:
        //public void OnDrawGizmos()
        //{
        //    if (spData != null && spData.activeCellList != null)
        //    {
        //        Color oldColor = Gizmos.color;
        //        Gizmos.color = Color.blue;
        //        foreach (GPUInstancerCell cell in spData.activeCellList)
        //        {
        //            if (cell != null)
        //                Gizmos.DrawWireCube(cell.cellInnerBounds.center, cell.cellInnerBounds.size);
        //        }
        //        Gizmos.color = oldColor;
        //    }
        //}

#if UNITY_EDITOR
        public override void CheckPrototypeChanges()
        {
            if (terrain != null && terrain.terrainData != null && terrainSettings != null)
            {
                if (terrain.terrainData.GetInstanceID() != terrainSettings.terrainDataInstanceID)
                    terrainSettings.terrainDataInstanceID = terrain.terrainData.GetInstanceID();
            }

            base.CheckPrototypeChanges();
        }
#endif

        public virtual void SetupManagerWithTerrain(Terrain terrain)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RecordObject(this, "Changed GPUInstancer Terrain Data for " + gameObject);
                if (_terrain != null && _terrain.GetComponent<GPUInstancerTerrainProxy>() != null)
                {
                    Undo.RecordObject(_terrain.gameObject, "Removed GPUInstancerTerrainProxy component");
                    DestroyImmediate(_terrain.GetComponent<GPUInstancerTerrainProxy>());
                }
            }
#endif

            _terrain = terrain;
            if (terrain != null)
            {
                if (terrainSettings != null)
                {
                    if (terrain.terrainData.GetInstanceID() == terrainSettings.terrainDataInstanceID)
                        return;
                    else
                    {
                        prototypeList.Clear();
                        //RemoveTerrainSettings(terrainSettings);
                        terrainSettings = null;
                    }
                }
                terrainSettings = GenerateTerrainSettings(terrain, gameObject);
                GeneratePrototypes(false);
                if (!Application.isPlaying)
                    AddProxyToTerrain();
            }
            else
            {
                prototypeList.Clear();
                //RemoveTerrainSettings(terrainSettings);
                terrainSettings = null;
            }
        }

        public GPUInstancerTerrainProxy AddProxyToTerrain()
        {
#if UNITY_EDITOR
            if (terrain != null && gameObject != terrain.gameObject)
            {
                GPUInstancerTerrainProxy terrainProxy = terrain.GetComponent<GPUInstancerTerrainProxy>();
                if (terrainProxy == null)
                {
                    Undo.RecordObject(terrain.gameObject, "Added GPUInstancerTerrainProxy component");
                    terrainProxy = terrain.gameObject.AddComponent<GPUInstancerTerrainProxy>();
                }
                if (this is GPUInstancerDetailManager && terrainProxy.detailManager != this)
                    terrainProxy.detailManager = (GPUInstancerDetailManager)this;
                else if (this is GPUInstancerTreeManager && terrainProxy.treeManager != this)
                    terrainProxy.treeManager = (GPUInstancerTreeManager)this;
                while (UnityEditorInternal.ComponentUtility.MoveComponentUp(terrainProxy)) ;

                return terrainProxy;
            }
#endif
            return null;
        }

        private GPUInstancerTerrainSettings GenerateTerrainSettings(Terrain terrain, GameObject gameObject)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                string[] guids = AssetDatabase.FindAssets("t:GPUInstancerTerrainSettings");
                for (int i = 0; i < guids.Length; i++)
                {
                    GPUInstancerTerrainSettings ts = AssetDatabase.LoadAssetAtPath<GPUInstancerTerrainSettings>(AssetDatabase.GUIDToAssetPath(guids[i]));
                    if (ts != null && ts.terrainDataInstanceID == terrain.terrainData.GetInstanceID())
                    {
                        prototypeList.Clear();
                        if (this is GPUInstancerDetailManager)
                        {
                            GPUInstancerUtility.SetPrototypeListFromAssets(ts, prototypeList, typeof(GPUInstancerDetailPrototype));
                        }
                        return ts;
                    }
                }
            }
#endif

            GPUInstancerTerrainSettings terrainSettings = ScriptableObject.CreateInstance<GPUInstancerTerrainSettings>();
            terrainSettings.name = (string.IsNullOrEmpty(terrain.terrainData.name) ? terrain.gameObject.name : terrain.terrainData.name) + "_" + terrain.terrainData.GetInstanceID();
            terrainSettings.terrainDataInstanceID = terrain.terrainData.GetInstanceID();
            terrainSettings.maxDetailDistance = terrain.detailObjectDistance;
            terrainSettings.maxTreeDistance = terrain.treeDistance;
            terrainSettings.detailDensity = terrain.detailObjectDensity;
            terrainSettings.healthyDryNoiseTexture = Resources.Load<Texture2D>(GPUInstancerConstants.NOISE_TEXTURES_PATH + GPUInstancerConstants.DEFAULT_HEALTHY_DRY_NOISE);
            terrainSettings.windWaveNormalTexture = Resources.Load<Texture2D>(GPUInstancerConstants.NOISE_TEXTURES_PATH + GPUInstancerConstants.DEFAULT_WIND_WAVE_NOISE);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                string assetPath = GPUInstancerConstants.GetDefaultPath() + GPUInstancerConstants.PROTOTYPES_TERRAIN_PATH + terrainSettings.name + ".asset";

                if (!System.IO.Directory.Exists(GPUInstancerConstants.GetDefaultPath() + GPUInstancerConstants.PROTOTYPES_TERRAIN_PATH))
                {
                    System.IO.Directory.CreateDirectory(GPUInstancerConstants.GetDefaultPath() + GPUInstancerConstants.PROTOTYPES_TERRAIN_PATH);
                }

                AssetDatabase.CreateAsset(terrainSettings, assetPath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
#endif
            return terrainSettings;
        }

        private static void RemoveTerrainSettings(GPUInstancerTerrainSettings terrainSettings)
        {
#if UNITY_EDITOR
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(terrainSettings));
#endif
        }

#if UNITY_EDITOR
        public void ShowTerrainPicker()
        {
            EditorGUIUtility.ShowObjectPicker<Terrain>(null, true, null, pickerControlID);
        }

        public void AddTerrainPickerObject(UnityEngine.Object pickerObject)
        {
            if (pickerObject == null)
                return;

            if (pickerObject is GameObject)
            {
                GameObject go = (GameObject)pickerObject;
                if(go.GetComponent<Terrain>() != null)
                {
                    SetupManagerWithTerrain(go.GetComponent<Terrain>());
                }
            }
        }
#endif
    }
}