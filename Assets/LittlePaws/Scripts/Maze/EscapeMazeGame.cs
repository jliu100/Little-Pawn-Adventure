using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace LittlePaws.Maze
{
    public sealed class EscapeMazeGame : MonoBehaviour
    {
        const string SaveKey="LittlePaws.EscapeMaze.Progress.v1";
        enum ScreenMode { Levels, Playing, Won }
        ScreenMode mode;
        MazeProgress progress;
        MazeLevel level;
        Vector2Int cell, held;
        RectTransform safe, boardArea, board, avatar, shadow, controls;
        GameObject selector, playView, modal;
        Text energyText, regenText, levelText, timerText, hint, menuHint, hazardHeading, hazardGuide, dialogTitle, dialogText;
        Button dialogPrimary, dialogSecondary;
        Text primaryLabel, secondaryLabel, zoomLabel;
        readonly List<Button> levelButtons=new List<Button>();
        readonly List<Text> levelLabels=new List<Text>();
        readonly List<Sprite> art=new List<Sprite>();
        Sprite rounded, wall, floor, star, paw;
        MazePet3DView petView;
        RawImage petImage;
        Font font;
        bool moving, paused, zoomed;
        float hazardTime, safeUntil;
        public int SnakeHits { get; private set; }
        readonly List<SnakeUI> snakes=new List<SnakeUI>();
        sealed class SnakeUI
        {
            public SnakePatrol patrol;
            public RectTransform root;
            public RectTransform[] segments;
        }
        float animationTime, elapsed, repeatAt, nextEnergyTick, cellSize;
        Vector2 fromPosition, toPosition;
        int lastEnergy=-1;
        Rect lastSafe;
        int lastWidth,lastHeight;
        readonly Color cream=new Color(.98f,.96f,.89f), ink=new Color(.19f,.28f,.25f), green=new Color(.30f,.51f,.38f), amber=new Color(.93f,.69f,.30f);
        public bool TestingMode { get; private set; }
        public bool Ready=>progress!=null && selector!=null;
        public int Energy=>progress.Energy;
        public int HighestUnlocked=>progress.HighestUnlocked;
        public int PlayingLevel=>level==null?0:level.Number;
        public Vector2Int PlayerCell=>cell;
        public bool IsPlaying=>mode==ScreenMode.Playing;
        public bool IsMoving=>moving;

        void Start()
        {
            Screen.orientation=ScreenOrientation.AutoRotation;
            Screen.autorotateToPortrait=false;Screen.autorotateToPortraitUpsideDown=false;
            Screen.autorotateToLandscapeLeft=true;Screen.autorotateToLandscapeRight=true;
            Application.targetFrameRate=60;
#if UNITY_EDITOR
            TestingMode=UnityEditor.EditorPrefs.GetBool("LittlePaws.Maze.AllLevelsForTesting",true)
                && !UnityEditor.SessionState.GetBool("LittlePaws.MazeCheck.Running",false);
#endif
            if(TestingMode)
            {
                progress=MazeProgress.Create(DateTime.UtcNow);
                progress.HighestUnlocked=MazeLevels.Count;
            }
            else
            {
            try{progress=JsonUtility.FromJson<MazeProgress>(PlayerPrefs.GetString(SaveKey,""));}catch{progress=null;}
            if(progress==null)progress=MazeProgress.Create(DateTime.UtcNow);
            }
            progress.Normalize(DateTime.UtcNow);progress.RefreshEnergy(DateTime.UtcNow);Save();
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rounded=Own(MazeArt.Rounded());wall=Own(MazeArt.Wall());floor=Own(MazeArt.Floor());star=Own(MazeArt.Star());paw=Own(MazeArt.Paw());
            petView=new GameObject("Maze 3D companion renderer").AddComponent<MazePet3DView>();
            petView.transform.SetParent(transform,false);
            petView.Initialize(PlayerPrefs.GetInt("LittlePaws.Species",0)==1);
            BuildUI();ShowLevels();
        }
        Sprite Own(Sprite sprite){art.Add(sprite);return sprite;}
        void BuildUI()
        {
            var events=FindAnyObjectByType<EventSystem>();
            if(!events)events=new GameObject("Maze Event System",typeof(EventSystem)).GetComponent<EventSystem>();
            if(!events.GetComponent<InputSystemUIInputModule>())
            {
                var old=events.GetComponent<StandaloneInputModule>();if(old)Destroy(old);
                events.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            var go=new GameObject("Escape Maze UI",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1440,810);scaler.matchWidthOrHeight=.5f;
            var background=Image("Forest backdrop",go.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(5000,3000),new Color(.14f,.27f,.23f));background.raycastTarget=false;
            safe=Rect("Safe area",go.transform,Vector2.zero,Vector2.zero);safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;safe.sizeDelta=Vector2.zero;
            var title=Panel("Title",safe,new Vector2(0,1),new Vector2(24,-20),new Vector2(560,84),cream);
            Label(title,"ESCAPE MAZE",new Vector2(20,-10),new Vector2(450,34),30,FontStyle.Bold);
            Label(title,"Little Paws Adventure / 50 trails to discover",new Vector2(20,-49),new Vector2(500,24),17);
            var energy=Panel("Energy",safe,Vector2.one,new Vector2(-24,-20),new Vector2(400,84),cream);
            energyText=Label(energy,"",new Vector2(18,-10),new Vector2(365,30),24,FontStyle.Bold);
            regenText=Label(energy,"",new Vector2(18,-47),new Vector2(365,25),16);

            selector=Panel("Level selector",safe,new Vector2(.5f,.5f),new Vector2(0,-30),new Vector2(1120,590),cream).gameObject;
            Label(selector.transform,"CHOOSE YOUR TRAIL",new Vector2(28,-18),new Vector2(650,34),25,FontStyle.Bold);
            Label(selector.transform,TestingMode?"TEST MODE: all 50 levels are open. Start and retry freely.":"Complete a level to unlock the next. Every start uses 5 energy.",new Vector2(28,-60),new Vector2(1020,28),18);
            for(int i=0;i<50;i++)
            {
                int number=i+1;
                var button=ActionButton(selector.transform,number.ToString(),new Vector2(28+(i%10)*106,-104-(i/10)*76),new Vector2(98,67),()=>StartLevel(number),out var label);
                levelButtons.Add(button);levelLabels.Add(label);
            }
            menuHint=Label(selector.transform,"",new Vector2(28,-495),new Vector2(850,66),18);
            ActionButton(selector.transform,"Village",new Vector2(905,-535),new Vector2(180,40),LeaveForVillage,out _);

            playView=Rect("Playing screen",safe,new Vector2(.5f,.5f),Vector2.zero).gameObject;
            var pr=playView.GetComponent<RectTransform>();pr.anchorMin=Vector2.zero;pr.anchorMax=Vector2.one;pr.sizeDelta=Vector2.zero;pr.anchoredPosition=Vector2.zero;
            var boardCard=Panel("Maze card",playView.transform,new Vector2(.5f,.5f),new Vector2(-130,-20),new Vector2(960,610),cream);
            levelText=Label(boardCard,"",new Vector2(20,-10),new Vector2(650,32),24,FontStyle.Bold);
            timerText=Label(boardCard,"",new Vector2(735,-15),new Vector2(200,28),18);timerText.alignment=TextAnchor.MiddleRight;
            boardArea=Rect("Maze viewport",boardCard,new Vector2(.5f,.5f),new Vector2(0,-15),new Vector2(910,520));
            boardArea.gameObject.AddComponent<RectMask2D>();
            controls=Panel("Controls",playView.transform,new Vector2(1,0),new Vector2(-24,24),new Vector2(235,370),cream);
            Label(controls,"MOVE",new Vector2(18,-12),new Vector2(200,30),20,FontStyle.Bold);
            DirectionButton("UP",new Vector2(80,-52),Vector2Int.up);
            DirectionButton("LEFT",new Vector2(12,-113),Vector2Int.left);
            DirectionButton("RIGHT",new Vector2(148,-113),Vector2Int.right);
            DirectionButton("DOWN",new Vector2(80,-174),Vector2Int.down);
            hazardHeading=Label(controls,"FIND THE EXIT",new Vector2(16,-242),new Vector2(205,26),17,FontStyle.Bold);
            hazardGuide=Label(controls,"Follow the open paths.\nKeyboard: arrows / WASD",new Vector2(16,-281),new Vector2(205,75),15);
            hint=Label(playView.transform,"",Vector2.zero,new Vector2(1020,48),18);var hr=hint.rectTransform;hr.anchorMin=hr.anchorMax=new Vector2(0,0);hr.pivot=Vector2.zero;hr.anchoredPosition=new Vector2(26,16);hint.color=cream;
            var levelsButton=ActionButton(playView.transform,"Levels",Vector2.zero,new Vector2(235,44),()=>ConfirmLeave(false),out _);
            PlaceTopRight(levelsButton.GetComponent<RectTransform>(),new Vector2(-24,-125));
            var retryButton=ActionButton(playView.transform,TestingMode?"Retry / Free test":"Retry / 5 energy",Vector2.zero,new Vector2(235,44),ConfirmRetry,out _);
            PlaceTopRight(retryButton.GetComponent<RectTransform>(),new Vector2(-24,-179));
            var zoomButton=ActionButton(playView.transform,"Zoom in",Vector2.zero,new Vector2(235,44),ToggleZoom,out zoomLabel);
            PlaceTopRight(zoomButton.GetComponent<RectTransform>(),new Vector2(-24,-233));
            playView.SetActive(false);

            modal=Panel("Modal shade",safe,new Vector2(.5f,.5f),Vector2.zero,new Vector2(5000,3000),new Color(0,0,0,.62f)).gameObject;
            var dialog=Panel("Dialog",modal.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(650,320),cream);
            dialogTitle=Label(dialog,"",new Vector2(28,-24),new Vector2(590,50),29,FontStyle.Bold);
            dialogText=Label(dialog,"",new Vector2(28,-90),new Vector2(590,116),20);
            dialogPrimary=ActionButton(dialog,"",new Vector2(28,-240),new Vector2(360,52),null,out primaryLabel);
            dialogSecondary=ActionButton(dialog,"",new Vector2(405,-240),new Vector2(215,52),null,out secondaryLabel);
            modal.SetActive(false);
        }
        void ShowLevels()
        {
            held=Vector2Int.zero;moving=false;progress.AbandonAttempt();mode=ScreenMode.Levels;
            selector.SetActive(true);playView.SetActive(false);modal.SetActive(false);
            menuHint.text=TestingMode?"Try any level. Practice does not change your saved progression or energy.":"1-5: find the path   /   6-10: avoid snakes   /   Level 1 starts unlocked.";
            RefreshEnergyUI(true);Save();
        }
        public bool StartLevel(int number)
        {
            if(mode==ScreenMode.Playing)return false;
            if(!progress.TryStart(number,DateTime.UtcNow))
            {
                string message=number>progress.HighestUnlocked?"Complete the previous level first.":"Not enough energy. Recover 1 point every 5 minutes.";
                if(modal.activeSelf)dialogText.text=message;else menuHint.text=message;
                RefreshEnergyUI(true);return false;
            }
            if(TestingMode)progress.Energy=MazeProgress.MaxEnergy;
            Save();level=MazeLevels.Get(number);cell=level.Start;mode=ScreenMode.Playing;
            elapsed=0;hazardTime=0;safeUntil=.8f;SnakeHits=0;held=Vector2Int.zero;moving=false;repeatAt=0;
            selector.SetActive(false);playView.SetActive(true);modal.SetActive(false);
            zoomed=number>10;zoomLabel.text=zoomed?"See full maze":"Zoom in";
            petView.ResetFacing();
            BuildBoard();
            bool hazards=level.Snakes.Count>0;
            hazardHeading.text=hazards?"AVOID THE SNAKES":"FIND THE EXIT";
            hazardGuide.text=hazards?"Wait for the snake\nto retreat, then cross.\nKeyboard: arrows / WASD":"Follow the open paths.\nKeyboard: arrows / WASD";
            levelText.text="TRAIL "+number.ToString("00")+" / "+(hazards?"SNAKE CROSSING":"FIND THE EXIT");
            hint.text=number==1?"Follow the open path from the paw to the golden star.":number==6?"The snake reaches one crossing, then retreats. Wait before it, then run past when clear.":hazards?"Avoid the patrols. A snake touch sends you to the start without spending more energy.":"Find the golden star. Take your time; there is no time limit.";
            RefreshEnergyUI(true);return true;
        }
        void BuildBoard()
        {
            if(board){board.gameObject.SetActive(false);Destroy(board.gameObject);}
            float fit=Mathf.Min(910f/level.Width,520f/level.Height);
            cellSize=zoomed?Mathf.Max(34,fit*1.4f):fit;
            board=Rect("Maze grid",boardArea,new Vector2(.5f,.5f),Vector2.zero,new Vector2(level.Width*cellSize,level.Height*cellSize));
            for(int y=0;y<level.Height;y++)for(int x=0;x<level.Width;x++)
            {
                var tile=level.TileAt(new Vector2Int(x,y));
                var image=Image("Tile "+x+","+y,board,new Vector2(.5f,.5f),CellPosition(new Vector2Int(x,y)),Vector2.one*cellSize,Color.white);
                image.sprite=tile==MazeTile.Wall?wall:floor;image.raycastTarget=false;
            }
            var start=Image("Start paw",board,new Vector2(.5f,.5f),CellPosition(level.Start),Vector2.one*cellSize*.8f,new Color(.24f,.49f,.38f));start.sprite=paw;start.raycastTarget=false;
            var goal=Image("Goal star",board,new Vector2(.5f,.5f),CellPosition(level.Goal),Vector2.one*cellSize*.88f,Color.white);goal.sprite=star;goal.raycastTarget=false;
            BuildSnakes();
            var shadowImage=Image("Pet shadow",board,new Vector2(.5f,.5f),CellPosition(cell),new Vector2(cellSize*.72f,cellSize*.3f),new Color(0,0,0,.18f));shadowImage.sprite=rounded;shadowImage.raycastTarget=false;shadow=shadowImage.rectTransform;
            avatar=Rect("3D maze companion",board,new Vector2(.5f,.5f),CellPosition(cell),Vector2.one*cellSize*1.25f);
            petImage=avatar.gameObject.AddComponent<RawImage>();petImage.texture=petView.Texture;petImage.raycastTarget=false;
            petImage.enabled=petView.IsReady && petView.Texture.IsCreated();
            FollowPlayer();
        }
        void BuildSnakes()
        {
            snakes.Clear();
            foreach(var patrol in level.Snakes)
            {
                var root=Rect("Patrolling snake",board,new Vector2(.5f,.5f),Vector2.zero,Vector2.one*cellSize);
                var snake=new SnakeUI{patrol=patrol,root=root,segments=new RectTransform[5]};
                for(int i=0;i<5;i++)
                {
                    var segment=Image("Snake body",root,new Vector2(.5f,.5f),Vector2.zero,new Vector2(cellSize*.25f,cellSize*.27f),i%2==0?new Color(.33f,.53f,.18f):new Color(.48f,.68f,.22f));
                    segment.sprite=rounded;segment.type=UnityEngine.UI.Image.Type.Sliced;segment.raycastTarget=false;snake.segments[i]=segment.rectTransform;
                }
                var head=Image("Snake head",root,new Vector2(.5f,.5f),new Vector2(cellSize*.27f,0),new Vector2(cellSize*.39f,cellSize*.36f),new Color(.48f,.69f,.22f));head.sprite=rounded;head.type=UnityEngine.UI.Image.Type.Sliced;head.raycastTarget=false;
                for(int side=-1;side<=1;side+=2)
                {
                    var eye=Image("Snake eye",root,new Vector2(.5f,.5f),new Vector2(cellSize*.33f,side*cellSize*.10f),Vector2.one*cellSize*.10f,Color.white);eye.sprite=rounded;eye.raycastTarget=false;
                    var pupil=Image("Snake pupil",root,new Vector2(.5f,.5f),new Vector2(cellSize*.35f,side*cellSize*.10f),Vector2.one*cellSize*.05f,ink);pupil.sprite=rounded;pupil.raycastTarget=false;
                }
                var tongue=Image("Snake tongue",root,new Vector2(.5f,.5f),new Vector2(cellSize*.48f,0),new Vector2(cellSize*.12f,cellSize*.035f),new Color(.83f,.33f,.31f));tongue.raycastTarget=false;
                snakes.Add(snake);
            }
            DrawSnakes();
        }
        void DrawSnakes()
        {
            foreach(var snake in snakes)
            {
                Vector2 p=snake.patrol.PositionAt(hazardTime),d=snake.patrol.DirectionAt(hazardTime);
                snake.root.anchoredPosition=GridPosition(p);
                snake.root.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
                for(int i=0;i<snake.segments.Length;i++)snake.segments[i].anchoredPosition=new Vector2((-.34f+i*.115f)*cellSize,Mathf.Sin(hazardTime*7-i*.8f)*cellSize*.055f);
            }
        }
        Vector2 GridPosition(Vector2 point)=>new Vector2((point.x+.5f-level.Width*.5f)*cellSize,(point.y+.5f-level.Height*.5f)*cellSize);
        Vector2 VisualCell()=>new Vector2(shadow.anchoredPosition.x/cellSize+level.Width*.5f-.5f,shadow.anchoredPosition.y/cellSize+level.Height*.5f-.5f);
        bool AdvanceSnakes(float dt,Vector2 before,Vector2 after,float movingSeconds)
        {
            bool touched=false;int steps=Mathf.Max(1,Mathf.CeilToInt(dt/.02f));
            foreach(var snake in snakes)
                for(int i=0;i<steps && !touched;i++)
                {
                    float a=(float)i/steps,b=(float)(i+1)/steps;
                    if(hazardTime+b*dt<safeUntil)continue;
                    if(dt>0)a=Mathf.Max(a,(safeUntil-hazardTime)/dt);
                    float pa=movingSeconds>.00001f?Mathf.Clamp01(a*dt/movingSeconds):1;
                    float pb=movingSeconds>.00001f?Mathf.Clamp01(b*dt/movingSeconds):1;
                    touched=MazeHazards.Touches(Vector2.Lerp(before,after,pa),Vector2.Lerp(before,after,pb),snake.patrol.PositionAt(hazardTime+a*dt),snake.patrol.PositionAt(hazardTime+b*dt),.43f);
                }
            hazardTime+=dt;DrawSnakes();return touched;
        }
        void SnakeCaughtYou()
        {
            SnakeHits++;cell=level.Start;moving=false;held=Vector2Int.zero;
            avatar.anchoredPosition=CellPosition(cell);shadow.anchoredPosition=CellPosition(cell);petView.ResetFacing();FollowPlayer();
            safeUntil=hazardTime+1.2f;repeatAt=Time.unscaledTime+.4f;
            hint.text="Snake caught you! Back to start. No extra energy spent.";
        }
        void ToggleZoom()
        {
            if(moving)return;
            zoomed=!zoomed;zoomLabel.text=zoomed?"See full maze":"Zoom in";BuildBoard();
        }
        void FollowPlayer()
        {
            float x=Mathf.Max(0,(board.sizeDelta.x-boardArea.sizeDelta.x)/2);
            float y=Mathf.Max(0,(board.sizeDelta.y-boardArea.sizeDelta.y)/2);
            board.anchoredPosition=new Vector2(Mathf.Clamp(-shadow.anchoredPosition.x,-x,x),Mathf.Clamp(-shadow.anchoredPosition.y,-y,y));
        }
        Vector2 CellPosition(Vector2Int point)=>new Vector2((point.x+.5f-level.Width*.5f)*cellSize,(point.y+.5f-level.Height*.5f)*cellSize);
        void Update()
        {
            if(progress==null)return;
            if(lastSafe!=Screen.safeArea||lastWidth!=Screen.width||lastHeight!=Screen.height)
            {
                lastSafe=Screen.safeArea;lastWidth=Screen.width;lastHeight=Screen.height;
                safe.anchorMin=new Vector2(lastSafe.x/Screen.width,lastSafe.y/Screen.height);safe.anchorMax=new Vector2(lastSafe.xMax/Screen.width,lastSafe.yMax/Screen.height);
            }
            if(Time.unscaledTime>=nextEnergyTick){nextEnergyTick=Time.unscaledTime+.5f;RefreshEnergyUI(false);}
            if(mode!=ScreenMode.Playing||modal.activeSelf||paused)return;
            elapsed+=Time.deltaTime;timerText.text=TimeLabel(Mathf.FloorToInt(elapsed));
            Vector2 previousPosition=VisualCell();bool arrivedAtGoal=false;
            float movingSeconds=moving?Mathf.Min(Time.deltaTime,Mathf.Max(0,.13f-animationTime)):0;
            if(moving)
            {
                animationTime+=Time.deltaTime;float duration=.13f;
                float t=Mathf.Clamp01(animationTime/duration);Vector2 baseline=Vector2.Lerp(fromPosition,toPosition,t);
                avatar.anchoredPosition=baseline;
                shadow.anchoredPosition=baseline;FollowPlayer();
                if(t>=1){moving=false;avatar.anchoredPosition=toPosition;arrivedAtGoal=cell==level.Goal;}
            }
            if(AdvanceSnakes(Time.deltaTime,previousPosition,VisualCell(),movingSeconds)){SnakeCaughtYou();return;}
            if(petImage)petImage.color=hazardTime<safeUntil && Mathf.Sin(hazardTime*25)>0?new Color(1,1,1,.45f):Color.white;
            if(arrivedAtGoal){Win();return;}
            if(moving)return;
            var direction=held;
            var keyboard=Keyboard.current;
            if(keyboard!=null)
            {
                if(keyboard.wKey.isPressed||keyboard.upArrowKey.isPressed)direction=Vector2Int.up;
                else if(keyboard.sKey.isPressed||keyboard.downArrowKey.isPressed)direction=Vector2Int.down;
                else if(keyboard.aKey.isPressed||keyboard.leftArrowKey.isPressed)direction=Vector2Int.left;
                else if(keyboard.dKey.isPressed||keyboard.rightArrowKey.isPressed)direction=Vector2Int.right;
            }
            if(direction!=Vector2Int.zero && Time.unscaledTime>=repeatAt){TryMove(direction);repeatAt=Time.unscaledTime+.14f;}
        }
        public bool TryMove(Vector2Int direction)
        {
            if(mode!=ScreenMode.Playing||moving||modal.activeSelf||paused)return false;
            if(Mathf.Abs(direction.x)+Mathf.Abs(direction.y)!=1)return false;
            petView.Face(direction);
            if(!MazeLevels.TryStep(level,cell,direction,out var landing))
            {
                hint.text="A hedge blocks that way. Try a different direction.";
                return false;
            }
            fromPosition=CellPosition(cell);toPosition=CellPosition(landing);cell=landing;
            moving=true;animationTime=0;
            return true;
        }
        void LateUpdate()
        {
            if(!petView)return;
            if(petImage)
            {
                // RawImage draws a white rectangle when its texture is null.
                petImage.texture=petView.Texture;
                petImage.enabled=petView.IsReady && petView.Texture.IsCreated();
            }
            petView.SetVisible(mode!=ScreenMode.Levels);
            petView.SetMoving(mode==ScreenMode.Playing && moving && !modal.activeSelf && !paused);
        }
        public void HoldDirection(Vector2Int direction){held=direction;repeatAt=0;}
        public void ReleaseDirection(Vector2Int direction){if(held==direction)held=Vector2Int.zero;}
        void Win()
        {
            held=Vector2Int.zero;
            if(!progress.Complete(level.Number))return;
            mode=ScreenMode.Won;Save();RefreshEnergyUI(true);
            bool last=level.Number==50;
            Dialog(last?"All 50 trails complete!":"Level "+level.Number+" complete!",
                "Time: "+TimeLabel(Mathf.FloorToInt(elapsed))+"\n"+(last?"Your little adventurer found every exit.":"Level "+(level.Number+1)+" is now unlocked."),
                last?"Choose a level":TestingMode?"Next level / Free test":"Next level / 5 energy",()=>{if(last)ShowLevels();else StartLevel(level.Number+1);},"Levels",ShowLevels);
        }
        void ConfirmRetry()
        {
            if(mode!=ScreenMode.Playing)return;held=Vector2Int.zero;
            Dialog("Restart this trail?",TestingMode?"Start another practice run for free.":"Starting again costs 5 energy. Your current run will end.",TestingMode?"Restart / Free test":"Restart / 5 energy",()=>
            {
                if(!progress.CanStart(level.Number,DateTime.UtcNow)){dialogText.text="Not enough energy. Your current run is still available.";return;}
                int number=level.Number;progress.AbandonAttempt();mode=ScreenMode.Levels;StartLevel(number);
            },"Keep playing",()=>modal.SetActive(false));
        }
        void ConfirmLeave(bool village)
        {
            if(mode!=ScreenMode.Playing){if(village)LeaveForVillage();else ShowLevels();return;}
            held=Vector2Int.zero;
            Dialog("Leave this trail?",TestingMode?"Return to the list and choose any level.":"The 5 energy used to start this run will not be refunded.","Leave trail",()=>{ShowLevels();if(village)LeaveForVillage();},"Keep playing",()=>modal.SetActive(false));
        }
        void LeaveForVillage(){progress.AbandonAttempt();Save();SceneManager.LoadScene("TownMap");}
        void Dialog(string title,string body,string primary,Action action,string secondary,Action other)
        {
            dialogTitle.text=title;dialogText.text=body;primaryLabel.text=primary;secondaryLabel.text=secondary;
            dialogPrimary.onClick.RemoveAllListeners();dialogPrimary.onClick.AddListener(()=>action());
            dialogSecondary.onClick.RemoveAllListeners();dialogSecondary.onClick.AddListener(()=>other());
            modal.SetActive(true);modal.transform.SetAsLastSibling();
        }
        void RefreshEnergyUI(bool force)
        {
            int old=progress.Energy;if(TestingMode)progress.Energy=MazeProgress.MaxEnergy;progress.RefreshEnergy(DateTime.UtcNow);
            energyText.text=TestingMode?"TEST MODE / ALL 50 OPEN":"ENERGY  "+progress.Energy+" / 50";
            regenText.text=TestingMode?"Free practice / Saved progress stays intact":progress.Energy==50?"Full energy / Each start costs 5":"+1 in "+TimeLabel(progress.SecondsToNextEnergy(DateTime.UtcNow))+" / Each start costs 5";
            if(force||lastEnergy!=progress.Energy)
            {
                for(int i=0;i<levelButtons.Count;i++)
                {
                    int number=i+1;bool unlocked=number<=progress.HighestUnlocked;
                    levelButtons[i].interactable=unlocked && progress.Energy>=5;
                    levelLabels[i].text=number.ToString("00")+"\n"+(!unlocked?"LOCKED":progress.Completed[i]?"DONE":progress.Energy<5?"REST":TestingMode?"TEST":"PLAY");
                    levelButtons[i].targetGraphic.color=!unlocked?new Color(.72f,.75f,.70f):progress.Completed[i]?new Color(.66f,.81f,.66f):new Color(.94f,.76f,.43f);
                }
                lastEnergy=progress.Energy;
            }
            if(old!=progress.Energy)Save();
        }
        static string TimeLabel(int seconds)=>Mathf.Max(0,seconds/60).ToString("00")+":"+Mathf.Max(0,seconds%60).ToString("00");
        void Save(){if(progress!=null && !TestingMode){PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(progress));PlayerPrefs.Save();}}
        void OnApplicationPause(bool value){paused=value;held=Vector2Int.zero;if(progress!=null){progress.RefreshEnergy(DateTime.UtcNow);Save();}}
        void OnApplicationFocus(bool focus){held=Vector2Int.zero;paused=!focus;}
        void OnApplicationQuit(){Save();}
        void OnDestroy(){foreach(var sprite in art)if(sprite){Destroy(sprite.texture);Destroy(sprite);}}

        void PlaceTopRight(RectTransform rect,Vector2 at){rect.anchorMin=rect.anchorMax=Vector2.one;rect.pivot=Vector2.one;rect.anchoredPosition=at;}
        RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size=default(Vector2))
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=r.anchorMax=anchor;r.pivot=anchor;r.anchoredPosition=position;r.sizeDelta=size;return r;
        }
        Image Image(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size,Color color)
        {var r=Rect(name,parent,anchor,position,size);var i=r.gameObject.AddComponent<Image>();i.color=color;return i;}
        RectTransform Panel(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size,Color color)
        {var i=Image(name,parent,anchor,position,size,color);i.sprite=rounded;i.type=UnityEngine.UI.Image.Type.Sliced;return i.rectTransform;}
        Text Label(Transform parent,string text,Vector2 position,Vector2 size,int fontSize,FontStyle style=FontStyle.Normal)
        {
            var r=Rect(text,parent,new Vector2(0,1),position,size);var label=r.gameObject.AddComponent<Text>();
            label.font=font;label.text=text;label.fontSize=fontSize;label.fontStyle=style;label.color=ink;label.raycastTarget=false;return label;
        }
        Button ActionButton(Transform parent,string text,Vector2 position,Vector2 size,Action callback,out Text label)
        {
            var panel=Panel(text,parent,new Vector2(0,1),position,size,amber);var button=panel.gameObject.AddComponent<Button>();button.targetGraphic=panel.GetComponent<Image>();
            button.navigation=new Navigation{mode=Navigation.Mode.None};if(callback!=null)button.onClick.AddListener(()=>callback());
            var colors=button.colors;colors.pressedColor=new Color(.69f,.84f,.68f);colors.disabledColor=new Color(.75f,.77f,.72f,.75f);button.colors=colors;
            label=Label(panel,text,Vector2.zero,size,18,FontStyle.Bold);label.alignment=TextAnchor.MiddleCenter;return button;
        }
        void DirectionButton(string title,Vector2 position,Vector2Int direction)
        {var b=ActionButton(controls,title,position,new Vector2(74,52),null,out _);var hold=b.gameObject.AddComponent<MazeDirectionHold>();hold.owner=this;hold.direction=direction;}
    }
    public sealed class MazeDirectionHold : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        public EscapeMazeGame owner;public Vector2Int direction;
        public void OnPointerDown(PointerEventData e){owner.HoldDirection(direction);}
        public void OnPointerUp(PointerEventData e){owner.ReleaseDirection(direction);}
        public void OnPointerExit(PointerEventData e){owner.ReleaseDirection(direction);}
        void OnDisable(){if(owner)owner.ReleaseDirection(direction);}
    }
}
