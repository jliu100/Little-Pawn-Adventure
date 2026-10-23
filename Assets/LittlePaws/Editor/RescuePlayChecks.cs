using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using LittlePaws.Rescue;

/// <summary>Runs real scene/UI checks and renders previews with Unity's own UI renderer.</summary>
[InitializeOnLoad]
public static class RescuePlayChecks
{
    const string Key="LittlePaws.RescueChecks.Running";
    static double next;
    static int phase;
    static RescuePlayChecks(){EditorApplication.update+=Update;}
    [MenuItem("Little Paws/Run Rescue scene checks")]
    public static void Run()
    {
        AssetDatabase.ImportAsset("Assets/LittlePaws/Resources/LittlePaws/Rescue/Heroes/RoosterKnight.png",ImportAssetOptions.ForceUpdate);
        AssetDatabase.ImportAsset("Assets/LittlePaws/Resources/LittlePaws/Rescue/Heroes/HenKnight.png",ImportAssetOptions.ForceUpdate);
        Require(RescueHeroAssets.Load(false)!=null&&RescueHeroAssets.Load(true)!=null,"Both approved hero sprites imported");
        RescueCombatChecks.Run();RescueArtChecks.Export();
        EditorSceneManager.OpenScene("Assets/Scenes/RescueEgg.unity");
        SessionState.SetBool(Key,true);SessionState.SetInt(Key+"Phase",0);
        EditorApplication.isPlaying=true;
    }
    public static void RunBatch(){SessionState.SetBool(Key+"Batch",true);Run();}
    static void Update()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.timeSinceStartup<next)return;
        phase=SessionState.GetInt(Key+"Phase",0);
        var game=UnityEngine.Object.FindAnyObjectByType<RescueEggGame>();
        if(!game&&phase<3)return;
        try
        {
            next=EditorApplication.timeSinceStartup+.5;
            phase=SessionState.GetInt(Key+"Phase",0);
            if(phase==0)
            {
                var buttons=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None);
                Require(buttons.Count(b=>b.name=="LOCKED"&&!b.interactable)==2,"Levels2 and3 locked");
                Require(buttons.Count(b=>b.name=="CHOOSE YOUR HERO"&&b.interactable)==1,"Level1 playable");
                Capture("01-levels");Click("CHOOSE YOUR HERO");
            }
            else if(phase==1){Capture("02-hero-designs");
                var hen=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.GetComponentInChildren<Text>()&&b.GetComponentInChildren<Text>().text=="SELECT HEN");hen.onClick.Invoke();game.StartRound();}
            else if(phase==2)
            {
                Require(game.IsRunning&&game.Combat.HeroHealth==50,"New round starts at50 health");
                Require(game.Combat.Enemies.Count==1,"Single roach test");
                typeof(RescueEggGame).GetField("paused",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(game,true);
                Require(UnityEngine.Object.FindAnyObjectByType<RescueHeroGraphic>()!=null,"Gameplay uses approved character artwork");
                Capture("03-first-stage");
                var c=game.Combat;c.HeroPosition=new Vector2(5,2);c.Enemies[0].Position=new Vector2(5.6f,2);
                c.TryAttack();c.Tick(.1f,Vector2.zero);
                typeof(RescueEggGame).GetField("attackAge",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(game,.09f);
                game.SendMessage("Render",(object)0f);Capture("04-sword-hit");
                c.Tick(.5f,Vector2.zero);game.SendMessage("Render",(object)0f);Capture("05-roach-down");
                for(int i=0;i<c.Enemies.Count;i++)c.Enemies[i].Health=0;
                game.SendMessage("Render",(object)0f);Capture("06-go-right");
                c.HeroPosition=new Vector2(c.StageEnd,2);c.Tick(.1f,Vector2.right);game.SendMessage("SyncStage");game.SendMessage("Render",(object)0f);
                Require(c.StageIndex==1,"Walking beyond clear stage opens next");
                Capture("07-second-stage");
                c.Enemies[0].Health=0;c.HeroPosition=new Vector2(c.StageEnd,2);c.Tick(.1f,Vector2.right);
                game.SendMessage("SyncStage");game.SendMessage("Render",(object)0f);
                c.Enemies[0].Health=0;c.HeroPosition=new Vector2(c.StageEnd,2);c.Tick(.1f,Vector2.right);
                Require(c.IsVictory,"All three cleared stages rescue egg");game.SendMessage("Finish");Capture("08-egg-rescued");
                game.StartRound();typeof(RescueEggGame).GetField("paused",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(game,true);
                c=game.Combat;c.HeroPosition=new Vector2(6,2);c.Enemies[0].Position=c.HeroPosition;c.Tick(3.1f,Vector2.zero);
                Require(c.HeroHealth==0&&c.IsGameOver,"Three enemy hits end round");game.SendMessage("Render",(object)0f);game.SendMessage("Finish");Capture("09-defeat");
                typeof(RescueEggGame).GetMethod("SelectHero",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(game,new object[]{false});
                game.StartRound();typeof(RescueEggGame).GetField("paused",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(game,true);
                Capture("10-rooster-gameplay");
                game.Combat.Tick(.1f,Vector2.left);game.SendMessage("Render",(object)0f);Capture("11-rooster-facing-left");
                SceneManager.LoadScene("TownMap");
            }
            else if(phase==3)
            {
                var town=UnityEngine.Object.FindAnyObjectByType<LittlePaws.PetHomeGame>();if(!town)return;
                Require(UnityEngine.Object.FindObjectsByType<LittlePaws.TownLocation>(FindObjectsSortMode.None).Count(p=>p.Id=="rescue")==1,"New rescue destination exists");
                Require(town.Visit("rescue"),"Navigation accepts rescue gate");
                SessionState.SetFloat(Key+"WalkStart",(float)EditorApplication.timeSinceStartup);
            }
            else if(phase==4)
            {
                var town=UnityEngine.Object.FindAnyObjectByType<LittlePaws.PetHomeGame>();
                if(town.IsWalking)
                {
                    Require(EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+"WalkStart",0)<20,"Rescue gate walk completes");return;
                }
                var entrance=UnityEngine.Object.FindObjectsByType<LittlePaws.TownLocation>(FindObjectsSortMode.None).First(p=>p.Id=="rescue");
                Require(Vector3.Distance(town.PetPosition,entrance.Entrance)<.05f,"Hero arrives at rescue gate");
                var enter=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).First(b=>b.name=="Rescue the egg"&&b.GetComponentInChildren<Text>()&&b.GetComponentInChildren<Text>().text=="Rescue the egg"&&b.transform.parent.name=="Location details");
                Require(enter.gameObject.activeInHierarchy,"Arrival exposes rescue scene entry");enter.onClick.Invoke();
            }
            else if(phase==5)
            {
                Require(game!=null&&!game.IsRunning,"Map entry opens Rescue level selector");
                File.WriteAllText(Path.GetFullPath("Exports/RescueDesigns/scene-checks.txt"),"PASS: scene/UI, level locks, hero choice, combat visuals, stage transitions, victory/defeat, and real village navigation/entry.\n");
                Debug.Log("PASS: Rescue the Egg scene, victory/defeat, village entry and eleven screenshots.");
                SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;
                if(SessionState.GetBool(Key+"Batch",false)){SessionState.SetBool(Key+"Batch",false);EditorApplication.delayCall+=()=>EditorApplication.Exit(0);}
                return;
            }
            SessionState.SetInt(Key+"Phase",phase+1);
        }
        catch(Exception e)
        {
            Debug.LogException(e);SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;
            if(SessionState.GetBool(Key+"Batch",false))EditorApplication.delayCall+=()=>EditorApplication.Exit(1);
        }
    }
    static void Click(string name)
    {
        var button=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).First(b=>b.name==name);
        button.onClick.Invoke();
    }
    static void Capture(string name)
    {
        Directory.CreateDirectory("Exports/RescueDesigns");
        var canvas=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c=>c.name=="Rescue the Egg canvas");
        var cameraObject=new GameObject("Screenshot camera",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=5;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.1f,.2f,.15f);
        var target=new RenderTexture(1440,810,24);camera.targetTexture=target;
        var previousMode=canvas.renderMode;var previousCamera=canvas.worldCamera;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        Canvas.ForceUpdateCanvases();
        var game=UnityEngine.Object.FindAnyObjectByType<RescueEggGame>();
        if(game&&game.Combat!=null) game.SendMessage("Render",(object)0f);
        Canvas.ForceUpdateCanvases();
        var heroGraphic=UnityEngine.Object.FindObjectsByType<RescueHeroGraphic>(FindObjectsSortMode.None).FirstOrDefault(h=>h.IsActive());
        if(heroGraphic)
        {
            var mesh=heroGraphic.canvasRenderer.GetMesh();
            Require(mesh&&mesh.vertexCount>0,"Approved hero produces a visible UI mesh");
            Require(!heroGraphic.canvasRenderer.cull,"Approved hero is not culled");
        }
        camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
        var texture=new Texture2D(1440,810,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1440,810),0,0);texture.Apply();
        File.WriteAllBytes("Exports/RescueDesigns/"+name+".png",texture.EncodeToPNG());
        canvas.renderMode=previousMode;canvas.worldCamera=previousCamera;RenderTexture.active=previous;camera.targetTexture=null;
        UnityEngine.Object.DestroyImmediate(texture);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception("Rescue scene check failed: "+message);}
}
