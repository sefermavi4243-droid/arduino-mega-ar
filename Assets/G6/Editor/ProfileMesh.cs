using System.Collections.Generic;
using UnityEngine;

namespace G6.EditorTools
{
    /// <summary>
    /// Yandan görünüşü (Z-Y profili) verilen bir parçayı X ekseni boyunca genişliğine uzatarak (extrude) mesh üretir.
    /// Zımbanın kolu ve tabanı yandan bakınca belirgin bir eğri profile sahip; kutudan çok daha iyi oturur.
    /// Profil noktaları saat yönünün tersine sıralanmalı ve ağırlık merkezinden görülebilmelidir (yıldız biçimli).
    /// </summary>
    public static class ProfileMesh
    {
        public static Mesh Extrude(string name, IList<Vector2> profileZY, float width)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            float hx = width / 2f;
            int n = profileZY.Count;

            Vector2 c = Vector2.zero;
            foreach (var p in profileZY) c += p;
            c /= n;

            // İki yan kapak: ağırlık merkezinden yelpaze üçgenleme.
            foreach (float x in new[] { -hx, hx })
            {
                int center = verts.Count;
                verts.Add(new Vector3(x, c.y, c.x));
                for (int i = 0; i < n; i++) verts.Add(new Vector3(x, profileZY[i].y, profileZY[i].x));
                for (int i = 0; i < n; i++)
                {
                    int a = center + 1 + i, b = center + 1 + (i + 1) % n;
                    if (x < 0) { tris.Add(center); tris.Add(a); tris.Add(b); }
                    else { tris.Add(center); tris.Add(b); tris.Add(a); }
                }
            }

            // Çevre yüzeyleri: her kenar için ayrı dörtgen (keskin kenarlı normaller).
            for (int i = 0; i < n; i++)
            {
                var p0 = profileZY[i];
                var p1 = profileZY[(i + 1) % n];
                int s = verts.Count;
                verts.Add(new Vector3(-hx, p0.y, p0.x));
                verts.Add(new Vector3(hx, p0.y, p0.x));
                verts.Add(new Vector3(hx, p1.y, p1.x));
                verts.Add(new Vector3(-hx, p1.y, p1.x));
                tris.Add(s); tris.Add(s + 1); tris.Add(s + 2);
                tris.Add(s); tris.Add(s + 2); tris.Add(s + 3);
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
