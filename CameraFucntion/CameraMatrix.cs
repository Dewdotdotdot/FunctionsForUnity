using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraMatrix : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Camera cam = Camera.main;
        Matrix4x4 proj = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);
        Matrix4x4 view = cam.worldToCameraMatrix;

        Shader.SetGlobalMatrix("_CameraProjection", proj);
        Shader.SetGlobalMatrix("_CameraInverseProjection", proj.inverse);
        Shader.SetGlobalMatrix("_WorldToCamera", view);
        Shader.SetGlobalMatrix("_CameraToWorld", cam.cameraToWorldMatrix);

        Matrix4x4 viewProj = proj * view;
        Shader.SetGlobalMatrix("_ViewProjMatrix", viewProj);
        Shader.SetGlobalMatrix("_InvViewProjMatrix", viewProj.inverse);
    }
}
