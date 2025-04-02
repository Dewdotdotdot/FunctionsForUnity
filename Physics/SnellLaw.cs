using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public static class EnumConverter
{
    struct Shell<T> where T : struct
    {
        public int IntValue;
        public T Enum;
    }

    //https://www.slideshare.net/slideshow/enum-boxing-enum-ndc2019/142689361
    public static int EnumToInt32<T>(T e) where T : struct
    {
        Shell<T> s;
        s.Enum = e;
        unsafe
        {
            int* p = &s.IntValue;
            p += 1;
            return *p;
        }
    }

    public static T IntToEnum32<T>(int value) where T : struct
    {
        Shell<T> s = new Shell<T>();
        unsafe
        {
            int* p = &s.IntValue;
            p += 1;
            *p = value;
        }
        return s.Enum;
    }
}

public static class IOR
{
    //굴절률  Index Of Refractive
    public const float vaccuum_IOR = 1.0003f;
    public const float air_IOR = 1.0003f;
    public const float ice_IOR = 1.31f;
    public const float water_IOR = 1.33f;
    public const float formal_Oil_IOR = 1.58f;
    public const float diamond_IOR = 2.417f;
    public const float benzen_IOR = 1.5f;
    public const float germanium_IOR = 4.05f; //~4.1
    public const float silicon_IOR = 3.42f; //~3.48
    public const float cubic_zirconia_IOR = 2.15f; //~2.18
    public const float bromine_IOR = 1.661f;

    public enum IOR_Type
    {
        Vaccuum,
        Air,
        Water,
        Ice,
        Fomal_Oil,
        Benzen,
        Bromine,
        Cubic_Zirconia,
        Diamond,
        Silicon,
        Germanium
    }
    public static float GetIORValue(IOR_Type ior)
    {
        return ior switch
        {
            IOR_Type.Vaccuum => vaccuum_IOR,

            _ => vaccuum_IOR
        };
    }

    public static Vector3 RandomDirection()
    {
        return Vector3.zero;
    }

    public static Vector3 GetDirection(Transform trans1, Transform trans2)
    {
        return GetDirection(trans1.position, trans2.position);
    }

    public static Vector3 GetDirection(Vector3 source, Vector3 destination)
    {
        return (destination - source).normalized;
    }

}

/// <summary>
/// 굴절률 계산
/// </summary>
public class SnellLaw : MonoBehaviour
{
    [SerializeField] private MeshCollider col;


    public IOR.IOR_Type outMaterial;
    public IOR.IOR_Type inMaterial;


    public Transform target;

    public Transform target2;

    private void Awake()
    {
        Physics.queriesHitBackfaces = true;
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            //아래 방향임으로 y는 음수의 값을 가짐
            Vector3 random = new Vector3(Random.Range(-1.00000f, 1.00000f), Random.Range(-1.00000f, 0.00000f), Random.Range(-1.00000f, 1.00000f));
            Debug.Log(random);
            RefractionCalculate(target, random, IOR.GetIORValue(outMaterial), IOR.GetIORValue(inMaterial), out rayLength);
        }
        RefractionCalculate(target, (target2.position - target.position).normalized, IOR.GetIORValue(outMaterial), IOR.GetIORValue(inMaterial), out rayLength);
    }

    [SerializeField] private float[] rayLength;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(.2f,.1f,.9f, .5f);

        Vector3 temp = transform.position;
        temp.y += transform.localScale.z / 2 / 10;
        Vector3 scale = transform.localScale;
        scale.z = 1;
        Gizmos.DrawMesh(col.sharedMesh, 0, temp, transform.rotation, scale);
        temp.y -= transform.localScale.z / 10;
        Gizmos.DrawMesh(col.sharedMesh, 0, temp, transform.rotation, scale);

        Gizmos.DrawLine(ray.origin, collsionPos);
        var p1 = ray.origin;
        var p2 = collsionPos;
        var thickness =20;
        Handles.DrawBezier(p1, p2, p1, p2, Color.red, null, thickness);

        Gizmos.DrawSphere(collsionPos, .25f);
        Gizmos.DrawLine(collsionPos, refractedPos);
         p1 = collsionPos;
         p2 = refractedPos;
        Handles.DrawBezier(p1, p2, p1, p2, Color.blue, null, thickness);

        if(refractedPos2 != Vector3.zero)
        {
            Gizmos.DrawSphere(refractedPos, .25f);
            Gizmos.DrawLine(refractedPos, refractedPos2);
            p1 = refractedPos;
            p2 = refractedPos2;
            Handles.DrawBezier(p1, p2, p1, p2, Color.blue, null, thickness);
        }
    }

    public Ray ray;
    public float length;
    public Vector3 collsionPos;
    public Vector3 refractedPos;
    public Vector3 refractedPos2;
    public Vector3 reflectedPos;

    public static Vector3 RefractionDir(Ray ray, Vector3 hitPoint, Vector3 surfaceNormal, float IOR1, float IOR2, out bool isReflected)
    {
        // Calculate the critical angle if IOR1 < IOR2
        float criticalRange = (IOR1 < IOR2) ? Mathf.Asin(IOR1 / IOR2) * Mathf.Rad2Deg : 9999f;
        Debug.Log("임계각 :  " + criticalRange);

        Vector3 shiftOrigin = ray.origin - hitPoint;      //surface를 0,0,0으로 Shift
        Vector3 perpendicular = new Vector3(shiftOrigin.x, 0, shiftOrigin.z); // Flattened to XZ plane

        /*
        //입사각 구하기
        //Dot 내적 연산 => 스칼라곱
        //AㆍB = ||A||*||B||cosθ
        // 
        // ||v1|| => v1(a1, a2, a3)  Magnitude(백터의 크기)
        // sqrt(a1^2 + a2^2 + a3^3)
        // 따라서
        //cosθ = AㆍB / (||A||*||B||)
        // θ = Arccos(AㆍB / (||A||*||B||))
        // θ(라디안) => Degree = Arccos(AㆍB / (||A||*||B||)) * (180/π)
        */
        /*float yTheta = Mathf.Acos(Vector3.Dot(shiftOrigin, perpendicular) /
                (Vector3.Magnitude(shiftOrigin) * Vector3.Magnitude(perpendicular))) * Mathf.Rad2Deg;
                Vector3 cross = Vector3.Cross(shiftOrigin, perpendicular);
        if (cross.y < 0)
        {
            yTheta = -yTheta;
        }
         float Angle0fIncidence = 90 - yTheta;
        */
        float yTheta = Vector3.SignedAngle(shiftOrigin, perpendicular, Vector3.up);
        float Angle0fIncidence = Mathf.Abs(90 - yTheta);
        Debug.Log("yTheta : " + yTheta + "     입사각 : " + Angle0fIncidence + "     " + (yTheta + Angle0fIncidence));

        if (Angle0fIncidence > criticalRange) //전반사
        {
            //float refracted = 90f;
            isReflected = true;
            return Reflection(surfaceNormal.normalized, ray.direction.normalized);
        }
        else
        {
            //Sinθ2 =( Sinθ * n1) / n2        ////θ2 = ArcSin(Sinθ2)
            float refracted = SnellLaw_Cal(Angle0fIncidence, IOR1, IOR2);
            //T = n1 / n2 * InputVector + (n1 / n2 cosθ1 - cosθ2) normalVector
            Vector3 refractedDir = Refract___(IOR1, IOR2, refracted, surfaceNormal, ray.direction).normalized;
            //refractedPos = Refract(IOR1, IOR2, hit.normal, dir);

            isReflected = false;
            return refractedDir;
        }
    }

    public float LightAttenuation(float density, float lightStrenth, float penetratedDistance)
    {
        float attenuationCoefficient = density * 0.1f;
        return lightStrenth * Mathf.Exp(-attenuationCoefficient * penetratedDistance);
    }


    /// <summary>
    /// 입사각을 구해야함 Ray를 기준으로
    /// </summary>
    public void RefractionCalculate(Transform obj, Vector3 dir, float IOR1, float IOR2, out float[] calLength, float rayLenth = 50) //법선 백터
    {
        ray = new Ray(obj.position, dir); // 출발 지점과 방향 벡터

        calLength = new float[3] { 0,0,0 };
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayLenth))// 충돌 수행
        {

            collsionPos = hit.point;
            length = hit.distance;
            rayLenth -= length;
            bool isReflected = false;
            Vector3 refractionDir = RefractionDir(ray, hit.point, hit.normal.normalized, IOR1, IOR2, out isReflected);

            refractedPos = collsionPos + refractionDir * length;

            calLength[0] = length;

            //2중 굴절
            RaycastHit hit2;
            Ray ray2 = new Ray(collsionPos + (refractionDir) * 50, -refractionDir);
            if (isReflected != true && Physics.Raycast(ray2, out hit2, 50))// 충돌 수행
            {
                refractedPos = hit2.point;
                length = Vector3.Distance(collsionPos, refractedPos);
                calLength[1] = length;

                ray2.origin = collsionPos;
                ray2.direction  = refractionDir;  
                Vector3 refractionDir2 = RefractionDir(ray2, refractedPos, -hit2.normal.normalized, IOR2, IOR1, out isReflected);

                calLength[2] = 50 - calLength[0] - calLength[1];
                refractedPos2 = refractedPos + refractionDir2 * calLength[2];
            }
            else
            {
                calLength[1] = 50 - calLength[0];
                refractedPos = collsionPos + refractionDir * calLength[1];
                refractedPos2 = Vector3.zero;
            }
        }
    }


    //https://physics.stackexchange.com/questions/435512/snells-law-in-vector-form
    public static Vector3 Refract___(float IOR1, float IOR2, float refracted, Vector3 surfaceNormal, Vector3 incidence) //정확함
    {
        float n = IOR1 / IOR2;
        float cosIncidence = -(Vector3.Dot(surfaceNormal, incidence));
        float sinT2 = n * n * (1f - cosIncidence * cosIncidence);
        if (sinT2 > 1)
            return Vector3.zero;
        float cosTarget = Mathf.Sqrt(1  - sinT2);
        return n * incidence + (n * cosIncidence - cosTarget) * surfaceNormal;
    }


    public static Vector3 Refract(float RI1, float RI2, Vector3 surfNorm, Vector3 incident)     //None Fail
    {


        surfNorm.Normalize(); //should already be normalized, but normalize just to be sure
        incident.Normalize();
        float n = RI1 / RI2;
        float sinT2 = Vector3.Dot(Vector3.Cross(surfNorm, incident) * (n * n), Vector3.Cross(surfNorm, incident));
        return (n * Vector3.Cross(surfNorm, Vector3.Cross(-surfNorm, incident)) - surfNorm * Mathf.Sqrt(1 - sinT2)).normalized;
    }

    public static Vector3 Reflection(Vector3 surfNorm, Vector3 incidents)
    {
        return Vector3.Reflect(incidents, surfNorm);
    }

    public static float SnellLaw_Cal(float theta1, float IOR1, float IOR2)
    {
        return Mathf.Asin((Mathf.Sin(theta1 * Mathf.Deg2Rad) * IOR1) / IOR2) * Mathf.Rad2Deg;
    }
}
