using UnityEditor;
using UnityEngine;

namespace Harvest.Editor
{
    // Large props use simple collision; trim and small dressing remain visual only.
    public static class FarmDetailGeometry
    {
        public static void Build()
        {
            Material timber = Surface("Timber","Farm Aged Timber",new Color(.31f,.25f,.16f));
            Material iron = Surface("Roofing","Farm Corrugated Iron",new Color(.32f,.35f,.31f),.35f);
            Material pale = WeaponModelGeometry.MaterialFor("Farm Trim", new Color(0.65f, 0.61f, 0.48f));
            Material hay = WeaponModelGeometry.MaterialFor("Hay Bales", new Color(0.55f, 0.43f, 0.17f));
            Material dark = WeaponModelGeometry.MaterialFor("Farm Soot", new Color(0.12f, 0.12f, 0.105f));
            Material warning = WeaponModelGeometry.MaterialFor("Checkpoint Stripe", new Color(0.71f, 0.52f, 0.17f));
            // Shed occupies a field edge, clear of the road and existing marine cover lanes.
            Vector3 shed = new Vector3(-23f, 0f, 19f);
            Box("Shed rear wall", timber, shed + new Vector3(0f, 1.5f, 3.8f), new Vector3(8f, 3f, 0.18f), true);
            Box("Shed field wall", timber, shed + new Vector3(-3.9f, 1.5f, 0f), new Vector3(0.18f, 3f, 8f), true);
            foreach (int side in new[] { -1, 1 })
            {
                float top=3.15f-side*3.8f*Mathf.Tan(5f*Mathf.Deg2Rad)-.08f/Mathf.Cos(5f*Mathf.Deg2Rad);
                GameObject post=Box("Shed front post",timber,shed+new Vector3(side*3.8f,1.5f,-3.8f),new Vector3(.2f,3f,.2f),true);
                post.transform.position=shed+new Vector3(side*3.8f,top*.5f,-3.8f);
                post.GetComponent<MeshFilter>().sharedMesh=FarmhouseMeshLibrary.Box(new Vector3(.2f,top,.2f),.008f);
                post.GetComponent<BoxCollider>().center=new Vector3(0,1.5f-top*.5f,0); // Original collision stays put.
            }
            GameObject roof = Box("Shed metal roof", iron, shed + Vector3.up * 3.15f, new Vector3(8.5f, 0.16f, 8.5f), true);
            roof.transform.rotation = Quaternion.Euler(0f, 0f, -5f);
            for (int i = 0; i < 13; i++)
            {
                float z=-3.9f+i*.65f;
                GameObject seam=Box("Shed roof seam",iron,shed+new Vector3(0,3.25f,z),new Vector3(8.5f,.025f,.025f));
                seam.transform.rotation=roof.transform.rotation;
            }
            for(int i=0;i<=10;i++)
            {
                float run=-3.8f+i*.76f;
                Box("Shed rear cladding batten",timber,shed+new Vector3(run,1.5f,3.695f),new Vector3(.055f,3f,.035f));
                Box("Shed field cladding batten",timber,shed+new Vector3(-3.795f,1.5f,run),new Vector3(.035f,3f,.055f));
            }
            Box("Shed rear header",timber,shed+new Vector3(0,3.15f-.17f/Mathf.Cos(5f*Mathf.Deg2Rad),3.6f),new Vector3(7.8f,.18f,.16f)).transform.rotation=roof.transform.rotation;
            Box("Shed front header",timber,shed+new Vector3(0,3.15f-.17f/Mathf.Cos(5f*Mathf.Deg2Rad),-3.8f),new Vector3(7.8f,.18f,.2f)).transform.rotation=roof.transform.rotation;
            for(int side=-1;side<=1;side+=2)
            {
                Box("Shed post shoe",iron,shed+new Vector3(side*3.8f,.12f,-3.8f),new Vector3(.24f,.24f,.24f));
                Beam("Shed corner brace",timber,shed+new Vector3(side*3.8f,2.5f-side*3.8f*Mathf.Tan(5f*Mathf.Deg2Rad),-3.8f),
                    shed+new Vector3(side*3.1f,3.15f-.26f/Mathf.Cos(5f*Mathf.Deg2Rad)-side*3.1f*Mathf.Tan(5f*Mathf.Deg2Rad),-3.8f),.11f);
            }
            for (int i = 0; i < 3; i++)
            {
                Vector3 center = shed + new Vector3(-2.4f + i * 1.8f, 0.55f, 2.3f);
                Box("Stacked hay bale", hay, center, new Vector3(1.4f, 1.1f, 1.1f), true);
                foreach (float band in new[] { -0.42f, 0.42f })
                    Box("Bale binding", timber, center + Vector3.right * band, new Vector3(0.035f, 1.115f, 1.115f));
            }
            Silo(iron,pale,dark);
            foreach (Vector3 site in new[] { new Vector3(-28f, 0f, -16f), new Vector3(19f, 0f, 6f), new Vector3(19f, 0f, -24f) })
            {
                Box("Farm storage crate", timber, site + Vector3.up * 0.6f, new Vector3(1.5f, 1.2f, 1.3f), true);
                for (int i = 0; i < 5; i++)
                    Box("Crate plank seam", dark, site + new Vector3(0f, 0.17f + i * 0.22f, -0.656f), new Vector3(1.4f, 0.018f, 0.012f));
                foreach (int side in new[] { -1, 1 })
                {
                    Box("Crate metal strap", iron, site + new Vector3(side * 0.5f, 0.6f, 0f), new Vector3(0.045f, 1.21f, 1.31f));
                    Box("Crate corner upright",timber,site+new Vector3(side*.69f,.6f,-.68f),new Vector3(.09f,1.2f,.06f));
                    foreach(float y in new[]{.12f,1.08f})
                        Box("Crate strap fastener",dark,site+new Vector3(side*.5f,y,-.662f),new Vector3(.02f,.025f,.015f));
                }
            }
            Sign("HARVEST / FREIGHT ROAD", new Vector3(10.2f, 2f, -28f), pale, timber, 180f);
            Sign("CHECKPOINT / KEEP CLEAR", new Vector3(-9.5f, 1.7f, -7f), warning, dark, 180f);
            Sign("EVACUATION", new Vector3(7f, 1.8f, -34f), pale, dark, 180f);
        }
        static Material Surface(string foundation,string fallback,Color tint,float metal=0f)
        {
            Material original=WeaponModelGeometry.MaterialFor(fallback,tint,metal);
            // Preserve imported or artist-owned surfaces; use the approved scan for the bare starter.
            if(original.shader.name!="Universal Render Pipeline/Lit" || original.GetTexture("_BaseMap")!=null) return original;
            return AssetDatabase.LoadAssetAtPath<Material>(FarmhouseMaterialLibrary.MaterialPath(foundation))??original;
        }
        static void Beam(string name,Material material,Vector3 from,Vector3 to,float width)
        {
            GameObject go=Box(name,material,(from+to)*.5f,new Vector3(width,width,(to-from).magnitude));
            go.transform.rotation=Quaternion.LookRotation(to-from);
        }
        static GameObject Lathe(string name,Material material,Vector3 position,params Vector2[] profile)
        {
            var go=new GameObject(name);go.transform.position=position;
            go.AddComponent<MeshFilter>().sharedMesh=FarmMachineryMeshes.Revolve(name.Replace(" ","_"),profile);
            go.AddComponent<MeshRenderer>().sharedMaterial=material;
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);return go;
        }
        static void Silo(Material iron,Material pale,Material dark)
        {
            Vector3 origin=new Vector3(24,0,48);
            GameObject silo=Lathe("Harvest grain silo",iron,origin,new Vector2(0,0),new Vector2(3,0),new Vector2(3,6.6f),new Vector2(0,6.6f));
            // Preserve the previous six-metre square broad collision proxy.
            var body=silo.AddComponent<BoxCollider>();body.center=Vector3.up*3.3f;body.size=new Vector3(6,6.6f,6);
            Lathe("Silo foundation collar",iron,origin,new Vector2(0,0),new Vector2(3.12f,0),new Vector2(3.12f,.14f),new Vector2(0,.14f));
            Lathe("Silo pitched lid",iron,origin+Vector3.up*6.6f,new Vector2(0,0),new Vector2(3.08f,0),new Vector2(.16f,1.06f),new Vector2(.16f,1.16f),new Vector2(0,1.16f));
            for(int i=0;i<5;i++)
                Lathe("Silo rolled ring "+i,iron,origin+Vector3.up*(.35f+i*1.48f),new Vector2(2.985f,0),new Vector2(3.035f,0),
                    new Vector2(3.035f,.045f),new Vector2(2.985f,.045f),new Vector2(2.985f,0));
            for(int i=0;i<16;i++)
            {
                float angle=i*Mathf.PI*2f/16f;
                Box("Silo vertical rib",iron,origin+new Vector3(Mathf.Sin(angle)*3.005f,3.3f,Mathf.Cos(angle)*3.005f),new Vector3(.035f,6.5f,.035f));
            }
            Box("Silo access door",dark,origin+new Vector3(0,1,-3.018f),new Vector3(1,2,.03f));
            Box("Silo door handle",pale,origin+new Vector3(.35f,1,-3.046f),new Vector3(.035f,.19f,.025f));
            Vector3 normal=new Vector3(2,0,-2.35f).normalized,right=Vector3.Cross(Vector3.up,normal);
            Vector3 anchor=origin+normal*3.16f;Quaternion yaw=Quaternion.LookRotation(normal);
            for(int side=-1;side<=1;side+=2)
                Box("Silo ladder rail",pale,anchor+right*(side*.29f)+Vector3.up*3.15f,new Vector3(.04f,5.7f,.04f)).transform.rotation=yaw;
            for(int i=0;i<11;i++)
                Box("Silo ladder rung",pale,anchor+Vector3.up*(.6f+i*.5f),new Vector3(.60f,.035f,.035f)).transform.rotation=yaw;
            foreach(float y in new[]{.8f,3f,5.2f}) foreach(int side in new[]{-1,1})
                Beam("Silo ladder stand-off",iron,origin+normal*3f+right*(side*.29f)+Vector3.up*y,
                    anchor+right*(side*.29f)+Vector3.up*y,.035f);
        }
        static void Sign(string words, Vector3 position, Material paint, Material pole, float yaw)
        {
            Box("Sign post", pole, new Vector3(position.x, position.y * 0.5f, position.z), new Vector3(0.12f, position.y, 0.12f));
            Box("Sign board", paint, position, new Vector3(2.4f, 0.75f, 0.08f));
            foreach (int side in new[] { -1, 1 })
            {
                GameObject text = new GameObject(words); text.transform.position = position + Vector3.forward * (side * 0.055f);
                text.transform.rotation = Quaternion.Euler(0f, side < 0 ? 0f : 180f, 0f);
                TextMesh label = text.AddComponent<TextMesh>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (label.font != null) text.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
                label.text = words.Replace(" / ", "\n");
                label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
                label.fontSize = 40; label.characterSize = 0.045f; label.color = new Color(0.1f, 0.13f, 0.1f);
            }
        }
        static GameObject Box(string name, Material material, Vector3 position, Vector3 size, bool solid = false)
        {
            GameObject part=new GameObject(name);part.transform.position=position;
            part.AddComponent<MeshFilter>().sharedMesh=FarmhouseMeshLibrary.Box(size,.008f,name=="Shed metal roof");
            part.AddComponent<MeshRenderer>().sharedMaterial=material;
            GameObjectUtility.SetStaticEditorFlags(part,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);
            if(solid) part.AddComponent<BoxCollider>().size=size;
            return part;
        }
    }
}
