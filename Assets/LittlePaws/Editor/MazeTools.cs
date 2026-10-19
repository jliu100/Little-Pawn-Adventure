using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using LittlePaws.Maze;

[InitializeOnLoad]
public static class MazeTools
{
    const string Scene="Assets/Scenes/EscapeMaze.unity", SaveKey="LittlePaws.EscapeMaze.Progress.v1", Running="LittlePaws.MazeCheck.Running";
    static EscapeMazeGame game;
    static int phase;
    static double started, turnStarted;
    static MazePet3DView petView;
    static bool failed;
    static string report;
    static Queue<Move> solution;
    static Transform runningPaw;
    static Quaternion restingPawRotation;
    static Vector3 restingPawPosition;
    static float largestPawSwing, largestPawLift;
    static Vector2Int rearInitialFacing;
    struct Move { public Vector2Int direction;  }
    static MazeTools(){EditorApplication.playModeStateChanged+=Changed;}
    [MenuItem("Little Paws/Testing/Unlock all maze levels")]
    public static void ToggleAllLevels()
    {
        bool enabled=EditorPrefs.GetBool("LittlePaws.Maze.AllLevelsForTesting",true);
        EditorPrefs.SetBool("LittlePaws.Maze.AllLevelsForTesting",!enabled);
    }
    [MenuItem("Little Paws/Testing/Unlock all maze levels",true)]
    public static bool ValidateAllLevels()
    {
        Menu.SetChecked("Little Paws/Testing/Unlock all maze levels",EditorPrefs.GetBool("LittlePaws.Maze.AllLevelsForTesting",true));
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }
    [MenuItem("Little Paws/Open Escape Maze")]
    public static void Open()
    {
        if(!EditorApplication.isPlaying && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(Scene);
    }
    [MenuItem("Little Paws/Run Escape Maze checks")]
    public static void Run(){BeginChecks(false);}
    [MenuItem("Little Paws/Run snake level 6 checks")]
    public static void RunSnake(){BeginChecks(true);}
    [MenuItem("Little Paws/Check snake placement")]
    public static void CheckSnakePlacement()
    {
        for(int number=1;number<=MazeLevels.Count;number++)
        {
            var level=MazeLevels.Get(number);
            Solve(level);
            foreach(var snake in level.Snakes)
            {
                var delta=snake.To-snake.From;
                if(Math.Abs(delta.x)+Math.Abs(delta.y)!=2 || (delta.x!=0&&delta.y!=0))
                    throw new Exception("Expected a short straight snake patrol in level "+number);
                var side=new Vector2Int(Math.Sign(delta.x),Math.Sign(delta.y));
                var along=new Vector2Int(-side.y,side.x);
                if(level.TileAt(snake.To-along)!=MazeTile.Floor || level.TileAt(snake.To+along)!=MazeTile.Floor)
                    throw new Exception("Snake crossing must have safe floor before and after it in level "+number);
                for(var at=snake.From;;at+=side)
                {
                    if(level.TileAt(at)!=MazeTile.Floor || at==level.Start || at==level.Goal)
                        throw new Exception("Unsafe snake placement in level "+number);
                    if(at==snake.To)break;
                }
                // Waiting on A or C is safe throughout an entire patrol cycle.
                for(int tick=0;tick<=100;tick++)
                {
                    var position=snake.PositionAt(tick*4f/(100*snake.CellsPerSecond));
                    if(Vector2.Distance(position,snake.To-along)<.99f || Vector2.Distance(position,snake.To+along)<.99f)
                        throw new Exception("Snake reaches a waiting tile in level "+number);
                }
            }
        }
        Debug.Log("PASS: all 50 mazes are solvable; snakes reach one crossing and leave its approach and exit clear.");
    }
    [MenuItem("Little Paws/Check mud and jumps")]
    public static void CheckMudAndJumps()
    {
        for(int number=1;number<=MazeLevels.Count;number++)
        {
            var level=MazeLevels.Get(number);var route=new List<Vector2Int>{level.Start};var cell=level.Start;
            foreach(var move in Solve(level)) {cell+=move.direction;route.Add(cell);}
            if(((number>=10&&number<=15)||(number>=25&&number<=34))!=(level.MudCount>0))
                throw new Exception("Mud should appear in levels 10-15 and 25-34: "+number);
            for(int i=1;i<route.Count-1;i++)
            {
                if(level.TileAt(route[i])!=MazeTile.Mud)continue;
                var direction=route[i]-route[i-1];
                if(i<2 || i+2>=route.Count || route[i]-route[i-2]!=direction*2 || route[i+2]-route[i]!=direction*2
                    || level.TileAt(route[i-2])!=MazeTile.Floor || level.TileAt(route[i+2])!=MazeTile.Floor)
                    throw new Exception("Puddle needs two straight dry approach and exit tiles in level "+number);
                if(level.TileAt(route[i-1])!=MazeTile.Floor || level.TileAt(route[i+1])!=MazeTile.Floor
                    || !MazeLevels.TryStep(level,route[i-1],direction,true,out var landing) || landing!=route[i+1])
                    throw new Exception("Puddle needs dry take-off and landing in level "+number);
            }
        }
        var effect=new MazeMudEffect();effect.LandOn(MazeTile.Mud,10);
        if(!effect.IsSlowed(12.99f)||effect.IsSlowed(13))throw new Exception("Mud slowdown must last 3 seconds");
        if(Mathf.Abs(MazeMovementRules.StepSeconds(true)-2*MazeMovementRules.StepSeconds(false))>.00001f)
            throw new Exception("Mud should halve running speed");
        effect.Clear();effect.LandOn(MazeTile.Floor,0);
        if(effect.IsSlowed(0))throw new Exception("Jumping onto dry floor should avoid slowdown");
        Debug.Log("PASS: mud in levels 10-15 and 25-34; puddles are jumpable; mud slows running by half for 3 seconds.");
    }
    [MenuItem("Little Paws/Run level 25 dust checks")]
    public static void RunDust(){BeginChecks(false,true);}
    [MenuItem("Little Paws/Check dust preview rules")]
    public static void CheckDustRules()
    {
        var preview=new MazeDustSequence();
        for(int number=1;number<=50;number++)
        {
            preview.Begin(number);
            if(preview.IsPreviewing!=(number>=25&&number<=34) || preview.DustOpacity!=0)
                throw new Exception("Cloud previews should start clear in levels 25-34");
        }
        var cloudMaze=MazeLevels.Get(25);
        if(cloudMaze.Width!=21 || cloudMaze.Height!=15 || cloudMaze.Snakes.Count!=2 || cloudMaze.MudCount!=2)
            throw new Exception("Level 25 should be a medium maze with two snakes and two puddles");
        preview.Begin(25);preview.Advance(MazeDustSequence.OverviewSeconds);
        if(!preview.IsPreviewing || preview.DustOpacity!=0)throw new Exception("Full-map overview must stay clear");
        preview.Advance(MazeDustSequence.TransitionSeconds);
        if(preview.IsPreviewing || preview.DustOpacity!=1)throw new Exception("Dust must fully cover distant paths after preview");
        if(MazeDustSequence.Obscurity(0)!=0 || MazeDustSequence.Obscurity(4)!=1)
            throw new Exception("Dust should reveal nearby paths and hide distant ones");
        Debug.Log("PASS: levels 25-34 start with a clear full map, then transition to nearby visibility in white clouds.");
    }
    [MenuItem("Little Paws/Run level 35 rear view checks")]
    public static void RunRear(){BeginChecks(false,false,true);}
    [MenuItem("Little Paws/Check rear view controls")]
    public static void CheckRearControls()
    {
        foreach(var direction in new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left})
        {
            if(MazeRearControls.TurnLeft(MazeRearControls.TurnLeft(direction))!=-direction
                || MazeRearControls.TurnRight(MazeRearControls.TurnRight(direction))!=-direction
                || MazeRearControls.IsForward(direction,-direction))
                throw new Exception("Turn-around and forward-only controls are inconsistent");
        }
        for(int number=1;number<=50;number++)
        {
            if(MazeRearControls.AppliesTo(number)!=(number>=35))throw new Exception("Rear view should start at level 35");
            if(number<35)continue;
            var level=MazeLevels.Get(number);Solve(level);
            if(!MazeLevels.TryStep(level,level.Start,MazeRearControls.InitialFacing(level),out _))
                throw new Exception("Rear view starts facing a hedge in level "+number);
        }
        Debug.Log("PASS: levels 35-50 use turns and forward movement, with a clear initial heading.");
    }
    static void BeginChecks(bool snake,bool dust=false,bool rear=false)
    {
        if(EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        CheckSnakePlacement();CheckMudAndJumps();CheckDustRules();CheckRearControls();
        SessionState.SetBool(Running,true);SessionState.SetBool("LittlePaws.MazeCheck.HadSave",PlayerPrefs.HasKey(SaveKey));
        SessionState.SetString("LittlePaws.MazeCheck.OldSave",PlayerPrefs.GetString(SaveKey,""));
        SessionState.SetBool("LittlePaws.MazeCheck.SnakeRun",snake);
        SessionState.SetBool("LittlePaws.MazeCheck.DustRun",dust);
        SessionState.SetBool("LittlePaws.MazeCheck.RearRun",rear);
        var initial=MazeProgress.Create(DateTime.UtcNow);if(snake)initial.HighestUnlocked=6;if(dust)initial.HighestUnlocked=25;if(rear)initial.HighestUnlocked=35;
        PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(initial));PlayerPrefs.Save();
        EditorSceneManager.OpenScene(Scene);EditorApplication.isPlaying=true;
    }
    static void Changed(PlayModeStateChange state)
    {
        if(!SessionState.GetBool(Running,false))return;
        if(state==PlayModeStateChange.EnteredPlayMode)
        {
            started=EditorApplication.timeSinceStartup;phase=SessionState.GetBool("LittlePaws.MazeCheck.RearRun",false)?300:SessionState.GetBool("LittlePaws.MazeCheck.DustRun",false)?200:SessionState.GetBool("LittlePaws.MazeCheck.SnakeRun",false)?100:0;failed=false;report="";
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
        if(state==PlayModeStateChange.EnteredEditMode)
        {
            if(SessionState.GetBool("LittlePaws.MazeCheck.HadSave",false))PlayerPrefs.SetString(SaveKey,SessionState.GetString("LittlePaws.MazeCheck.OldSave",""));
            else PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();SessionState.SetBool(Running,false);EditorApplication.update-=Tick;
        }
    }
    static void Check(bool ok,string message){report+=(ok?"PASS: ":"FAIL: ")+message+"\n";if(!ok)failed=true;}
    static void Tick()
    {
        try
        {
            if(!EditorApplication.isPlaying)return;
            if(EditorApplication.timeSinceStartup-started>30){Finish("Runtime check timed out");return;}
            if(phase==300)
            {
                game=UnityEngine.Object.FindAnyObjectByType<EscapeMazeGame>();if(!game||!game.Ready)return;
                Check(game.StartLevel(35)&&game.Energy==45,"Rear-view trail spends 5 energy once");
                Check(game.IsRearView,"Level 35 uses rear view");
                var view=UnityEngine.Object.FindAnyObjectByType<MazeThirdPersonView>();
                Check(view && view.IsReady,"Real 3D maze renderer is ready");
                Check(!game.IsMiniMapMinimized,"Map begins expanded");game.ToggleMiniMap();
                Check(game.IsMiniMapMinimized,"Map can be minimized");game.ToggleMiniMap();
                Check(!game.IsMiniMapMinimized,"Map can be restored");
                rearInitialFacing=game.FacingCellDirection;
                Check(game.TurnLeft()&&game.PlayerCell==MazeLevels.Get(35).Start,"Left turns without moving");
                Check(game.FacingCellDirection==MazeRearControls.TurnLeft(rearInitialFacing),"Left is relative to the pet");
                turnStarted=EditorApplication.timeSinceStartup;phase=301;return;
            }
            if(phase==301)
            {
                if(EditorApplication.timeSinceStartup-turnStarted<.45)return;
                var view=UnityEngine.Object.FindAnyObjectByType<MazeThirdPersonView>();
                Check(view.CameraIsBehindPet,"Camera stays behind the pet through a left turn");
                CheckRearRendered(view.Texture);
                Check(game.TurnRight()&&game.FacingCellDirection==rearInitialFacing,"Right reverses the left turn");
                turnStarted=EditorApplication.timeSinceStartup;phase=302;return;
            }
            if(phase==302)
            {
                if(EditorApplication.timeSinceStartup-turnStarted<.45)return;
                Check(!game.TryMove(-game.FacingCellDirection),"Direct backward movement is rejected");
                Check(game.MoveForward(),"Forward moves in the facing direction");phase=303;return;
            }
            if(phase==303)
            {
                if(game.IsMoving)return;
                Check(game.PlayerCell==MazeLevels.Get(35).Start+rearInitialFacing,"Forward lands in the tile ahead");
                Check(game.Energy==45,"Turns and movement don't cost extra energy");Finish(null);return;
            }
            if(phase==200)
            {
                game=UnityEngine.Object.FindAnyObjectByType<EscapeMazeGame>();if(!game||!game.Ready)return;
                Check(game.StartLevel(25)&&game.Energy==45,"Dust trail spends 5 energy once");
                Check(game.IsPreviewingDust && game.DustOpacity==0,"Level 25 begins with a clear map preview");
                Check(!game.TryMove(Vector2Int.right)&&!game.TryJump(Vector2Int.right),"Movement and jumps wait for the overview");
                Canvas.ForceUpdateCanvases();
                var grid=GameObject.Find("Maze grid").GetComponent<RectTransform>();
                var viewport=grid.parent.GetComponent<RectTransform>();var corners=new Vector3[4];grid.GetWorldCorners(corners);
                var bounds=viewport.rect;bounds.xMin-=.1f;bounds.xMax+=.1f;bounds.yMin-=.1f;bounds.yMax+=.1f;
                foreach(var corner in corners)Check(bounds.Contains(viewport.InverseTransformPoint(corner)),"Entire maze fits inside the clear overview");
                phase=201;return;
            }
            if(phase==201)
            {
                if(game.IsPreviewingDust)return;
                turnStarted=EditorApplication.timeSinceStartup;phase=202;return;
            }
            if(phase==202)
            {
                if(EditorApplication.timeSinceStartup-turnStarted<.1)return;
                var curtain=UnityEngine.Object.FindAnyObjectByType<MazeDustView>();
                Check(curtain && curtain.Opacity>.99f && game.DustOpacity==1,"Dust arrives after the overview");
                var shadow=GameObject.Find("Pet shadow").GetComponent<RectTransform>();
                Vector2 center=curtain.rectTransform.InverseTransformPoint(shadow.position);
                Check(Vector2.Distance(center,curtain.ClearCenter)<.1f,"Clear circle follows the pet on the floor");
                Check(game.Energy==45 && game.PlayerCell==MazeLevels.Get(25).Start,"Preview keeps position and energy unchanged");
                Finish(null);return;
            }
            if(phase==100)
            {
                game=UnityEngine.Object.FindAnyObjectByType<EscapeMazeGame>();if(!game||!game.Ready)return;
                Check(game.StartLevel(6)&&game.Energy==45,"Snake tutorial start costs 5 energy");
                Check(GameObject.Find("Patrolling snake")!=null,"Moving snake is rendered");
                solution=Solve(MazeLevels.Get(6),new Vector2Int(5,4));phase=101;return;
            }
            if(phase==101)
            {
                if(game.SnakeHits>0)
                {
                    Check(game.PlayerCell==MazeLevels.Get(6).Start,"Snake contact returns pet to the start");
                    Check(game.Energy==45&&game.IsPlaying,"Snake contact keeps the run active without extra energy charge");
                    Finish(null);return;
                }
                if(game.IsMoving || solution.Count==0)return;
                var move=solution.Dequeue();Check(game.TryMove(move.direction),"Move toward snake crossing accepted");return;
            }
            if(phase==0)
            {
                game=UnityEngine.Object.FindAnyObjectByType<EscapeMazeGame>();if(!game||!game.Ready)return;
                Check(UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>()!=null,"Standalone maze input system exists");
                Check(game.Energy==50&&game.HighestUnlocked==1,"New game has 50 energy and only level 1 unlocked");
                Check(!game.StartLevel(2)&&game.Energy==50,"Locked start is rejected without spending");
                Check(game.StartLevel(1)&&game.Energy==45,"Level 1 start spends exactly 5 energy");
                Check(!game.StartLevel(1)&&game.Energy==45,"Repeated start callback cannot double charge");
                petView=UnityEngine.Object.FindAnyObjectByType<MazePet3DView>();
                Check(petView && petView.IsReady && petView.Texture!=null,"Live 3D pet renderer is ready");
                Check(GameObject.Find("3D maze companion").GetComponent<UnityEngine.UI.RawImage>().texture==petView.Texture,"Maze displays the live 3D texture");
                petView.Face(Vector2Int.left);turnStarted=EditorApplication.timeSinceStartup;phase=10;return;
            }
            if(phase>=10 && EditorApplication.timeSinceStartup-turnStarted>=.3)
            {
                var model=petView.GetComponentInChildren<LittlePaws.PetVisual>();
                Vector3 wanted=phase==10?Vector3.left:phase==11?Vector3.right:Vector3.back;
                Check(Vector3.Dot(model.transform.forward,wanted)>.98f,"3D model turns "+(phase==10?"left":phase==11?"right":"toward viewer"));
                if(phase==10){CheckRenderedPet();petView.Face(Vector2Int.right);phase=11;turnStarted=EditorApplication.timeSinceStartup;return;}
                if(phase==11){petView.ResetFacing();phase=12;turnStarted=EditorApplication.timeSinceStartup;return;}
                solution=Solve(MazeLevels.Get(1));phase=1;
                runningPaw=model.transform.Find("Pet model/Leg 0");
                Check(runningPaw!=null,"Run animation has an articulated front paw");
                if(runningPaw){restingPawRotation=runningPaw.localRotation;restingPawPosition=runningPaw.localPosition;}
                largestPawSwing=0;largestPawLift=0;return;
            }
            if(phase==1)
            {
                if(game.IsMoving)
                {
                    if(runningPaw)
                    {
                        largestPawSwing=Mathf.Max(largestPawSwing,Quaternion.Angle(restingPawRotation,runningPaw.localRotation));
                        largestPawLift=Mathf.Max(largestPawLift,Vector3.Distance(restingPawPosition,runningPaw.localPosition));
                    }
                    return;
                }
                if(solution.Count>0)
                {
                    var move=solution.Dequeue();Check(game.TryMove(move.direction),"Tutorial move accepted");return;
                }
                if(game.IsPlaying)return;
                Check(largestPawSwing>20 && largestPawLift>.03f,"Pet visibly swings and lifts its paws while moving through the maze");
                Check(game.HighestUnlocked==2&&game.Energy==45,"Goal completion unlocks level 2 without a second charge");
                Check(game.StartLevel(2)&&game.Energy==40,"Next level costs 5 energy");
                var saved=JsonUtility.FromJson<MazeProgress>(PlayerPrefs.GetString(SaveKey));
                Check(saved.HighestUnlocked==2&&saved.Completed[0]&&saved.Energy==40,"Unlock, completion, and energy are saved");
                Finish(null);
            }
        }
        catch(Exception e){Finish(e.ToString());}
    }
    static void CheckRearRendered(RenderTexture target)
    {
        var previous=RenderTexture.active;Texture2D pixels=null;
        try
        {
            RenderTexture.active=target;pixels=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();var colors=pixels.GetPixels32();
            int different=0;var background=colors[0];
            foreach(var color in colors)
                if(Mathf.Abs(color.r-background.r)+Mathf.Abs(color.g-background.g)+Mathf.Abs(color.b-background.b)>30)different++;
            Check(different>500,"Rear view renders geometry rather than a blank rectangle");
        }
        finally{RenderTexture.active=previous;if(pixels)UnityEngine.Object.Destroy(pixels);}
    }
    static void CheckRenderedPet()
    {
        var previous=RenderTexture.active;
        Texture2D pixels=null;
        try
        {
            var target=petView.Texture;RenderTexture.active=target;
            pixels=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();
            int opaque=0,transparent=0;
            foreach(var pixel in pixels.GetPixels32()){if(pixel.a>200)opaque++;if(pixel.a<10)transparent++;}
            Check(opaque>200 && transparent>200,"3D pet renders visible geometry over a transparent background");
        }
        finally{RenderTexture.active=previous;if(pixels)UnityEngine.Object.Destroy(pixels);}
    }
    static Queue<Move> Solve(MazeLevel level,Vector2Int? target=null)
    {
        var goal=target??level.Goal;
        var queue=new Queue<Vector2Int>();var seen=new HashSet<Vector2Int>();
        var parent=new Dictionary<Vector2Int,Vector2Int>();var moves=new Dictionary<Vector2Int,Move>();
        queue.Enqueue(level.Start);seen.Add(level.Start);
        var directions=new[]{Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
        while(queue.Count>0)
        {
            var from=queue.Dequeue();if(from==goal)break;
            foreach(var direction in directions)
                if(MazeLevels.TryStep(level,from,direction,out var next)&&seen.Add(next))
                {parent[next]=from;moves[next]=new Move{direction=direction};queue.Enqueue(next);}
        }
        if(!seen.Contains(goal))throw new Exception("No safe route in maze level "+level.Number);
        var reverse=new Stack<Move>();for(var p=goal;p!=level.Start;p=parent[p])reverse.Push(moves[p]);
        return new Queue<Move>(reverse);
    }
    static void Finish(string error)
    {
        if(error!=null){failed=true;report+="FAIL: "+error+"\n";}
        Directory.CreateDirectory("Library/LittlePawsChecks");
        File.WriteAllText(SessionState.GetBool("LittlePaws.MazeCheck.RearRun",false)?"Library/LittlePawsChecks/RearResults.txt":SessionState.GetBool("LittlePaws.MazeCheck.DustRun",false)?"Library/LittlePawsChecks/DustResults.txt":SessionState.GetBool("LittlePaws.MazeCheck.SnakeRun",false)?"Library/LittlePawsChecks/SnakeResults.txt":"Library/LittlePawsChecks/EscapeMazeResults.txt",(failed?"FAILED\n":"PASSED\n")+report);
        Debug.Log("Escape Maze checks\n"+report);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;
    }
}
