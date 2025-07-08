using System.Collections;
using UnityEngine;
using static UnityEditor.PlayerSettings;

/// <summary>
/// GPT 못알아쳐먹어서 만듬
/// Gemini도 못알아먹어서 추가 작성
/// 
/// LHS : Unity, Unreal, DirectX
/// RHS : OpenGL
/// forward = (target - cam).normalized 기준으로
/// Forward  : Unreal, OpenGL
/// -Forward : Unity, HLSL(DirectX)
/// </summary>


//Matrix4x4 t = GetProjectionMatrix() * GetWorldToCameraMatrix();
public static class VirtualProjectionCameraUtility
{
    #region Calculate Frustum
    /// <summary>
    /// Perspective Parameters to Frustum Parameters
    /// </summary>
    public static void PerspectiveToFrustumParameters(float fov, float aspect, float near, out float left, out float right, out float bottom, out float top)
    {
        // fov가 degree 단위이므로 radian으로 변환하여 계산
        float fovRad = fov * Mathf.Deg2Rad;
        top = near * Mathf.Tan(fovRad * 0.5f);
        bottom = -top;
        right = top * aspect;
        left = -right;
    }
    /// <summary>
    /// Frustum Parameters to Perspective Parameters
    /// </summary>
    public static void FrustumToPerspectiveParameters(float left, float right, float bottom, float top, float near, out float fov, out float aspect)
    {
        // 대칭 조건 가정: left = -right, bottom = -top
        fov = 2f * Mathf.Atan(top / near) * Mathf.Rad2Deg;
        aspect = right / top;
    }
    /// <summary>
    /// Get Projection from Perspective Parameters
    /// </summary>
    /// <returns>Projection Matrix</returns>
    public static Matrix4x4 Perspective(float fov, float aspect, float near, float far)
    {
        PerspectiveToFrustumParameters(fov, aspect, near, out float left, out float right, out float bottom, out float top);
        return Frustum(left, right, bottom, top, near, far);
    }
    /// <summary>
    /// Get Projection from Frustum Parameters
    /// </summary>
    /// <returns>Projection Matrix</returns>
    public static Matrix4x4 Frustum(float left, float right, float bottom, float top, float near, float far)
    {
        Matrix4x4 m = new Matrix4x4();

        // 첫 번째 행
        m[0, 0] = (2 * near) / (right - left);
        m[0, 1] = 0;
        m[0, 2] = (right + left) / (right - left);
        m[0, 3] = 0;

        // 두 번째 행
        m[1, 0] = 0;
        m[1, 1] = (2 * near) / (top - bottom);
        m[1, 2] = (top + bottom) / (top - bottom);
        m[1, 3] = 0;

        // 세 번째 행
        m[2, 0] = 0;
        m[2, 1] = 0;
        m[2, 2] = -(far + near) / (far - near);
        m[2, 3] = -(2 * far * near) / (far - near);

        // 네 번째 행
        m[3, 0] = 0;
        m[3, 1] = 0;
        m[3, 2] = -1;
        m[3, 3] = 0;

        return m;
    }
    #endregion


    /// <summary>
    /// Get Projection Matrix
    /// </summary>
    /// <returns>Projection Matrix</returns>
    public static Matrix4x4 GetProjectionMatrix(float fov, float aspect, float near, float far)
    {
        return Matrix4x4.Perspective(fov, aspect, near, far);
    }

    /// <summary>
    /// View => WorldToCamera : Camera Posistion, Camera Rotation
    /// </summary>
    /// <returns>View Matrix</returns>
    public static Matrix4x4 CalculateViewMatrixManual(Vector3 position, Quaternion rotation)
    {
        // 카메라 기준 방향 벡터
        Vector3 forward = rotation * Vector3.forward;
        Vector3 up = rotation * Vector3.up;
        Vector3 right = Vector3.Cross(up, forward).normalized;
        up = Vector3.Cross(forward, right).normalized;
        forward = forward.normalized;

        // View matrix는 Camera 좌표계 기준: 오른손 → 왼손 전환 포함 (Z축 뒤로 뒤집힘)
        Matrix4x4 rotationMatrix = new Matrix4x4();
        rotationMatrix.SetRow(0, new Vector4(right.x, up.x, -forward.x, 0));
        rotationMatrix.SetRow(1, new Vector4(right.y, up.y, -forward.y, 0));
        rotationMatrix.SetRow(2, new Vector4(right.z, up.z, -forward.z, 0));
        rotationMatrix.SetRow(3, new Vector4(0, 0, 0, 1));

        Matrix4x4 translationMatrix = Matrix4x4.Translate(-position);

        return rotationMatrix * translationMatrix;
    }

    public static Matrix4x4 CalculateViewMatrixManual(Vector3 cam, Vector3 target)
    {
        // 카메라 기준 방향 벡터
        Vector3 forward = (target - cam).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 up = Vector3.Cross(forward, right);

        // forward는 맨 위 참조
        Matrix4x4 rotationMatrix = new Matrix4x4();
        rotationMatrix.SetRow(0, new Vector4(right.x, up.x, -forward.x, 0));
        rotationMatrix.SetRow(1, new Vector4(right.y, up.y, -forward.y, 0));
        rotationMatrix.SetRow(2, new Vector4(right.z, up.z, -forward.z, 0));
        rotationMatrix.SetRow(3, new Vector4(0, 0, 0, 1));

        Matrix4x4 translationMatrix = Matrix4x4.Translate(-cam);

        return rotationMatrix * translationMatrix;
    }

    /// <summary>
    /// LookAt 기반 WorldToCameraMatrix 구성, virtualCamera -> target으로의 ViewMatrix
    /// View => WorldToCamera : Camera Position, Target Position
    /// </summary>
    /// <returns>View Matrix</returns>
    public static Matrix4x4 GetWorldToCameraMatrixLookAt(Vector3 virtualCamera, Vector3 target)
    {
        Vector3 pos = virtualCamera;
        Vector3 forward = (target - pos).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 up = Vector3.Cross(forward, right);

        Matrix4x4 rotation = new Matrix4x4();
        //구
        //rotation.SetColumn(0, new Vector4(right.x, up.x, -forward.x, 0));
        //rotation.SetColumn(1, new Vector4(right.y, up.y, -forward.y, 0));
        //rotation.SetColumn(2, new Vector4(right.z, up.z, -forward.z, 0));
        //rotation.SetColumn(3, new Vector4(0, 0, 0, 1));

        //신
        rotation.m00 = right.x;     rotation.m01 = right.y;         rotation.m02 = right.z;
        rotation.m10 = up.x;        rotation.m11 = up.y;            rotation.m12 = up.z;
        rotation.m20 = -forward.x;  rotation.m21 = -forward.y;      rotation.m22 = -forward.z;

        // 이동 부분 (-dot(axis, cameraPosition)을 마지막 열에 배치)
        //rotation.m03 = -Vector3.Dot(right, pos);
        //rotation.m13 = -Vector3.Dot(up, pos);
        //rotation.m23 = -Vector3.Dot(-forward, pos);

        rotation.m30 = 0;   rotation.m31 = 0;   rotation.m32 = 0;   rotation.m33 = 1;

        Matrix4x4 translation = Matrix4x4.Translate(-pos);
        //변환된 벡터 = 변환 행렬 * 원래 벡터, 행렬의 곱셈은 오른쪽에서 왼쪽으로
        return rotation * translation;          //먼저 이동 후, 회전을 적용해야 함
    }
    public static Matrix4x4 GetCameraToWorldMatrixLookAt(Vector3 virtualCamera, Vector3 target)
    {
        Vector3 pos = virtualCamera;
        Vector3 forward = (target - pos).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector3 up = Vector3.Cross(forward, right);

        Matrix4x4 rotation = new Matrix4x4();
        //구
        //rotation.SetColumn(0, new Vector4(right.x, right.y, right.z, 0));
        //rotation.SetColumn(1, new Vector4(up.x, up.y, up.z, 0));
        //rotation.SetColumn(2, new Vector4(-forward.x, -forward.y, -forward.z, 0));
        //rotation.SetColumn(3, new Vector4(pos.x, pos.y, pos.z, 1));

        // 회전 부분 (R^T)
        rotation.m00 = right.x; rotation.m01 = up.x;    rotation.m02 = -forward.x;
        rotation.m10 = right.y; rotation.m11 = up.y;    rotation.m12 = -forward.y;
        rotation.m20 = right.z; rotation.m21 = up.z;    rotation.m22 = -forward.z;

        // 이동 부분 (-R^T * t)
        //rotation.m03 = Vector3.Dot(right, pos);
        //rotation.m13 = Vector3.Dot(up, pos);
        //rotation.m23 = -Vector3.Dot(forward, pos); // -(-forward) = forward 이므로 부호 주의

        rotation.m30 = 0;   rotation.m31 = 0;   rotation.m32 = 0;   rotation.m33 = 1;

        Matrix4x4 translation = Matrix4x4.Translate(pos);

        //수행 순서가 반대임
        return translation * rotation;      // 반대로, 역회전이 먼저 일어나고 역이동을 해야 원래좌표로 복구됨 
    }


    public static Vector3 WorldToScreenPoint(Vector3 worldPos, Matrix4x4 worldToCamera, Matrix4x4 projectionMatrix, int screenWidth, int screenHeight)
    {
        /*
        Matrix4x4 vpMatrix = projectionMatrix * worldToCamera;    // projection * view
        Vector4 clip = vpMatrix * new Vector4(worldPos.x, worldPos.y, worldPos.z, 1);
        clip /= clip.w;

        float x = (clip.x * 0.5f + 0.5f) * screenWidth;
        float y = (clip.y * 0.5f + 0.5f) * screenHeight;
        float z = (clip.z * 0.5f + 0.5f); // 깊이값 (0~1)

        return new Vector3(x, y, z);
        */
        // 1. World → Clip space
        Vector4 clipSpace = (projectionMatrix * worldToCamera) * new Vector4(worldPos.x, worldPos.y, worldPos.z, 1);

        // 2. Clip → NDC (Normalized Device Coordinates)
        Vector3 ndc = new Vector3(clipSpace.x, clipSpace.y, clipSpace.z) / clipSpace.w;

        // 3. NDC → Screen space
        return new Vector3
        {
            x = (ndc.x * 0.5f + 0.5f) * screenWidth,
            y = (ndc.y * 0.5f + 0.5f) * screenHeight,
            z = ndc.z * 0.5f + 0.5f //Gemini가 *.5f +.5f를 계산에서 빼먹음 // depth 값은 -1 ~ 1 범위
        };

    }

    public static Vector3 ScreenToWorldPoint(Vector3 screenPos, Matrix4x4 cameraToWorld, Matrix4x4 projectionMatrix, int screenWidth, int screenHeight)
    {
        //(AB)^-1 = B^-1 * A^-1
        Matrix4x4 vpMatrix = projectionMatrix * cameraToWorld.inverse; // projection * view.inverse
        Matrix4x4 invVP = vpMatrix.inverse;

        // 1. Screen → NDC (Normalized Device Coodinate : 정규좌표)
        // Screen Position을 -1~1의 NDC 좌표계로 변환
        Vector3 ndc = new Vector3(
            (screenPos.x / screenWidth) * 2f - 1f,  // 0~screenWidth → -1~1 NDC x
            (screenPos.y / screenHeight) * 2f - 1f, // 0~screenHeight → -1~1 NDC x
            screenPos.z * 2f - 1f                   // 0~1 depth → -1~1 NDC z
        );

        Vector4 clip = new Vector4(ndc.x, ndc.y, ndc.z, 1f);

        Vector4 world = invVP * clip;

        if (Mathf.Abs(world.w) > 1e-5f)
            world /= world.w;

        return new Vector3(world.x, world.y, world.z);

        /*
        Vector3 ndc = new Vector3
        {
            x = (screenPos.x / screenWidth) * 2f - 1f,          
            y = (screenPos.y / screenHeight) * 2f - 1f,         
            z = screenPos.z * 2f - 1f                          
        };

        //
        Vector4 clip = new Vector4(ndc.x, ndc.y, ndc.z, 1f);

        // 2. Inverse Projection → View space
        Matrix4x4 invProj = projectionMatrix.inverse;
        Vector4 viewSpace = invProj * clip;
        viewSpace /= viewSpace.w;

        // 3. Inverse View → World space
        Matrix4x4 invView = cameraToWorld;     //Gemini가 viewMatrix를 반대로 이해함
        Vector4 worldPos = invView * viewSpace;

        return new Vector3(worldPos.x, worldPos.y, worldPos.z);
        */
    }


    public static bool Invert_Full(Matrix4x4 input, out Matrix4x4 result)
    {
        float[] m = new float[16];
        float[] inv = new float[16];

        // Copy input to float array
        m[0] = input.m00; m[1] = input.m01; m[2] = input.m02; m[3] = input.m03;
        m[4] = input.m10; m[5] = input.m11; m[6] = input.m12; m[7] = input.m13;
        m[8] = input.m20; m[9] = input.m21; m[10] = input.m22; m[11] = input.m23;
        m[12] = input.m30; m[13] = input.m31; m[14] = input.m32; m[15] = input.m33;

        inv[0] = m[5] * m[10] * m[15] -
                 m[5] * m[11] * m[14] -
                 m[9] * m[6] * m[15] +
                 m[9] * m[7] * m[14] +
                 m[13] * m[6] * m[11] -
                 m[13] * m[7] * m[10];

        inv[4] = -m[4] * m[10] * m[15] +
                  m[4] * m[11] * m[14] +
                  m[8] * m[6] * m[15] -
                  m[8] * m[7] * m[14] -
                  m[12] * m[6] * m[11] +
                  m[12] * m[7] * m[10];

        inv[8] = m[4] * m[9] * m[15] -
                 m[4] * m[11] * m[13] -
                 m[8] * m[5] * m[15] +
                 m[8] * m[7] * m[13] +
                 m[12] * m[5] * m[11] -
                 m[12] * m[7] * m[9];

        inv[12] = -m[4] * m[9] * m[14] +
                   m[4] * m[10] * m[13] +
                   m[8] * m[5] * m[14] -
                   m[8] * m[6] * m[13] -
                   m[12] * m[5] * m[10] +
                   m[12] * m[6] * m[9];

        inv[1] = -m[1] * m[10] * m[15] +
                  m[1] * m[11] * m[14] +
                  m[9] * m[2] * m[15] -
                  m[9] * m[3] * m[14] -
                  m[13] * m[2] * m[11] +
                  m[13] * m[3] * m[10];

        inv[5] = m[0] * m[10] * m[15] -
                 m[0] * m[11] * m[14] -
                 m[8] * m[2] * m[15] +
                 m[8] * m[3] * m[14] +
                 m[12] * m[2] * m[11] -
                 m[12] * m[3] * m[10];

        inv[9] = -m[0] * m[9] * m[15] +
                  m[0] * m[11] * m[13] +
                  m[8] * m[1] * m[15] -
                  m[8] * m[3] * m[13] -
                  m[12] * m[1] * m[11] +
                  m[12] * m[3] * m[9];

        inv[13] = m[0] * m[9] * m[14] -
                  m[0] * m[10] * m[13] -
                  m[8] * m[1] * m[14] +
                  m[8] * m[2] * m[13] +
                  m[12] * m[1] * m[10] -
                  m[12] * m[2] * m[9];

        inv[2] = m[1] * m[6] * m[15] -
                 m[1] * m[7] * m[14] -
                 m[5] * m[2] * m[15] +
                 m[5] * m[3] * m[14] +
                 m[13] * m[2] * m[7] -
                 m[13] * m[3] * m[6];

        inv[6] = -m[0] * m[6] * m[15] +
                  m[0] * m[7] * m[14] +
                  m[4] * m[2] * m[15] -
                  m[4] * m[3] * m[14] -
                  m[12] * m[2] * m[7] +
                  m[12] * m[3] * m[6];

        inv[10] = m[0] * m[5] * m[15] -
                  m[0] * m[7] * m[13] -
                  m[4] * m[1] * m[15] +
                  m[4] * m[3] * m[13] +
                  m[12] * m[1] * m[7] -
                  m[12] * m[3] * m[5];

        inv[14] = -m[0] * m[5] * m[14] +
                   m[0] * m[6] * m[13] +
                   m[4] * m[1] * m[14] -
                   m[4] * m[2] * m[13] -
                   m[12] * m[1] * m[6] +
                   m[12] * m[2] * m[5];

        inv[3] = -m[1] * m[6] * m[11] +
                  m[1] * m[7] * m[10] +
                  m[5] * m[2] * m[11] -
                  m[5] * m[3] * m[10] -
                  m[9] * m[2] * m[7] +
                  m[9] * m[3] * m[6];

        inv[7] = m[0] * m[6] * m[11] -
                 m[0] * m[7] * m[10] -
                 m[4] * m[2] * m[11] +
                 m[4] * m[3] * m[10] +
                 m[8] * m[2] * m[7] -
                 m[8] * m[3] * m[6];

        inv[11] = -m[0] * m[5] * m[11] +
                   m[0] * m[7] * m[9] +
                   m[4] * m[1] * m[11] -
                   m[4] * m[3] * m[9] -
                   m[8] * m[1] * m[7] +
                   m[8] * m[3] * m[5];

        inv[15] = m[0] * m[5] * m[10] -
                  m[0] * m[6] * m[9] -
                  m[4] * m[1] * m[10] +
                  m[4] * m[2] * m[9] +
                  m[8] * m[1] * m[6] -
                  m[8] * m[2] * m[5];

        float det = m[0] * inv[0] + m[1] * inv[4] + m[2] * inv[8] + m[3] * inv[12];

        if (Mathf.Abs(det) < Mathf.Epsilon)
        {
            result = Matrix4x4.identity;
            return false;
        }

        det = 1.0f / det;

        for (int i = 0; i < 16; i++)
            inv[i] *= det;

        result = new Matrix4x4(
            new Vector4(inv[0], inv[1], inv[2], inv[3]),
            new Vector4(inv[4], inv[5], inv[6], inv[7]),
            new Vector4(inv[8], inv[9], inv[10], inv[11]),
            new Vector4(inv[12], inv[13], inv[14], inv[15])
        );

        return true;
    }

}
