using UnityEngine;

namespace Harvest.Editor
{
    // Layout generation is separate from encounter waves, actors, and combat configuration.
    public static class FarmEncounterGeometry
    {
        public static void Build(Material soil, Material road, Material grain, Material concrete, Material timber)
        {
            FarmNaturalGround.Build(soil, road);
            FarmhouseFoundationBuilder.Build();
            FarmGroundTransitions.Build(soil);
            FarmGrainGeometry.Build();
            for (int side = -1; side <= 1; side += 2)
                for (int z = -39; z <= 57; z += 8)
                {
                    if (side == -1 && z > -18 && z < 11) continue;
                    Box("Fence post", timber, new Vector3(side * 30f, 0.75f, z), new Vector3(0.16f, 1.5f, 0.16f));
                    Box("Field fence rail", timber, new Vector3(side * 30f, 0.95f, z + 3.8f), new Vector3(0.1f, 0.12f, 7.6f));
                }
        }
        static GameObject Box(string name, Material material, Vector3 position, Vector3 size, bool collider = true)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = name; root.transform.position = position; root.transform.localScale = size;
            root.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(root.GetComponent<Collider>());
            return root;
        }
    }
}
