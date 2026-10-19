using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace LittlePaws
{
    public sealed class PetHomeGame : MonoBehaviour
    {
        public const float WashDuration = 2.5f;
        public bool townMap;
        readonly Dictionary<string,TownLocation> places = new Dictionary<string,TownLocation>();
        readonly Dictionary<TownLocation,RectTransform> placeLabels = new Dictionary<TownLocation,RectTransform>();
        TownLocation pendingVisit;
        RectTransform placeCard;
        Text placeTitle, placeDescription;
        GameObject enterHomeButton;
        public bool Puppy { get; private set; }
        public float Cleanliness { get; private set; } = 35;
        public float WashProgress { get; private set; }
        public bool StatusVisible { get; private set; }
        public Vector3 PetPosition => pet ? pet.position : Vector3.zero;
        Transform pet;
        PetVisual visual;
        Camera cam;
        RectTransform safeRoot, soap, soapDock;
        GameObject statusPanel;
        Text hint, washText, speciesText;
        Image washFill, cleanFill;
        Text cleanValue;
        readonly List<Bubble> bubbles = new List<Bubble>();
        Font font;
        bool dragging, wasRubbing;
        int soapPointer = int.MinValue;
        Vector2 lastSoapScreen;
        FloorPathfinder navigation;
        readonly Queue<Vector3> route = new Queue<Vector3>();
        readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        Transform destinationMarker;
        public bool IsWalking => route.Count > 0;
        public Vector3? Destination { get; private set; }
        float emission, lastActive, attention;
        Sprite rounded, soapSprite;
        Material bubbleMaterial;
        readonly List<Material> effectMaterials = new List<Material>();
        Rect lastSafe;
        int lastWidth, lastHeight;
        readonly Color ink = new Color(.25f,.21f,.19f), cream = new Color(.98f,.96f,.91f);
        class Bubble { public Transform t; public float age, life, size, phase; public Vector3 origin; }

        void Start()
        {
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToPortrait = false; Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true; Screen.autorotateToLandscapeRight = true;
            Application.targetFrameRate = 60;
            var home = townMap ? TownVisual.Build() : HomeVisual.Build();
            if(townMap) foreach(var place in home.GetComponentsInChildren<TownLocation>())places.Add(place.Id,place);
            // Auto-sync is disabled: flush generated positions/scales before caching bounds.
            // Otherwise the navigation snapshot can retain default colliders at the origin.
            Physics.SyncTransforms();
            var floor = home.GetComponentInChildren<HomeFloorSurface>().GetComponent<BoxCollider>();
            var obstacles = new List<Bounds>();
            foreach(var c in home.GetComponentsInChildren<Collider>())
                if(c.enabled && !c.GetComponent<HomeFloorSurface>()) obstacles.Add(c.bounds);
            navigation = new FloorPathfinder(floor.bounds, obstacles, .48f, townMap ? .5f : .25f);
            var marker = new GameObject("Walking destination",typeof(LineRenderer));
            var line = marker.GetComponent<LineRenderer>();
            line.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            line.sharedMaterial.SetColor("_BaseColor",new Color(.98f,.77f,.33f));
            effectMaterials.Add(line.sharedMaterial);
            line.useWorldSpace=false;line.loop=true;line.widthMultiplier=.035f;line.positionCount=32;
            for(int i=0;i<32;i++){float angle=i*Mathf.PI*2/32;line.SetPosition(i,new Vector3(Mathf.Cos(angle)*.2f,.025f,Mathf.Sin(angle)*.2f));}
            destinationMarker=marker.transform;marker.SetActive(false);
            cam = Camera.main;
            if (!cam) cam = new GameObject("Home Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
            cam.transform.position = townMap ? new Vector3(15,22,-28) : new Vector3(7.8f,9.2f,-12.5f);
            cam.transform.LookAt(new Vector3(0,.4f,0));
            cam.orthographic = true; cam.orthographicSize = 5.8f;
            cam.backgroundColor = new Color(.78f,.83f,.79f); cam.clearFlags = CameraClearFlags.SolidColor;
            cam.nearClipPlane = .1f; cam.farClipPlane = 80;
            if (!FindAnyObjectByType<Light>())
            {
                var l = new GameObject("Afternoon sunlight",typeof(Light)).GetComponent<Light>();
                l.type=LightType.Directional; l.intensity=1.8f; l.color=new Color(1,.89f,.72f);
                l.transform.rotation=Quaternion.Euler(48,-30,0); l.shadows=LightShadows.Soft;
            }
            RenderSettings.ambientLight = new Color(.66f,.69f,.72f);
            pet = new GameObject("Companion").transform; pet.position=townMap ? new Vector3(0,0,-2) : new Vector3(0,0,-.45f);
            visual=pet.gameObject.AddComponent<PetVisual>();
            Puppy=PlayerPrefs.GetInt("LittlePaws.Species",0)==1;
            Cleanliness=Mathf.Clamp(PlayerPrefs.GetFloat(Puppy ? "LittlePaws.Clean.Puppy" : "LittlePaws.Clean.Kitten",35),0,100);
            visual.Build(Puppy); pet.rotation=FacingCamera();
            BuildUI();
            if(townMap)return; // Bath effects belong to the interior scene.
            bubbleMaterial = new Material(Resources.Load<Material>("LittlePaws/Bubble"));
            bubbleMaterial.SetColor("_BaseColor",new Color(.68f,.89f,1f,.43f));
            bubbleMaterial.SetFloat("_Surface",1); bubbleMaterial.SetFloat("_ZWrite",0);
            bubbleMaterial.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            bubbleMaterial.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            bubbleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); bubbleMaterial.renderQueue=3000;
            for(int i=0;i<28;i++)
            {
                var b=GameObject.CreatePrimitive(PrimitiveType.Sphere); b.name="Soap bubble";
                Destroy(b.GetComponent<Collider>()); b.GetComponent<Renderer>().sharedMaterial=bubbleMaterial;
                var shine=GameObject.CreatePrimitive(PrimitiveType.Sphere); shine.name="Bubble glint"; Destroy(shine.GetComponent<Collider>());
                shine.transform.SetParent(b.transform,false); shine.transform.localPosition=new Vector3(-.22f,.25f,-.31f); shine.transform.localScale=Vector3.one*.15f;
                var m=new Material(Shader.Find("Universal Render Pipeline/Unlit")); m.SetColor("_BaseColor",Color.white); shine.GetComponent<Renderer>().sharedMaterial=m;effectMaterials.Add(m);
                b.SetActive(false); bubbles.Add(new Bubble{t=b.transform});
            }
        }

        void BuildUI()
        {
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); rounded=RoundedSprite(); soapSprite=SoapSprite();
            var canvasGO=new GameObject("Landscape UI",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=canvasGO.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasGO.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1440,810);scaler.matchWidthOrHeight=.5f;
            safeRoot=Rect("Safe area",canvasGO.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var events=FindAnyObjectByType<EventSystem>();
            if(!events) events=new GameObject("Event System",typeof(EventSystem)).GetComponent<EventSystem>();
            if(!events.GetComponent<InputSystemUIInputModule>())
            {
                var old=events.GetComponent<StandaloneInputModule>(); if(old) Destroy(old);
                events.gameObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            var header=Panel("Header",safeRoot,new Vector2(0,1),new Vector2(0,1),new Vector2(22,-22),new Vector2(410,94),cream);
            Label(header,townMap?"LITTLE PAWS / VILLAGE":"LITTLE PAWS ADVENTURE",new Vector2(20,-14),new Vector2(370,30),24,ink,FontStyle.Bold);
            speciesText=Label(header,Puppy?"Mochi / small puppy":"Miso / little kitten",new Vector2(20,-48),new Vector2(365,28),19,ink);
            var choices=Panel("Choose companion",safeRoot,Vector2.one,Vector2.one,new Vector2(-22,-22),new Vector2(304,94),cream);
            Label(choices,"YOUR COMPANION",new Vector2(18,-10),new Vector2(268,22),14,ink,FontStyle.Bold);
            Button(choices,"Kitten",new Vector2(15,-38),new Vector2(130,42),()=>Switch(false));
            Button(choices,"Puppy",new Vector2(157,-38),new Vector2(130,42),()=>Switch(true));
            var card=Panel("Status after washing",safeRoot,new Vector2(0,1),new Vector2(0,1),new Vector2(22,-132),new Vector2(270,202),cream);
            statusPanel=card.gameObject;
            Label(card,"PET STATUS",new Vector2(18,-12),new Vector2(238,25),19,ink,FontStyle.Bold);
            Meter(card,"Health",82,48,new Color(.43f,.68f,.48f),out _,out _);
            Meter(card,"Clean",Cleanliness,96,new Color(.36f,.69f,.79f),out cleanFill,out cleanValue);
            Meter(card,"Hunger",58,144,new Color(.87f,.60f,.32f),out _,out _);
            statusPanel.SetActive(false);
            var footer=Panel("Care tray",safeRoot,Vector2.zero,Vector2.zero,new Vector2(22,22),new Vector2(475,126),cream);
            soapDock=Rect("Soap dock",footer,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(79,0),new Vector2(122,88));
            soapDock.pivot=new Vector2(.5f,.5f);
            soap=Rect("Draggable soap",soapDock,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(122,88));
            var soapImage=soap.gameObject.AddComponent<Image>();soapImage.sprite=soapSprite;soapImage.preserveAspect=true;
            soap.gameObject.AddComponent<SoapDrag>().owner=this;
            Label(footer,"BATH TIME",new Vector2(159,-14),new Vector2(280,28),21,ink,FontStyle.Bold);
            washText=Label(footer,"Drag soap over your pet",new Vector2(159,-47),new Vector2(295,30),17,ink);
            washFill=Bar(footer,new Vector2(159,-90),new Vector2(280,9),new Color(.36f,.69f,.79f),0);
            var instructions=Panel("Instruction",safeRoot,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(15,-130),new Vector2(490,42),new Color(.98f,.96f,.91f,.93f));
            hint=Label(instructions,"A little home. A lot of love.",new Vector2(14,-7),new Vector2(462,30),17,ink);hint.alignment=TextAnchor.MiddleCenter;
            hint.text=townMap ? "Explore the village. Tap a place to visit." : "Tap an empty spot on the floor to walk.";
            if(townMap)
            {
                footer.gameObject.SetActive(false);
                BuildTownUI();
            }
            else
            {
                var mapButton=Button(safeRoot,"Village map",Vector2.zero,new Vector2(175,48),()=>LoadScene("TownMap"));
                var rect=mapButton.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=Vector2.one;rect.pivot=Vector2.one;rect.anchoredPosition=new Vector2(-22,-132);
            }
        }

        void BuildTownUI()
        {
            var tray=Panel("Village destinations",safeRoot,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,22),new Vector2(960,92),cream);
            Label(tray,"EXPLORE YOUR VILLAGE",new Vector2(18,-8),new Vector2(800,22),14,ink,FontStyle.Bold);
            string[] ids={"home","weapon","hospital","food","adventure"};
            for(int i=0;i<ids.Length;i++)
            {
                string id=ids[i];var place=places[id];
                Button(tray,place.Title,new Vector2(16+i*187,-36),new Vector2(179,42),()=>Visit(id));
                var label=Button(safeRoot,place.Title,Vector2.zero,new Vector2(160,34),()=>Visit(id));
                var rect=label.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);
                placeLabels.Add(place,rect);
            }
            placeCard=Panel("Location details",safeRoot,Vector2.one,Vector2.one,new Vector2(-22,-132),new Vector2(304,230),cream);
            placeTitle=Label(placeCard,"",new Vector2(20,-17),new Vector2(266,35),25,ink,FontStyle.Bold);
            placeDescription=Label(placeCard,"",new Vector2(20,-62),new Vector2(264,90),18,ink);
            enterHomeButton=Button(placeCard,"Enter home",new Vector2(20,-174),new Vector2(167,40),()=>LoadScene("PetHome")).gameObject;
            Button(placeCard,"Close",new Vector2(198,-174),new Vector2(86,40),()=>placeCard.gameObject.SetActive(false));
            placeCard.gameObject.SetActive(false);
        }
        void LoadScene(string scene){Save();SceneManager.LoadScene(scene);}
        public bool Visit(string id)
        {
            if(!townMap || !places.TryGetValue(id,out var place) || !TryWalkTo(place.Entrance))return false;
            pendingVisit=place;placeCard.gameObject.SetActive(false);hint.text="Walking to "+place.Title+"...";return true;
        }
        void ShowPlace(TownLocation place)
        {
            placeTitle.text=place.Title;placeDescription.text=place.Description;
            enterHomeButton.SetActive(place.Id=="home");placeCard.gameObject.SetActive(true);
            hint.text="Welcome to "+place.Title+".";
        }

        void Update()
        {
            if(!pet) return;
            if(Screen.safeArea!=lastSafe||Screen.width!=lastWidth||Screen.height!=lastHeight)
            {
                lastSafe=Screen.safeArea;lastWidth=Screen.width;lastHeight=Screen.height;
                safeRoot.anchorMin=new Vector2(lastSafe.x/Screen.width,lastSafe.y/Screen.height);
                safeRoot.anchorMax=new Vector2(lastSafe.xMax/Screen.width,lastSafe.yMax/Screen.height);
            }
            cam.orthographicSize=townMap?Mathf.Max(13.5f,21.5f/cam.aspect):Mathf.Max(5.4f,9.6f/cam.aspect);
            foreach(var entry in placeLabels)
            {
                Vector3 screen=cam.WorldToScreenPoint(entry.Key.transform.position+Vector3.up*4.2f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(safeRoot,screen,null,out var point);
                entry.Value.localPosition=point;
            }
            ReadFloorTap();
            Vector3 before=pet.position;
            if(!dragging && route.Count>0)
            {
                Vector3 next=route.Peek();
                Vector3 direction=next-pet.position;direction.y=0;
                if(direction.sqrMagnitude>.0001f)pet.rotation=Quaternion.Slerp(pet.rotation,Quaternion.LookRotation(direction),12*Time.deltaTime);
                pet.position=Vector3.MoveTowards(pet.position,next,(townMap?3.8f:2.1f)*Time.deltaTime);
                if(Vector3.Distance(pet.position,next)<.005f)route.Dequeue();
                if(route.Count==0)
                {
                    destinationMarker.gameObject.SetActive(false);Destination=null;
                    if(pendingVisit){var arrived=pendingVisit;pendingVisit=null;ShowPlace(arrived);}
                }
            }
            bool lookingAtYou = dragging || route.Count == 0;
            if(lookingAtYou)
                pet.rotation=Quaternion.Slerp(pet.rotation,FacingCamera(),1-Mathf.Exp(-6*Time.deltaTime));
            attention=Mathf.MoveTowards(attention,lookingAtYou?1:0,3*Time.deltaTime);
            visual.Animate(Vector3.Distance(before,pet.position)/Mathf.Max(.0001f,Time.deltaTime),Time.time);
            visual.LookToward(cam.transform.position,attention);
            if(dragging&&Time.unscaledTime-lastActive>.16f) wasRubbing=false;
            if(dragging&&wasRubbing)
            {
                WashProgress=Mathf.Min(WashDuration,WashProgress+Time.deltaTime);
                Cleanliness=Mathf.Min(100,Cleanliness+26*Time.deltaTime);
                washFill.fillAmount=WashProgress/WashDuration;
                washText.text="Washing... "+Mathf.CeilToInt(Mathf.Max(0,WashDuration-WashProgress))+"s";
                emission+=Time.deltaTime;
                if(emission>.07f){emission=0;EmitBubble();}
                if(WashProgress>=WashDuration)
                {
                    if(!StatusVisible){ShowStatus(); Save();}
                    hint.text="All fresh! Your companion's status is ready."; washText.text="Lovely and clean!";
                }
                if(cleanFill){cleanFill.fillAmount=Cleanliness/100;cleanValue.text=Mathf.RoundToInt(Cleanliness)+"%";}
            }
            for(int i=0;i<bubbles.Count;i++)
            {
                var b=bubbles[i];if(!b.t.gameObject.activeSelf)continue;b.age+=Time.deltaTime;
                if(b.age>=b.life){b.t.gameObject.SetActive(false);continue;}
                float t=b.age/b.life;
                b.t.position=b.origin+new Vector3(Mathf.Sin(t*5+b.phase)*.13f,t*1.5f,Mathf.Cos(t*4+b.phase)*.1f);
                b.t.localScale=Vector3.one*b.size*Mathf.Sin(Mathf.PI*t);
            }
        }

        public void BeginSoap(PointerEventData e)
        {
            if(dragging)return;
            dragging=true; soapPointer=e.pointerId;lastSoapScreen=e.position;wasRubbing=false;StopWalking();
            soap.SetParent(safeRoot,true);soap.SetAsLastSibling();PositionSoap(e.position);
            hint.text="Rub the soap across your companion for 2.5 seconds.";
        }
        public void DragSoap(PointerEventData e)
        {
            if(!dragging||e.pointerId!=soapPointer)return;PositionSoap(e.position);
            var center=cam.WorldToScreenPoint(pet.position+Vector3.up*.85f);
            float radius=Mathf.Abs(cam.WorldToScreenPoint(pet.position+Vector3.up*2).y-cam.WorldToScreenPoint(pet.position).y)*.66f;
            float distance=Vector2.Distance(e.position,center);
            wasRubbing=distance<radius&&Vector2.Distance(e.position,lastSoapScreen)>Screen.height*.0006f;
            lastSoapScreen=e.position;lastActive=Time.unscaledTime;
            if(!wasRubbing)hint.text="Move the soap over the pet to make bubbles.";
            else if(!StatusVisible)hint.text="Scrub, scrub... a little care goes a long way.";
        }
        public void EndSoap(PointerEventData e)
        {if(dragging&&e.pointerId==soapPointer)ReturnSoap();}
        void ReturnSoap()
        {
            dragging=false;wasRubbing=false;soapPointer=int.MinValue;soap.SetParent(soapDock,false);
            soap.anchorMin=soap.anchorMax=new Vector2(.5f,.5f);soap.anchoredPosition=Vector2.zero;
            washText.text=StatusVisible?"Lovely and clean!":"Drag soap over your pet";Save();
        }
        void PositionSoap(Vector2 screen)
        {RectTransformUtility.ScreenPointToLocalPointInRectangle(safeRoot,screen,null,out var local);soap.localPosition=local;}
        void Switch(bool puppy)
        {
            if(dragging)ReturnSoap();StopWalking();Save();Puppy=puppy;visual.Build(puppy);pet.rotation=FacingCamera();
            Cleanliness=PlayerPrefs.GetFloat(Puppy ? "LittlePaws.Clean.Puppy" : "LittlePaws.Clean.Kitten",35);WashProgress=0;StatusVisible=false;statusPanel.SetActive(false);washFill.fillAmount=0;
            cleanFill.fillAmount=Cleanliness/100;cleanValue.text=Mathf.RoundToInt(Cleanliness)+"%";
            speciesText.text=puppy?"Mochi / small puppy":"Miso / little kitten";
            hint.text=puppy?"Meet Mochi. A tiny puppy with a big heart.":"Meet Miso. Your curious little kitten.";
            washText.text="Drag soap over your pet";Save();
        }
        void EmitBubble()
        {
            foreach(var b in bubbles)if(!b.t.gameObject.activeSelf)
            {
                b.age=0;b.life=Random.Range(.8f,1.5f);b.size=Random.Range(.10f,.28f);b.phase=Random.value*6;
                b.origin=pet.position+new Vector3(Random.Range(-.65f,.65f),Random.Range(.3f,1.1f),Random.Range(-.45f,.45f));
                b.t.position=b.origin;b.t.gameObject.SetActive(true);return;
            }
        }
        void Save(){PlayerPrefs.SetInt("LittlePaws.Species",Puppy?1:0);PlayerPrefs.SetFloat(Puppy ? "LittlePaws.Clean.Puppy" : "LittlePaws.Clean.Kitten",Cleanliness);PlayerPrefs.Save();}
        void OnApplicationPause(bool paused){if(paused){StopWalking();if(dragging)ReturnSoap();Save();}}
        void OnApplicationFocus(bool focus){if(!focus){StopWalking();if(dragging)ReturnSoap();}}
        void OnApplicationQuit(){Save();}
        void OnDestroy()
        {
            if(bubbleMaterial)Destroy(bubbleMaterial);
            foreach(var m in effectMaterials)if(m)Destroy(m);
            if(rounded){Destroy(rounded.texture);Destroy(rounded);}
            if(soapSprite){Destroy(soapSprite.texture);Destroy(soapSprite);}
        }
        Quaternion FacingCamera()
        {
            Vector3 towardViewer=cam.transform.position-pet.position;
            towardViewer.y=0;
            return towardViewer.sqrMagnitude>.0001f?Quaternion.LookRotation(towardViewer):pet.rotation;
        }
        void StopWalking(){pendingVisit=null;route.Clear();Destination=null;if(destinationMarker)destinationMarker.gameObject.SetActive(false);}

        void ReadFloorTap()
        {
            if(dragging)return;
            var touch=Touchscreen.current;
            if(touch!=null)
                foreach(var finger in touch.touches)
                    if(finger.press.wasPressedThisFrame){HandleTap(finger.position.ReadValue());return;}
            if(Mouse.current!=null && Mouse.current.leftButton.wasPressedThisFrame)
                HandleTap(Mouse.current.position.ReadValue());
        }
        void HandleTap(Vector2 screen)
        {
            if(TryTapPet(screen))return;
            if(townMap && TryTapPlace(screen))return;
            TryTapFloor(screen);
        }
        bool TapBlockedByUI(Vector2 screen)
        {
            if(dragging || !cam || !cam.pixelRect.Contains(screen))return true;
            if(!EventSystem.current)return false;
            uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},uiHits);
            return uiHits.Count>0;
        }
        bool RayHitsPet(Ray ray,out float distance)
        {
            distance=float.PositiveInfinity;
            foreach(var renderer in pet.GetComponentsInChildren<Renderer>())
                if(renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.bounds.IntersectRay(ray,out float hitDistance))
                    distance=Mathf.Min(distance,hitDistance);
            return !float.IsPositiveInfinity(distance);
        }
        public bool TryTapPlace(Vector2 screen)
        {
            if(TapBlockedByUI(screen))return false;
            Physics.SyncTransforms();
            if(!Physics.Raycast(cam.ScreenPointToRay(screen),out var hit,100,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))return false;
            var place=hit.collider.GetComponentInParent<TownLocation>();
            return place && Visit(place.Id);
        }
        public bool TryTapPet(Vector2 screen)
        {
            if(TapBlockedByUI(screen))return false;
            Ray ray=cam.ScreenPointToRay(screen);
            if(!RayHitsPet(ray,out float petDistance))return false;
            Physics.SyncTransforms();
            // A wall or furnishing in front of the companion prevents selecting it through objects.
            if(Physics.Raycast(ray,out var obstruction,petDistance,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))return false;
            ShowStatus();
            hint.text=(Puppy?"Mochi":"Miso")+"'s health, hunger and cleanliness.";
            return true;
        }
        void ShowStatus()
        {
            cleanFill.fillAmount=Cleanliness/100;
            cleanValue.text=Mathf.RoundToInt(Cleanliness)+"%";
            StatusVisible=true;statusPanel.SetActive(true);
        }
        public bool TryTapFloor(Vector2 screen)
        {
            if(TapBlockedByUI(screen))return false;
            Ray ray=cam.ScreenPointToRay(screen);
            // A pet tap shows status and must not also create a walking destination.
            if(RayHitsPet(ray,out _))return false;
            // Newly generated/moved colliders must be visible to this frame's query.
            Physics.SyncTransforms();
            if(!Physics.Raycast(ray,out var hit,100,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)
                || !hit.collider.GetComponent<HomeFloorSurface>())return false;
            return TryWalkTo(hit.point);
        }
        public bool TryWalkTo(Vector3 destination)
        {
            if(dragging)return false;
            if(!navigation.TryPath(pet.position,destination,out var path))
            {
                hint.text="That spot is too close to furniture or cannot be reached.";
                return false;
            }
            pendingVisit=null;
            route.Clear();foreach(var point in path)route.Enqueue(point);
            Destination=destination;destinationMarker.position=new Vector3(destination.x,0,destination.z);
            destinationMarker.gameObject.SetActive(route.Count>0);
            hint.text="On my way! Tap another clear floor spot to change direction.";
            return true;
        }

        RectTransform Rect(string name,Transform parent,Vector2 min,Vector2 max,Vector2 at,Vector2 size)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin=min;r.anchorMax=max;
            r.pivot=new Vector2(min.x==max.x?min.x:.5f,min.y==max.y?min.y:.5f);
            r.sizeDelta=size;r.anchoredPosition=at;return r;
        }
        RectTransform Panel(string name,Transform parent,Vector2 min,Vector2 max,Vector2 at,Vector2 size,Color color)
        {var r=Rect(name,parent,min,max,at,size);var i=r.gameObject.AddComponent<Image>();i.sprite=rounded;i.type=Image.Type.Sliced;i.color=color;i.raycastTarget=true;return r;}
        Text Label(Transform parent,string text,Vector2 at,Vector2 size,int fontSize,Color color,FontStyle style=FontStyle.Normal)
        {
            var r=Rect(text,parent,new Vector2(0,1),new Vector2(0,1),at,size);var t=r.gameObject.AddComponent<Text>();
            t.font=font;t.text=text;t.fontSize=fontSize;t.color=color;t.fontStyle=style;t.raycastTarget=false;return t;
        }
        Button Button(Transform parent,string text,Vector2 at,Vector2 size,UnityEngine.Events.UnityAction callback)
        {
            var r=Panel(text,parent,new Vector2(0,1),new Vector2(0,1),at,size,new Color(.91f,.85f,.75f));
            r.GetComponent<Image>().raycastTarget=true;var b=r.gameObject.AddComponent<Button>();
            b.targetGraphic=r.GetComponent<Image>();b.navigation=new Navigation{mode=Navigation.Mode.None};
            var colors=b.colors;colors.pressedColor=new Color(.77f,.85f,.73f);colors.highlightedColor=new Color(1,.96f,.88f);b.colors=colors;
            if(callback!=null)b.onClick.AddListener(callback);
            var l=Label(r,text,Vector2.zero,size,16,ink,FontStyle.Bold);l.alignment=TextAnchor.MiddleCenter;
            return b;
        }
        Image Bar(Transform parent,Vector2 at,Vector2 size,Color color,float value)
        {
            var back=Panel("Track",parent,new Vector2(0,1),new Vector2(0,1),at,size,new Color(.85f,.84f,.79f));
            var fill=Rect("Fill",back,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero).gameObject.AddComponent<Image>();
            fill.sprite=rounded;fill.color=color;fill.type=Image.Type.Filled;fill.fillMethod=Image.FillMethod.Horizontal;fill.fillAmount=value;fill.raycastTarget=false;return fill;
        }
        void Meter(Transform parent,string name,float value,float y,Color color,out Image fill,out Text amount)
        {
            Label(parent,name,new Vector2(18,-y),new Vector2(140,24),16,ink);
            amount=Label(parent,Mathf.RoundToInt(value)+"%",new Vector2(185,-y),new Vector2(62,24),16,ink);amount.alignment=TextAnchor.MiddleRight;
            fill=Bar(parent,new Vector2(18,-y-28),new Vector2(230,8),color,value/100);
        }
        Sprite RoundedSprite()
        {
            var t=new Texture2D(32,32);for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {float dx=Mathf.Max(8-x,x-23),dy=Mathf.Max(8-y,y-23);float d=new Vector2(Mathf.Max(0,dx),Mathf.Max(0,dy)).magnitude;t.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(8.5f-d)));}
            t.Apply();return Sprite.Create(t,new Rect(0,0,32,32),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(10,10,10,10));
        }
        Sprite SoapSprite()
        {
            const int w=256,h=184;var t=new Texture2D(w,h,TextureFormat.RGBA32,false);
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                Vector2 p=new Vector2(x-128,y-82);float d=new Vector2(Mathf.Max(Mathf.Abs(p.x)-65,0),Mathf.Max(Mathf.Abs(p.y)-20,0)).magnitude-35;
                Color c=Color.clear;
                if(d<1){float shade=Mathf.Clamp01((p.y+55)/110);c=Color.Lerp(new Color(.79f,.48f,.46f),new Color(1,.80f,.72f),shade);c.a=Mathf.Clamp01(1-d);}
                float oval=p.x*p.x/(62*62f)+p.y*p.y/(25*25f);if(d<0&&oval>.88f&&oval<1)c=new Color(.76f,.45f,.44f);
                foreach(var b in new[]{new Vector3(81,146,16),new Vector3(112,157,20),new Vector3(146,148,13)})
                {float bd=Vector2.Distance(new Vector2(x,y),new Vector2(b.x,b.y));if(bd<b.z)c=bd>b.z-3?new Color(.63f,.83f,.89f):new Color(.92f,.98f,1);}
                t.SetPixel(x,y,c);
            }
            t.Apply();return Sprite.Create(t,new Rect(0,0,w,h),Vector2.one*.5f,100);
        }
    }
    public sealed class SoapDrag : MonoBehaviour,IPointerDownHandler,IBeginDragHandler,IDragHandler,IEndDragHandler,IPointerUpHandler
    {
        public PetHomeGame owner;
        public void OnPointerDown(PointerEventData e){owner.BeginSoap(e);}
        public void OnBeginDrag(PointerEventData e){}
        public void OnDrag(PointerEventData e){owner.DragSoap(e);}
        public void OnEndDrag(PointerEventData e){owner.EndSoap(e);}
        public void OnPointerUp(PointerEventData e){owner.EndSoap(e);}
    }
}
