using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Harvest.Editor
{
    public static class FarmhouseLighting
    {
        public const string RootName="Harvest farmhouse lighting";
        public static void Build(HarvestVisualProfile look)
        {
            var house=UnityEngine.Object.FindFirstObjectByType<FarmhouseFoundation>();
            if(house==null || house.Layout==null) return;
            FarmhouseLayout l=house.Layout;
            if(l.AuthoredPrefab!=null) return; // An artist-owned replacement supplies its own fixtures/probes.
            var root=new GameObject(RootName).transform;root.position=l.WorldOrigin;
            Material steel=AssetDatabase.LoadAssetAtPath<Material>(FarmhouseMaterialLibrary.MaterialPath("Steel"));
            if(steel==null) throw new InvalidOperationException("Farmhouse steel material is missing.");
            // Upgrade only the exact previous defaults; preserve deliberate edits, including disabled lamps.
            if(look.InteriorReadabilityRevision<1)
            {
                if(Mathf.Approximately(look.GroundPracticalIntensity,1.4f)) look.GroundPracticalIntensity=5.5f;
                if(Mathf.Approximately(look.UpperPracticalIntensity,1.2f)) look.UpperPracticalIntensity=4.5f;
                if(Mathf.Approximately(look.PracticalRange,7f)) look.PracticalRange=9f;
                look.InteriorReadabilityRevision=1;EditorUtility.SetDirty(look);
            }
            Material bulb=BulbMaterial();
            Pendant(root,"Ground practical",new Vector3(2.4f,l.UpperFloorTop-.18f,2.3f),steel,bulb,look.GroundPracticalIntensity,look);
            Pendant(root,"Upper practical",new Vector3(2.8f,l.EavesHeight-.14f,-3.2f),steel,bulb,look.UpperPracticalIntensity,look);
            Pendant(root,"Ground entry practical",new Vector3(1.8f,l.UpperFloorTop-.18f,-3.6f),steel,bulb,look.GroundPracticalIntensity,look);
            Pendant(root,"Upper rear practical",new Vector3(1.8f,l.EavesHeight-.14f,3.4f),steel,bulb,look.UpperPracticalIntensity,look);
            Sconce(root,steel,bulb,l,look);
            WindowBounce(root,l,look);
            Reflection(root,"Ground room reflection",new Vector3(0,(l.GroundFloorTop+l.UpperFloorTop-.18f)*.5f,0),
                new Vector3(l.HalfWidth*2f-.3f,l.UpperFloorTop-l.GroundFloorTop-.28f,l.HalfDepth*2f-.3f),256,10,40);
            Reflection(root,"Upper room reflection",new Vector3(0,(l.UpperFloorTop+l.EavesHeight-.14f)*.5f,0),
                new Vector3(l.HalfWidth*2f-.3f,l.EavesHeight-l.UpperFloorTop-.24f,l.HalfDepth*2f-.3f),256,10,40);
            Reflection(root,"Farmyard reflection",new Vector3(4,3,-1),new Vector3(45,18,42),128,0,150);
            Probes(root,l);
            Pipeline(look);
        }
        static void Pendant(Transform root,string name,Vector3 anchor,Material steel,Material bulb,float intensity,HarvestVisualProfile look)
        {
            var fixture=new GameObject(name).transform;fixture.SetParent(root,false);fixture.localPosition=anchor;
            Box(fixture,"Ceiling rose",steel,new Vector3(0,-.011f,0),new Vector3(.20f,.022f,.20f));
            Box(fixture,"Pendant stem",steel,new Vector3(0,-.08f,0),new Vector3(.016f,.14f,.016f));
            GameObject shade=Visual(fixture,"Downward shade",FarmhousePropMeshes.Vessel("Lamp_shade",.20f,.12f,.012f),steel,new Vector3(0,-.14f,0));
            shade.transform.localRotation=Quaternion.Euler(180,0,0);
            GameObject glass=Visual(fixture,"Practical bulb",FarmMachineryMeshes.Revolve("Practical_bulb",new Vector2(0,0),new Vector2(.038f,0),
                new Vector2(.045f,.03f),new Vector2(.025f,.07f),new Vector2(0,.08f)),bulb,new Vector3(0,-.32f,0));
            glass.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(glass,StaticEditorFlags.BatchingStatic); // Opaque emissive glass must not occlude its own baked light.
            Point(fixture,new Vector3(0,-.28f,0),intensity,look.PracticalRange,look.PracticalColor);
        }
        static void Sconce(Transform root,Material steel,Material bulb,FarmhouseLayout l,HarvestVisualProfile look)
        {
            var fixture=new GameObject("Porch practical").transform;fixture.SetParent(root,false);
            fixture.localPosition=new Vector3(1.35f,2.35f,-l.HalfDepth-.135f);
            Box(fixture,"Wall plate",steel,Vector3.zero,new Vector3(.16f,.25f,.03f));
            Box(fixture,"Sconce arm",steel,new Vector3(0,.045f,-.075f),new Vector3(.025f,.025f,.15f));
            GameObject glass=Visual(fixture,"Porch bulb",FarmMachineryMeshes.Revolve("Porch_bulb",new Vector2(0,0),new Vector2(.055f,0),
                new Vector2(.055f,.12f),new Vector2(0,.12f)),bulb,new Vector3(0,-.08f,-.14f));
            glass.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(glass,StaticEditorFlags.BatchingStatic); // Opaque emissive glass must not occlude its own baked light.
            Box(fixture,"Porch lamp cap",steel,new Vector3(0,.05f,-.14f),new Vector3(.15f,.025f,.15f));
            Point(fixture,new Vector3(0,-.025f,-.14f),look.PorchPracticalIntensity,4f,look.PracticalColor);
        }
        static void WindowBounce(Transform root,FarmhouseLayout l,HarvestVisualProfile look)
        {
            float inset=l.WallThickness*.5f+.025f;
            foreach(float x in new[]{-3.8f,3.8f}) Area("Ground front "+x,new Vector3(x,1.60f,-l.HalfDepth+inset),Vector3.forward,1.8f,1.10f);
            Area("Ground road",new Vector3(l.HalfWidth-inset,1.55f,1),Vector3.left,3f,1.10f);
            Area("Ground field",new Vector3(-l.HalfWidth+inset,1.55f,3),Vector3.right,2.2f,1.10f);
            Area("Upper front",new Vector3(1,l.UpperFloorTop+1.575f,-l.HalfDepth+inset),Vector3.forward,3f,1.25f);
            Area("Upper rear",new Vector3(1,l.UpperFloorTop+1.575f,l.HalfDepth-inset),Vector3.back,3f,1.25f);
            Area("Upper road",new Vector3(l.HalfWidth-inset,l.UpperFloorTop+1.575f,1),Vector3.left,3.8f,1.25f);
            Area("Upper field",new Vector3(-l.HalfWidth+inset,l.UpperFloorTop+1.575f,3),Vector3.right,2.2f,1.25f);
            void Area(string name,Vector3 position,Vector3 inward,float width,float height)
            {
                var go=new GameObject(name+" baked sky fill");go.transform.SetParent(root,false);go.transform.localPosition=position;
                go.transform.localRotation=Quaternion.LookRotation(inward);
                var light=go.AddComponent<Light>();light.type=LightType.Rectangle;light.areaSize=new Vector2(width,height);
                light.lightmapBakeType=LightmapBakeType.Baked;light.color=look.WindowBounceColor;light.intensity=look.WindowBounceIntensity;
                light.shadows=LightShadows.Soft;light.range=10f;
            }
        }
        static void Point(Transform fixture,Vector3 position,float intensity,float range,Color color)
        {
            var go=new GameObject("Light source");go.transform.SetParent(fixture,false);go.transform.localPosition=position;
            var lamp=go.AddComponent<Light>();lamp.type=LightType.Point;lamp.color=color;lamp.intensity=intensity;lamp.range=range;
            lamp.lightmapBakeType=LightmapBakeType.Mixed;lamp.shadows=LightShadows.Soft;lamp.shadowBias=.025f;lamp.shadowNormalBias=.12f;
        }
        static void Reflection(Transform root,string name,Vector3 position,Vector3 size,int resolution,int importance,float far)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=position;
            var probe=go.AddComponent<ReflectionProbe>();probe.size=size;probe.resolution=resolution;probe.importance=importance;
            probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;probe.boxProjection=true;probe.blendDistance=.3f;
            probe.nearClipPlane=.1f;probe.farClipPlane=far;probe.hdr=true;probe.renderDynamicObjects=false;
        }
        static void Probes(Transform root,FarmhouseLayout l)
        {
            var points=new List<Vector3>();Physics.SyncTransforms();
            foreach(float floor in new[]{l.GroundFloorTop,l.UpperFloorTop})
                foreach(float x in new[]{-l.HalfWidth+1.2f,-1.5f,1.5f,l.HalfWidth-1.2f})
                    foreach(float z in new[]{-l.HalfDepth+1.2f,-2f,1.5f,l.HalfDepth-1.2f})
                        foreach(float height in new[]{.45f,1.5f,2.5f}) Add(new Vector3(x,floor+height,z));
            for(int i=0;i<l.StairCount;i+=3)
                foreach(float height in new[]{.45f,1.5f}) Add(l.StairPoint(i)+Vector3.up*height);
            foreach(float x in new[]{-35f,-28f,-10f,0f,12f,26f})
                foreach(float z in new[]{-32f,-16f,0f,16f,32f,48f})
                    foreach(float y in new[]{.7f,1.7f,3.5f}) Add(new Vector3(x,y,z)-l.WorldOrigin);
            var go=new GameObject("Rooms stairs and approach light probes");go.transform.SetParent(root,false);
            go.AddComponent<LightProbeGroup>().probePositions=points.ToArray();
            void Add(Vector3 local)
            {
                // Reject probes buried in floors, roof, furniture or terrain; placement follows layout edits.
                Vector3 world=root.TransformPoint(local);
                if(!Physics.CheckSphere(world,.12f,~0,QueryTriggerInteraction.Ignore)) points.Add(local);
            }
        }
        static void Pipeline(HarvestVisualProfile look)
        {
            if(look.PracticalSetupRevision>=1) return;
            var pipeline=GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if(pipeline==null) throw new InvalidOperationException("URP is required for farmhouse practical lights.");
            var settings=new SerializedObject(pipeline);
            var shadows=settings.FindProperty("m_AdditionalLightShadowsSupported");var mode=settings.FindProperty("m_AdditionalLightsRenderingMode");
            if(shadows==null || mode==null) throw new InvalidOperationException("URP 17 additional-light settings are missing. Check Package Manager.");
            shadows.boolValue=true;mode.intValue=(int)LightRenderingMode.PerPixel;settings.ApplyModifiedPropertiesWithoutUndo();
            pipeline.additionalLightsShadowmapResolution=Mathf.Max(4096,pipeline.additionalLightsShadowmapResolution);
            EditorUtility.SetDirty(pipeline);look.PracticalSetupRevision=1;EditorUtility.SetDirty(look);
        }
        static Material BulbMaterial()
        {
            const string path="Assets/Harvest/Materials/Farmhouse Practical Glass.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null) return material;
            Shader shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null) throw new InvalidOperationException("URP Lit is missing.");
            material=new Material(shader);material.SetColor("_BaseColor",new Color(.88f,.78f,.60f));material.SetFloat("_Smoothness",.3f);
            material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",new Color(1.0f,.65f,.32f)*1.8f);
            material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None; // Explicit light owns energy; avoid counting the bulb twice.
            AssetDatabase.CreateAsset(material,path);return material;
        }
        static void Box(Transform root,string name,Material material,Vector3 center,Vector3 size) =>
            Visual(root,name,FarmhouseMeshLibrary.Box(size,.003f),material,center);
        static GameObject Visual(Transform root,string name,Mesh mesh,Material material,Vector3 position)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=position;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
            GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.ContributeGI|StaticEditorFlags.BatchingStatic);return go;
        }
    }
}
