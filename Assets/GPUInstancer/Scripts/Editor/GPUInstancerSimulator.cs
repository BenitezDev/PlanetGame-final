// Overwritten by GPUInstancerEditorSimulator

//using UnityEditor;
//using UnityEngine;

//namespace GPUInstancer
//{
//    /// <summary>
//    /// Simulate GPU Instancing while game is not running
//    /// </summary>
//    public class GPUInstancerSimulator
//    {
//        public GPUInstancerDetailManager detailManager;
//        public Editor editor;
//        public bool simulateAtEditor;
//        public bool initializingInstances;

//        private Camera previousCamera;

//        public GPUInstancerSimulator(GPUInstancerDetailManager detailManager, Editor editor)
//        {
//            this.detailManager = detailManager;
//            this.editor = editor;
//        }

//        public void StartSimulation()
//        {
//            if (Application.isPlaying || detailManager == null)
//                return;

//            previousCamera = detailManager.cameraData.mainCamera;
//            initializingInstances = true;

//            simulateAtEditor = true;
//            EditorApplication.update += EditorUpdate;
//#if UNITY_2017_2_OR_NEWER
//            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
//            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
//#else
//            EditorApplication.playmodeStateChanged = HandlePlayModeStateChanged;
//#endif
//        }

//        public void StopSimulation()
//        {
//            if (!Application.isPlaying)
//                detailManager.ClearInstancingData();
//            detailManager.cameraData.SetCamera(previousCamera);

//            simulateAtEditor = false;
//            Camera.onPreCull -= CameraOnPreCull;
//            EditorApplication.update -= EditorUpdate;
//#if UNITY_2017_2_OR_NEWER
//            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
//#else
//            EditorApplication.playmodeStateChanged = null;
//#endif
//        }

//        private void EditorUpdate()
//        {
//            if (Application.isPlaying)
//            {
//                EditorApplication.update -= EditorUpdate;
//                return;
//            }

//            if (detailManager.cameraData.mainCamera == null || detailManager.cameraData.mainCamera.name != "SceneCamera")
//            {
//                Camera currentCam = Camera.current;
//                if (currentCam != null && currentCam.name == "SceneCamera")
//                    detailManager.cameraData.SetCamera(currentCam);
//                else
//                    return;
//            }
            
//            if (initializingInstances)
//            {
//                detailManager.Awake();
//                detailManager.InitializeRuntimeDataAndBuffers();
//                initializingInstances = false;
//                return;
//            }
                        
//            Camera.onPreCull -= CameraOnPreCull;
//            Camera.onPreCull += CameraOnPreCull;
//            EditorApplication.update -= EditorUpdate;
//        }

//        private void CameraOnPreCull(Camera cam)
//        {
//            if(detailManager.cameraData.mainCamera == cam)
//            {
//                detailManager.UpdateBuffers();
//            }
//        }


//#if UNITY_2017_2_OR_NEWER        
//        public void HandlePlayModeStateChanged(PlayModeStateChange state)
//        {
//            StopSimulation();
//        }
//#else
//        public void HandlePlayModeStateChanged()
//        {
//            StopSimulation();
//        }
//#endif
//    }
//}