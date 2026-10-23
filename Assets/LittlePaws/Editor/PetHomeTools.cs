using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using LittlePaws;

[InitializeOnLoad]
public static class PetHomeTools
{
    const string Scene = "Assets/Scenes/PetHome.unity";
    const string Running = "LittlePaws.SmokeRunning";
    static PetHomeGame game;
    static int phase;
    static double start;
    static Vector3 before;
    static bool failed;
    static string details="";
    static readonly Vector3[] destinations={new Vector3(1.5f,0,-1.1f),new Vector3(-1.2f,0,-1.1f),new Vector3(-1,0,.5f),new Vector3(0,0,-.45f)};
    static PetHomeTools()
    {
        EditorApplication.playModeStateChanged += Changed;
        EditorApplication.update += CheckRequest;
    }
    [MenuItem("Little Paws/Open Pet Home")]
    public static void Open()
    {
        if(EditorApplication.isPlaying)return;
        if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(Scene);
    }
    [MenuItem("Little Paws/Open Village Map")]
    public static void OpenMap()
    {
        if(EditorApplication.isPlaying)return;
        if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene("Assets/Scenes/TownMap.unity");
    }
    [MenuItem("Little Paws/Run village map checks")]
    public static void RunMap()
    {
        if(EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        StartRun(true);
    }
    [MenuItem("Little Paws/Run play-mode smoke checks")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)return;
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        StartRun();
    }
    static void CheckRequest()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        const string request="Library/LittlePaws.RunSmoke";
        if(!File.Exists(request))return;
        // Never discard a user's unsaved scene to run automated checks.
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)return;
        File.Delete(request);StartRun();
    }
    static void StartRun(bool map=false)
    {
        SessionState.SetBool(Running,true);
        SessionState.SetBool("LittlePaws.TestTown",map);
        SessionState.SetInt("LittlePaws.OldSpecies",PlayerPrefs.GetInt("LittlePaws.Species",0));
        SessionState.SetFloat("LittlePaws.OldKittenClean",PlayerPrefs.GetFloat("LittlePaws.Clean.Kitten",35));
        SessionState.SetFloat("LittlePaws.OldPuppyClean",PlayerPrefs.GetFloat("LittlePaws.Clean.Puppy",35));
        EditorSceneManager.OpenScene(map?"Assets/Scenes/TownMap.unity":Scene);EditorApplication.isPlaying=true;
    }
    static void Changed(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Running,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            phase=SessionState.GetBool("LittlePaws.TestTown",false)?100:0;mapIndex=0;failed=false;checkedEarly=false;details="";start=EditorApplication.timeSinceStartup;
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            PlayerPrefs.SetInt("LittlePaws.Species",SessionState.GetInt("LittlePaws.OldSpecies",0));
            PlayerPrefs.SetFloat("LittlePaws.Clean.Kitten",SessionState.GetFloat("LittlePaws.OldKittenClean",35));
            PlayerPrefs.SetFloat("LittlePaws.Clean.Puppy",SessionState.GetFloat("LittlePaws.OldPuppyClean",35));
            PlayerPrefs.Save();SessionState.SetBool(Running,false);
        }
    }
    static void Require(bool condition,string description)
    { details+=(condition?"PASS: ":"FAIL: ")+description+"\n";if(!condition)failed=true; }
    static void Tick()
    {
        try
        {
            if(!EditorApplication.isPlaying)return;
            double elapsed=EditorApplication.timeSinceStartup-start;
            if(phase==100)
            {
                game=UnityEngine.Object.FindAnyObjectByType<PetHomeGame>();
                if(!game || !game.townMap || !GameObject.Find("Companion")){if(elapsed>15)Finish("Village initialization timeout");return;}
                if(elapsed<.5)return;
                Require(UnityEngine.Object.FindObjectsByType<TownLocation>().Length==7,"Seven village destinations exist");
                Require(game.TryTapFloor(Camera.main.WorldToScreenPoint(new Vector3(0,0,-9.3f))),"Village ground accepts floor raycast");
                Require(game.TryTapPlace(Camera.main.WorldToScreenPoint(new Vector3(0,1.2f,6))),"Tapping forge selects its entrance");
                Require(game.Visit(mapIds[0]),"Home entrance reachable");phase=101;start=EditorApplication.timeSinceStartup;return;
            }
            if(phase==101)
            {
                if(game.IsWalking){if(elapsed>15)Finish("Village walking timed out at "+mapIds[mapIndex]);return;}
                TownLocation destination=null;
                foreach(var place in UnityEngine.Object.FindObjectsByType<TownLocation>())if(place.Id==mapIds[mapIndex])destination=place;
                Require(destination && Vector3.Distance(game.PetPosition,destination.Entrance)<.02f,"Arrived at "+mapIds[mapIndex]);
                if(mapIds[mapIndex]=="garden")
                {
                    Require(game.GardenIsOpen,"Arriving opens the Garden preview");
                    Require(UnityEngine.Object.FindAnyObjectByType<GardenWorldView>()!=null,"Garden fields and one scarecrow exist");
                    game.CloseGarden();Require(!game.GardenIsOpen,"Garden closes back to village");
                }
                mapIndex++;
                if(mapIndex==mapIds.Length){Finish(null);return;}
                Require(game.Visit(mapIds[mapIndex]),mapIds[mapIndex]+" entrance reachable");start=EditorApplication.timeSinceStartup;return;
            }
            if(phase==0)
            {
                game=UnityEngine.Object.FindAnyObjectByType<PetHomeGame>();if(!game||!GameObject.Find("Companion")){if(elapsed>15)Finish("Initialization timeout");return;}
                Require(GameObject.Find("Draggable soap")!=null,"Draggable 2D soap exists");
                Require(!game.StatusVisible,"Status bars start hidden");
                Require(!PlayerSettings.allowedAutorotateToPortrait&&!PlayerSettings.allowedAutorotateToPortraitUpsideDown,"Portrait orientations disabled");
                Require(UnityEngine.Object.FindAnyObjectByType<PetVisual>()!=null,"3D pet model exists");
                if(elapsed<.5)return;
                Require(game.TryWalkTo(new Vector3(1.5f,0,-1.1f)),"Generated-home navigation accepts route from spawn");
                Require(GameObject.Find("Movement")==null,"Arrow controls removed");
                Require(!game.TryTapFloor(Camera.main.WorldToScreenPoint(new Vector3(9,0,-1))),"Off-floor taps rejected");
                Require(!game.TryTapFloor(Camera.main.WorldToScreenPoint(new Vector3(-3.69f,.4f,-.22f))),"Furniture taps rejected");
                Require(!game.TryWalkTo(new Vector3(-3.69f,0,-.22f)),"Box footprint rejected");
                var panel=GameObject.Find("Header").GetComponent<RectTransform>();
                Require(!game.TryTapFloor(RectTransformUtility.WorldToScreenPoint(null,panel.TransformPoint(panel.rect.center))),"UI taps rejected");
                phase=1;before=game.PetPosition;
                Require(game.TryTapFloor(Camera.main.WorldToScreenPoint(destinations[0])),"Clear floor tap accepted");
                start=EditorApplication.timeSinceStartup;return;
            }
            if(phase>=1&&phase<=4)
            {
                if(game.IsWalking){if(elapsed>8)Finish("Walking timed out");return;}
                Require(Vector3.Distance(game.PetPosition,destinations[phase-1])<.02f,"Arrived at floor destination "+phase);
                if(phase<4){before=game.PetPosition;Require(game.TryTapFloor(Camera.main.WorldToScreenPoint(destinations[phase])),"New floor destination accepted");phase++;start=EditorApplication.timeSinceStartup;return;}
                phase=5;start=EditorApplication.timeSinceStartup;
                var e=new PointerEventData(EventSystem.current){pointerId=99,position=Vector2.zero};game.BeginSoap(e);Require(!game.TryWalkTo(new Vector3(1,0,0)),"Walking disabled while dragging soap");return;
            }
            if(phase==5)
            {
                var camera=Camera.main;Vector2 center=camera.WorldToScreenPoint(game.PetPosition+Vector3.up*.85f);
                var e=new PointerEventData(EventSystem.current){pointerId=99,position=center+new Vector2(Mathf.Sin((float)EditorApplication.timeSinceStartup*25)*25,0)};
                game.DragSoap(e);
                if(elapsed<1.8)RequireOnceHidden();
                if(game.WashProgress>=PetHomeGame.WashDuration)
                {
                    Require(game.StatusVisible,"Status bars revealed after 2.5 seconds of rubbing");
                    Require(game.Cleanliness>=95,"Rubbing improves cleanliness");game.EndSoap(e);
                    phase=6;start=EditorApplication.timeSinceStartup;return;
                }
                if(elapsed>15)Finish("Soap timer did not complete");return;
            }
            if(phase==6)
            {
                // Validate the second model and switching without relying on screen coordinates.
                typeof(PetHomeGame).GetMethod("Switch",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(game,new object[]{!game.Puppy});
                Require(!game.StatusVisible&&game.WashProgress==0,"Switching companion resets bath session");
                Require(GameObject.Find("Draggable soap").transform.parent.name=="Soap dock","Soap returns to its dock");
                phase=7;start=EditorApplication.timeSinceStartup;return;
            }
            if(phase==7 && elapsed>=1)
            {
                var companion=GameObject.Find("Companion").transform;
                var towardViewer=Camera.main.transform.position-companion.position;towardViewer.y=0;
                Require(Vector3.Dot(companion.forward,towardViewer.normalized)>.98f,"Resting pet faces the camera after switching");
                Vector2 petScreen=Camera.main.WorldToScreenPoint(companion.position+Vector3.up*.9f);
                Require(game.TryTapPet(petScreen)&&game.StatusVisible,"Tapping companion reveals status without washing");
                Require(!game.TryTapFloor(petScreen)&&!game.IsWalking,"Pet tap does not start floor movement");
                Require(game.WashProgress==0,"Pet tap does not count as washing");
                typeof(PetHomeGame).GetMethod("Switch",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(game,new object[]{!game.Puppy});
                Require(game.TryTapPet(petScreen)&&game.StatusVisible,"Pet-tap status works for both species");
                Finish(null);
            }
        }
        catch(Exception ex){Finish(ex.ToString());}
    }
    static int mapIndex;
    static readonly string[] mapIds={"home","weapon","hospital","food","adventure","garden","rescue"};
    static bool checkedEarly;
    static void RequireOnceHidden(){if(checkedEarly)return;checkedEarly=true;Require(!game.StatusVisible,"Status remains hidden before washing completes");}
    static void Finish(string error)
    {
        if(error!=null){failed=true;details+="FAIL: "+error+"\n";}
        Directory.CreateDirectory("Library/LittlePawsChecks");
        File.WriteAllText(SessionState.GetBool("LittlePaws.TestTown",false)?"Library/LittlePawsChecks/VillageResults.txt":"Library/LittlePawsChecks/Results.txt",(failed?"FAILED\n":"PASSED\n")+details);
        Debug.Log("Little Paws smoke checks: "+(failed?"FAILED":"PASSED")+"\n"+details);
        EditorApplication.update-=Tick;EditorApplication.isPlaying=false;
    }
}
