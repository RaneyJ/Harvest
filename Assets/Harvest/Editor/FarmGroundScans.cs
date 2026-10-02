using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Object=UnityEngine.Object;

namespace Harvest.Editor
{
    // Fixed CC0 sources, downloaded asynchronously in Edit mode. Each local source receipt
    // records its first verified SHA256; subsequent imports validate that snapshot.
    public static class FarmGroundScans
    {
        const string Folder="Assets/Harvest/Art/Surfaces/FarmGroundScans";
        static readonly string[] Ids={"gravelly_sand","brown_mud"};
        static readonly string[] Roles={"diff","disp"};
        [Serializable] sealed class Receipt { public string Url,Sha256; public long Bytes; }
        sealed class Job { public string Id,Role,Url,Cache; }
        static Queue<Job> jobs;static Job active;static UnityWebRequest request;static int completed;
        public static bool IsInstalling=>jobs!=null;
        static string CacheRoot=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Library/HarvestGroundScanCache"));
        public static string PackedPath(bool gravel)=>Folder+"/"+(gravel?Ids[0]:Ids[1])+"_Packed.asset";

        public static void InstallIfMissing()
        {
            if(IsInstalling||AssetDatabase.LoadAssetAtPath<Texture2D>(PackedPath(true))!=null&&AssetDatabase.LoadAssetAtPath<Texture2D>(PackedPath(false))!=null)return;
            Install();
        }
        [MenuItem("Harvest/Materials/Install Ground Scans")]
        public static void Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||IsInstalling)return;
            if(FarmhouseMaterialLibrary.IsInstalling)
            {
                Debug.Log("Finish the farmhouse material installation first, then run Harvest > Materials > Install Ground Scans. Generated ground detail remains active.");return;
            }
            jobs=new Queue<Job>();completed=0;
            foreach(string id in Ids)foreach(string role in Roles)
            {
                string extension=role=="diff"?"jpg":"png";
                jobs.Enqueue(new Job{Id=id,Role=role,Url="https://dl.polyhaven.org/file/ph-assets/Textures/"+extension+"/2k/"+id+"/"+id+"_"+role+"_2k."+extension,Cache=Path.Combine(CacheRoot,id+"_"+role+"."+extension)});
            }
            EditorApplication.update+=Tick;AssemblyReloadEvents.beforeAssemblyReload+=Cancel;EditorApplication.quitting+=Cancel;
            Debug.Log("Installing 2K Poly Haven gravel/soil scans in the background. Generated detail remains active until all maps are validated. Source credits: GROUND_SURFACES.md.");
        }
        static void Tick()
        {
            try
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling){Cancel();return;}
                if(EditorUtility.DisplayCancelableProgressBar("Farm ground scans",active!=null?active.Id+" / "+active.Role:"Checking verified cache",(completed+(request!=null?Mathf.Max(0,request.downloadProgress):0))/4f)){Cancel();return;}
                if(request!=null)
                {
                    if(!request.isDone)return;
                    if(request.result!=UnityWebRequest.Result.Success)throw new IOException(request.error);
                    request.Dispose();request=null;ValidateAndReceipt(active.Cache+".partial",active);
                    File.Copy(active.Cache+".partial",active.Cache,true);File.Delete(active.Cache+".partial");completed++;active=null;
                }
                while(jobs.Count>0)
                {
                    active=jobs.Dequeue();
                    if(File.Exists(active.Cache))
                    {
                        try{ValidateAndReceipt(active.Cache,active);completed++;active=null;continue;}
                        catch(InvalidDataException){File.Delete(active.Cache);} // Re-download corrupt cache against its retained receipt.
                    }
                    Directory.CreateDirectory(CacheRoot);request=new UnityWebRequest(active.Url,UnityWebRequest.kHttpVerbGET);
                    request.timeout=90;request.SetRequestHeader("User-Agent","HarvestGroundArt/1.0");request.downloadHandler=new DownloadHandlerFile(active.Cache+".partial"){removeFileOnAbort=true};request.SendWebRequest();return;
                }
                Stop();PackSources();ApplyPacked();AssetDatabase.SaveAssets();SceneView.RepaintAll();
                Debug.Log("Ground scan installation complete. Matching generated material slots now use scanned gravel and soil with packed height relief. Review in Game view and rebake lighting/reflections.");
            }
            catch(Exception error){Cancel();Debug.LogWarning("Ground scans were not applied: "+error.Message+". Generated detail remains available; retry Harvest > Materials > Install Ground Scans.");}
        }
        static void ValidateAndReceipt(string path,Job job)
        {
            Texture2D image=null;
            try
            {
                try{image=FarmhouseMaterialLibrary.ReadLinearImage(path);}
                catch(Exception error){throw new InvalidDataException("Cannot decode ground source: "+job.Id+" / "+job.Role,error);}
                if(image.width!=2048||image.height!=2048)throw new InvalidDataException("Expected a 2K source: "+job.Id+" / "+job.Role);
            }
            finally{if(image!=null)Object.DestroyImmediate(image);}
            string sha;using(var stream=File.OpenRead(path))using(var hash=SHA256.Create())sha=BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
            var receipt=new Receipt{Url=job.Url,Sha256=sha,Bytes=new FileInfo(path).Length};string stamp=job.Cache+".json";
            if(File.Exists(stamp))
            {
                var old=JsonUtility.FromJson<Receipt>(File.ReadAllText(stamp));
                if(old==null||old.Url!=job.Url||old.Sha256!=sha||old.Bytes!=receipt.Bytes)throw new InvalidDataException("Ground source differs from its local verified receipt: "+job.Id+" / "+job.Role);
            }
            else File.WriteAllText(stamp,JsonUtility.ToJson(receipt,true));
        }
        static void PackSources()
        {
            FarmhouseMeshLibrary.EnsureFolder(Folder);
            for(int i=0;i<Ids.Length;i++)
            {
                if(AssetDatabase.LoadAssetAtPath<Texture2D>(PackedPath(i==0))!=null)continue;
                Texture2D albedo=null,height=null,packed=null;
                try
                {
                    albedo=FarmhouseMaterialLibrary.ReadLinearImage(Path.Combine(CacheRoot,Ids[i]+"_diff.jpg"));height=FarmhouseMaterialLibrary.ReadLinearImage(Path.Combine(CacheRoot,Ids[i]+"_disp.png"));
                    Color32[] pixels=albedo.GetPixels32(),heights=height.GetPixels32();
                    for(int p=0;p<pixels.Length;p++)pixels[p].a=heights[p].r; // Height remains linear in alpha; diffuse RGB is sampled as sRGB.
                    packed=new Texture2D(2048,2048,TextureFormat.RGBA32,true,false){name=Ids[i]+" ground albedo and height",wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=8};
                    packed.SetPixels32(pixels);packed.Apply(true,false);AssetDatabase.CreateAsset(packed,PackedPath(i==0));packed=null;
                }
                finally{if(albedo!=null)Object.DestroyImmediate(albedo);if(height!=null)Object.DestroyImmediate(height);if(packed!=null)Object.DestroyImmediate(packed);}
            }
        }
        public static void ApplyPacked()
        {
            var gravel=AssetDatabase.LoadAssetAtPath<Texture2D>(PackedPath(true));var soil=AssetDatabase.LoadAssetAtPath<Texture2D>(PackedPath(false));if(gravel==null||soil==null)return;
            foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Harvest/Materials"}))
            {
                var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));if(material.shader==null||material.shader.name!="Harvest/Farm Surface")continue;
                bool changed=false;
                foreach(string slot in new[]{"_SurfaceMap","_GroundMap"})
                {
                    string old=AssetDatabase.GetAssetPath(material.GetTexture(slot));bool isGravel=old==FarmGroundTextures.Path(true),isSoil=old==FarmGroundTextures.Path(false);if(!isGravel&&!isSoil)continue;
                    material.SetTexture(slot,isGravel?gravel:soil);material.SetFloat(slot=="_SurfaceMap"?"_WorldScale":"_GroundScale",1f/(isGravel?2.5f:1.3f));
                    string tint=slot=="_SurfaceMap"?"_BaseColor":"_GroundTint";Color oldTint=material.GetColor(tint);
                    Color original=isGravel?new Color(.38f,.37f,.32f):new Color(.29f,.24f,.17f);
                    bool tyre=slot=="_SurfaceMap"&&material.name=="Farm Natural Tyre Wear";if(tyre)original*=.86f;
                    if((new Vector3(oldTint.r-original.r,oldTint.g-original.g,oldTint.b-original.b)).sqrMagnitude<.000001f)
                    {Color newTint=isGravel?new Color(.90f,.90f,.85f):new Color(.82f,.82f,.78f);if(tyre)newTint*=.86f;newTint.a=oldTint.a;material.SetColor(tint,newTint);}
                    changed=true;
                }
                if(changed){material.SetFloat("_GroundArtRevision",2);EditorUtility.SetDirty(material);}
            }
        }
        static void Stop(){EditorApplication.update-=Tick;AssemblyReloadEvents.beforeAssemblyReload-=Cancel;EditorApplication.quitting-=Cancel;EditorUtility.ClearProgressBar();jobs=null;active=null;}
        static void Cancel()
        {
            string partial=active!=null?active.Cache+".partial":null;
            try{if(request!=null){request.Abort();request.Dispose();request=null;}}
            finally
            {
                Stop();
                if(partial!=null&&File.Exists(partial))
                    try{File.Delete(partial);}catch(IOException error){Debug.LogWarning("Could not remove partial ground download: "+error.Message);}
            }
        }
    }
}
