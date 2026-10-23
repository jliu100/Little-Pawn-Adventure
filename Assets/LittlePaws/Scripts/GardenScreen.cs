using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace LittlePaws
{
    /// <summary>The farming screen shares the village's real 3D garden and its safe-area canvas.</summary>
    public sealed class GardenScreen : MonoBehaviour
    {
        enum Tool { Corn, Tomato, Sickle }
        RectTransform parent, root, shop, shade;
        Sprite rounded;
        Font font;
        GardenWorldView world;
        Camera cameraView;
        GardenProgress progress;
        GardenScarecrowGame scareGame;
        Action onClose;
        Tool selected;
        Text levelText, coinsText, inventoryText, guidance, toast, cornText, tomatoText, dailyText;
        Text shopCoins, shopCorn, shopTomato;
        Image skillFill;
        Button cornTool,tomatoTool,sickleTool,buyCorn,buyTomato,sellCorn,sellTomato,dailyButton;
        readonly Button[] plotButtons=new Button[4];
        readonly Text[] plotLabels=new Text[4];
        readonly List<Sprite> icons=new List<Sprite>();
        Sprite cornIcon,tomatoIcon,sickleIcon;
        bool visible,shopOpen;
        float nextRefresh,toastUntil;
        readonly Color ink=new Color(.19f,.28f,.23f),cream=new Color(.98f,.96f,.88f),green=new Color(.36f,.58f,.33f);
        public GardenProgress Progress=>progress;
        public bool IsOpen=>visible;
        public void Initialize(RectTransform safeArea,Sprite panelSprite,Font uiFont,GardenWorldView gardenWorld,Camera view,Action close)
        {
            parent=safeArea;rounded=panelSprite;font=uiFont;world=gardenWorld;cameraView=view;onClose=close;
            progress=GardenProgressStore.Load();
            if(progress.ScarecrowGameActive){progress.AbandonScarecrowGame();GardenProgressStore.Save(progress);}
            cornIcon=Icon(0);tomatoIcon=Icon(1);sickleIcon=Icon(2);
            root=Stretch("Garden farming screen",parent);
            var header=Panel("Garden header",root,new Vector2(16,-16),new Vector2(1408,94),cream);
            header.anchorMax=new Vector2(1,1);header.sizeDelta=new Vector2(-32,94);
            Label(header,"GARDEN",new Vector2(18,-10),new Vector2(200,30),26,true);
            Label(header,"OPEN FOR TESTING",new Vector2(18,-48),new Vector2(205,24),14);
            levelText=Label(header,"",new Vector2(260,-12),new Vector2(720,26),20,true);
            skillFill=Bar(header,new Vector2(260,-51),new Vector2(620,14));
            coinsText=Label(header,"",new Vector2(-18,-17),new Vector2(220,28),23,true);AnchorRight(coinsText.rectTransform);
            inventoryText=Label(header,"",new Vector2(-18,-54),new Vector2(300,24),16);AnchorRight(inventoryText.rectTransform);
            var tools=Panel("Garden tools",root,new Vector2(16,-130),new Vector2(220,345),cream);
            Label(tools,"PLANT & HARVEST",new Vector2(14,-12),new Vector2(194,25),18,true);
            cornTool=ToolButton(tools,cornIcon,new Vector2(13,-51),()=>Select(Tool.Corn),out cornText);
            tomatoTool=ToolButton(tools,tomatoIcon,new Vector2(13,-127),()=>Select(Tool.Tomato),out tomatoText);
            sickleTool=ToolButton(tools,sickleIcon,new Vector2(13,-203),()=>Select(Tool.Sickle),out var sickleText);sickleText.text="Sickle\nHarvest ripe crops";
            Label(tools,"Seeds use 1 field each.\nCrops keep growing while\nyou are away.",new Vector2(14,-277),new Vector2(194,59),14);
            var market=Panel("Garden market",root,new Vector2(-16,-130),new Vector2(226,196),cream);AnchorRight(market);
            Label(market,"GARDEN STORE",new Vector2(14,-12),new Vector2(198,26),19,true);
            Button(market,"Buy seeds / Sell produce",new Vector2(12,-54),new Vector2(202,50),()=>SetShop(true),16);
            Label(market,"Corn: buy 2 / sell 1 each\nTomato: buy 3 / sell 2 each",new Vector2(14,-119),new Vector2(198,58),15);
            for(int i=0;i<4;i++)
            {
                int index=i;plotButtons[i]=Button(root,"",Vector2.zero,new Vector2(126,60),()=>TapPlot(index),15);
                var r=plotButtons[i].GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);
                plotLabels[i]=plotButtons[i].GetComponentInChildren<Text>();
            }
            dailyButton=Button(root,"",Vector2.zero,new Vector2(174,58),StartScarecrow,15);
            var dailyRect=dailyButton.GetComponent<RectTransform>();dailyRect.anchorMin=dailyRect.anchorMax=dailyRect.pivot=new Vector2(.5f,.5f);
            dailyText=dailyButton.GetComponentInChildren<Text>();
            var footer=Panel("Garden instructions",root,new Vector2(16,16),new Vector2(1408,74),cream);
            footer.anchorMin=Vector2.zero;footer.anchorMax=new Vector2(1,0);footer.pivot=Vector2.zero;footer.sizeDelta=new Vector2(-32,74);
            Button(footer,"Back to village",new Vector2(12,-15),new Vector2(180,44),()=>onClose?.Invoke(),17);
            guidance=Label(footer,"",new Vector2(215,-10),new Vector2(900,55),17);
            var toastPanel=Panel("Garden message",root,new Vector2(0,106),new Vector2(740,48),new Color(.98f,.94f,.78f));
            toastPanel.anchorMin=toastPanel.anchorMax=new Vector2(.5f,0);toastPanel.pivot=new Vector2(.5f,0);
            toast=Label(toastPanel,"",new Vector2(12,-7),new Vector2(716,36),16,true);toast.alignment=TextAnchor.MiddleCenter;toastPanel.gameObject.SetActive(false);
            BuildShop();
            scareGame=new GameObject("Daily scarecrow game controller").AddComponent<GardenScarecrowGame>();scareGame.transform.SetParent(transform,false);
            scareGame.Initialize(root,rounded,font,FinishScarecrow,AbandonScarecrow);
            Select(Tool.Corn);root.gameObject.SetActive(false);
        }
        public void Show()
        {
            visible=true;root.gameObject.SetActive(true);root.SetAsLastSibling();SetShop(false);Refresh();
            Notify("3 fields ready. Choose a seed, then tap an empty field.");
        }
        public void Hide()
        {
            if(scareGame)scareGame.Hide();
            visible=false;root.gameObject.SetActive(false);GardenProgressStore.Save(progress);
        }
        void Select(Tool tool)
        {
            selected=tool;
            if(cornTool)cornTool.image.color=tool==Tool.Corn?new Color(.78f,.88f,.60f):new Color(.91f,.86f,.73f);
            if(tomatoTool)tomatoTool.image.color=tool==Tool.Tomato?new Color(.78f,.88f,.60f):new Color(.91f,.86f,.73f);
            if(sickleTool)sickleTool.image.color=tool==Tool.Sickle?new Color(.78f,.88f,.60f):new Color(.91f,.86f,.73f);
            guidance.text=tool==Tool.Sickle?"Sickle selected. Tap a ripe field to harvest 5 crops.\nCorn: +3 gardening points. Tomato: +4 gardening points.":
                (tool==Tool.Corn?"Corn seed selected / 2-minute growth.":"Tomato seed selected / 3-minute growth.")+" Tap an empty field.\nSeed stage -> halfway grown -> ripe for harvest.";
        }
        public bool TapPlot(int index)
        {
            if(!visible||shopOpen||scareGame.IsShowing)return false;
            if(index>=progress.UnlockedPlotCount){Notify("Field 4 unlocks at gardening level 5.");return false;}
            DateTime now=DateTime.UtcNow;var crop=progress.Plots[index].Crop;
            if(crop==GardenCrop.None)
            {
                if(selected==Tool.Sickle){Notify("Choose corn or tomato seeds to plant an empty field.");return false;}
                crop=selected==Tool.Corn?GardenCrop.Corn:GardenCrop.Tomato;
                if(!progress.Plant(index,crop,now)){Notify("No seeds left. Buy more in the Garden store.");return false;}
                Changed("Planted "+Name(crop)+". It will be ready in "+(GardenProgress.GrowSeconds(crop)/60)+" minutes.");return true;
            }
            if(progress.StageAt(index,now)!=GardenGrowthStage.Harvest){Notify(Name(crop)+" is still growing / "+Clock(progress.SecondsRemaining(index,now))+" remaining.");return false;}
            if(selected!=Tool.Sickle){Notify("Select the sickle, then tap this ripe field to harvest.");return false;}
            int oldLevel=progress.Level;
            if(!progress.Harvest(index,now))return false;
            Changed("Harvested 5 "+Name(crop)+" / +"+GardenProgress.HarvestPoints(crop)+" gardening points"+(progress.Level>oldLevel?" / Level up!":""));return true;
        }
        void Changed(string message){GardenProgressStore.Save(progress);Refresh();Notify(message);}
        void Refresh()
        {
            DateTime now=DateTime.UtcNow;
            levelText.text="GARDENING LEVEL "+progress.Level+"  /  "+progress.PointsInLevel+" / "+progress.PointsToNextLevel+" points";
            skillFill.fillAmount=progress.PointsInLevel/(float)progress.PointsToNextLevel;
            coinsText.text=progress.Coins+" COINS";inventoryText.text="Harvest: "+progress.CornProduce+" corn / "+progress.TomatoProduce+" tomato";
            cornText.text="Corn seeds: "+progress.CornSeeds+"\n2 minutes";tomatoText.text="Tomato seeds: "+progress.TomatoSeeds+"\n3 minutes";
            for(int i=0;i<4;i++)
            {
                bool unlocked=i<progress.UnlockedPlotCount;var crop=progress.Plots[i].Crop;var stage=progress.StageAt(i,now);
                plotButtons[i].interactable=unlocked;
                plotLabels[i].text=!unlocked?"FIELD 4\nGarden level 5":crop==GardenCrop.None?"FIELD "+(i+1)+"\nTap to plant":
                    stage==GardenGrowthStage.Harvest?Name(crop)+" / RIPE\nSickle: harvest x5":Name(crop)+" / "+(stage==GardenGrowthStage.Seed?"SEED":"GROWING")+"\n"+Clock(progress.SecondsRemaining(i,now));
                plotButtons[i].image.color=!unlocked?new Color(.68f,.67f,.60f,.96f):stage==GardenGrowthStage.Harvest?new Color(.97f,.85f,.40f,.96f):cream;
            }
            bool daily=progress.CanPlayScarecrow(now);
            dailyText.text=daily?"SCARECROW\nDaily crop guard":"SCARECROW\nPlayed today (UTC)";
            if(world)world.Refresh(progress,now);
            if(shopOpen)RefreshShop();
        }
        void Update()
        {
            if(!visible)return;
            if(Time.unscaledTime>=nextRefresh){nextRefresh=Time.unscaledTime+.25f;Refresh();}
            if(toast.transform.parent.gameObject.activeSelf&&Time.unscaledTime>toastUntil)toast.transform.parent.gameObject.SetActive(false);
            if(scareGame.IsShowing||shopOpen)return;
            for(int i=0;i<4;i++)Place(plotButtons[i].GetComponent<RectTransform>(),world.PlotWorldPosition(i)+new Vector3(0,.12f,-.45f),new Vector2(0,-25));
            Place(dailyButton.GetComponent<RectTransform>(),world.Scarecrow.position+Vector3.up*2.25f,Vector2.zero);
            Vector2 screen=Vector2.zero;bool tap=false;
            if(Touchscreen.current!=null&&Touchscreen.current.primaryTouch.press.wasPressedThisFrame){screen=Touchscreen.current.primaryTouch.position.ReadValue();tap=true;}
            else if(Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame){screen=Mouse.current.position.ReadValue();tap=true;}
            if(!tap||HitUI(screen))return;
            Physics.SyncTransforms();
            if(Physics.Raycast(cameraView.ScreenPointToRay(screen),out var hit,100))
            {
                if(world.OwnsScarecrowHit(hit.collider))StartScarecrow();
                else{int plot=world.PlotIndex(hit.collider);if(plot>=0)TapPlot(plot);}
            }
        }
        bool HitUI(Vector2 screen)
        {
            if(!UnityEngine.EventSystems.EventSystem.current)return false;
            var hits=new List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=screen},hits);return hits.Count>0;
        }
        void Place(RectTransform label,Vector3 position,Vector2 offset)
        {
            Vector3 screen=cameraView.WorldToScreenPoint(position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screen,null,out var point);
            point+=offset;point.y=Mathf.Clamp(point.y,root.rect.yMin+190,root.rect.yMax-150);label.localPosition=(Vector3)point;
        }
        void StartScarecrow()
        {
            if(!visible||shopOpen||scareGame.IsShowing)return;
            if(!progress.TryStartScarecrowGame(DateTime.UtcNow)){Notify("One scarecrow game per UTC day. Come back after midnight UTC.");return;}
            GardenProgressStore.Save(progress);scareGame.Begin();
        }
        void FinishScarecrow(int remaining)
        {
            int reward=progress.CompleteScarecrowGame(remaining);Changed(remaining+" crops protected / +"+reward+" gardening points.");
        }
        void AbandonScarecrow(){if(!progress.ScarecrowGameActive)return;progress.AbandonScarecrowGame();GardenProgressStore.Save(progress);nextRefresh=0;}
        void BuildShop()
        {
            shade=Stretch("Garden store backdrop",root);shade.gameObject.AddComponent<Image>().color=new Color(.09f,.15f,.12f,.70f);
            shop=Panel("Garden seed shop",shade,Vector2.zero,new Vector2(720,480),cream);shop.anchorMin=shop.anchorMax=shop.pivot=new Vector2(.5f,.5f);
            Label(shop,"GARDEN STORE",new Vector2(24,-16),new Vector2(470,34),27,true);
            shopCoins=Label(shop,"",new Vector2(494,-24),new Vector2(200,26),20,true);
            Label(shop,"SEEDS / one seed plants one field",new Vector2(24,-70),new Vector2(650,25),18,true);
            AddIcon(shop,cornIcon,new Vector2(25,-109),new Vector2(52,52));
            shopCorn=Label(shop,"",new Vector2(88,-113),new Vector2(365,48),17);
            buyCorn=Button(shop,"Buy corn seed / 2 coins",new Vector2(462,-110),new Vector2(234,46),()=>Buy(GardenCrop.Corn),16);
            AddIcon(shop,tomatoIcon,new Vector2(25,-179),new Vector2(52,52));
            shopTomato=Label(shop,"",new Vector2(88,-183),new Vector2(365,48),17);
            buyTomato=Button(shop,"Buy tomato seed / 3 coins",new Vector2(462,-180),new Vector2(234,46),()=>Buy(GardenCrop.Tomato),16);
            Label(shop,"SELL YOUR HARVEST",new Vector2(24,-259),new Vector2(650,26),18,true);
            sellCorn=Button(shop,"Sell all corn / 1 coin each",new Vector2(24,-307),new Vector2(323,50),()=>Sell(GardenCrop.Corn),17);
            sellTomato=Button(shop,"Sell all tomatoes / 2 coins each",new Vector2(369,-307),new Vector2(327,50),()=>Sell(GardenCrop.Tomato),17);
            Button(shop,"Back to fields",new Vector2(24,-407),new Vector2(672,46),()=>SetShop(false),18);
            shade.gameObject.SetActive(false);
        }
        void SetShop(bool value){shopOpen=value;shade.gameObject.SetActive(value);if(value){shade.SetAsLastSibling();RefreshShop();}}
        void RefreshShop()
        {
            shopCoins.text=progress.Coins+" COINS";
            shopCorn.text="Corn seeds: "+progress.CornSeeds+" / Harvest: "+progress.CornProduce+"\n2 minutes -> 5 corn, +3 skill";
            shopTomato.text="Tomato seeds: "+progress.TomatoSeeds+" / Harvest: "+progress.TomatoProduce+"\n3 minutes -> 5 tomatoes, +4 skill";
            buyCorn.interactable=progress.Coins>=2;buyTomato.interactable=progress.Coins>=3;
            sellCorn.interactable=progress.CornProduce>0;sellTomato.interactable=progress.TomatoProduce>0;
        }
        void Buy(GardenCrop crop){if(progress.BuySeed(crop))Changed("Bought 1 "+Name(crop)+" seed.");else Notify("Not enough coins.");}
        void Sell(GardenCrop crop){int coins=progress.SellProduce(crop);if(coins>0)Changed("Sold "+Name(crop)+" harvest / +"+coins+" coins.");}
        void Notify(string message){toast.text=message;toastUntil=Time.unscaledTime+4f;toast.transform.parent.gameObject.SetActive(true);}
        void OnApplicationPause(bool paused){if(paused&&progress!=null)GardenProgressStore.Save(progress);}
        void OnDestroy(){foreach(var sprite in icons)if(sprite){Destroy(sprite.texture);Destroy(sprite);}}
        static string Name(GardenCrop crop)=>crop==GardenCrop.Corn?"Corn":"Tomato";
        static string Clock(double value){int seconds=(int)Math.Ceiling(value);return (seconds/60)+":"+(seconds%60).ToString("00");}
        RectTransform Stretch(string name,Transform holder)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(holder,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.sizeDelta=Vector2.zero;return r;
        }
        RectTransform Panel(string name,Transform holder,Vector2 at,Vector2 size,Color color)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(holder,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=at;r.sizeDelta=size;
            var image=r.gameObject.AddComponent<Image>();image.sprite=rounded;image.type=Image.Type.Sliced;image.color=color;return r;
        }
        static void AnchorRight(RectTransform r){r.anchorMin=r.anchorMax=r.pivot=Vector2.one;}
        Text Label(Transform holder,string text,Vector2 at,Vector2 size,int fontSize,bool bold=false)
        {
            var r=new GameObject(text,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(holder,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=at;r.sizeDelta=size;
            var label=r.gameObject.AddComponent<Text>();label.text=text;label.font=font;label.fontSize=fontSize;label.color=ink;label.raycastTarget=false;label.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;return label;
        }
        Button Button(Transform holder,string text,Vector2 at,Vector2 size,UnityEngine.Events.UnityAction action,int fontSize)
        {
            var r=Panel(text,holder,at,size,new Color(.91f,.86f,.73f));var button=r.gameObject.AddComponent<Button>();button.targetGraphic=r.GetComponent<Image>();button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(action);
            var label=Label(r,text,Vector2.zero,size,fontSize,true);label.alignment=TextAnchor.MiddleCenter;return button;
        }
        Button ToolButton(Transform holder,Sprite icon,Vector2 at,Action action,out Text text)
        {
            var button=Button(holder,"",at,new Vector2(194,64),()=>action(),16);button.GetComponentInChildren<Text>().gameObject.SetActive(false);
            AddIcon(button.transform,icon,new Vector2(7,-8),new Vector2(45,45));text=Label(button.transform,"",new Vector2(59,-8),new Vector2(128,50),16,true);return button;
        }
        void AddIcon(Transform holder,Sprite sprite,Vector2 at,Vector2 size)
        {
            var r=new GameObject("Tool icon",typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(holder,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=at;r.sizeDelta=size;
            var image=r.gameObject.AddComponent<Image>();image.sprite=sprite;image.raycastTarget=false;
        }
        Image Bar(Transform holder,Vector2 at,Vector2 size)
        {
            var back=Panel("Gardening skill bar",holder,at,size,new Color(.79f,.81f,.69f));
            var r=Stretch("Gardening skill fill",back);var fill=r.gameObject.AddComponent<Image>();fill.sprite=rounded;fill.color=green;fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.raycastTarget=false;return fill;
        }
        Sprite Icon(int type)
        {
            const int size=64;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                Color color=Color.clear;
                if(type==0)
                {
                    if(Ellipse(x,y,32,31,12,24))color=new Color(.96f,.76f,.16f);
                    if(Ellipse(x,y,22,21,8,21)||Ellipse(x,y,43,20,7,20))color=green;
                    if(x>24&&x<41&&y>17&&y<50&&(x%7==0||y%7==0))color=new Color(.85f,.57f,.13f);
                }
                else if(type==1)
                {
                    if(Ellipse(x,y,32,28,23,21))color=new Color(.86f,.22f,.16f);
                    if(Ellipse(x,y,23,34,7,10))color=new Color(1f,.43f,.26f);
                    if(Segment(x,y,20,48,43,48)<3||Segment(x,y,32,43,30,58)<3||Segment(x,y,19,42,43,54)<2.5f)color=green;
                }
                else
                {
                    if(Segment(x,y,13,7,31,29)<5)color=new Color(.49f,.29f,.15f);
                    float radius=new Vector2(x-32,y-37).magnitude,angle=Mathf.Atan2(y-37,x-32)*Mathf.Rad2Deg;
                    if(radius>16&&radius<22&&angle>-28&&angle<215)color=new Color(.72f,.80f,.78f);
                    if(Segment(x,y,30,28,47,26)<3)color=new Color(.51f,.61f,.59f);
                }
                pixels[x+y*size]=color;
            }
            texture.SetPixels(pixels);texture.Apply();texture.filterMode=FilterMode.Bilinear;
            var sprite=Sprite.Create(texture,new Rect(0,0,size,size),Vector2.one*.5f,100);icons.Add(sprite);return sprite;
        }
        static bool Ellipse(float x,float y,float cx,float cy,float rx,float ry)=>Mathf.Pow((x-cx)/rx,2)+Mathf.Pow((y-cy)/ry,2)<=1;
        static float Segment(float x,float y,float ax,float ay,float bx,float by)
        {
            var a=new Vector2(ax,ay);var delta=new Vector2(bx-ax,by-ay);var p=new Vector2(x,y);return Vector2.Distance(p,a+delta*Mathf.Clamp01(Vector2.Dot(p-a,delta)/delta.sqrMagnitude));
        }
    }
}
