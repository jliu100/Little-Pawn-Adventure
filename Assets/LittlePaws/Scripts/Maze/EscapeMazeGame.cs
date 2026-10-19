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
        Text primaryLabel, secondaryLabel, zoomLabel, jumpLabel, movementStatus;
        Button jumpButton, zoomButton, levelsButton, retryButton;
        Text levelsButtonLabel, retryButtonLabel;
        readonly Dictionary<Vector2Int,Button> directionButtons=new Dictionary<Vector2Int,Button>();
        readonly Dictionary<Vector2Int,Text> directionLabels=new Dictionary<Vector2Int,Text>();
        MazeThirdPersonView rearView;
        RawImage rearImage, miniMapImage;
        RectTransform miniMapPanel, miniMapArea, miniMapPlayer;
        Text miniMapLabel;
        Texture2D miniMapTexture;
        bool miniMapMinimized;
        Vector2Int rearFacing=Vector2Int.up;
        float nextTurnAt;
        CanvasGroup movementControls, overviewMarkers;
        RectTransform overviewStart, overviewGoal;
        MazeDustView dust;
        readonly MazeDustSequence dustSequence=new MazeDustSequence();
        readonly List<Button> levelButtons=new List<Button>();
        readonly List<Text> levelLabels=new List<Text>();
        readonly List<Sprite> art=new List<Sprite>();
        Sprite rounded, wall, floor, star, paw, mud, mapArrow;
        MazePet3DView petView;
        RawImage petImage;
        UnityEngine.UI.Image startMarker;
        Font font;
        bool moving, paused, zoomed, jumping, jumpRequested;
        float moveDuration=MazeMovementRules.WalkSeconds, nextJumpAt, jumpRequestUntil;
        readonly MazeMudEffect mudEffect=new MazeMudEffect();
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
        public bool IsJumping=>moving && jumping;
        public bool IsSlowed=>mudEffect.IsSlowed(hazardTime);
        public float MudSlowRemaining=>mudEffect.Remaining(hazardTime);
        public bool IsPreviewingDust=>dustSequence.IsPreviewing;
        public float DustOpacity=>dustSequence.DustOpacity;
        public bool IsRearView=>level!=null && MazeRearControls.AppliesTo(level.Number);
        public bool IsMiniMapMinimized=>miniMapMinimized;
        public Vector2Int FacingCellDirection=>IsRearView?rearFacing:petView.FacingDirection;

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
            rounded=Own(MazeArt.Rounded());wall=Own(MazeArt.Wall());floor=Own(MazeArt.Floor());star=Own(MazeArt.Star());paw=Own(MazeArt.Paw());mud=Own(MazeArt.Mud());mapArrow=Own(MazeArt.MapArrow());
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
            levelText=Label(boardCard,"",new Vector2(20,-10),new Vector2(460,32),24,FontStyle.Bold);
            movementStatus=Label(boardCard,"",new Vector2(490,-15),new Vector2(230,28),17,FontStyle.Bold);
            movementStatus.color=new Color(.54f,.32f,.19f);
            timerText=Label(boardCard,"",new Vector2(735,-15),new Vector2(200,28),18);timerText.alignment=TextAnchor.MiddleRight;
            boardArea=Rect("Maze viewport",boardCard,new Vector2(.5f,.5f),new Vector2(0,-15),new Vector2(910,520));
            boardArea.gameObject.AddComponent<RectMask2D>();
            controls=Panel("Controls",playView.transform,new Vector2(1,0),new Vector2(-24,24),new Vector2(235,430),cream);
            movementControls=controls.gameObject.AddComponent<CanvasGroup>();
            Label(controls,"MOVE",new Vector2(18,-12),new Vector2(200,30),20,FontStyle.Bold);
            DirectionButton("UP",new Vector2(80,-52),Vector2Int.up);
            DirectionButton("LEFT",new Vector2(12,-113),Vector2Int.left);
            DirectionButton("RIGHT",new Vector2(148,-113),Vector2Int.right);
            DirectionButton("DOWN",new Vector2(80,-174),Vector2Int.down);
            jumpButton=ActionButton(controls,"JUMP / SPACE",new Vector2(16,-242),new Vector2(203,48),RequestJump,out jumpLabel);
            hazardHeading=Label(controls,"FIND THE EXIT",new Vector2(16,-302),new Vector2(205,26),17,FontStyle.Bold);
            hazardGuide=Label(controls,"Follow the open paths.\nKeyboard: arrows / WASD",new Vector2(16,-338),new Vector2(205,78),15);
            hint=Label(playView.transform,"",Vector2.zero,new Vector2(1020,48),18);var hr=hint.rectTransform;hr.anchorMin=hr.anchorMax=new Vector2(0,0);hr.pivot=Vector2.zero;hr.anchoredPosition=new Vector2(26,16);hint.color=cream;
            levelsButton=ActionButton(playView.transform,"Levels",Vector2.zero,new Vector2(235,44),()=>ConfirmLeave(false),out levelsButtonLabel);
            PlaceTopRight(levelsButton.GetComponent<RectTransform>(),new Vector2(-24,-125));
            retryButton=ActionButton(playView.transform,TestingMode?"Retry / Free test":"Retry / 5 energy",Vector2.zero,new Vector2(235,44),ConfirmRetry,out retryButtonLabel);
            PlaceTopRight(retryButton.GetComponent<RectTransform>(),new Vector2(-24,-179));
            zoomButton=ActionButton(playView.transform,"Zoom in",Vector2.zero,new Vector2(235,44),ToggleZoom,out zoomLabel);
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
            held=Vector2Int.zero;moving=false;jumping=false;jumpRequested=false;progress.AbandonAttempt();mode=ScreenMode.Levels;
            dustSequence.Begin(0);if(rearView)rearView.SetVisible(false);
            selector.SetActive(true);playView.SetActive(false);modal.SetActive(false);
            menuHint.text=TestingMode?"Try any level. Practice does not change your saved progression or energy.":"1-5: find the path / 6-9: snakes / 10-15: jump over mud. Level 1 starts unlocked.";
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
            jumping=false;jumpRequested=false;mudEffect.Clear();nextJumpAt=0;nextTurnAt=0;movementStatus.text="";
            rearFacing=MazeRearControls.InitialFacing(level);
            selector.SetActive(false);playView.SetActive(true);modal.SetActive(false);
            dustSequence.Begin(number);
            zoomed=number>10;zoomLabel.text=dustSequence.Enabled?"Clouds limit view":zoomed?"See full maze":"Zoom in";
            zoomButton.interactable=!dustSequence.Enabled;zoomButton.gameObject.SetActive(!IsRearView);
            petView.ResetFacing();
            BuildBoard();
            bool hazards=level.Snakes.Count>0, hasMud=level.MudCount>0;
            jumpButton.gameObject.SetActive(number>=10);
            RefreshMovementUI();
            hazardHeading.text=hasMud?"JUMP OVER MUD":hazards?"AVOID THE SNAKES":"FIND THE EXIT";
            hazardGuide.text=hasMud?"Stop before mud.\nTap JUMP to cross.\nArrows / WASD / Space":hazards?"Wait for the snake\nto retreat, then cross.\nKeyboard: arrows / WASD":"Follow the open paths.\nKeyboard: arrows / WASD";
            levelText.text="TRAIL "+number.ToString("00")+" / "+(dustSequence.Enabled?"CLOUDY MAZE":hasMud?"MUDDY TRAIL":hazards?"SNAKE CROSSING":"FIND THE EXIT");
            hint.text=number==1?"Follow the open path from the paw to the golden star.":hasMud?"Stop on the dry tile before mud, face forward, then tap JUMP or Space. Landing in mud slows running for 3 seconds.":number==6?"The snake reaches one crossing, then retreats. Wait before it, then run past when clear.":hazards?"Avoid the patrols. A snake touch sends you to the start without spending more energy.":"Find the golden star. Take your time; there is no time limit.";
            PrepareDustPreview();ConfigureRearMode();
            RefreshEnergyUI(true);return true;
        }
        void BuildBoard()
        {
            if(board){board.gameObject.SetActive(false);Destroy(board.gameObject);}
            float fit=Mathf.Min(910f/level.Width,520f/level.Height);
            cellSize=zoomed?Mathf.Max(34,fit*1.4f):fit;
            board=Rect("Maze grid",boardArea,new Vector2(.5f,.5f),Vector2.zero,new Vector2(level.Width*cellSize,level.Height*cellSize));
            if(IsRearView)
            {
                board.gameObject.SetActive(false);petImage=null;startMarker=null;
                shadow=Rect("Rear-view floor position",board,new Vector2(.5f,.5f),CellPosition(cell));
                avatar=Rect("Rear-view jump position",board,new Vector2(.5f,.5f),CellPosition(cell));
                snakes.Clear();foreach(var patrol in level.Snakes)snakes.Add(new SnakeUI{patrol=patrol});
                if(dust)dust.gameObject.SetActive(false);
                if(overviewMarkers)overviewMarkers.gameObject.SetActive(false);
                return;
            }
            for(int y=0;y<level.Height;y++)for(int x=0;x<level.Width;x++)
            {
                var tile=level.TileAt(new Vector2Int(x,y));
                var image=Image("Tile "+x+","+y,board,new Vector2(.5f,.5f),CellPosition(new Vector2Int(x,y)),Vector2.one*cellSize,Color.white);
                image.sprite=tile==MazeTile.Wall?wall:floor;image.raycastTarget=false;
                if(tile==MazeTile.Mud)
                {
                    var puddle=Image("Mud puddle "+x+","+y,board,new Vector2(.5f,.5f),CellPosition(new Vector2Int(x,y)),Vector2.one*cellSize*.93f,Color.white);
                    puddle.sprite=mud;puddle.raycastTarget=false;
                }
            }
            startMarker=Image("Start paw",board,new Vector2(.5f,.5f),CellPosition(level.Start),Vector2.one*cellSize*.8f,new Color(.24f,.49f,.38f));
            startMarker.sprite=paw;startMarker.raycastTarget=false;startMarker.enabled=cell!=level.Start;
            var goal=Image("Goal star",board,new Vector2(.5f,.5f),CellPosition(level.Goal),Vector2.one*cellSize*.88f,Color.white);goal.sprite=star;goal.raycastTarget=false;
            BuildSnakes();
            var shadowImage=Image("Pet shadow",board,new Vector2(.5f,.5f),CellPosition(cell),new Vector2(cellSize*.72f,cellSize*.3f),new Color(0,0,0,.18f));shadowImage.sprite=rounded;shadowImage.raycastTarget=false;shadow=shadowImage.rectTransform;
            avatar=Rect("3D maze companion",board,new Vector2(.5f,.5f),CellPosition(cell),Vector2.one*cellSize*1.25f);
            petImage=avatar.gameObject.AddComponent<RawImage>();petImage.texture=petView.Texture;petImage.raycastTarget=false;
            petImage.enabled=petView.IsReady && petView.Texture.IsCreated();
            FollowPlayer();
            if(dust)dust.transform.SetAsLastSibling();
            if(overviewMarkers)overviewMarkers.transform.SetAsLastSibling();
        }
        void ConfigureRearMode()
        {
            bool rear=IsRearView;
            directionLabels[Vector2Int.up].text=rear?"FORWARD":"UP";
            directionLabels[Vector2Int.up].fontSize=rear?14:18;
            directionLabels[Vector2Int.left].text=rear?"TURN\nLEFT":"LEFT";
            directionLabels[Vector2Int.right].text=rear?"TURN\nRIGHT":"RIGHT";
            directionButtons[Vector2Int.down].gameObject.SetActive(!rear);
            jumpButton.GetComponent<RectTransform>().anchoredPosition=new Vector2(16,rear?-174:-242);
            hazardHeading.rectTransform.anchoredPosition=new Vector2(16,rear?-234:-302);
            hazardGuide.rectTransform.anchoredPosition=new Vector2(16,rear?-270:-338);
            MoveMenuButton(levelsButton,levelsButtonLabel,rear,new Vector2(12,-366),new Vector2(-24,-125));
            MoveMenuButton(retryButton,retryButtonLabel,rear,new Vector2(123,-366),new Vector2(-24,-179));
            retryButtonLabel.text=rear?(TestingMode?"Retry\nFree":"Retry\n5 energy"):(TestingMode?"Retry / Free test":"Retry / 5 energy");
            if(rearImage)rearImage.gameObject.SetActive(rear);
            if(miniMapPanel)miniMapPanel.gameObject.SetActive(rear);
            if(!rear){if(rearView)rearView.SetVisible(false);return;}
            if(!rearView)
            {
                rearView=new GameObject("Rear-view maze renderer").AddComponent<MazeThirdPersonView>();rearView.transform.SetParent(transform,false);
                var picture=Rect("Rear-view maze picture",boardArea,new Vector2(.5f,.5f),Vector2.zero);
                picture.anchorMin=Vector2.zero;picture.anchorMax=Vector2.one;picture.sizeDelta=Vector2.zero;
                rearImage=picture.gameObject.AddComponent<RawImage>();rearImage.raycastTarget=false;
            }
            rearView.Initialize(level,PlayerPrefs.GetInt("LittlePaws.Species",0)==1);
            rearImage.gameObject.SetActive(true);rearImage.texture=rearView.Texture;rearImage.transform.SetAsLastSibling();
            BuildMiniMap();
            levelText.text="TRAIL "+level.Number.ToString("00")+" / FOLLOW THE TRAIL";
            hazardHeading.text="TURN THEN MOVE";
            hazardGuide.text="Forward: W / Up\nTurn: A/D or Left/Right\nTurn twice to go back.";
            hint.text="You are behind your pet. Turn left or right, then move FORWARD. Use the map at the top right; tap its header to minimize it.";
        }
        void MoveMenuButton(Button button,Text label,bool rear,Vector2 position,Vector2 normalPosition)
        {
            var rect=button.GetComponent<RectTransform>();rect.SetParent(rear?controls:playView.transform,false);
            rect.sizeDelta=rear?new Vector2(100,48):new Vector2(235,44);
            label.rectTransform.sizeDelta=rect.sizeDelta;label.fontSize=rear?14:18;
            if(rear){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;}
            else PlaceTopRight(rect,normalPosition);
        }
        void BuildMiniMap()
        {
            if(!miniMapPanel)
            {
                miniMapPanel=Panel("Rear-view mini map",playView.transform,Vector2.one,new Vector2(-24,-125),new Vector2(235,218),cream);
                ActionButton(miniMapPanel,"MAP -",new Vector2(16,-10),new Vector2(203,32),ToggleMiniMap,out miniMapLabel);
                miniMapArea=Rect("Mini map image",miniMapPanel,new Vector2(0,1),new Vector2(16,-52),new Vector2(203,154));
                miniMapImage=miniMapArea.gameObject.AddComponent<RawImage>();miniMapImage.raycastTarget=false;
                var marker=Image("Your location and facing",miniMapArea,new Vector2(.5f,.5f),Vector2.zero,new Vector2(14,14),new Color(1,.31f,.08f));
                marker.sprite=mapArrow;marker.raycastTarget=false;miniMapPlayer=marker.rectTransform;
            }
            miniMapPanel.gameObject.SetActive(true);
            float scale=Mathf.Min(203f/level.Width,154f/level.Height);
            miniMapArea.sizeDelta=new Vector2(level.Width*scale,level.Height*scale);
            miniMapArea.anchoredPosition=new Vector2(16+(203-miniMapArea.sizeDelta.x)/2,-52-(154-miniMapArea.sizeDelta.y)/2);
            if(miniMapTexture)Destroy(miniMapTexture);
            const int pixels=3;miniMapTexture=new Texture2D(level.Width*pixels,level.Height*pixels,TextureFormat.RGBA32,false){name="Trail mini map",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            var colors=new Color[level.Width*pixels*level.Height*pixels];
            for(int y=0;y<level.Height;y++)for(int x=0;x<level.Width;x++)
            {
                Color color=level.Tiles[x,y]==MazeTile.Wall?new Color(.17f,.35f,.27f):level.Tiles[x,y]==MazeTile.Mud?new Color(.48f,.32f,.20f):new Color(.92f,.87f,.74f);
                if(new Vector2Int(x,y)==level.Start)color=new Color(.25f,.66f,.91f);
                if(new Vector2Int(x,y)==level.Goal)color=new Color(1f,.77f,.15f);
                for(int py=0;py<pixels;py++)for(int px=0;px<pixels;px++)colors[(y*pixels+py)*level.Width*pixels+x*pixels+px]=color;
            }
            miniMapTexture.SetPixels(colors);miniMapTexture.Apply();miniMapImage.texture=miniMapTexture;
            ApplyMiniMapState();
        }
        public void ToggleMiniMap(){if(!IsRearView)return;miniMapMinimized=!miniMapMinimized;ApplyMiniMapState();}
        void ApplyMiniMapState()
        {
            miniMapPanel.sizeDelta=new Vector2(235,miniMapMinimized?52:218);
            miniMapArea.gameObject.SetActive(!miniMapMinimized);miniMapLabel.text=miniMapMinimized?"MAP +":"MAP -";
        }
        void UpdateRearView()
        {
            if(!rearView)return;
            bool shown=IsRearView && mode!=ScreenMode.Levels;rearView.SetVisible(shown);
            if(!shown)return;
            rearView.SetState(VisualCell(),rearFacing,mode==ScreenMode.Playing&&moving&&!jumping&&!modal.activeSelf&&!paused,
                moving&&jumping,Mathf.Clamp01(animationTime/moveDuration),hazardTime,IsSlowed?MazeMovementRules.MudSpeedMultiplier:1f);
            rearImage.texture=rearView.Texture;rearImage.enabled=rearView.IsReady && rearView.Texture.IsCreated();
            Vector2 at=VisualCell();
            miniMapPlayer.anchoredPosition=MazeRearControls.MapPosition(at,level,miniMapArea.sizeDelta);
            miniMapPlayer.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(rearFacing.x,rearFacing.y)*Mathf.Rad2Deg);
        }
        void PrepareDustPreview()
        {
            movementControls.interactable=!IsPreviewingDust;movementControls.blocksRaycasts=!IsPreviewingDust;
            if(dustSequence.Enabled && !dust)
            {
                var root=Rect("White cloud curtain",boardArea,new Vector2(.5f,.5f),Vector2.zero);
                root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.sizeDelta=Vector2.zero;
                dust=root.gameObject.AddComponent<MazeDustView>();dust.raycastTarget=false;
            }
            if(dustSequence.Enabled && !overviewMarkers)
            {
                var root=Rect("Dust overview markers",boardArea,new Vector2(.5f,.5f),Vector2.zero);
                root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.sizeDelta=Vector2.zero;
                overviewMarkers=root.gameObject.AddComponent<CanvasGroup>();overviewMarkers.blocksRaycasts=false;overviewMarkers.interactable=false;
                overviewStart=OverviewBadge("START",root,green);
                overviewGoal=OverviewBadge("GOAL",root,new Color(.63f,.43f,.14f));
            }
            if(overviewMarkers)overviewMarkers.gameObject.SetActive(IsPreviewingDust);
            if(IsPreviewingDust)
            {
                hazardHeading.text="STUDY THE MAZE";
                hazardGuide.text="Start is near your pet.\nFind the golden goal.\nWhite clouds are coming.";
                ApplyDustPreview();
            }
            UpdateDustView();
        }
        RectTransform OverviewBadge(string title,Transform parent,Color color)
        {
            var badge=Panel(title+" overview badge",parent,new Vector2(.5f,.5f),Vector2.zero,new Vector2(80,26),color);
            badge.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            var label=Label(badge,title,Vector2.zero,new Vector2(80,26),13,FontStyle.Bold);
            label.alignment=TextAnchor.MiddleCenter;label.color=Color.white;
            return badge;
        }
        void ApplyDustPreview()
        {
            if(!board || !dustSequence.Enabled)return;
            float transition=dustSequence.Transition;
            float fit=Mathf.Min(1f,Mathf.Min(boardArea.rect.width/board.sizeDelta.x,boardArea.rect.height/board.sizeDelta.y));
            float scale=Mathf.Lerp(fit,1f,transition);board.localScale=Vector3.one*scale;
            float x=Mathf.Max(0,(board.sizeDelta.x*scale-boardArea.rect.width)/2);
            float y=Mathf.Max(0,(board.sizeDelta.y*scale-boardArea.rect.height)/2);
            Vector2 follow=new Vector2(Mathf.Clamp(-shadow.anchoredPosition.x*scale,-x,x),Mathf.Clamp(-shadow.anchoredPosition.y*scale,-y,y));
            board.anchoredPosition=follow*transition;
            if(overviewMarkers)
            {
                overviewMarkers.alpha=1f-transition;
                PlaceOverviewBadge(overviewStart,level.Start,1);
                PlaceOverviewBadge(overviewGoal,level.Goal,-1);
                float pulse=1f+Mathf.Sin(dustSequence.Elapsed*4f)*.07f;
                overviewStart.localScale=Vector3.one*(dustSequence.Elapsed<1.5f?pulse:1f);
                overviewGoal.localScale=Vector3.one*(dustSequence.Elapsed>=1.5f?pulse:1f);
            }
            if(IsPreviewingDust)
            {
                movementStatus.text="STUDY THE MAP";
                timerText.text="PREVIEW / "+Mathf.CeilToInt(MazeDustSequence.OverviewSeconds+MazeDustSequence.TransitionSeconds-dustSequence.Elapsed)+"s";
                hint.text=dustSequence.Elapsed<1.5f?"Memorize the whole maze. START is your pet; GOAL is the golden star.":"Remember the turns from START to GOAL. White clouds are rolling in.";
            }
        }
        void PlaceOverviewBadge(RectTransform badge,Vector2Int at,int vertical)
        {
            Vector2 point=boardArea.InverseTransformPoint(board.TransformPoint(CellPosition(at)));
            point+=Vector2.up*vertical*(cellSize*board.localScale.x*.9f+20f);
            Rect bounds=boardArea.rect;
            badge.anchoredPosition=new Vector2(Mathf.Clamp(point.x,bounds.xMin+44,bounds.xMax-44),Mathf.Clamp(point.y,bounds.yMin+17,bounds.yMax-17));
        }
        void UpdateDustView()
        {
            if(!dust)return;
            bool show=dustSequence.Enabled && dustSequence.DustOpacity>0f && mode!=ScreenMode.Levels;
            dust.gameObject.SetActive(show);
            if(show)
            {
                Vector2 center=boardArea.InverseTransformPoint(shadow.position);
                dust.SetState(center,cellSize*board.localScale.x,dustSequence.DustOpacity,elapsed+dustSequence.Elapsed);
            }
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
                if(!snake.root)continue;
                Vector2 p=snake.patrol.PositionAt(hazardTime),d=snake.patrol.DirectionAt(hazardTime);
                snake.root.anchoredPosition=GridPosition(p);
                snake.root.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
                for(int i=0;i<snake.segments.Length;i++)snake.segments[i].anchoredPosition=new Vector2((-.34f+i*.115f)*cellSize,Mathf.Sin(hazardTime*7-i*.8f)*cellSize*.055f);
            }
        }
        Vector2 GridPosition(Vector2 point)=>new Vector2((point.x+.5f-level.Width*.5f)*cellSize,(point.y+.5f-level.Height*.5f)*cellSize);
        Vector2 VisualCell()=>new Vector2(shadow.anchoredPosition.x/cellSize+level.Width*.5f-.5f,shadow.anchoredPosition.y/cellSize+level.Height*.5f-.5f);
        bool AdvanceSnakes(float dt,Vector2 before,Vector2 after,float movingSeconds,bool jumpedThisFrame,float jumpTimeBefore)
        {
            bool touched=false;int steps=Mathf.Max(1,Mathf.CeilToInt(dt/.02f));
            bool Contact(SnakePatrol patrol,float a,float b)
            {
                if(b<a)return false;
                float pa=movingSeconds>.00001f?Mathf.Clamp01(a*dt/movingSeconds):1;
                float pb=movingSeconds>.00001f?Mathf.Clamp01(b*dt/movingSeconds):1;
                return MazeHazards.Touches(Vector2.Lerp(before,after,pa),Vector2.Lerp(before,after,pb),patrol.PositionAt(hazardTime+a*dt),patrol.PositionAt(hazardTime+b*dt),.43f);
            }
            foreach(var snake in snakes)
                for(int i=0;i<steps && !touched;i++)
                {
                    float a=(float)i/steps,b=(float)(i+1)/steps;
                    if(hazardTime+b*dt<safeUntil)continue;
                    if(dt>0)a=Mathf.Max(a,(safeUntil-hazardTime)/dt);
                    if(jumpedThisFrame && dt>0)
                    {
                        // Snakes can touch the pet during take-off and landing,
                        // but not through the middle of its airborne arc.
                        float takeoff=(moveDuration*MazeMovementRules.TakeoffGroundedUntil-jumpTimeBefore)/dt;
                        float landing=(moveDuration*MazeMovementRules.LandingGroundedFrom-jumpTimeBefore)/dt;
                        touched=Contact(snake.patrol,a,Mathf.Min(b,takeoff)) || Contact(snake.patrol,Mathf.Max(a,landing),b);
                    }
                    else touched=Contact(snake.patrol,a,b);
                }
            hazardTime+=dt;DrawSnakes();return touched;
        }
        void SnakeCaughtYou()
        {
            SnakeHits++;cell=level.Start;moving=false;jumping=false;jumpRequested=false;mudEffect.Clear();held=Vector2Int.zero;
            avatar.anchoredPosition=CellPosition(cell);shadow.anchoredPosition=CellPosition(cell);petView.ResetFacing();
            if(IsRearView){rearFacing=MazeRearControls.InitialFacing(level);rearView.SnapFacing(rearFacing);}
            FollowPlayer();
            safeUntil=hazardTime+1.2f;repeatAt=Time.unscaledTime+.4f;RefreshMovementUI();
            hint.text="Snake caught you! Back to start. No extra energy spent.";
        }
        void ToggleZoom()
        {
            if(moving || dustSequence.Enabled || IsRearView)return;
            zoomed=!zoomed;zoomLabel.text=zoomed?"See full maze":"Zoom in";BuildBoard();
        }
        void FollowPlayer()
        {
            if(IsRearView)return;
            if(IsPreviewingDust){ApplyDustPreview();return;}
            board.localScale=Vector3.one;
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
            if(IsPreviewingDust)
            {
                dustSequence.Advance(Time.deltaTime);ApplyDustPreview();
                if(!IsPreviewingDust)
                {
                    held=Vector2Int.zero;jumpRequested=false;repeatAt=Time.unscaledTime+.15f;
                    movementControls.interactable=true;movementControls.blocksRaycasts=true;
                    if(overviewMarkers)overviewMarkers.gameObject.SetActive(false);
                    FollowPlayer();RefreshMovementUI();timerText.text="00:00";
                    hint.text="White clouds hide distant paths. Jump mud, wait for snakes, and find the golden star.";
                    hazardHeading.text="CLOUDY MAZE";
                    hazardGuide.text="Jump over mud.\nWait for snake crossings.\nArrows / WASD / Space";
                }
                return;
            }
            elapsed+=Time.deltaTime;timerText.text=TimeLabel(Mathf.FloorToInt(elapsed));
            if(Keyboard.current!=null && Keyboard.current.spaceKey.wasPressedThisFrame)RequestJump();
            Vector2 previousPosition=VisualCell();bool arrivedAtGoal=false,landed=false;
            bool jumpedThisFrame=moving && jumping;float jumpTimeBefore=animationTime;
            float movingSeconds=moving?Mathf.Min(Time.deltaTime,Mathf.Max(0,moveDuration-animationTime)):0;
            if(moving)
            {
                animationTime+=Time.deltaTime;
                float t=Mathf.Clamp01(animationTime/moveDuration);Vector2 baseline=Vector2.Lerp(fromPosition,toPosition,t);
                avatar.anchoredPosition=baseline+Vector2.up*(jumping?MazeMovementRules.JumpHeight(t)*cellSize:0);
                shadow.anchoredPosition=baseline;FollowPlayer();
                if(t>=1){moving=false;jumping=false;avatar.anchoredPosition=toPosition;arrivedAtGoal=cell==level.Goal;landed=true;}
            }
            if(AdvanceSnakes(Time.deltaTime,previousPosition,VisualCell(),movingSeconds,jumpedThisFrame,jumpTimeBefore)){SnakeCaughtYou();return;}
            if(landed)
            {
                mudEffect.LandOn(level.TileAt(cell),hazardTime);
                if(level.TileAt(cell)==MazeTile.Mud)
                    hint.text="Muddy paws! Running is half speed for 3 seconds. Jump over the next puddle.";
            }
            RefreshMovementUI();
            if(arrivedAtGoal){Win();return;}
            if(moving)return;
            var direction=InputDirection();
            if(IsRearView && Time.unscaledTime>=repeatAt)
            {
                int turn=TurnInput();
                if(turn!=0){TryTurn(turn);repeatAt=Time.unscaledTime+MazeRearControls.TurnRepeatSeconds;return;}
            }
            if(jumpRequested)
            {
                jumpRequested=false;
                if(hazardTime<=jumpRequestUntil && TryJump(IsRearView?rearFacing:direction==Vector2Int.zero?petView.FacingDirection:direction))return;
            }
            if(direction!=Vector2Int.zero && Time.unscaledTime>=repeatAt){TryMove(direction);repeatAt=Time.unscaledTime+(IsRearView?MazeRearControls.WalkSeconds:.14f);}
        }
        Vector2Int InputDirection()
        {
            var keyboard=Keyboard.current;
            if(IsRearView)
            {
                bool forward=held==Vector2Int.up || (keyboard!=null&&(keyboard.wKey.isPressed||keyboard.upArrowKey.isPressed));
                return forward?rearFacing:Vector2Int.zero;
            }
            if(keyboard!=null)
            {
                if(keyboard.wKey.isPressed||keyboard.upArrowKey.isPressed)return Vector2Int.up;
                if(keyboard.sKey.isPressed||keyboard.downArrowKey.isPressed)return Vector2Int.down;
                if(keyboard.aKey.isPressed||keyboard.leftArrowKey.isPressed)return Vector2Int.left;
                if(keyboard.dKey.isPressed||keyboard.rightArrowKey.isPressed)return Vector2Int.right;
            }
            return held;
        }
        int TurnInput()
        {
            var keyboard=Keyboard.current;
            if(held==Vector2Int.left || (keyboard!=null&&(keyboard.aKey.isPressed||keyboard.leftArrowKey.isPressed)))return -1;
            if(held==Vector2Int.right || (keyboard!=null&&(keyboard.dKey.isPressed||keyboard.rightArrowKey.isPressed)))return 1;
            return 0;
        }
        public bool TurnLeft()=>TryTurn(-1);
        public bool TurnRight()=>TryTurn(1);
        public bool MoveForward()=>IsRearView && TryMove(rearFacing);
        bool TryTurn(int direction)
        {
            if(!IsRearView||mode!=ScreenMode.Playing||moving||modal.activeSelf||paused||elapsed<nextTurnAt)return false;
            rearFacing=direction<0?MazeRearControls.TurnLeft(rearFacing):MazeRearControls.TurnRight(rearFacing);
            nextTurnAt=elapsed+MazeRearControls.TurnRepeatSeconds;
            return true;
        }
        public void RequestJump()
        {
            if(mode!=ScreenMode.Playing||IsPreviewingDust||level.Number<10||modal.activeSelf||paused||hazardTime<nextJumpAt)return;
            jumpRequested=true;jumpRequestUntil=hazardTime+.4f;
        }
        public bool TryJump(Vector2Int direction)=>TryTravel(direction,true);
        public bool TryMove(Vector2Int direction)=>TryTravel(direction,false);
        bool TryTravel(Vector2Int direction,bool jump)
        {
            if(mode!=ScreenMode.Playing||IsPreviewingDust||moving||modal.activeSelf||paused)return false;
            if(Mathf.Abs(direction.x)+Mathf.Abs(direction.y)!=1)return false;
            if(IsRearView && !MazeRearControls.IsForward(rearFacing,direction))return false;
            if(jump && (level.Number<10 || hazardTime<nextJumpAt))return false;
            petView.Face(direction);
            if(!MazeLevels.TryStep(level,cell,direction,jump,out var landing))
            {
                hint.text=jump?"Jump blocked. Face along the corridor: jump over the next tile and land on the following tile. Hedges cannot be crossed.":"A hedge blocks that way. Try a different direction.";
                return false;
            }
            fromPosition=CellPosition(cell);toPosition=CellPosition(landing);cell=landing;
            moveDuration=jump?MazeMovementRules.JumpSeconds:IsRearView?MazeRearControls.WalkSeconds/(IsSlowed?MazeMovementRules.MudSpeedMultiplier:1f):MazeMovementRules.StepSeconds(IsSlowed);
            moving=true;jumping=jump;animationTime=0;
            if(jump){jumpRequested=false;nextJumpAt=hazardTime+MazeMovementRules.JumpCooldown;}
            return true;
        }
        void RefreshMovementUI()
        {
            movementStatus.text=IsSlowed?"MUDDY / "+MudSlowRemaining.ToString("0.0")+"s":"";
            if(jumpButton)
            {
                float wait=Mathf.Max(0,nextJumpAt-hazardTime);
                jumpButton.interactable=wait<=0 && !IsPreviewingDust;
                jumpLabel.text=wait>0?"JUMP / "+wait.ToString("0.0")+"s":"JUMP / SPACE";
            }
        }
        void LateUpdate()
        {
            if(!petView)return;
            // The spawn marker identifies an empty start tile; never draw it under the pet.
            if(startMarker && shadow && level!=null)
                startMarker.enabled=Vector2.Distance(shadow.anchoredPosition,CellPosition(level.Start))>cellSize*.9f;
            if(petImage)
            {
                // RawImage draws a white rectangle when its texture is null.
                petImage.texture=petView.Texture;
                petImage.enabled=petView.IsReady && petView.Texture.IsCreated();
            }
            UpdateDustView();
            UpdateRearView();
            petView.SetVisible(mode!=ScreenMode.Levels && !IsRearView);
            petView.SetMoving(mode==ScreenMode.Playing && moving && !jumping && !modal.activeSelf && !paused,IsSlowed?MazeMovementRules.MudSpeedMultiplier:1f);
            petView.SetJumping(mode==ScreenMode.Playing && moving && jumping,Mathf.Clamp01(animationTime/moveDuration));
        }
        public void HoldDirection(Vector2Int direction)
        {
            if(IsPreviewingDust || (IsRearView && direction==Vector2Int.down))return;
            held=direction;repeatAt=0;
            if(IsRearView && direction.x!=0)
            {
                TryTurn(direction.x);repeatAt=Time.unscaledTime+MazeRearControls.TurnRepeatSeconds;
            }
        }
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
            jumpRequested=false;dialogTitle.text=title;dialogText.text=body;primaryLabel.text=primary;secondaryLabel.text=secondary;
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
        void OnApplicationPause(bool value){paused=value;held=Vector2Int.zero;jumpRequested=false;if(progress!=null){progress.RefreshEnergy(DateTime.UtcNow);Save();}}
        void OnApplicationFocus(bool focus){held=Vector2Int.zero;jumpRequested=false;paused=!focus;}
        void OnApplicationQuit(){Save();}
        void OnDestroy(){if(miniMapTexture)Destroy(miniMapTexture);foreach(var sprite in art)if(sprite){Destroy(sprite.texture);Destroy(sprite);}}

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
        {var b=ActionButton(controls,title,position,new Vector2(74,52),null,out var label);directionButtons[direction]=b;directionLabels[direction]=label;var hold=b.gameObject.AddComponent<MazeDirectionHold>();hold.owner=this;hold.direction=direction;}
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
