using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LittlePaws.Maze
{
    /// <summary>A real 3D maze and pet, rendered from a camera that stays behind the pet.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class MazeThirdPersonView : MonoBehaviour
    {
        public const float TileMetres=2.4f;
        public RenderTexture Texture { get; private set; }
        public bool IsReady => stage && pet && previewCamera && Texture;
        public bool CameraIsBehindPet => IsReady && Vector3.Dot(previewCamera.transform.position-pet.transform.position,pet.transform.forward)<0;
        public float VisualYaw => pet?pet.transform.localEulerAngles.y:0f;
        private GameObject stage;
        private MazeLevel maze;
        private PetVisual pet;
        private Camera previewCamera;
        private Light keyLight;
        private Transform goal;
        private UniversalRenderPipeline.SingleCameraRequest request;
        private int layer=-1, mask;
        private bool visible=true, moving, jumping;
        private float jumpProgress, runIntensity=1, hazardClock;
        private Vector2 position;
        private Quaternion facing=Quaternion.identity;
        private readonly List<Material> materials=new List<Material>();
        private readonly List<Mesh> meshes=new List<Mesh>();
        private readonly List<Snake> snakes=new List<Snake>();
        private readonly Dictionary<Camera,int> cameraMasks=new Dictionary<Camera,int>();
        private readonly Dictionary<Light,int> lightMasks=new Dictionary<Light,int>();
        private sealed class Snake { public SnakePatrol patrol; public Transform root; public Transform[] body; }

        public void Initialize(MazeLevel level,bool puppy)
        {
            ReleaseStage();
            maze=level;
            if(layer<0)layer=FindFreeLayer();
            if(layer<0){Debug.LogError("No free layer for the rear-view maze.",this);return;}
            mask=1<<layer;
            stage=new GameObject("Rear-view 3D maze stage");stage.transform.SetParent(transform,false);
            stage.transform.position=new Vector3(3000,0,3000);stage.transform.rotation=Quaternion.identity;
            var inherited=stage.transform.lossyScale;
            stage.transform.localScale=new Vector3(Inverse(inherited.x),Inverse(inherited.y),Inverse(inherited.z));
            var stone=Mat("Warm path",new Color(.83f,.75f,.58f));
            var hedge=Mat("Green hedge",new Color(.24f,.46f,.34f));
            var leaves=Mat("Hedge tops",new Color(.38f,.61f,.39f));
            var gold=Mat("Golden goal",new Color(1f,.75f,.18f),true);
            var earth=Mat("Mud",new Color(.43f,.29f,.18f));
            var geometry=new GameObject("Maze geometry").transform;geometry.SetParent(stage.transform,false);
            Cube("Maze floor",geometry,new Vector3((level.Width-1)*TileMetres/2,-.10f,(level.Height-1)*TileMetres/2),new Vector3(level.Width*TileMetres,.2f,level.Height*TileMetres),stone);
            for(int y=0;y<level.Height;y++)for(int x=0;x<level.Width;x++)
            {
                var tile=level.Tiles[x,y];var at=new Vector3(x*TileMetres,0,y*TileMetres);
                if(tile==MazeTile.Wall)
                {
                    var body=Cube("Hedge "+x+","+y,geometry,at+Vector3.up*.96f,new Vector3(TileMetres,1.92f,TileMetres),hedge);
                    Cube("Leaf crown",body.transform,new Vector3(0,.51f,0),new Vector3(.98f,.12f,.98f),leaves);
                }
                else if(tile==MazeTile.Mud)
                    Sphere("Mud puddle",geometry,at+Vector3.up*.015f,new Vector3(TileMetres*.86f,.04f,TileMetres*.75f),earth);
            }
            var actor=new GameObject("Rear-view animated pet");actor.transform.SetParent(stage.transform,false);
            pet=actor.AddComponent<PetVisual>();pet.Build(puppy);pet.transform.localPosition=Cell(level.Start)+Vector3.up*.02f;
            BuildSnakes(level);
            goal=BuildGoal(geometry,gold);
            goal.localPosition=Cell(level.Goal)+Vector3.up*.9f;
            Cube("Goal pad",geometry,Cell(level.Goal)+Vector3.up*.02f,new Vector3(1.4f,.04f,1.4f),gold);
            Texture=new RenderTexture(1050,600,24,RenderTextureFormat.ARGB32){name="Rear-view maze texture",antiAliasing=1,filterMode=FilterMode.Bilinear};
            Texture.Create();
            var cameraObject=new GameObject("Rear-view maze camera");cameraObject.transform.SetParent(stage.transform,false);
            previewCamera=cameraObject.AddComponent<Camera>();previewCamera.enabled=false;
            previewCamera.fieldOfView=62;previewCamera.aspect=1.75f;previewCamera.nearClipPlane=.08f;previewCamera.farClipPlane=50f;
            previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=new Color(.72f,.85f,.89f);
            previewCamera.cullingMask=mask;previewCamera.targetTexture=Texture;previewCamera.allowHDR=false;previewCamera.allowMSAA=false;
            var data=previewCamera.GetUniversalAdditionalCameraData();data.renderType=CameraRenderType.Base;data.renderPostProcessing=false;
            data.volumeLayerMask=0;data.renderShadows=false;data.allowXRRendering=false;data.antialiasing=AntialiasingMode.None;
            data.requiresColorOption=CameraOverrideOption.Off;data.requiresDepthOption=CameraOverrideOption.Off;
            var lightObject=new GameObject("Rear-view soft sunlight");lightObject.transform.SetParent(stage.transform,false);
            lightObject.transform.localRotation=Quaternion.Euler(42,-25,0);keyLight=lightObject.AddComponent<Light>();
            keyLight.type=LightType.Directional;keyLight.intensity=1.2f;keyLight.color=new Color(1,.97f,.90f);keyLight.shadows=LightShadows.None;keyLight.cullingMask=mask;
            SetLayers(stage.transform,layer);IsolateLayer();
            request=new UniversalRenderPipeline.SingleCameraRequest{destination=Texture};
            position=level.Start;SnapFacing(MazeRearControls.InitialFacing(level));
            stage.SetActive(visible);ClearTexture();
        }
        public void SetVisible(bool value){visible=value;if(stage)stage.SetActive(value);}
        public void SetState(Vector2 cell,Vector2Int direction,bool running,bool inJump,float progress,float clock,float intensity)
        {
            position=cell;facing=Rotation(direction);moving=running;jumping=inJump;jumpProgress=progress;hazardClock=clock;runIntensity=intensity;
        }
        public void SnapFacing(Vector2Int direction)
        {
            facing=Rotation(direction);
            if(pet)pet.transform.localRotation=facing;
            if(previewCamera && pet)PoseCamera();
        }
        private static Quaternion Rotation(Vector2Int direction) => Quaternion.Euler(0,Mathf.Atan2(direction.x,direction.y)*Mathf.Rad2Deg,0);
        private void LateUpdate()
        {
            if(!visible||!IsReady||!stage.activeInHierarchy)return;
            if(!Texture.IsCreated()){Texture.Create();ClearTexture();}
            pet.transform.localPosition=Cell(position)+Vector3.up*(.02f+(jumping?MazeMovementRules.JumpHeight(jumpProgress)*TileMetres:0));
            pet.transform.localRotation=Quaternion.RotateTowards(pet.transform.localRotation,facing,540*Time.deltaTime);
            if(jumping)pet.AnimateJump(jumpProgress,Time.time);else pet.AnimateRun(moving?runIntensity:0,Time.time);
            PoseCamera();
            foreach(var snake in snakes)
            {
                Vector2 at=snake.patrol.PositionAt(hazardClock),direction=snake.patrol.DirectionAt(hazardClock);
                snake.root.localPosition=Cell(at)+Vector3.up*.13f;
                snake.root.localRotation=Rotation(new Vector2Int(Mathf.RoundToInt(direction.x),Mathf.RoundToInt(direction.y)));
                for(int i=0;i<snake.body.Length;i++)snake.body[i].localPosition=new Vector3(Mathf.Sin(hazardClock*7-i*.8f)*.06f,0,-i*.16f);
            }
            goal.rotation=previewCamera.transform.rotation;
            if(GraphicsSettings.currentRenderPipeline!=null&&RenderPipelineManager.currentPipeline==null)return;
            var previousSun=RenderSettings.sun;
            try
            {
                RenderSettings.sun=keyLight;
                if(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)RenderPipeline.SubmitRenderRequest(previewCamera,request);
                else if(GraphicsSettings.currentRenderPipeline==null)previewCamera.Render();
            }
            finally{RenderSettings.sun=previousSun;}
        }
        private void PoseCamera()
        {
            // Keep every hedge solid. Move the camera inside the corridor instead of
            // cutting holes in walls when the pet turns at a dead end or a corner.
            Vector3 backwards=pet.transform.localRotation*Vector3.back;
            float distance=MazeRearControls.CameraDistance(maze,position,
                new Vector2(backwards.x,backwards.z),3.9f/TileMetres)*TileMetres;
            Vector3 target=pet.transform.localPosition+Vector3.up*.85f;
            previewCamera.transform.localPosition=pet.transform.localPosition+backwards*distance+Vector3.up*1.7f;
            // A wider view keeps the whole pet visible when a nearby hedge pushes us in.
            previewCamera.fieldOfView=Mathf.Lerp(90,62,Mathf.InverseLerp(1f,3.9f,distance));
            previewCamera.transform.LookAt(stage.transform.TransformPoint(target));
        }

        private void BuildSnakes(MazeLevel level)
        {
            var skin=Mat("Snake green",new Color(.42f,.65f,.22f));var dark=Mat("Snake eyes",new Color(.08f,.13f,.05f));var white=Mat("Snake eye whites",Color.white);
            foreach(var patrol in level.Snakes)
            {
                var root=new GameObject("Patrolling 3D snake").transform;root.SetParent(stage.transform,false);
                var body=new Transform[5];
                for(int i=0;i<body.Length;i++)body[i]=Sphere("Snake segment",root,new Vector3(0,0,-i*.16f),new Vector3(.30f-i*.025f,.24f-i*.02f,.30f),skin).transform;
                Sphere("Snake head",root,new Vector3(0,.025f,.14f),new Vector3(.36f,.28f,.4f),skin);
                for(int side=-1;side<=1;side+=2)
                {
                    Sphere("Snake eye",root,new Vector3(side*.13f,.13f,.24f),Vector3.one*.105f,white);
                    Sphere("Snake pupil",root,new Vector3(side*.13f,.14f,.28f),Vector3.one*.05f,dark);
                }
                snakes.Add(new Snake{patrol=patrol,root=root,body=body});
            }
        }
        private Transform BuildGoal(Transform parent,Material material)
        {
            var root=new GameObject("Golden goal star");root.transform.SetParent(parent,false);
            var mesh=new Mesh{name="3D maze goal star"};meshes.Add(mesh);
            var vertices=new Vector3[11];var triangles=new int[60];
            for(int i=0;i<10;i++)
            {
                float angle=Mathf.PI/2+i*Mathf.PI/5,radius=i%2==0?.65f:.29f;
                vertices[i+1]=new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,0);
                int next=(i+1)%10+1,k=i*6;triangles[k]=0;triangles[k+1]=i+1;triangles[k+2]=next;
                triangles[k+3]=0;triangles[k+4]=next;triangles[k+5]=i+1;
            }
            mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();
            root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=material;
            return root.transform;
        }
        private Material Mat(string label,Color color,bool unlit=false)
        {
            var shader=Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit");
            if(!shader)shader=Shader.Find("Universal Render Pipeline/Lit");
            if(!shader)shader=Shader.Find("Standard");
            var material=new Material(shader){name=label,enableInstancing=true};
            material.color=color;if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);
            if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.1f);
            materials.Add(material);return material;
        }
        private static GameObject Cube(string name,Transform parent,Vector3 at,Vector3 scale,Material material) => Shape(PrimitiveType.Cube,name,parent,at,scale,material);
        private static GameObject Sphere(string name,Transform parent,Vector3 at,Vector3 scale,Material material) => Shape(PrimitiveType.Sphere,name,parent,at,scale,material);
        private static GameObject Shape(PrimitiveType type,string name,Transform parent,Vector3 at,Vector3 scale,Material material)
        {
            var shape=GameObject.CreatePrimitive(type);shape.name=name;shape.transform.SetParent(parent,false);shape.transform.localPosition=at;shape.transform.localScale=scale;
            shape.GetComponent<Renderer>().sharedMaterial=material;
            var collider=shape.GetComponent<Collider>();collider.enabled=false;Destroy(collider);return shape;
        }
        private static Vector3 Cell(Vector2 at) => new Vector3(at.x*TileMetres,0,at.y*TileMetres);
        private static float Inverse(float value) => Mathf.Abs(value)<.00001f?1:1/value;
        private int FindFreeLayer()
        {
            var used=new HashSet<int>();foreach(var item in FindObjectsByType<Transform>(FindObjectsInactive.Include))if(item.gameObject.scene.IsValid())used.Add(item.gameObject.layer);
            for(int i=31;i>=8;i--)if(string.IsNullOrEmpty(LayerMask.LayerToName(i))&&!used.Contains(i))return i;return -1;
        }
        private static void SetLayers(Transform root,int layer){root.gameObject.layer=layer;foreach(Transform child in root)SetLayers(child,layer);}
        private void IsolateLayer()
        {
            foreach(var camera in FindObjectsByType<Camera>(FindObjectsInactive.Include))
                if(camera!=previewCamera&&camera.gameObject.scene.IsValid()){cameraMasks[camera]=camera.cullingMask;camera.cullingMask&=~mask;}
            foreach(var light in FindObjectsByType<Light>(FindObjectsInactive.Include))
                if(light!=keyLight&&light.gameObject.scene.IsValid()){lightMasks[light]=light.cullingMask;light.cullingMask&=~mask;}
        }
        private void ClearTexture()
        {
            var previous=RenderTexture.active;try{RenderTexture.active=Texture;GL.Clear(true,true,previewCamera.backgroundColor);}finally{RenderTexture.active=previous;}
        }
        private void ReleaseStage()
        {
            foreach(var pair in cameraMasks)if(pair.Key)pair.Key.cullingMask=(pair.Key.cullingMask&~mask)|(pair.Value&mask);
            foreach(var pair in lightMasks)if(pair.Key)pair.Key.cullingMask=(pair.Key.cullingMask&~mask)|(pair.Value&mask);
            cameraMasks.Clear();lightMasks.Clear();snakes.Clear();
            if(stage){stage.SetActive(false);Destroy(stage);stage=null;}
            if(previewCamera)previewCamera.targetTexture=null;
            if(Texture){Texture.Release();Destroy(Texture);Texture=null;}
            foreach(var material in materials)if(material)Destroy(material);materials.Clear();
            foreach(var mesh in meshes)if(mesh)Destroy(mesh);meshes.Clear();pet=null;previewCamera=null;keyLight=null;
        }
        private void OnDestroy(){ReleaseStage();}
    }
}
