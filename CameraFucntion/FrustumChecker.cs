using System.Collections;
using System.Linq;
using UnityEngine;

public class FrustumChecker : MonoBehaviour
{

    private static Camera Camera
    {
        get 
        {
            if(m_Camera == null)
            {

            }
            else
            {
                if(m_Camera != Camera.main)
                {

                }
            }
            return m_Camera;
        }
        set
        {
            if(value == Camera.main)
                m_Camera = value;
        }
    }
    private static Camera m_Camera;

    public enum FrustumState : byte
    {
        Inside,
        Intersect,
        Outside
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        var camera = Camera.main;
        Gizmos.matrix = camera.transform.localToWorldMatrix;
        Gizmos.DrawFrustum(Vector3.zero, camera.fieldOfView, camera.farClipPlane, camera.nearClipPlane, (float)Screen.width / Screen.height);
    }


    //Frustum 설명
    //Frustum 내부에 있는 경우, 모든 평면에 대해 distance >= 0
    //Frustum 외부에 있는 경우, 최소한 하나의 평면에서 distance< 0


    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Keypad1))
        {
            Matrix4x4 projectionMat = Matrix4x4.Perspective(Camera.main.fieldOfView, (float)Screen.width / Screen.height, Camera.main.nearClipPlane, Camera.main.farClipPlane);
            Debug.Log(projectionMat.ToString());

            var perspec = Perspective(Camera.main.fieldOfView, (float)Screen.width / Screen.height, Camera.main.nearClipPlane, Camera.main.farClipPlane);
            Debug.Log(perspec.ToString());
        }
        else if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            Camera camera = Camera.main;
            //Matrix4x4 projectionMat = Matrix4x4.Perspective(camera.fieldOfView, (float)Screen.width / Screen.height, camera.nearClipPlane, camera.farClipPlane);
            PerspectiveToFrustumParameters(camera.fieldOfView, (float)Screen.width / Screen.height, camera.nearClipPlane, out float left, out float right, out float top, out float bottom);
            Matrix4x4 projectionMat = Frustum(left, right, top, bottom, camera.nearClipPlane, camera.farClipPlane);

            var planes = CalculateFrustumPlanes(projectionMat, camera.transform, camera.farClipPlane, camera.nearClipPlane);

            for(int i = 0; i < planes.Length; i++)
            {
                Debug.Log(planes[i].ToString());
            }

            Debug.Log("------------------------------------------------------------");

            planes = GeometryUtility.CalculateFrustumPlanes(camera);
            for (int i = 0; i < planes.Length; i++)
            {
                Debug.Log(planes[i].ToString());
            }
        }
    }

    /// <summary>
    /// From Renderer or Collider
    /// </summary>
    /// <param name="bounds">From Renderer or Collider</param>
    /// <returns></returns>
    public static bool FrustumCheck(Transform target, Bounds  bounds)
    {
        //6개
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(FrustumChecker.Camera);
        
        //점(P)이 카메라 Frustum 내부에 있으려면, 모든 평면에 대해 GetDistanceToPoint(P) 값이 0 이상, Intersect상태를 알 수가 없음
        //target.position자체를 사용한다면 p_vertex, n_vertex에 대한 값을 알 수가 없음
        //bool isVisible = planes.All(plane => plane.GetDistanceToPoint(target.position) >= 0); 
        bool isVisible = !planes.Any(plane => plane.GetDistanceToPoint(target.position) < 0); //전부가 아닌 하나이상 기준으로

        if (GeometryUtility.TestPlanesAABB(planes, bounds) == true)
        {
            //카메라 렌더링 범위 내
            return true;
        }
        return false;
    }



    //Refered https://edom18.hateblo.jp/entry/2017/10/29/112908
    //https://www.researchgate.net/figure/The-negative-far-point-n-vertex-and-positive-far-point-p-vertex-of-a-bounding-box_fig3_2296524
    //https://www.lighthouse3d.com/tutorials/view-frustum-culling/geometric-approach-testing-boxes-ii/
    private static FrustumState CustomPlanesAABB(Camera camera, Bounds targetBounds)
    {
        //CalculateObliqueMatrix랑 CalculateFrustumCorners 있음
        Matrix4x4 projectionMat = Matrix4x4.Perspective(camera.fieldOfView, (float)Screen.width / Screen.height, camera.nearClipPlane, camera.farClipPlane);
        //GeometryUtility.CalculateFrustumPlanes에 해당됨
        var planes = CalculateFrustumPlanes(projectionMat, camera.transform, camera.farClipPlane, camera.nearClipPlane);

        FrustumState state = FrustumState.Inside;
        for (int iv = 0; iv < planes.Length; iv++)
        {
            Vector3 p_vertex = GetPositivePoint(targetBounds, planes[iv].normal);   //normal은 Frustum 안쪽으로 되어있음
            Vector3 n_vertex = GetNegativePoint(targetBounds, planes[iv].normal);

            // distance = (p_vertex - plane.pos)・normal
            // -dot(p, n) = dot(p, -n)이 성립 : 법선의 방향을 반대로 바꾸면 내적 결과가 반대로 나오지만, 최종적으로 같은 d를 얻을 수 있기 때문에 두 식은 동일
            // 0이상이면 카메라 Frustum 내부, 0미만이면 Frustum 외부
            float p_distance = planes[iv].GetDistanceToPoint(p_vertex);
            if(p_distance < 0)      //Plane.normal이 가르키지 않는 쪽(뒤쪽)에 위치
            {
                return state = FrustumState.Outside;    //하나라도 p_vertex가 normal의 반대방향에 존재하면 frustum에서 벗어난 것
            }

            float n_distance = planes[iv].GetDistanceToPoint(p_vertex);
            if (n_distance < 0)
            {
                // p_vertex는 안쪽에 있지만 n_vertex는 밖에 있음 => Frustum에 걸쳐있음
                state = FrustumState.Intersect;     //return이 아닌 이유 : 다른 bound는 outside일수가 있음
            }
        }
        return state;
    }

    private static Plane[] CalculateFrustumPlanes(Matrix4x4 projection ,Transform eye, float farClip, float nearClip)
    {
        // 0 : left      1 : right       2 : bottom          3 : top         4 : near        5 : far      
        Plane[] planes = new Plane[6];

        for(int q = 0; q < 4; q++)
        {
            int p = q / 2;  // p = 0~1 :  Left,Right : 0~1/2 => 0      Bottom,Top : 2~3/2 => 1

            Vector3 normal; //abc    Plane : ax + by + cz + d = 0
            if (q % 2 == 1)          //Left, Bottom : +
            {
                normal.x = projection[3, 0] + projection[p, 0];     // a = m41 + m11  → m.m30 + m.m[p]0 
                normal.y = projection[3, 1] + projection[p, 1];     // b = m42 + m12  → m.m31 + m.m[p]1  
                normal.z = projection[3, 2] + projection[p, 2];     // c = m43 + m13  → m.m32 + m.m[p]2  
                float d = projection[3, 3] + projection[p, 3];      // d = m44 + m14  → m.m33 + m.m[p]3  
            }
            else //q % 2 == 0       //Right, Top : -    
            {
                normal.x = projection[3, 0] - projection[p, 0];     // a = m41 - m11  → m.m30 - m.m[p]0 
                normal.y = projection[3, 1] - projection[p, 1];     // b = m42 - m12  → m.m31 - m.m[p]1  
                normal.z = projection[3, 2] - projection[p, 2];     // c = m43 - m13  → m.m32 - m.m[p]2  
                float d = projection[3, 3] - projection[p, 3];      // d = m44 - m14  → m.m33 - m.m[p]3  
            }
            normal = (-normal.normalized);      //GetDistanceToPoint를 계산할 때, normal에 -를 곱함 그래서 여기서 미리 처리해줌
            normal = eye.rotation * normal;


            //동일값
            //plane.distance = 0f - Vector3.Dot(m_Normal, inPoint);  m_Normal => normalized(vector/magnitude)된 상태
            //plane.distance =  projection[3, 3] - projection[p, 3] / magnitude;
            //magnitude = Vector3(a, b, c).normalized   또는  Vector3(a, b, c) / Mathf.Sqrt(a^2 + b^2 + c^2);
            planes[q] = new Plane(normal, eye.position);            //eye.position가 d를 대체함
        }


        //Near, Far => p = 2
        {   //Near Plane
            Vector3 nearNormal;
            nearNormal.x = projection[3, 0] + projection[2, 0];     // a = m41 + m11  → m.m30 + m.m20 
            nearNormal.y = projection[3, 1] + projection[2, 1];     // b = m42 + m12  → m.m31 + m.m21  
            nearNormal.z = projection[3, 2] + projection[2, 2];     // c = m43 + m13  → m.m32 + m.m22  
            float d = projection[3, 3] + projection[2, 3];          // d = m44 + m14  → m.m33 + m.m23  
            nearNormal = -(nearNormal.normalized);
            nearNormal = eye.rotation * nearNormal;

            planes[4] = new Plane(nearNormal, eye.position + (eye.forward * nearClip));
        }

        {   //Far Plane
            Vector3 farNormal;
            farNormal.x = projection[3, 0] - projection[2, 0];     // a = m41 - m11  → m.m30 - m.m20 
            farNormal.y = projection[3, 1] - projection[2, 1];     // b = m42 - m12  → m.m31 - m.m21  
            farNormal.z = projection[3, 2] - projection[2, 2];     // c = m43 - m13  → m.m32 - m.m22  
            float d = projection[3, 3] - projection[2, 3];         // d = m44 - m14  → m.m33 - m.m23  
            farNormal = -(farNormal.normalized);
            farNormal = eye.rotation * farNormal;

            planes[5] = new Plane(farNormal, eye.position + (eye.forward * (farClip)));
        }

        return planes;

    }

    //p-vertex와 n-vertex : plane의 normal(법선) 방향을 따라 가장 먼 정점(p-vertex)과 가장 가까운 정점(n-vertex)

    // Axis Aligned Boxes(AAB)   / positionve far point (p-vertex)
    private static Vector3 GetPositivePoint(Bounds bounds, Vector3 normal)
    {
        Vector3 result = bounds.min;

        if (normal.x > 0)
            result.x += bounds.size.x;
        if (normal.y > 0)
            result.y += bounds.size.y;
        if (normal.z > 0)
            result.z += bounds.size.z;

        return result;
    }

    // Axis Aligned Boxes(AAB)    / negative far point (n-vertex)
    private static Vector3 GetNegativePoint(Bounds bounds, Vector3 normal)
    {
        Vector3 result = bounds.min;

        if (normal.x < 0)
            result.x += bounds.size.x;
        if (normal.y < 0)
            result.y += bounds.size.y;
        if (normal.z < 0)
            result.z += bounds.size.z;

        return result;
    }


    #region Calculate Frustum
    public static void PerspectiveToFrustumParameters(float fov, float aspect, float near, out float left, out float right, out float bottom, out float top)
    {
        // fov가 degree 단위이므로 radian으로 변환하여 계산
        float fovRad = fov * Mathf.Deg2Rad;
        top = near * Mathf.Tan(fovRad * 0.5f);
        bottom = -top;
        right = top * aspect;
        left = -right;
    }
    public static void FrustumToPerspectiveParameters(float left, float right, float bottom, float top, float near, out float fov, out float aspect)
    {
        // 대칭 조건 가정: left = -right, bottom = -top
        fov = 2f * Mathf.Atan(top / near) * Mathf.Rad2Deg;
        aspect = right / top;
    }
    public static Matrix4x4 Perspective(float fov, float aspect, float near, float far)
    {
        PerspectiveToFrustumParameters(fov, aspect, near, out float left, out float right, out float bottom, out float top);
        return Frustum(left, right, bottom, top, near, far);
    }

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
}
