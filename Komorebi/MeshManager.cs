using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Komorebi
{
    static class MeshManager
    {
        private static Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();

        public static Mesh getMesh(String filename)
        {
            if(meshes.ContainsKey(filename))
            {
                return meshes[filename];
            }
            else
            {
                Mesh newMesh = new Mesh(filename);
                meshes.Add(filename, newMesh);
                return newMesh;
            }
        }
    }
}
