using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LittlePaws.Rescue
{
    public sealed class RescueEggGame : MonoBehaviour
    {
        readonly Color ink = new Color(.18f, .25f, .20f), cream = new Color(.99f, .96f, .87f), gold = new Color(.96f, .72f, .29f);
        RectTransform safe, layout, levels, choose, play, actors, pausePanel, resultPanel, goSign, goalEgg;
        Font font;
        Text timer, health, stageText, enemyCount, message, resultTitle, resultBody, selectionText;
        Image healthFill;
        RescueCombat combat;
        RescueFighterView hero;
        readonly List<RescueFighterView> enemies = new List<RescueFighterView>();
        readonly List<RectTransform> enemyBars = new List<RectTransform>();
        readonly List<Image> enemyFills = new List<Image>();
        readonly List<Text> enemyLabels = new List<Text>();
        readonly List<Vector2> lastEnemyPositions = new List<Vector2>();
        readonly List<RescueHoldButton> holds = new List<RescueHoldButton>();
        Button roosterButton, henButton;
        bool hen, running, paused, appPaused, ended;
        int lastStage = -1, lastAttack, lastHealth = 50;
        float attackAge = 10, hurtUntil, walkClock, stageViewStart;
        Rect lastSafe;
        int lastWidth, lastHeight;
        float Width => layout ? layout.rect.width : 1440;
        float Height => layout ? layout.rect.height : 810;
        public RescueCombat Combat => combat;
        public bool IsRunning => running && !ended;

        void Start()
        {
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToPortrait = Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = true;
            Application.targetFrameRate = 60;
            if (!Camera.main)
            {
                var camera = new GameObject("Rescue background camera", typeof(Camera)).GetComponent<Camera>();
                camera.tag="MainCamera";camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.24f,.18f);
            }
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build(); ShowLevels();
        }
        void Build()
        {
            var events = FindAnyObjectByType<EventSystem>();
            if (!events) events = new GameObject("Rescue input", typeof(EventSystem)).GetComponent<EventSystem>();
            if (!events.GetComponent<InputSystemUIInputModule>())
            {
                var old = events.GetComponent<StandaloneInputModule>(); if (old) Destroy(old);
                events.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            var canvas = new GameObject("Rescue the Egg canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 810); scaler.matchWidthOrHeight = 1;
            var backdrop = Picture("Woodland", canvas.transform, RescueArt.Forest(), Vector2.zero, Vector2.zero);
            Stretch(backdrop); backdrop.GetComponent<Image>().preserveAspect = false;
            safe = Rect("Device safe area", canvas.transform, Vector2.zero, Vector2.zero); Stretch(safe);
            layout = Rect("Landscape layout", safe, Vector2.zero, new Vector2(1440,810)); Stretch(layout);
            levels = ScreenRoot("Three rescue levels"); choose = ScreenRoot("Choose a guardian"); play = ScreenRoot("Fight screen");
            BuildLevels(); BuildChoose(); BuildPlay(); BuildPause(); BuildResult(); UpdateSafe();
        }
        void BuildLevels()
        {
            var shade = Panel("Menu shade", levels, Vector2.zero, Vector2.zero, new Color(.06f,.16f,.13f,.82f)); Stretch(shade);
            TextAt(levels, "A WOODLAND ADVENTURE", new Vector2(0,295), new Vector2(1100,35), 20, gold);
            TextAt(levels, "RESCUE THE EGG", new Vector2(0,236), new Vector2(1100,80), 58, cream, true);
            TextAt(levels, "A tiny guardian. An iron sword. One very important egg.", new Vector2(0,173), new Vector2(1100,45), 23, cream);
            for (int i=0;i<3;i++)
            {
                bool available=i==0;
                var card=Panel("Level "+(i+1),levels,new Vector2((i-1)*390,-40),new Vector2(356,337), available?cream:new Color(.22f,.32f,.28f,.97f));
                Picture("Level egg",card,RescueArt.Egg(),new Vector2(0,74),new Vector2(93,116));
                TextAt(card,"LEVEL 0"+(i+1),new Vector2(0,-4),new Vector2(310,42),29,available?ink:cream,true);
                TextAt(card,available?"THE WOODLAND PATH":"COMING SOON",new Vector2(0,-53),new Vector2(320,36),20,available?ink:new Color(.69f,.77f,.69f),true);
                var button=MakeButton(card,available?"CHOOSE YOUR HERO":"LOCKED",new Vector2(0,-117),new Vector2(302,54),()=>ShowChoose(),available?gold:new Color(.29f,.39f,.34f));
                button.interactable=available;
            }
            TextAt(levels,"Level 1 · 3 stages · 2-hour limit · one roach per stage in this test",new Vector2(0,-255),new Vector2(1200,40),20,cream);
            MakeButton(levels,"BACK TO VILLAGE",new Vector2(0,-329),new Vector2(280,54),Village,gold);
        }
        void BuildChoose()
        {
            var shade=Panel("Selection shade",choose,Vector2.zero,Vector2.zero,new Color(.06f,.16f,.13f,.86f));Stretch(shade);
            TextAt(choose,"CHOOSE YOUR GUARDIAN",new Vector2(0,308),new Vector2(1100,62),42,cream,true);
            TextAt(choose,"Egg shields • silver armor • iron swords • protected boots",new Vector2(0,253),new Vector2(1100,38),21,gold);
            roosterButton=HeroCard(false,-264);henButton=HeroCard(true,264);
            selectionText=TextAt(choose,"",new Vector2(0,-213),new Vector2(1150,45),22,cream);
            MakeButton(choose,"BEGIN LEVEL 1",new Vector2(0,-286),new Vector2(330,64),StartRound,gold);
            MakeButton(choose,"LEVELS",new Vector2(-490,-323),new Vector2(190,48),ShowLevels,new Color(.76f,.83f,.70f));
            SelectHero(false);
        }
        Button HeroCard(bool isHen,float x)
        {
            var card=Panel(isHen?"Hen guardian":"Rooster guardian",choose,new Vector2(x,19),new Vector2(460,379),cream);
            var preview=Picture("Hero design",card,RescueArt.Hero(isHen),new Vector2(-90,10),new Vector2(210,280));
            TextAt(card,isHen?"THE HEN":"THE ROOSTER",new Vector2(100,96),new Vector2(215,44),23,ink,true);
            TextAt(card,isHen?"Egg-crested helmet\nIvory feathers\nSilver battle armor":"Crimson crest\nEgg-emblem shield\nSilver battle armor",new Vector2(106,13),new Vector2(210,115),20,ink);
            return MakeButton(card,"SELECT",new Vector2(0,-143),new Vector2(390,51),()=>SelectHero(isHen),gold);
        }
        void SelectHero(bool value)
        {
            hen=value;
            if(selectionText) selectionText.text=(hen?"Hen":"Rooster")+" selected · Both guardians have 50 health and deal 1 damage per sword tap.";
            if(roosterButton)roosterButton.GetComponentInChildren<Text>().text=hen?"SELECT ROOSTER":"ROOSTER SELECTED";
            if(henButton)henButton.GetComponentInChildren<Text>().text=hen?"HEN SELECTED":"SELECT HEN";
        }
        void BuildPlay()
        {
            var treeLine=Picture("Trees beside the path",play,RescueArt.Forest(),Vector2.zero,Vector2.zero);
            treeLine.anchorMin=new Vector2(0,.74f);treeLine.anchorMax=Vector2.one;treeLine.offsetMin=treeLine.offsetMax=Vector2.zero;treeLine.GetComponent<Image>().preserveAspect=false;
            var dirt=Picture("Dirt path",play,RescueArt.Dirt(),new Vector2(0,-100),new Vector2(1440,580));
            dirt.anchorMin=new Vector2(0,0);dirt.anchorMax=new Vector2(1,.77f);dirt.offsetMin=Vector2.zero;dirt.offsetMax=Vector2.zero;dirt.GetComponent<Image>().preserveAspect=false;
            goalEgg=Picture("Egg to rescue",play,RescueArt.Egg(),Vector2.zero,new Vector2(74,98));
            goalEgg.anchorMin=goalEgg.anchorMax=Vector2.zero;goalEgg.gameObject.SetActive(false);
            actors=Rect("Combat actors",play,Vector2.zero,Vector2.zero);Stretch(actors);
            var hud=Panel("Status ribbon",play,new Vector2(0,-52),new Vector2(1392,82),new Color(.08f,.18f,.14f,.94f));
            hud.anchorMin=hud.anchorMax=new Vector2(.5f,1);
            TextAt(hud,"RESCUE THE EGG",new Vector2(-520,15),new Vector2(310,34),23,cream,true);
            health=TextAt(hud,"50 / 50 HP",new Vector2(-520,-17),new Vector2(300,28),18,cream);
            var healthBack=Panel("Health track",hud,new Vector2(-245,-12),new Vector2(225,17),new Color(.19f,.29f,.24f));
            healthFill=Panel("Health",healthBack,Vector2.zero,new Vector2(225,17),new Color(.65f,.83f,.40f)).GetComponent<Image>();
            healthFill.type=Image.Type.Filled;healthFill.fillMethod=Image.FillMethod.Horizontal;
            stageText=TextAt(hud,"STAGE 1 / 3",new Vector2(-20,15),new Vector2(230,32),22,gold,true);
            enemyCount=TextAt(hud,"",new Vector2(-20,-18),new Vector2(270,27),16,cream);
            timer=TextAt(hud,"02:00:00",new Vector2(258,4),new Vector2(220,42),30,cream,true);
            MakeButton(hud,"PAUSE",new Vector2(550,0),new Vector2(162,47),()=>Pause(true),new Color(.77f,.82f,.66f));
            goSign=Panel("Go right sign",play,new Vector2(0,-125),new Vector2(340,63),gold);goSign.anchorMin=goSign.anchorMax=new Vector2(.5f,1);
            TextAt(goSign,"GO RIGHT",new Vector2(-36,0),new Vector2(218,50),27,ink,true);
            Picture("Right arrow",goSign,RescueArt.Arrow(),new Vector2(113,0),new Vector2(65,41));
            message=TextAt(play,"",new Vector2(0,101),new Vector2(770,48),19,cream,true);
            message.rectTransform.anchorMin=message.rectTransform.anchorMax=new Vector2(.5f,0);
            var controls=Panel("Movement pad",play,new Vector2(154,106),new Vector2(278,182),new Color(.08f,.17f,.13f,.86f));controls.anchorMin=controls.anchorMax=Vector2.zero;
            Direction(controls,"↑",new Vector2(0,49),Vector2.up);Direction(controls,"↓",new Vector2(0,-49),Vector2.down);
            Direction(controls,"←",new Vector2(-87,0),Vector2.left);Direction(controls,"→",new Vector2(87,0),Vector2.right);
            var fight=MakeButton(play,"FIGHT",new Vector2(-114,90),new Vector2(172,132),Attack,gold);
            fight.GetComponent<RectTransform>().anchorMin=fight.GetComponent<RectTransform>().anchorMax=Vector2.right;
            TextAt(fight.transform,"TAP EACH HIT",new Vector2(0,-33),new Vector2(156,25),14,ink,true);
            var hint=TextAt(play,"WASD / arrows to move     SPACE to swing",new Vector2(0,30),new Vector2(760,28),16,new Color(.93f,.90f,.74f));
            hint.rectTransform.anchorMin=hint.rectTransform.anchorMax=new Vector2(.5f,0);
        }
        void Direction(Transform parent,string label,Vector2 pos,Vector2 direction)
        {
            var button=MakeButton(parent,label,pos,new Vector2(76,67),()=>{},new Color(.88f,.90f,.77f));
            var hold=button.gameObject.AddComponent<RescueHoldButton>();hold.Direction=direction;holds.Add(hold);
        }
        void BuildPause()
        {
            pausePanel=ScreenRoot("Pause overlay");var shade=Panel("Shade",pausePanel,Vector2.zero,Vector2.zero,new Color(.04f,.12f,.09f,.85f));Stretch(shade);
            var card=Panel("Pause card",pausePanel,Vector2.zero,new Vector2(740,530),cream);
            TextAt(card,"GUARDIAN'S REST",new Vector2(0,194),new Vector2(650,55),36,ink,true);
            TextAt(card,"Tap FIGHT for every swing. Close the gap after knockback.\nA roach glows when stunned, then falls for 2 seconds.\nWhile it is down, your sword cannot hurt it.\nClear the stage, then walk past the GO sign.\nThe 2-hour play timer rests while paused.",new Vector2(0,51),new Vector2(650,200),23,ink);
            MakeButton(card,"RESUME",new Vector2(0,-100),new Vector2(510,57),()=>Pause(false),gold);
            MakeButton(card,"RESTART LEVEL",new Vector2(0,-169),new Vector2(510,51),StartRound,new Color(.76f,.83f,.69f));
            MakeButton(card,"BACK TO LEVELS",new Vector2(0,-226),new Vector2(510,45),ShowLevels,new Color(.84f,.83f,.74f));
        }
        void BuildResult()
        {
            resultPanel=ScreenRoot("Round result");var shade=Panel("Shade",resultPanel,Vector2.zero,Vector2.zero,new Color(.04f,.12f,.09f,.87f));Stretch(shade);
            var card=Panel("Result card",resultPanel,Vector2.zero,new Vector2(760,570),cream);
            Picture("Rescued egg",card,RescueArt.Egg(),new Vector2(0,159),new Vector2(104,136));
            resultTitle=TextAt(card,"",new Vector2(0,43),new Vector2(690,66),38,ink,true);
            resultBody=TextAt(card,"",new Vector2(0,-27),new Vector2(660,80),23,ink);
            MakeButton(card,"PLAY AGAIN",new Vector2(0,-126),new Vector2(510,58),StartRound,gold);
            MakeButton(card,"LEVELS",new Vector2(-139,-203),new Vector2(235,52),ShowLevels,new Color(.77f,.83f,.70f));
            MakeButton(card,"VILLAGE",new Vector2(139,-203),new Vector2(235,52),Village,new Color(.77f,.83f,.70f));
        }
        public void StartRound()
        {
            combat=new RescueCombat();running=true;ended=paused=false;lastStage=-1;lastAttack=0;lastHealth=50;attackAge=10;
            levels.gameObject.SetActive(false);choose.gameObject.SetActive(false);play.gameObject.SetActive(true);pausePanel.gameObject.SetActive(false);resultPanel.gameObject.SetActive(false);
            if(hero!=null)Destroy(hero.Root.gameObject);hero=new RescueFighterView(actors,hen,false);
            SyncStage();ClearHolds();Render(0);
        }
        void ShowLevels()
        {
            running=false;paused=false;ClearHolds();levels.gameObject.SetActive(true);choose.gameObject.SetActive(false);play.gameObject.SetActive(false);pausePanel.gameObject.SetActive(false);resultPanel.gameObject.SetActive(false);
        }
        void ShowChoose(){levels.gameObject.SetActive(false);choose.gameObject.SetActive(true);}
        void Pause(bool value){paused=value;ClearHolds();pausePanel.gameObject.SetActive(value);}
        void Village(){ClearHolds();SceneManager.LoadScene("TownMap");}
        void ClearHolds(){foreach(var hold in holds)hold.Clear();}
        void Attack(){if(running&&!paused&&!appPaused&&!ended){combat.TryAttack();if(combat.AttackSerial!=lastAttack){lastAttack=combat.AttackSerial;attackAge=0;}}}
        void Update()
        {
            UpdateSafe();
            if(!running||ended||appPaused)return;
            var keyboard=Keyboard.current;
            if(keyboard!=null&&keyboard.escapeKey.wasPressedThisFrame)Pause(!paused);
            if(paused)return;
            Vector2 move=Vector2.zero;
            foreach(var hold in holds)if(hold.Held)move+=hold.Direction;
            if(keyboard!=null)
            {
                if(keyboard.aKey.isPressed||keyboard.leftArrowKey.isPressed)move.x--;
                if(keyboard.dKey.isPressed||keyboard.rightArrowKey.isPressed)move.x++;
                if(keyboard.wKey.isPressed||keyboard.upArrowKey.isPressed)move.y++;
                if(keyboard.sKey.isPressed||keyboard.downArrowKey.isPressed)move.y--;
                if(keyboard.spaceKey.wasPressedThisFrame)Attack();
            }
            float dt=Time.deltaTime;walkClock+=dt;attackAge+=dt;
            combat.Tick(dt,Vector2.ClampMagnitude(move,1));
            if(combat.HeroHealth<lastHealth)hurtUntil=Time.time+.24f;lastHealth=combat.HeroHealth;
            if(combat.StageIndex!=lastStage)SyncStage();
            Render(dt);
            if(combat.IsGameOver||combat.IsVictory)Finish();
        }
        void SyncStage()
        {
            lastStage=combat.StageIndex;stageViewStart=combat.StageStart;
            foreach(var view in enemies)Destroy(view.Root.gameObject);
            foreach(var bar in enemyBars)Destroy(bar.gameObject);
            enemies.Clear();enemyBars.Clear();enemyFills.Clear();enemyLabels.Clear();lastEnemyPositions.Clear();
            for(int i=0;i<combat.Enemies.Count;i++)
            {
                var view=new RescueFighterView(actors,false,true);enemies.Add(view);lastEnemyPositions.Add(combat.Enemies[i].Position);
                var bar=Panel("Roach health",actors,Vector2.zero,new Vector2(110,32),new Color(.08f,.16f,.11f,.93f));
                var fill=Panel("Remaining health",bar,new Vector2(0,-8),new Vector2(98,7),gold).GetComponent<Image>();fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;
                var label=TextAt(bar,"30 / 30",new Vector2(0,4),new Vector2(100,22),13,cream,true);
                enemyBars.Add(bar);enemyFills.Add(fill);enemyLabels.Add(label);
            }
        }
        Vector2 OnPath(Vector2 world)
        {
            float characterHeight=Height*.2f;
            float margin=characterHeight*.68f+8;
            return new Vector2(margin+(world.x-stageViewStart)/15f*(Width-2*margin),132+world.y/4f*Mathf.Max(20,Height-characterHeight-144));
        }
        void Place(RescueFighterView fighter,Vector2 world)
        {
            var p=OnPath(world);fighter.Root.anchorMin=fighter.Root.anchorMax=Vector2.zero;fighter.Root.anchoredPosition=p;
            fighter.Root.localScale=Vector3.one*(Height*.2f/167f);
        }
        void Render(float dt)
        {
            if(combat==null||hero==null)return;
            health.text=combat.HeroHealth+" / 50 HP";healthFill.fillAmount=combat.HeroHealth/50f;
            stageText.text="STAGE "+(combat.StageIndex+1)+" / 3";
            int alive=0;foreach(var enemy in combat.Enemies)if(enemy.Health>0)alive++;
            enemyCount.text=alive+" ROACH"+(alive==1?"":"ES")+" REMAINING";
            var remaining=TimeSpan.FromSeconds(Math.Ceiling(combat.TimeRemaining));timer.text=remaining.ToString(@"hh\:mm\:ss");
            goalEgg.gameObject.SetActive(combat.StageIndex==2);
            goalEgg.anchoredPosition=new Vector2(Width-100,Height*.5f);
            Place(hero,combat.HeroPosition);hero.Animate(walkClock,combat.HeroWalking,combat.Facing,attackAge,combat.LastSlashDirection,combat.HeroHealth<=0,Time.time<hurtUntil);
            for(int i=0;i<enemies.Count;i++)
            {
                var enemy=combat.Enemies[i];bool dead=enemy.Health<=0;
                enemies[i].Root.gameObject.SetActive(!dead);
                bool walking=Vector2.Distance(lastEnemyPositions[i],enemy.Position)>.0001f;
                Place(enemies[i],enemy.Position);
                enemies[i].Animate(walkClock+i,walking,enemy.Facing,enemy.AttackAge,enemy.AttackSerial%2==0?1:-1,enemy.Mode==RescueEnemyMode.Lying,enemy.Mode==RescueEnemyMode.Damaged);
                lastEnemyPositions[i]=enemy.Position;
                var bar=enemyBars[i];bar.gameObject.SetActive(!dead&&enemy.HealthVisible);bar.anchorMin=bar.anchorMax=Vector2.zero;var barPosition=OnPath(enemy.Position)+Vector2.up*(Height*.2f+17);
                barPosition.x=Mathf.Clamp(barPosition.x,65,Width-65);barPosition.y=Mathf.Min(Height-112,barPosition.y);bar.anchoredPosition=barPosition;
                enemyFills[i].fillAmount=enemy.Health/30f;enemyLabels[i].text=enemy.Mode==RescueEnemyMode.Lying?"DOWN · "+Mathf.CeilToInt(enemy.ModeTime)+"s":enemy.Health+" / 30";
            }
            // Feet determine draw order, as in classic belt-scrolling brawlers.
            var sorted=new List<RescueFighterView>(enemies);sorted.Add(hero);sorted.Sort((a,b)=>b.Root.anchoredPosition.y.CompareTo(a.Root.anchoredPosition.y));
            foreach(var view in sorted)view.Root.SetAsLastSibling();foreach(var bar in enemyBars)bar.SetAsLastSibling();
            goSign.gameObject.SetActive(combat.StageCleared&&!combat.IsVictory);
            if(goSign.gameObject.activeSelf)goSign.anchoredPosition=new Vector2(Mathf.Sin(Time.time*4)*12,-125);
            message.text=combat.StageCleared?"Path clear! Walk past the right edge to continue.":"One roach test · Tap FIGHT for every swing · Close the gap after knockback";
        }
        void Finish()
        {
            ended=true;ClearHolds();resultPanel.gameObject.SetActive(true);
            resultTitle.text=combat.IsVictory?"THE EGG IS SAFE!":"RESCUE ENDED";
            resultBody.text=combat.IsVictory?"Three stages cleared. Your little guardian brought the egg home.":combat.HeroHealth<=0?"Your guardian ran out of health. Choose Play Again to retry.":"The 2-hour rescue time has ended. Try the woodland path again.";
        }
        void OnApplicationPause(bool value){appPaused=value;if(value)ClearHolds();}
        void OnApplicationFocus(bool value){appPaused=!value;if(!value)ClearHolds();}
        void UpdateSafe()
        {
            if(!safe)return;Rect area=Screen.safeArea;if(area==lastSafe&&lastWidth==Screen.width&&lastHeight==Screen.height)return;
            lastSafe=area;lastWidth=Screen.width;lastHeight=Screen.height;
            if(Screen.width<=0||Screen.height<=0)return;
            safe.anchorMin=new Vector2(area.xMin/Screen.width,area.yMin/Screen.height);safe.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);
        }
        RectTransform ScreenRoot(string name){var root=Rect(name,layout,Vector2.zero,Vector2.zero);Stretch(root);return root;}
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        static RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return r;
        }
        RectTransform Panel(string name,Transform parent,Vector2 pos,Vector2 size,Color color)
        {
            var r=Rect(name,parent,pos,size);var image=r.gameObject.AddComponent<Image>();image.sprite=RescueArt.Rounded();image.type=Image.Type.Sliced;image.color=color;return r;
        }
        RectTransform Picture(string name,Transform parent,Sprite sprite,Vector2 pos,Vector2 size)
        {
            var r=Rect(name,parent,pos,size);var image=r.gameObject.AddComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return r;
        }
        Text TextAt(Transform parent,string value,Vector2 pos,Vector2 size,int fontSize,Color color,bool bold=false)
        {
            var r=Rect(value,parent,pos,size);var text=r.gameObject.AddComponent<Text>();text.font=font;text.text=value;text.fontSize=fontSize;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;text.raycastTarget=false;return text;
        }
        Button MakeButton(Transform parent,string label,Vector2 pos,Vector2 size,UnityEngine.Events.UnityAction action,Color color)
        {
            var r=Panel(label,parent,pos,size,color);var button=r.gameObject.AddComponent<Button>();button.targetGraphic=r.GetComponent<Image>();button.onClick.AddListener(action);
            TextAt(r,label,Vector2.zero,size-new Vector2(12,6),20,ink,true);return button;
        }
    }
    public sealed class RescueHoldButton : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        public Vector2 Direction;
        public bool Held { get; private set; }
        int pointer=int.MinValue;
        public void OnPointerDown(PointerEventData e){if(pointer!=int.MinValue)return;pointer=e.pointerId;Held=true;}
        public void OnPointerUp(PointerEventData e){if(e.pointerId==pointer)Clear();}
        public void OnPointerExit(PointerEventData e){if(e.pointerId==pointer)Clear();}
        public void Clear(){Held=false;pointer=int.MinValue;}
        void OnDisable(){Clear();}
    }
}
