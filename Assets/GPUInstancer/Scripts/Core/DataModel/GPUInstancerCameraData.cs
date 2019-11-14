using System;
using UnityEngine;

namespace GPUInstancer
{
    [Serializable]
    public class GPUInstancerCameraData
    {
        public Camera mainCamera;
        public GPUInstancerHiZOcclusionGenerator hiZOcclusionGenerator;
        public float[] mvpMatrixFloats;
        public Vector3 cameraPosition = Vector3.zero;
        public bool hasOcclusionGenerator = false;
        public float halfAngle;
        public bool renderOnlySelectedCamera = false;

        public GPUInstancerCameraData() : this(null) { }

        public GPUInstancerCameraData(Camera mainCamera)
        {
            this.mainCamera = mainCamera;
            mvpMatrixFloats = new float[16];
            CalculateHalfAngle();
        }

        public void SetCamera(Camera mainCamera)
        {
            this.mainCamera = mainCamera;
            CalculateHalfAngle();
        }

        public void CalculateCameraData()
        {
            Matrix4x4 mvpMatrix = mainCamera.projectionMatrix * mainCamera.worldToCameraMatrix;
            if (mvpMatrixFloats == null || mvpMatrixFloats.Length != 16)
                mvpMatrixFloats = new float[16];
            mvpMatrix.Matrix4x4ToFloatArray(mvpMatrixFloats);

            cameraPosition = mainCamera.transform.position;

            hasOcclusionGenerator = hiZOcclusionGenerator != null && hiZOcclusionGenerator.hiZDepthTexture != null;
        }

        public void CalculateHalfAngle()
        {
            if (mainCamera != null)
                halfAngle = Mathf.Tan(Mathf.Deg2Rad * mainCamera.fieldOfView * 0.25f);
        }

        public Camera GetRenderingCamera()
        {
            if (renderOnlySelectedCamera)
                return mainCamera;
            return null;
        }
    }
}