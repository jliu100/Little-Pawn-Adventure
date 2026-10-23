using System.Collections.Generic;
using UnityEngine;

namespace LittlePaws
{
    /// <summary>A warm little town assembled from lightweight, original geometry.</summary>
    public static class TownVisual
    {
        private static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();
        private static readonly Color Timber = new Color(.35f,.23f,.18f);
        private static readonly Color Cream = new Color(1f,.89f,.67f);
        private static readonly Color Stone = new Color(.61f,.68f,.63f);
        private static readonly Color Glass = new Color(.43f,.77f,.83f);

        public static GameObject Build()
        {
            Materials.Clear();
            var town = new GameObject("Little Paws Town — seven destinations");
            var owned = town.AddComponent<RuntimeVisualResources>();
            Shape(town.transform,"Town ground",PrimitiveType.Cube,new Vector3(0,-.22f,0),new Vector3(26,.44f,22),new Color(.47f,.66f,.37f));
            // Dedicated surface avoids retrieving a disabled primitive collider awaiting destruction.
            var floor = new GameObject("Village walkable ground",typeof(BoxCollider),typeof(HomeFloorSurface));
            floor.transform.SetParent(town.transform,false);
            var surface=floor.GetComponent<BoxCollider>();
            surface.center=new Vector3(0,-.22f,0);surface.size=new Vector3(26,.44f,22);

            // The plaza and every path are visual surfaces, never raycast obstacles.
            Disc(town.transform,"Sandstone village square",new Vector3(0,.018f,-2),4.3f,new Color(.84f,.76f,.55f));
            Disc(town.transform,"Plaza inset",new Vector3(0,.025f,-2),3.8f,new Color(.91f,.84f,.65f));
            Path(town.transform,new Vector3(-10,0,-2),new Vector3(10,0,-2),1.9f);
            Path(town.transform,new Vector3(-8,0,-2),new Vector3(-8,0,1.1f),1.6f);
            Path(town.transform,new Vector3(0,0,-2),new Vector3(0,0,3.1f),1.6f);
            Path(town.transform,new Vector3(8,0,-2),new Vector3(8,0,1.1f),1.6f);
            Path(town.transform,new Vector3(-3.6f,0,-2),new Vector3(-3.6f,0,-8.2f),1.6f);
            Path(town.transform,new Vector3(3.6f,0,-2),new Vector3(3.6f,0,-8.2f),1.6f);
            Path(town.transform,new Vector3(-7,0,-8.2f),new Vector3(7,0,-8.2f),1.6f);
            // A paw mosaic is flat and keeps the square open for walking.
            Disc(town.transform,"Paw mosaic pad",new Vector3(0,.04f,-2.3f),.58f,new Color(.75f,.55f,.33f));
            for(int i=0;i<4;i++)
                Disc(town.transform,"Paw mosaic toe",new Vector3(-.75f+i*.5f,.041f,-1.45f-Mathf.Sin(i*Mathf.PI/3f)*.2f),.21f,new Color(.75f,.55f,.33f));

            Home(town.transform);
            Forge(town.transform);
            Hospital(town.transform);
            Food(town.transform);
            Adventure(town.transform);
            Garden(town.transform);
            EggRescue(town.transform);

            // Sparse edge planting frames the town without obscuring the destinations.
            Tree(town.transform,new Vector3(-11.6f,0,8.5f),1.1f);
            Tree(town.transform,new Vector3(-5.2f,0,9.3f),.9f);
            Tree(town.transform,new Vector3(5.3f,0,9.2f),1f);
            Tree(town.transform,new Vector3(11.8f,0,8.2f),1.05f);
            Tree(town.transform,new Vector3(-11.9f,0,-4.6f),.85f);
            Tree(town.transform,new Vector3(11.9f,0,-4.3f),.9f);
            Bush(town.transform,new Vector3(-11.7f,0,1),1f);
            Bush(town.transform,new Vector3(11.7f,0,.7f),1.1f);
            FlowerPatch(town.transform,new Vector3(-4.2f,0,2.7f),new Color(1f,.74f,.34f));
            FlowerPatch(town.transform,new Vector3(4.4f,0,2.8f),new Color(1f,.62f,.64f));
            FlowerPatch(town.transform,new Vector3(-3.5f,0,-5.3f),new Color(.73f,.59f,.93f));
            FlowerPatch(town.transform,new Vector3(3.6f,0,-5.1f),new Color(1f,.87f,.4f));
            owned.CaptureMaterials();
            return town;
        }

        private static Transform Location(Transform parent,string id,string title,string description,Vector3 center,Vector3 entrance)
        {
            var root=new GameObject(title); root.transform.SetParent(parent,false); root.transform.localPosition=center;
            var spot=root.AddComponent<TownLocation>(); spot.Id=id; spot.Title=title; spot.Description=description; spot.Entrance=entrance;
            var bounds=root.AddComponent<BoxCollider>(); bounds.center=new Vector3(0,1.7f,0); bounds.size=new Vector3(4.6f,3.4f,4f);
            return root.transform;
        }

        private static void Home(Transform parent)
        {
            var p=Location(parent,"home","Home","A little place to rest, care for your pet, and grow together.",new Vector3(-8,0,4),new Vector3(-8,0,1.1f));
            Box(p,"Weathered cottage",new Vector3(0,1.1f,0),new Vector3(4.2f,2.2f,3.6f),new Color(.80f,.64f,.42f));
            Roof(p,"Patchwork terracotta roof",new Vector3(0,2.15f,0),4.7f,4f,1.25f,new Color(.65f,.31f,.22f));
            for(int i=-1;i<=1;i++) Box(p,"Timber post",new Vector3(i*1.92f,1.12f,-1.83f),new Vector3(.16f,2.25f,.14f),Timber);
            Box(p,"Low beam",new Vector3(0,.2f,-1.85f),new Vector3(4.2f,.18f,.15f),Timber);
            Box(p,"Lintel beam",new Vector3(0,2.08f,-1.85f),new Vector3(4.2f,.16f,.15f),Timber);
            Door(p,new Vector3(0,.02f,-1.84f),new Color(.37f,.46f,.32f),1.05f);
            Window(p,new Vector3(-1.35f,1.25f,-1.86f),.75f,.7f);
            Window(p,new Vector3(1.35f,1.25f,-1.86f),.75f,.7f);
            Box(p,"Roof repair",new Vector3(.9f,2.8f,-.9f),new Vector3(.8f,.08f,.62f),new Color(.75f,.43f,.29f),new Vector3(32,0,0));
            Box(p,"Chimney",new Vector3(-1.1f,3f,.65f),new Vector3(.48f,1.05f,.48f),Stone);
            Box(p,"Chimney cap",new Vector3(-1.1f,3.55f,.65f),new Vector3(.65f,.13f,.65f),Timber);
            Disc(p,"Welcome stepping stone",new Vector3(0,.04f,-2.28f),.57f,new Color(.71f,.66f,.52f));
        }

        private static void Forge(Transform parent)
        {
            var p=Location(parent,"weapon","Weapon","Tools and equipment for your next adventure.",new Vector3(0,0,6),new Vector3(0,0,3.1f));
            Box(p,"Forge walls",new Vector3(0,1.15f,0),new Vector3(4.3f,2.3f,3.6f),new Color(.48f,.46f,.43f));
            Roof(p,"Slate forge roof",new Vector3(0,2.28f,0),4.7f,4f,1.1f,new Color(.28f,.35f,.39f));
            for(int i=-1;i<=1;i+=2) Box(p,"Forge pillar",new Vector3(i*1.98f,1.25f,-1.85f),new Vector3(.3f,2.5f,.3f),new Color(.34f,.31f,.3f));
            Door(p,new Vector3(0,0,-1.84f),new Color(.24f,.21f,.18f),1.25f);
            Box(p,"Furnace opening",new Vector3(1.3f,.9f,-1.83f),new Vector3(.75f,.85f,.08f),new Color(.19f,.16f,.15f));
            Box(p,"Furnace embers",new Vector3(1.3f,.69f,-1.89f),new Vector3(.58f,.33f,.04f),new Color(1f,.46f,.12f));
            Box(p,"Tall brick flue",new Vector3(1.2f,3.3f,.8f),new Vector3(.7f,2.2f,.7f),new Color(.49f,.31f,.26f));
            Box(p,"Flue crown",new Vector3(1.2f,4.43f,.8f),new Vector3(.95f,.18f,.95f),Stone);
            Box(p,"Sword sign backing",new Vector3(0,2.5f,-2.06f),new Vector3(1.25f,1.18f,.13f),new Color(.30f,.25f,.20f));
            Sword(p,new Vector3(-.03f,2.53f,-2.17f),-37);
            Sword(p,new Vector3(.03f,2.53f,-2.22f),37);
            Box(p,"Anvil base",new Vector3(-1.25f,.35f,-1.9f),new Vector3(.45f,.6f,.5f),Timber);
            Box(p,"Anvil",new Vector3(-1.25f,.72f,-1.9f),new Vector3(.85f,.22f,.35f),new Color(.22f,.26f,.28f));
        }

        private static void Hospital(Transform parent)
        {
            var p=Location(parent,"hospital","Hospital","A friendly clinic for checkups and a healthy companion.",new Vector3(8,0,4),new Vector3(8,0,1.1f));
            Box(p,"Ivory clinic",new Vector3(0,1.3f,0),new Vector3(4.3f,2.6f,3.6f),Cream);
            Box(p,"Mint foundation",new Vector3(0,.25f,0),new Vector3(4.4f,.5f,3.7f),new Color(.40f,.66f,.55f));
            Roof(p,"Clinic terracotta roof",new Vector3(0,2.55f,0),4.7f,4f,.9f,new Color(.77f,.43f,.32f));
            Door(p,new Vector3(0,.02f,-1.85f),new Color(.39f,.67f,.58f),1.15f);
            Window(p,new Vector3(-1.4f,1.42f,-1.84f),.83f,.98f);
            Window(p,new Vector3(1.4f,1.42f,-1.84f),.83f,.98f);
            Box(p,"Clinic cross backing",new Vector3(0,2.8f,-1.99f),new Vector3(1.12f,1.05f,.14f),Cream);
            Box(p,"Mint cross vertical",new Vector3(0,2.8f,-2.08f),new Vector3(.24f,.75f,.08f),new Color(.22f,.61f,.48f));
            Box(p,"Mint cross horizontal",new Vector3(0,2.8f,-2.09f),new Vector3(.75f,.24f,.08f),new Color(.22f,.61f,.48f));
            Box(p,"Door canopy",new Vector3(0,2.05f,-2.08f),new Vector3(1.48f,.14f,.62f),new Color(.40f,.66f,.55f));
        }

        private static void Food(Transform parent)
        {
            var p=Location(parent,"food","Food store","Fresh food and treats for little paws.",new Vector3(-7,0,-5),new Vector3(-7,0,-7.9f));
            Box(p,"Food shop",new Vector3(0,1.15f,0),new Vector3(4.2f,2.3f,3.6f),new Color(.96f,.77f,.44f));
            Roof(p,"Food shop roof",new Vector3(0,2.25f,0),4.6f,4f,.9f,new Color(.60f,.35f,.23f));
            Door(p,new Vector3(0,.02f,-1.84f),new Color(.39f,.52f,.33f),.95f);
            Window(p,new Vector3(-1.35f,1.25f,-1.85f),.9f,1f);
            Window(p,new Vector3(1.35f,1.25f,-1.85f),.9f,1f);
            for(int i=0;i<8;i++)
            {
                Color c=i%2==0?new Color(.90f,.39f,.30f):new Color(1f,.92f,.74f);
                Box(p,"Striped market awning",new Vector3(-1.84f+i*.525f,2.12f,-1.98f),new Vector3(.525f,.13f,.65f),c,new Vector3(9,0,0));
                Box(p,"Awning scallop",new Vector3(-1.84f+i*.525f,1.97f,-2.27f),new Vector3(.51f,.2f,.1f),c);
            }
            Crate(p,new Vector3(-1.45f,.27f,-1.96f),new Color(.88f,.26f,.18f));
            Crate(p,new Vector3(1.45f,.27f,-1.96f),new Color(.56f,.75f,.24f));
            Box(p,"Food sign",new Vector3(0,2.73f,-2.02f),new Vector3(1.25f,.6f,.13f),new Color(.39f,.52f,.33f));
            Shape(p,"Food bowl icon",PrimitiveType.Sphere,new Vector3(0,2.65f,-2.13f),new Vector3(.6f,.24f,.1f),Cream);
            Shape(p,"Food icon kibble",PrimitiveType.Sphere,new Vector3(0,2.81f,-2.13f),new Vector3(.32f,.2f,.1f),new Color(.75f,.43f,.23f));
        }

        private static void Adventure(Transform parent)
        {
            var p=Location(parent,"adventure","Adventure","Your gateway to Escape Maze, puzzles, and finding stars.",new Vector3(7,0,-5),new Vector3(7,0,-7.9f));
            // A single footprint collider marks the destination. The glowing arch is decorative.
            Box(p,"Gate platform",new Vector3(0,.12f,0),new Vector3(4.6f,.24f,3.8f),Stone);
            for(int side=-1;side<=1;side+=2)
            {
                Box(p,"Old gate tower",new Vector3(side*1.65f,1.5f,0),new Vector3(1.05f,3f,2.3f),new Color(.54f,.62f,.60f));
                Box(p,"Tower cap",new Vector3(side*1.65f,3.02f,0),new Vector3(1.28f,.24f,2.5f),new Color(.72f,.76f,.67f));
                for(int j=0;j<3;j++) Box(p,"Gate stone course",new Vector3(side*1.65f,.48f+j*.83f,-1.17f),new Vector3(1.04f,.09f,.04f),new Color(.42f,.51f,.49f));
                Box(p,"Teal banner",new Vector3(side*1.65f,2.1f,-1.23f),new Vector3(.48f,1.15f,.07f),new Color(.24f,.54f,.57f));
                Star(p,new Vector3(side*1.65f,2.2f,-1.3f),.23f,new Color(1f,.83f,.36f));
            }
            Box(p,"Gate lintel",new Vector3(0,2.88f,0),new Vector3(3.2f,.5f,2.25f),new Color(.67f,.73f,.66f));
            Box(p,"Deep portal",new Vector3(0,1.38f,.55f),new Vector3(2.2f,2.55f,.16f),new Color(.24f,.31f,.45f));
            Box(p,"Portal inset",new Vector3(0,1.38f,.42f),new Vector3(1.7f,2.25f,.05f),new Color(.39f,.54f,.67f));
            Star(p,new Vector3(0,2.97f,-1.22f),.64f,new Color(1f,.82f,.30f));
            for(int i=0;i<3;i++) Star(p,new Vector3(-.5f+i*.5f,1.05f+i*.4f,.33f),.13f,new Color(1f,.91f,.58f));
            for(int i=0;i<3;i++) Disc(p,"Star trail stepping stone",new Vector3((i%2==0?-.23f:.23f),.04f,-2.05f-i*.3f),.21f,new Color(.98f,.86f,.52f));
        }

        private static void Garden(Transform parent)
        {
            var p=Location(parent,"garden","Garden","Plant corn and tomatoes, harvest with your sickle, and visit the Scarecrow for a daily crop-guard game. Open for testing.",new Vector3(0,0,-5.2f),new Vector3(0,0,-8.2f));
            p.gameObject.AddComponent<GardenWorldView>().Build();
            Path(parent,new Vector3(0,0,-8.2f),new Vector3(0,0,-7.65f),1.2f);
        }

        private static void EggRescue(Transform parent)
        {
            var p=Location(parent,"rescue","Rescue the egg","Rooster or hen? Lead your knight through three forest stages. Level 1 is open. Levels 2 and 3: coming soon.",new Vector3(-4.2f,0,6),new Vector3(-4.2f,0,2.2f));
            p.GetComponent<BoxCollider>().size=new Vector3(2.3f,3.4f,3f);
            Path(parent,new Vector3(-4.2f,0,-2),new Vector3(-4.2f,0,2.2f),1.2f);
            Box(p,"Rescue gate stone base",new Vector3(0,.15f,0),new Vector3(2.25f,.3f,2.7f),Stone);
            for(int side=-1;side<=1;side+=2)
            {
                Box(p,"Forest rescue gatepost",new Vector3(side*.94f,1.45f,0),new Vector3(.3f,2.9f,.4f),Timber);
                Box(p,"Crimson rescue banner",new Vector3(side*.94f,2.0f,-.28f),new Vector3(.48f,.85f,.08f),new Color(.68f,.23f,.19f));
            }
            Box(p,"Rescue gate lintel",new Vector3(0,2.9f,0),new Vector3(2.4f,.3f,.5f),Timber);
            Box(p,"Rescue crest",new Vector3(0,2.85f,-.32f),new Vector3(1.15f,.85f,.1f),new Color(.23f,.39f,.32f));
            Shape(p,"Egg crest",PrimitiveType.Sphere,new Vector3(0,2.89f,-.4f),new Vector3(.4f,.58f,.08f),Cream);
            Sword(p,new Vector3(-.6f,1.35f,-.33f),-26);
            Sword(p,new Vector3(.6f,1.35f,-.33f),26);
            Disc(p,"Rescue trail stone",new Vector3(0,.04f,-1.9f),.48f,new Color(.86f,.69f,.39f));
        }

        private static void Door(Transform p,Vector3 at,Color color,float width)
        {
            Box(p,"Door frame",at+new Vector3(0,.84f,0),new Vector3(width+.18f,1.72f,.12f),Timber);
            Box(p,"Door",at+new Vector3(0,.8f,-.08f),new Vector3(width,1.6f,.1f),color);
            Shape(p,"Brass handle",PrimitiveType.Sphere,at+new Vector3(width*.29f,.78f,-.17f),new Vector3(.095f,.095f,.095f),new Color(.95f,.74f,.27f));
        }
        private static void Window(Transform p,Vector3 at,float width,float height)
        {
            Box(p,"Window frame",at,new Vector3(width+.16f,height+.16f,.13f),Timber);
            Box(p,"Blue window",at+new Vector3(0,0,-.08f),new Vector3(width,height,.08f),Glass);
            Box(p,"Window mullion",at+new Vector3(0,0,-.13f),new Vector3(.055f,height,.05f),Cream);
            Box(p,"Window crossbar",at+new Vector3(0,0,-.13f),new Vector3(width,.055f,.05f),Cream);
        }
        private static void Sword(Transform p,Vector3 at,float angle)
        {
            var root=new GameObject("Crossed sword").transform;root.SetParent(p,false);root.localPosition=at;root.localRotation=Quaternion.Euler(0,0,angle);
            Box(root,"Silver blade",new Vector3(0,.16f,0),new Vector3(.12f,.72f,.045f),new Color(.83f,.9f,.88f));
            Box(root,"Gold guard",new Vector3(0,-.2f,-.01f),new Vector3(.4f,.08f,.07f),new Color(.93f,.69f,.28f));
            Box(root,"Sword grip",new Vector3(0,-.34f,0),new Vector3(.1f,.24f,.065f),new Color(.62f,.32f,.20f));
        }
        private static void Crate(Transform p,Vector3 at,Color produce)
        {
            Box(p,"Produce crate",at,new Vector3(.9f,.5f,.46f),new Color(.58f,.37f,.22f));
            for(int i=0;i<3;i++) Shape(p,"Fresh produce",PrimitiveType.Sphere,at+new Vector3(-.27f+i*.27f,.3f,0),new Vector3(.25f,.25f,.25f),produce);
        }
        private static void Tree(Transform p,Vector3 at,float size)
        {
            var root=new GameObject("Round village tree").transform;root.SetParent(p,false);root.localPosition=at;
            Shape(root,"Tree trunk",PrimitiveType.Cylinder,new Vector3(0,.85f*size,0),new Vector3(.4f*size,.85f*size,.4f*size),Timber);
            Shape(root,"Tree crown",PrimitiveType.Sphere,new Vector3(0,2.15f*size,0),new Vector3(2.15f,2.15f,2.15f)*size,new Color(.30f,.52f,.28f));
            Shape(root,"Sunlit crown",PrimitiveType.Sphere,new Vector3(-.35f,2.55f,-.25f)*size,new Vector3(1.5f,1.5f,1.5f)*size,new Color(.46f,.65f,.31f));
            var c=root.gameObject.AddComponent<BoxCollider>();c.center=new Vector3(0,.8f*size,0);c.size=new Vector3(.6f,1.6f,.6f)*size;
        }
        private static void Bush(Transform p,Vector3 at,float size)
        {
            Shape(p,"Garden shrub",PrimitiveType.Sphere,at+Vector3.up*.4f,new Vector3(1.4f,.8f,1f)*size,new Color(.35f,.55f,.28f));
        }
        private static void FlowerPatch(Transform p,Vector3 at,Color color)
        {
            Bush(p,at,.65f);
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI*2/5;
                Shape(p,"Little flower",PrimitiveType.Sphere,at+new Vector3(Mathf.Cos(a)*.38f,.55f,Mathf.Sin(a)*.3f),new Vector3(.19f,.12f,.19f),color);
            }
        }
        private static void Path(Transform p,Vector3 start,Vector3 end,float width)
        {
            Vector3 d=end-start;
            var o=Box(p,"Village path",(start+end)*.5f+Vector3.up*.015f,new Vector3(width,.025f,d.magnitude+width*.25f),new Color(.86f,.78f,.59f));
            o.transform.localRotation=Quaternion.LookRotation(d,Vector3.up);
        }
        private static void Disc(Transform p,string name,Vector3 position,float radius,Color color)
        { Shape(p,name,PrimitiveType.Cylinder,position,new Vector3(radius*2,.012f,radius*2),color); }
        private static GameObject Box(Transform p,string name,Vector3 pos,Vector3 scale,Color color,Vector3 euler=default(Vector3))
        { var o=Shape(p,name,PrimitiveType.Cube,pos,scale,color);o.transform.localRotation=Quaternion.Euler(euler);return o; }
        private static GameObject Shape(Transform p,string name,PrimitiveType kind,Vector3 pos,Vector3 scale,Color color)
        {
            var o=GameObject.CreatePrimitive(kind);o.name=name;o.transform.SetParent(p,false);o.transform.localPosition=pos;o.transform.localScale=scale;
            var c=o.GetComponent<Collider>();if(c!=null){ c.enabled=false;if(Application.isPlaying)Object.Destroy(c);else Object.DestroyImmediate(c); }
            o.GetComponent<Renderer>().sharedMaterial=Mat(color);return o;
        }
        private static Material Mat(Color color)
        {
            Material m;if(Materials.TryGetValue(color,out m))return m;
            Shader shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)shader=Shader.Find("Standard");
            m=new Material(shader){name="Town "+ColorUtility.ToHtmlStringRGB(color),color=color};
            if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",.12f);
            Materials.Add(color,m);return m;
        }
        private static void Roof(Transform p,string name,Vector3 pos,float width,float depth,float height,Color color)
        {
            float x=width*.5f,z=depth*.5f;
            Vector3[] v={new Vector3(-x,0,-z),new Vector3(x,0,-z),new Vector3(0,height,-z),new Vector3(-x,0,z),new Vector3(x,0,z),new Vector3(0,height,z)};
            int[] tri={0,2,1,3,4,5,0,3,5,0,5,2,1,2,5,1,5,4,0,1,4,0,4,3};
            MeshObject(p,name,pos,v,tri,color);
        }
        private static void Star(Transform p,Vector3 pos,float radius,Color color)
        {
            var v=new Vector3[11];v[0]=Vector3.zero;
            for(int i=0;i<10;i++){float a=(90+i*36)*Mathf.Deg2Rad;float r=i%2==0?radius:radius*.45f;v[i+1]=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0);}
            var tri=new int[60];for(int i=0;i<10;i++){int next=(i+1)%10+1;int n=i*6;tri[n]=0;tri[n+1]=next;tri[n+2]=i+1;tri[n+3]=0;tri[n+4]=i+1;tri[n+5]=next;}
            MeshObject(p,"Golden adventure star",pos,v,tri,color);
        }
        private static void MeshObject(Transform p,string name,Vector3 pos,Vector3[] verts,int[] indices,Color color)
        {
            var go=new GameObject(name);go.transform.SetParent(p,false);go.transform.localPosition=pos;
            var mesh=new Mesh{name=name};mesh.vertices=verts;mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=Mat(color);
            p.GetComponentInParent<RuntimeVisualResources>().meshes.Add(mesh);
        }
    }
}
