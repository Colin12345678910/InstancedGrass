using System;
using System.Linq;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
/// <summary>
/// MeshInstancer
/// Jan 12th, 2025
/// The brains behind the revamped grass renderer that is going to be used by GreenThing.
/// </summary>
public class MeshInstancerV2 : MonoBehaviour
{
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material mat;
    [SerializeField] int numInstances = 1000;
    [SerializeField] float renderDistance = 150.0f;
    [SerializeField] Camera cam;
    [SerializeField] private Transform[] GrassBlockers;
    [SerializeField] private float density = 1.0f;
    Matrix4x4[] instData;
    Matrix4x4[] tempData;
    MaterialPropertyBlock properties;

    static bool isReading = false;
    int count = 0;

    bool isDirty = false;

    private Camera cacheCam;
    Vector3 cameraPos;
    Vector3 ourPos;
    float[] IDs;
    Vector4[] worldPos;
    float[] tempIDs;
    Plane[] cachePlanes;
    static MeshInstancerV2()
    {

    }
    private void Start()
    {
        cacheCam = cam;
        worldPos = new Vector4[numInstances * numInstances];
        IDs = new float[numInstances * numInstances];
        tempIDs = new float[numInstances * numInstances];
        instData = new Matrix4x4[numInstances * numInstances];
        tempData = new Matrix4x4[numInstances * numInstances];
        properties = new MaterialPropertyBlock();
        UpdateMatrices();

        Thread thread = new Thread(new ThreadStart(Tick));
        thread.Start();
    }
    private Vector2 RandVector(int x, int y) 
    {
        Vector2 vec = Vector2.zero;
        vec.x = ((x * x * y * 3125.2351f) % 1.0f - 0.5f) * 12.0f;
        vec.y = ((y * y * x * 1252.1351f) % 1.0f - 0.5f) * 12.0f;
        return vec;
    }
    private void Tick()
    {
        while (this != null)
        {
            UpdateMatrices();
        }
    }
    /// <summary>
    /// Updates the matrices for all grass planes, while also handling culling non-visible blades
    /// (Note it is somewhat slow and unoptimized.
    /// </summary>
    void UpdateMatrices()
    {
        Vector3 Scale1 = new Vector3(2.0f, 1.0f, 2.0f);
        Vector3 Scale2 = new Vector3(4.0f, 1.0f, 4.0f);
        Vector3 Scale3 = new Vector3(8.0f, 1.0f, 8.0f);

        Matrix4x4 matrix;
        int i = 0;

        for (int x = 0; x < numInstances; x++)
        {

            for (int z = 0; z < numInstances; z++)
            {
                Vector3 position = new Vector3(x * density, 0, z * density) + ourPos;
                Vector2 offset = RandVector(x, z);
                position.x += offset.x;
                position.z += offset.y;

                if ((x / 50 == 0 && z / 50 == 0) && (!TestPlanesPoint(cachePlanes, new Bounds(position, Vector3.one * 15))))
                {
                    // Fast Cull
                    // This causes a visual error when moving the instancer with the camera.
                    x += 50;
                    z += 50;
                }
                else
                {
                    float dist = Vector3.Distance(position, cameraPos);

                    if (dist < renderDistance * 0.5f)
                    {
                        worldPos[i] = new Vector4(position.x, position.y, position.z, 0);
                        matrix = Matrix4x4.Translate(position);
                        matrix *= Matrix4x4.Rotate(Quaternion.EulerRotation(0, ((position.x + 1.0f) * (position.z + 1.0f)), 0));
                        tempData[i] = matrix;
                        tempIDs[i] = (position.x * position.z);
                        i++;
                    }
                    else if (dist < renderDistance)
                    {
                        if (TestPlanesPoint(cachePlanes, new Bounds(position, Vector3.one * 15)))
                        {
                            matrix = Matrix4x4.Translate(position);
                            matrix *= Matrix4x4.Rotate(Quaternion.EulerRotation(0, ((position.x + 1.0f) * (position.z + 1.0f)), 0));
                            tempData[i] = matrix;
                            tempIDs[i] = (position.x * position.z);
                            i++;
                        }
                    }
                    else if (dist < renderDistance * 2 && (x % 2 == 0 && z % 2 == 0))
                    {
                        if (TestPlanesPoint(cachePlanes, new Bounds(position, Vector3.one * 15)))
                        {
                            matrix = Matrix4x4.Translate(position) * Matrix4x4.Scale(Scale1);
                            matrix *= Matrix4x4.Rotate(Quaternion.EulerRotation(0, ((position.x + 1.0f) * (position.z + 1.0f)), 0));
                            tempData[i] = matrix;
                            tempIDs[i] = (position.x * position.z);
                            i++;
                        }
                    }
                    else if (dist < renderDistance * 4 && (x % 4 == 0 && z % 4 == 0))
                    {
                        if (TestPlanesPoint(cachePlanes, new Bounds(position, Vector3.one * 15)))
                        {
                            matrix = Matrix4x4.Translate(position) * Matrix4x4.Scale(Scale2);
                            matrix *= Matrix4x4.Rotate(Quaternion.EulerRotation(0, ((position.x + 1.0f) * (position.z + 1.0f)), 0));
                            tempData[i] = matrix;
                            tempIDs[i] = (position.x * position.z);
                            i++;
                        }
                    }
                }
            }
        }
        while (isReading) { }
        float[] currIDs;
        currIDs = IDs;
        IDs = tempIDs;
        tempIDs = currIDs;
        // Buffer Swap.
        Matrix4x4[] currData;
        currData = instData;
        instData = tempData;
        tempData = currData;
        count = i;

        properties.SetFloatArray("_GrassID", IDs);

        //Thread.Sleep(10);
    }
    // Update is called once per frame
    void Update()
    {
        ourPos = gameObject.transform.position;
        renderDistance += Input.mouseScrollDelta.y * 10.0f;
        renderDistance = renderDistance < 0.0f ? 0.0f : renderDistance;
        isReading = true;
        cacheCam.fieldOfView += 90;
        cachePlanes = GeometryUtility.CalculateFrustumPlanes(cacheCam);
        cacheCam.fieldOfView -= 90;
        cameraPos = cacheCam.transform.position;
        for(int i = 0; i < GrassBlockers.Length; i++)
        {
            mat.SetVector("_Player" + i, GrassBlockers[i].transform.position);
        }
        
        if (instData != null && properties != null)
        {
            Graphics.DrawMeshInstanced(mesh, 0, mat, instData, count, properties, UnityEngine.Rendering.ShadowCastingMode.Off, false, 0, null);
        }
        isReading = false;
    }
    /// <summary>
    /// A Collision check that sees if a point is within a group of camera planes.
    /// </summary>
    /// <param name="planes">The planes of a camera</param>
    /// <param name="bounds">Bounds of a point (Note that only points are valid.)</param>
    /// <returns></returns>
    bool TestPlanesPoint(Plane[] planes, Bounds bounds)
    {
        if (cachePlanes == null) { return false; }

        foreach (Plane plane in cachePlanes)
        {
            if (plane.GetDistanceToPoint(bounds.center) < 0)
                return false;
        }
        return true;
    }
}
