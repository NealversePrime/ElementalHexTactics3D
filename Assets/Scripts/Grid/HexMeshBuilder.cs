using UnityEngine;

namespace ElementalHexTactics3D.Grid
{
    /// <summary>
    /// Generates procedural 3D Hex Pillar meshes with:
    ///   - Submesh 0: Flat top hexagonal face (mapped with planar UVs for elemental textures)
    ///   - Submesh 1: 6 extruded side walls (mapped for soil / cliff / rock texture)
    /// Normals and triangle windings are physically aligned for standard forward-facing rendering.
    /// </summary>
    public static class HexMeshBuilder
    {
        public static Mesh CreateHexPillarMesh(float radius = 1f, float depth = 1.2f)
        {
            Mesh mesh = new Mesh();
            mesh.name = $"HexPillar_R{radius}_D{depth}";

            // Calculate 6 outer corners in 2D (XZ plane)
            Vector3[] topCorners = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float angleDeg = 60f * i + 30f; // Pointy-top orientation
                float rad = angleDeg * Mathf.Deg2Rad;
                topCorners[i] = new Vector3(radius * Mathf.Cos(rad), 0f, radius * Mathf.Sin(rad));
            }

            #region Top Cap (Submesh 0)

            // Center + 6 corners = 7 vertices
            Vector3[] topVertices = new Vector3[7];
            Vector3[] topNormals = new Vector3[7];
            Vector2[] topUVs = new Vector2[7];

            topVertices[0] = Vector3.zero;
            topNormals[0] = Vector3.up;
            topUVs[0] = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < 6; i++)
            {
                topVertices[i + 1] = topCorners[i];
                topNormals[i + 1] = Vector3.up;
                // Planar UV projection normalized to [0, 1] covering the entire square texture
                topUVs[i + 1] = new Vector2(
                    (topCorners[i].x / (Mathf.Sqrt(3f) * radius)) + 0.5f,
                    (topCorners[i].z / (2f * radius)) + 0.5f
                );
            }

            // 6 triangles wound Clockwise viewed from above (+Y)
            int[] topTriangles = new int[6 * 3];
            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                topTriangles[i * 3 + 0] = 0;
                topTriangles[i * 3 + 1] = next + 1; // Clockwise winding
                topTriangles[i * 3 + 2] = i + 1;
            }

            #endregion

            #region Side Skirt Walls (Submesh 1)

            // 6 sides * 4 vertices = 24 vertices
            Vector3[] sideVertices = new Vector3[24];
            Vector3[] sideNormals = new Vector3[24];
            Vector2[] sideUVs = new Vector2[24];
            int[] sideTriangles = new int[6 * 6];

            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                Vector3 pTopA = topCorners[i];
                Vector3 pTopB = topCorners[next];
                Vector3 pBotA = new Vector3(pTopA.x, -depth, pTopA.z);
                Vector3 pBotB = new Vector3(pTopB.x, -depth, pTopB.z);

                // Normal pointing strictly outwards horizontally from the hex center
                Vector3 midPoint = (pTopA + pTopB) * 0.5f;
                Vector3 outwardNormal = new Vector3(midPoint.x, 0f, midPoint.z).normalized;

                int vIdx = i * 4;
                sideVertices[vIdx + 0] = pTopA;
                sideVertices[vIdx + 1] = pTopB;
                sideVertices[vIdx + 2] = pBotB;
                sideVertices[vIdx + 3] = pBotA;

                sideNormals[vIdx + 0] = outwardNormal;
                sideNormals[vIdx + 1] = outwardNormal;
                sideNormals[vIdx + 2] = outwardNormal;
                sideNormals[vIdx + 3] = outwardNormal;

                sideUVs[vIdx + 0] = new Vector2(0f, 1f);
                sideUVs[vIdx + 1] = new Vector2(1f, 1f);
                sideUVs[vIdx + 2] = new Vector2(1f, 0f);
                sideUVs[vIdx + 3] = new Vector2(0f, 0f);

                int tIdx = i * 6;
                int baseV = 7 + vIdx; // Offset by top cap vertex count

                // Clockwise winding viewed from outside the hex:
                // Triangle 1: TopA -> TopB -> BotB
                sideTriangles[tIdx + 0] = baseV + 0;
                sideTriangles[tIdx + 1] = baseV + 1;
                sideTriangles[tIdx + 2] = baseV + 2;

                // Triangle 2: TopA -> BotB -> BotA
                sideTriangles[tIdx + 3] = baseV + 0;
                sideTriangles[tIdx + 4] = baseV + 2;
                sideTriangles[tIdx + 5] = baseV + 3;
            }

            #endregion

            // Combine into single mesh with 2 submeshes
            Vector3[] allVertices = new Vector3[7 + 24];
            Vector3[] allNormals = new Vector3[7 + 24];
            Vector2[] allUVs = new Vector2[7 + 24];

            System.Array.Copy(topVertices, 0, allVertices, 0, 7);
            System.Array.Copy(sideVertices, 0, allVertices, 7, 24);

            System.Array.Copy(topNormals, 0, allNormals, 0, 7);
            System.Array.Copy(sideNormals, 0, allNormals, 7, 24);

            System.Array.Copy(topUVs, 0, allUVs, 0, 7);
            System.Array.Copy(sideUVs, 0, allUVs, 7, 24);

            mesh.subMeshCount = 2;
            mesh.vertices = allVertices;
            mesh.normals = allNormals;
            mesh.uv = allUVs;

            mesh.SetTriangles(topTriangles, 0);
            mesh.SetTriangles(sideTriangles, 1);

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
