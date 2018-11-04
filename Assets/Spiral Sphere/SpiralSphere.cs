using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SpiralSphere : MonoBehaviour
{
    public int n = 5000;
    public float radius = 1;
    public bool fixUvSeams = true;
    public bool createPrefab = true;
    static float goldenAngle = Mathf.PI * (3 - Mathf.Sqrt(5));
    private Mesh mesh;
    private List<Vector3> vertices;
    private List<Vector3> normals;
    private List<Vector2> uv;
    private List<int> triangles;
	public GameObject surfaceObj;
    private float[] ys; // keeping track of the y coördinates of vertices
    private Dictionary<long, bool> edgeDict = new Dictionary<long, bool>(); 
    // This dictionary keeps track of the triangle edges. Edges have to be unique (except for the reverse edge).
    float distanceFactor = 1.5f; // This factor determines maximum distance of vertex neighbors.
                                 // the value of 1.5 is based on trial and error. With this value, triangulation works for all n > 3.
    private void Awake()
    {
        Generate();
    }

    private void Generate()
    {
        if (n < 4)
            n = 4;
        if (n > 60000)
            n = 60000;
        vertices = new List<Vector3>();
        normals = new List<Vector3>();
        uv = new List<Vector2>();
        ys = new float[n];
        triangles = new List<int>();
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Spiral Sphere";

        float off = 2 / (float)n;
        for (int i = 0; i < n; i++)
        {
            // This is the golden spiral algorithm. The points on the sphere are calculated in order of increasing y
            float y = i * off - 1 + (off / 2);
            ys[i] = y;
            float r = Mathf.Sqrt(1 - y * y);
            float phi = (i * goldenAngle) % (2 * Mathf.PI);
            Vector3 vertex = new Vector3(Mathf.Cos(phi) * r, y, Mathf.Sin(phi) * r);
            
            vertices.Add(vertex * radius);
            normals.Add(vertex.normalized);

            uv.Add(new Vector2(0.5f * phi / Mathf.PI, (Mathf.PI - Mathf.Acos(y)) / Mathf.PI)); // Using polar coordinates converted to uv
        }

        float maxDist = Mathf.Sqrt(4 * Mathf.PI / n) * distanceFactor * radius; // We add as few neighbors as possible for performance reasons
        float maxDistSq = maxDist * maxDist; 

        for (int i = 0; i < n; i++)
        {
            Vector3 vi = vertices[i];
            List<int> neighbors = new List<int>();
            for (int j = i + 1; j < n; j++)
            {
                float dy = ys[j] - ys[i]; // We calculate how far above the current vertex the other vertex is located.
                if (dy * radius > maxDist) // If other vertices are too far above the current vertex, we stop looping.
                    continue;
                else if ((vi - vertices[j]).sqrMagnitude < maxDistSq) // if Ihe neighbor is within range, we add it to the list.
                    neighbors.Add(j);
            }

            for (int j = 0; j < neighbors.Count; j++) // Now we traverse the list to find potential triangle candidates.
            {
                int nj = neighbors[j]; // We pick a first neighbor on the list
                Vector3 vj = vertices[nj];
                for (int k = j + 1; k < neighbors.Count; k++) // We pick a second neighbor on the list.
                {
                    int nk = neighbors[k];
                    Vector3 vk = vertices[nk];

                    Vector3 center = CircleCenter(vi, vj, vk); // We calculate the center of the circle through the 3 vertices.
                    float radiusSq = (center - vi).sqrMagnitude; // We calculate the radius of the circle.

                    bool validTriangle = true; 
                    for (int l = 0; l < neighbors.Count; l++) // We check if any other vertex is within a circle-radius of the center.
                    {
                        if (l != j && l != k) // Only other vertices are considered (usually no more than 1 or 2)
                        {
                            if ((vertices[neighbors[l]] - center).sqrMagnitude < radiusSq)  
                            {
                                validTriangle = false; // The triangle is rejected, another vertex is too close.
                                break;                // This rejection principle comes from Delaunay triangulation
                            }
                        }
                    }

                    if (validTriangle)
                    {
                        Vector3 normal = Vector3.Cross(vj - vi, vk - vi);
                        if (Vector3.Dot(normal, center) > 0) // Ensure the correct winding order of the triangle.
                            CheckEdgesAddTriangle(i, nj, nk); // Check if the edges are unique and if so, add the triangle.
                        else
                            CheckEdgesAddTriangle(i, nk, nj);
                    }
                }
            }
        }


        if (fixUvSeams)
        {
            RemoveZipper();
            ReplacePoles();
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uv.ToArray();


		//foreach (Vector3 vertex in vertices) {
		//	Instantiate (surfaceObj, vertex, Quaternion.LookRotation (vertex, transform.up));
		//}

        if (createPrefab)
        {
            // Create some asset folders.


            if (!AssetDatabase.IsValidFolder("Assets/MyMeshes"))
                AssetDatabase.CreateFolder("Assets", "MyMeshes");
            if (!AssetDatabase.IsValidFolder("Assets/MyPrefabs"))
                AssetDatabase.CreateFolder("Assets", "MyPrefabs");
            // The paths to the mesh/prefab assets.

            string meshPath = "Assets/MyMeshes/SpiralSphere.mesh";
            string prefabPath = "Assets/MyPrefabs/SpiralSphere.prefab";

            // Save the mesh as an asset.
            AssetDatabase.CreateAsset(mesh, meshPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Save a sphere without material as a prefab asset.
            GameObject go = new GameObject();
            go.transform.position = transform.position;
            go.transform.rotation = transform.rotation;
            go.transform.localScale = transform.localScale;
            go.AddComponent<MeshRenderer>();
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.mesh = mesh;

            PrefabUtility.CreatePrefab(prefabPath, go);
            Destroy(go);
        }
    }


    Vector3 CircleCenter(Vector3 a, Vector3 b, Vector3 c) // This calculates the center of a circle trough all 3 vertices of a triangle.
    {                                                     // It assumes no degenerate triangles
        Vector3 t = b - a;
        Vector3 u = c - a;
        Vector3 v = c - b;

        Vector3 w = Vector3.Cross(t, u);
        float ww = w.sqrMagnitude;
        float tt = t.sqrMagnitude;
        float uu = u.sqrMagnitude;
        float iww = 0.5f / ww;

        return a + iww * (u * tt * Vector3.Dot(u, v) - t * uu * Vector3.Dot(t, v));
    }

    private void CheckEdgesAddTriangle(int t1, int t2, int t3)
    {
        long key1 = t1 + ((long)t2 << 32);
        long key2 = t2 + ((long)t3 << 32);
        long key3 = t3 + ((long)t1 << 32);
        // Creating unique keys by combining two ints into a long.
        if (!edgeDict.ContainsKey(key1) && !edgeDict.ContainsKey(key2) && !edgeDict.ContainsKey(key3))
        { // Ensure uniqueness of edges.
            triangles.Add(t1);
            triangles.Add(t2);
            triangles.Add(t3);
            edgeDict.Add(key1, true);
            edgeDict.Add(key2, true);
            edgeDict.Add(key3, true);
        }
    }
 
    private void RemoveZipper()
    {// The uv mapping goes wrong when phi goes from 2 pi to 0, resulting in a zipper artifact. This can be solved by duplicating a vertex.
     // By duplicating the vertex we can give it a different u-coördinate that combines correclty with the other two vertices.

        for (int i = 0; i < triangles.Count; i = i + 3)
        {
            int t1 = triangles[i];
            int t2 = triangles[i + 1];
            int t3 = triangles[i + 2];

            Vector3 v1 = vertices[t1];
            Vector3 v2 = vertices[t2];
            Vector3 v3 = vertices[t3];

            if (v1.x < 0 || v2.x < 0 || v3.x < 0) // the zipper is on the positive x-side, so there is no problem here
                continue;

            if (v1.z >= 0 && v2.z >= 0 && v3.z >= 0) // if all  vertices are on the positive z-side there is no problem
                continue;

            else if (v1.z < 0 && v2.z < 0 && v3.z < 0) // if all  vertices are on the negative  z-side there is no problem
                continue;

            else if (v1.z >= 0 && v2.z < 0 && v3.z < 0) // v1 has to be duplicated
            {
                triangles[i] = vertices.Count;
                vertices.Add(v1);
                normals.Add(normals[t1]);
                uv.Add(new Vector2(1 + uv[t1].x, uv[t1].y)); // 1 is added to the u-coördinate
            }
            else if (v1.z < 0 && v2.z >= 0 && v3.z < 0) // v2 has to be duplicated
            {
                triangles[i + 1] = vertices.Count;
                vertices.Add(v2);
                normals.Add(normals[t2]);
                uv.Add(new Vector2(1 + uv[t2].x, uv[t2].y)); // 1 is added to the u-coördinate
            }
            else if (v1.z < 0 && v2.z < 0 && v3.z >= 0) // v3 has to be duplicated
            {
                triangles[i + 2] = vertices.Count;
                vertices.Add(v3);
                normals.Add(normals[t3]);
                uv.Add(new Vector2(1 + uv[t3].x, uv[t3].y)); // 1 is added to the u-coördinate
            }
            else if (v1.z < 0 && v2.z >= 0 && v3.z >= 0) // v1 has to be duplicated 
            {
                triangles[i] = vertices.Count;
                vertices.Add(v1);
                normals.Add(normals[t1]);
                uv.Add(new Vector2(uv[t1].x - 1, uv[t1].y)); // the u-coördinate is made negative
            }
            else if (v1.z >= 0 && v2.z < 0 && v3.z >= 0) // v2 has to be duplicated
            {
                triangles[i + 1] = vertices.Count;
                vertices.Add(v2);
                normals.Add(normals[t2]);
                uv.Add(new Vector2(uv[t2].x - 1, uv[t2].y)); // the u-coördinate is made negative
            }
            else                                        // v3 has to be duplicated
            {
                triangles[i + 2] = vertices.Count;
                vertices.Add(v3);
                normals.Add(normals[t3]);
                uv.Add(new Vector2(uv[t3].x - 1, uv[t3].y)); // the u-coördinate is made negative
            }
        }
    }

    private void ReplacePoles()
    {
        // the poles both have triangle that goes around the pole, creating a uv artifact
        // we remove those triangles and replace them with 3 smaller ones, connected to the pole

        int n1 = triangles[triangles.Count - 1]; // getting the last triangle ints
        int n2 = triangles[triangles.Count - 2];
        int n3 = triangles[triangles.Count - 3];

        triangles.RemoveRange(0, 3); // removing the first triangle
        triangles.RemoveRange(triangles.Count - 3, 3); // removing the last triangle

        BottomPoleTriangle(0, 1); // replacing the first triangle connected to the pole
        BottomPoleTriangle(1, 2); // replacing the second triangle connected to the pole

        // the last triangle is special, because one of the vertices has u-coördinate 0, but has to have 1 when connected from the other side.
        triangles.Add(2); 
        triangles.Add(vertices.Count);
        vertices.Add(vertices[0]);
        normals.Add(vertices[0]);
        uv.Add(new Vector2(1, uv[0].y));

        triangles.Add(vertices.Count);
        vertices.Add(new Vector3(0, -1, 0) * radius);
        normals.Add(new Vector3(0, -1, 0));
        uv.Add(new Vector2((1 + uv[2].x) / 2, 0)); // 1 added to the u coördinate because it is connected from the other side

        float u1 = uv[n1].x; // getting the u coördinates around the top pole.
        float u2 = uv[n2].x;
        float u3 = uv[n3].x;

        int low, med, high;

        // The difficulty is in determining the winding order of the original 3 triangles.
        // We solve this by ordering them by u coordinate.

        if (u1 > u2)
        {
            if (u2 > u3)
            {
                low = n3; med = n2; high = n1;
            }
            else if (u1 > u3)
            {
                low = n2; med = n3; high = n1;
            }
            else
            {
                low = n2; med = n1; high = n3;
            }
        }
        else
        {
            if (u1 > u3)
            {
                low = n3; med = n1; high = n2;
            }
            else if (u2 > u3)
            {
                low = n1; med = n3; high = n2;
            }
            else
            {
                low = n1; med = n2; high = n3;
            }
        }

        TopPoleTriangle(low, med);
        TopPoleTriangle(med, high);

        // the last triangle is special, because the last vertex needs to be duplicated, adding 1 to the u-coördinate.

        triangles.Add(high);

        triangles.Add(vertices.Count);
        vertices.Add(new Vector3(0, 1, 0) * radius);
        normals.Add(new Vector3(0, 1, 0));
        uv.Add(new Vector2((uv[high].x + uv[low].x + 1) / 2, 1));

        triangles.Add(vertices.Count);
        vertices.Add(vertices[low]);
        normals.Add(vertices[low]);
        uv.Add(new Vector2(uv[low].x + 1, uv[low].y)); // 1 added to the u coördinate because it is connected from the other side
    }

    private void BottomPoleTriangle(int t1, int t2)
    {
        triangles.Add(t1); // creating a triangle from 2 vertices and the bottom pole
        triangles.Add(t2);
        triangles.Add(vertices.Count);
        vertices.Add(new Vector3(0, -1, 0) * radius);
        normals.Add(new Vector3(0, -1, 0));
        uv.Add(new Vector2((uv[t1].x + uv[t2].x) / 2f, 0)); // taking the average u coord, the pole has no u.
    }

    private void TopPoleTriangle(int t1, int t2)
    {
        triangles.Add(t1); // creating a triangle from 2 vertices and the top pole
        triangles.Add(vertices.Count);
        triangles.Add(t2);
        vertices.Add(new Vector3(0, 1, 0) * radius);
        normals.Add(new Vector3(0, 1, 0));
        uv.Add(new Vector2((uv[t1].x + uv[t2].x) / 2f, 1));
    }
}
