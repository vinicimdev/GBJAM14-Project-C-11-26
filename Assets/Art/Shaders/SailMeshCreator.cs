#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Tools > Create Sail Mesh  ->  saves Assets/SailMesh.asset
// A flat 1x1 plane facing +Z, pivot at the top-center (hangs from the yard),
// UVs 0-1 with v = 1 at the top. Use it for every sail.
public static class SailMeshCreator
{
    const int Res = 20; // quads per side; more = smoother bulge

    [MenuItem("Tools/Create Sail Mesh")]
    static void Create()
    {
        var verts = new Vector3[(Res + 1) * (Res + 1)];
        var uvs   = new Vector2[verts.Length];
        var tris  = new int[Res * Res * 6];

        for (int y = 0, i = 0; y <= Res; y++)
        for (int x = 0; x <= Res; x++, i++)
        {
            var uv = new Vector2((float)x / Res, (float)y / Res);
            uvs[i]   = uv;
            verts[i] = new Vector3(uv.x - 0.5f, uv.y - 1f, 0f);
        }

        for (int y = 0, t = 0; y < Res; y++)
        for (int x = 0; x < Res; x++, t += 6)
        {
            int v00 = y * (Res + 1) + x, v10 = v00 + 1, v01 = v00 + Res + 1, v11 = v01 + 1;
            tris[t]     = v00; tris[t + 1] = v10; tris[t + 2] = v01;
            tris[t + 3] = v10; tris[t + 4] = v11; tris[t + 5] = v01;
        }

        var mesh = new Mesh { name = "Sail", vertices = verts, uv = uvs, triangles = tris };
        mesh.RecalculateNormals();
        // Extra depth so the bulge doesn't get culled at the screen edges
        mesh.bounds = new Bounds(new Vector3(0f, -0.5f, 1f), new Vector3(1f, 1f, 3f));

        AssetDatabase.CreateAsset(mesh, "Assets/SailMesh.asset");
        Selection.activeObject = mesh;
    }
}
#endif
