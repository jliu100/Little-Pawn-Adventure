using System.Collections.Generic;
using UnityEngine;
namespace LittlePaws
{
    // Scene transitions must release generated materials/meshes, not just their renderers.
    public sealed class RuntimeVisualResources : MonoBehaviour
    {
        public readonly List<Mesh> meshes = new List<Mesh>();
        readonly HashSet<Material> materials = new HashSet<Material>();
        public void CaptureMaterials()
        {
            foreach(var r in GetComponentsInChildren<Renderer>())
                foreach(var m in r.sharedMaterials)if(m)materials.Add(m);
        }
        void OnDestroy()
        {
            foreach(var m in materials)if(m)Destroy(m);
            foreach(var m in meshes)if(m)Destroy(m);
        }
    }
}
