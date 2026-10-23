using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittlePaws
{
    /// <summary>Four little farm beds and their one friendly, handmade scarecrow.</summary>
    public sealed class GardenWorldView : MonoBehaviour
    {
        public Transform Scarecrow { get; private set; }
        readonly Transform[] beds = new Transform[4];
        readonly Transform[] plants = new Transform[4];
        readonly GameObject[] locks = new GameObject[4];
        readonly GardenCrop[] displayedCrop = new GardenCrop[4];
        readonly GardenGrowthStage[] displayedStage = new GardenGrowthStage[4];
        readonly Dictionary<Color,Material> materials = new Dictionary<Color,Material>();
        bool built;

        static readonly Color Soil = new Color(.25f,.14f,.085f);
        static readonly Color Furrow = new Color(.37f,.23f,.12f);
        static readonly Color Timber = new Color(.42f,.25f,.14f);
        static readonly Color WoodLight = new Color(.63f,.41f,.22f);
        static readonly Color Straw = new Color(.89f,.67f,.30f);
        static readonly Color StrawLight = new Color(1f,.83f,.43f);
        static readonly Color Sack = new Color(.83f,.69f,.46f);
        static readonly Color Dark = new Color(.15f,.18f,.17f);
        static readonly Color Teal = new Color(.14f,.47f,.46f);
        static readonly Color Coral = new Color(.80f,.30f,.19f);
        static readonly Color Leaf = new Color(.23f,.54f,.20f);
        static readonly Color LeafLight = new Color(.45f,.70f,.23f);
        static readonly Color CornYellow = new Color(1f,.76f,.12f);
        static readonly Color TomatoRed = new Color(.94f,.20f,.13f);
        static readonly Color Gold = new Color(1f,.78f,.27f);

        public void Build()
        {
            if(built)return;
            built=true;
            var owner=GetComponentInParent<RuntimeVisualResources>();
            if(!owner)owner=gameObject.AddComponent<RuntimeVisualResources>();
            // A disabled palette renderer lets the existing resource owner capture every
            // material now, including the crop colors first needed after planting later.
            foreach(var color in new[]{Soil,Furrow,Timber,WoodLight,Straw,StrawLight,Sack,Dark,Teal,Coral,Leaf,LeafLight,CornYellow,TomatoRed,Gold})MaterialFor(color);
            var palette=new GameObject("Garden material palette").AddComponent<MeshRenderer>();
            palette.transform.SetParent(transform,false);
            palette.sharedMaterials=new List<Material>(materials.Values).ToArray();
            palette.enabled=false;

            Shape(transform,"Garden foundation",PrimitiveType.Cube,new Vector3(0,.07f,0),new Vector3(4.4f,.14f,3.8f),Timber);
            Shape(transform,"Garden grass carpet",PrimitiveType.Cube,new Vector3(0,.17f,0),new Vector3(4.30f,.08f,3.70f),Leaf);
            // Front and central paths remain clear for the character and crop labels.
            Shape(transform,"Garden stone cross path",PrimitiveType.Cube,new Vector3(.10f,.219f,0),new Vector3(.20f,.025f,3.35f),Sack);
            Shape(transform,"Garden path between beds",PrimitiveType.Cube,new Vector3(.10f,.220f,.02f),new Vector3(2.9f,.025f,.22f),Sack);
            for(int i=0;i<4;i++)BuildBed(i);
            BuildScarecrow();
            BuildFence();
            owner.CaptureMaterials();
        }

        public void Refresh(GardenProgress progress, DateTime utcNow)
        {
            Build();
            if(progress==null)return;
            for(int i=0;i<4;i++)
            {
                bool unlocked=i<progress.UnlockedPlotCount;
                locks[i].SetActive(!unlocked);
                var crop=unlocked?progress.Plots[i].Crop:GardenCrop.None;
                var stage=unlocked?progress.StageAt(i,utcNow):GardenGrowthStage.Empty;
                if(plants[i] && crop==displayedCrop[i] && stage==displayedStage[i])continue;
                if(plants[i]){plants[i].gameObject.SetActive(false);Destroy(plants[i].gameObject);}
                displayedCrop[i]=crop;displayedStage[i]=stage;
                var root=new GameObject("Plot "+(i+1)+" / "+crop+" / "+stage).transform;
                root.SetParent(beds[i],false);root.localPosition=Vector3.up*.12f;plants[i]=root;
                if(crop==GardenCrop.None || stage==GardenGrowthStage.Empty)continue;
                if(stage==GardenGrowthStage.Seed)BuildSeed(root,crop);
                else if(crop==GardenCrop.Corn)BuildCorn(root,stage==GardenGrowthStage.Harvest);
                else if(crop==GardenCrop.Tomato)BuildTomato(root,stage==GardenGrowthStage.Harvest);
            }
        }

        public Vector3 PlotWorldPosition(int index)
        {
            Build();
            return beds[Mathf.Clamp(index,0,3)].TransformPoint(new Vector3(0,.26f,0));
        }
        public bool OwnsScarecrowHit(Collider hit)=>hit && Scarecrow && hit.transform.IsChildOf(Scarecrow);
        public int PlotIndex(Collider hit)
        {
            if(!hit)return -1;
            for(int i=0;i<4;i++)if(beds[i] && hit.transform.IsChildOf(beds[i]))return i;
            return -1;
        }

        void BuildBed(int index)
        {
            var p=new GameObject("Planting field "+(index+1)).transform;p.SetParent(transform,false);
            p.localPosition=new Vector3(index%2==0?-.65f:.85f,.25f,index<2?-.68f:.70f);beds[index]=p;
            Shape(p,"Rich planting soil",PrimitiveType.Cube,Vector3.zero,new Vector3(1.16f,.14f,1.05f),Soil);
            for(int side=-1;side<=1;side+=2)
            {
                Shape(p,"Raised bed timber edge",PrimitiveType.Cube,new Vector3(side*.61f,.02f,0),new Vector3(.085f,.20f,1.20f),WoodLight);
                Shape(p,"Raised bed timber edge",PrimitiveType.Cube,new Vector3(0,.02f,side*.56f),new Vector3(1.30f,.20f,.085f),Timber);
            }
            for(int j=0;j<3;j++)Shape(p,"Prepared soil furrow",PrimitiveType.Cube,new Vector3(-.35f+j*.35f,.075f,0),new Vector3(.065f,.025f,.87f),Furrow);
            var hit=p.gameObject.AddComponent<BoxCollider>();hit.center=new Vector3(0,.18f,0);hit.size=new Vector3(1.30f,.45f,1.20f);
            var locked=new GameObject("Field opens at gardening level 5").transform;locked.SetParent(p,false);locks[index]=locked.gameObject;
            Shape(locked,"Closed field brace",PrimitiveType.Cube,new Vector3(0,.17f,0),new Vector3(1.08f,.09f,.16f),WoodLight,new Vector3(0,40,0));
            Shape(locked,"Closed field brace",PrimitiveType.Cube,new Vector3(0,.18f,0),new Vector3(1.08f,.09f,.16f),WoodLight,new Vector3(0,-40,0));
            Shape(locked,"Lock brass body",PrimitiveType.Cube,new Vector3(0,.37f,-.12f),new Vector3(.24f,.22f,.12f),Gold);
            Shape(locked,"Lock shackle left",PrimitiveType.Cube,new Vector3(-.07f,.51f,-.12f),new Vector3(.045f,.12f,.06f),StrawLight);
            Shape(locked,"Lock shackle right",PrimitiveType.Cube,new Vector3(.07f,.51f,-.12f),new Vector3(.045f,.12f,.06f),StrawLight);
            Shape(locked,"Lock shackle top",PrimitiveType.Cube,new Vector3(0,.56f,-.12f),new Vector3(.16f,.045f,.06f),StrawLight);
            locked.gameObject.SetActive(index==3);
        }

        void BuildSeed(Transform root,GardenCrop crop)
        {
            for(int i=0;i<3;i++)
            {
                var at=new Vector3(-.33f+i*.33f,0,(i%2==0?-.17f:.19f));
                Shape(root,"Planted seed mound",PrimitiveType.Sphere,at,new Vector3(.25f,.10f,.20f),Furrow);
                Shape(root,"First sprout stem",PrimitiveType.Cylinder,at+Vector3.up*.065f,new Vector3(.025f,.065f,.025f),Leaf);
                LeafBlade(root,"Seedling first leaf",at+new Vector3(-.045f,.11f,0),new Vector3(.13f,.06f,.055f),new Vector3(0,0,25));
                LeafBlade(root,"Seedling second leaf",at+new Vector3(.045f,.12f,0),new Vector3(.13f,.06f,.055f),new Vector3(0,0,-30));
                Shape(root,crop+" seed marker",PrimitiveType.Sphere,at+new Vector3(0,.025f,-.06f),new Vector3(.07f,.035f,.055f),crop==GardenCrop.Corn?CornYellow:TomatoRed);
            }
        }

        void BuildCorn(Transform root,bool ripe)
        {
            for(int i=0;i<3;i++)
            {
                var at=new Vector3(-.30f+i*.30f,0,i%2==0?-.16f:.18f);
                float height=ripe?1.05f:.55f;
                Shape(root,"Corn stalk",PrimitiveType.Cylinder,at+Vector3.up*(height*.5f),new Vector3(.055f,height*.5f,.055f),Leaf);
                for(int j=0;j<4;j++)
                {
                    int side=j%2==0?-1:1;
                    LeafBlade(root,"Long corn leaf",at+new Vector3(side*.14f,height*(.22f+j*.14f),0),new Vector3(ripe?.48f:.30f,.065f,.12f),new Vector3(0,side*18,side*30));
                }
                if(!ripe)continue;
                Shape(root,"Golden corn tassel",PrimitiveType.Sphere,at+Vector3.up*(height+.045f),new Vector3(.10f,.20f,.10f),StrawLight);
                // Five golden ears across the cluster match the five-corn harvest.
                int ears=i==2?1:2;
                for(int j=0;j<ears;j++)
                {
                    int side=j==0?-1:1;var ear=at+new Vector3(side*.105f,height*(j==0?.59f:.77f),-.045f);
                    Shape(root,"Ripe corn husk",PrimitiveType.Sphere,ear,new Vector3(.18f,.36f,.17f),LeafLight,new Vector3(0,0,side*22));
                    Shape(root,"Golden corn ear",PrimitiveType.Sphere,ear+new Vector3(0,.025f,-.065f),new Vector3(.125f,.28f,.09f),CornYellow,new Vector3(0,0,side*22));
                    for(int k=0;k<3;k++)Shape(root,"Corn kernel row",PrimitiveType.Sphere,ear+new Vector3(0,-.065f+k*.065f,-.11f),new Vector3(.09f,.035f,.025f),StrawLight);
                }
            }
        }

        void BuildTomato(Transform root,bool ripe)
        {
            float height=ripe?.76f:.42f;
            Shape(root,"Tomato garden stake",PrimitiveType.Cylinder,new Vector3(0,height*.54f,.04f),new Vector3(.045f,height*.59f,.045f),WoodLight);
            Shape(root,"Tomato vine",PrimitiveType.Cylinder,new Vector3(-.035f,height*.46f,0),new Vector3(.055f,height*.46f,.055f),Leaf);
            for(int i=0;i<6;i++)
            {
                float side=i%2==0?-1:1;float y=height*(.23f+i*.11f);
                LeafBlade(root,"Tomato leafy branch",new Vector3(side*.17f,y,i%3==0?.12f:-.02f),new Vector3(ripe?.40f:.28f,.12f,.19f),new Vector3(0,i*39,side*20));
            }
            if(!ripe)return;
            for(int i=0;i<5;i++)
            {
                float angle=i*137.5f*Mathf.Deg2Rad;var at=new Vector3(Mathf.Cos(angle)*.25f,.23f+i*.095f,Mathf.Sin(angle)*.21f-.04f);
                Shape(root,"Ripe red tomato",PrimitiveType.Sphere,at,new Vector3(.25f,.23f,.25f),TomatoRed);
                for(int j=0;j<3;j++)LeafBlade(root,"Tomato star calyx",at+Vector3.up*.11f,new Vector3(.14f,.025f,.04f),new Vector3(0,j*60,0));
            }
        }

        void BuildScarecrow()
        {
            Scarecrow=new GameObject("Scarecrow / the Garden keeper").transform;Scarecrow.SetParent(transform,false);
            Scarecrow.localPosition=new Vector3(-1.70f,.21f,.38f);Scarecrow.localRotation=Quaternion.Euler(0,-12,0);
            var p=Scarecrow;
            Shape(p,"Scarecrow wooden pole",PrimitiveType.Cylinder,new Vector3(0,.63f,.055f),new Vector3(.095f,.63f,.095f),Timber);
            Shape(p,"Scarecrow crossbar",PrimitiveType.Cube,new Vector3(0,1.04f,.05f),new Vector3(.86f,.075f,.075f),WoodLight);
            Shape(p,"Patchwork teal shirt",PrimitiveType.Sphere,new Vector3(0,.95f,0),new Vector3(.43f,.52f,.27f),Teal);
            for(int side=-1;side<=1;side+=2)
            {
                Shape(p,"Loose rolled sleeve",PrimitiveType.Capsule,new Vector3(side*.29f,1.05f,0),new Vector3(.19f,.20f,.19f),Teal,new Vector3(0,0,side*75));
                for(int i=0;i<3;i++)Shape(p,"Straw fingers",PrimitiveType.Cylinder,new Vector3(side*(.43f+i*.018f),1.02f-i*.03f,0),new Vector3(.025f,.10f,.025f),StrawLight,new Vector3(0,0,side*(70+i*15)));
                Shape(p,"Straw trouser leg",PrimitiveType.Capsule,new Vector3(side*.11f,.60f,-.015f),new Vector3(.14f,.19f,.16f),WoodLight,new Vector3(0,0,side*8));
                for(int i=0;i<3;i++)Shape(p,"Loose trouser straw",PrimitiveType.Cylinder,new Vector3(side*.12f+(i-1)*.026f,.40f,-.015f),new Vector3(.025f,.095f,.025f),StrawLight,new Vector3(0,0,(i-1)*9));
            }
            Shape(p,"Coral shirt repair",PrimitiveType.Cube,new Vector3(-.095f,.88f,-.14f),new Vector3(.15f,.16f,.025f),Coral,new Vector3(0,0,-13));
            for(int i=0;i<3;i++)Shape(p,"Golden shirt button",PrimitiveType.Sphere,new Vector3(.035f,.82f+i*.10f,-.139f),Vector3.one*.035f,Gold);
            Shape(p,"Soft sack face",PrimitiveType.Sphere,new Vector3(0,1.34f,-.015f),new Vector3(.39f,.42f,.31f),Sack);
            Shape(p,"Rust red neck scarf",PrimitiveType.Sphere,new Vector3(0,1.16f,-.015f),new Vector3(.31f,.11f,.27f),Coral);
            Shape(p,"Scarf tail",PrimitiveType.Cube,new Vector3(.15f,1.07f,-.13f),new Vector3(.10f,.23f,.045f),Coral,new Vector3(0,0,-25));
            for(int side=-1;side<=1;side+=2)
            {
                Shape(p,"Stitched button eye",PrimitiveType.Sphere,new Vector3(side*.082f,1.37f,-.157f),new Vector3(.058f,.062f,.025f),Dark);
                Shape(p,"Button eye glint",PrimitiveType.Sphere,new Vector3(side*.082f-.009f,1.382f,-.172f),Vector3.one*.015f,StrawLight);
                Shape(p,"Warm cheek patch",PrimitiveType.Sphere,new Vector3(side*.125f,1.30f,-.145f),new Vector3(.075f,.045f,.020f),Coral);
            }
            Shape(p,"Little sack nose",PrimitiveType.Sphere,new Vector3(0,1.32f,-.18f),new Vector3(.045f,.055f,.055f),Straw);
            for(int i=0;i<5;i++)
            {
                float x=(i-2)*.033f;float y=1.255f+Mathf.Abs(i-2)*.013f;
                Shape(p,"Stitched friendly smile",PrimitiveType.Cube,new Vector3(x,y,-.155f),new Vector3(.023f,.012f,.018f),Dark,new Vector3(0,0,(i-2)*14));
            }
            Shape(p,"Wide straw hat brim",PrimitiveType.Cylinder,new Vector3(0,1.52f,0),new Vector3(.68f,.035f,.51f),StrawLight,new Vector3(0,0,-6));
            Shape(p,"Straw hat crown",PrimitiveType.Sphere,new Vector3(0,1.62f,0),new Vector3(.40f,.27f,.35f),Straw);
            Shape(p,"Teal hat ribbon",PrimitiveType.Cylinder,new Vector3(0,1.565f,0),new Vector3(.405f,.035f,.355f),Teal);
            LeafBlade(p,"Hat leaf feather",new Vector3(.23f,1.70f,0),new Vector3(.12f,.36f,.055f),new Vector3(0,0,-25));
            Shape(p,"Hat sunflower center",PrimitiveType.Sphere,new Vector3(-.21f,1.59f,-.11f),Vector3.one*.09f,Timber);
            for(int i=0;i<6;i++)
            {
                float a=i*Mathf.PI/3;Shape(p,"Hat sunflower petal",PrimitiveType.Sphere,new Vector3(-.21f+Mathf.Cos(a)*.065f,1.59f+Mathf.Sin(a)*.065f,-.10f),new Vector3(.075f,.065f,.035f),CornYellow);
            }
            var collider=p.gameObject.AddComponent<BoxCollider>();collider.center=new Vector3(0,.92f,0);collider.size=new Vector3(1.02f,1.75f,.49f);
        }

        void BuildFence()
        {
            for(int side=-1;side<=1;side+=2)
            {
                for(int j=0;j<5;j++)Shape(transform,"Garden perimeter post",PrimitiveType.Cube,new Vector3(side*2.10f,.52f,-1.50f+j*.75f),new Vector3(.085f,.62f,.085f),WoodLight);
                Shape(transform,"Garden side fence rail",PrimitiveType.Cube,new Vector3(side*2.10f,.64f,0),new Vector3(.07f,.09f,3.10f),Straw);
            }
            Shape(transform,"Garden rear fence rail",PrimitiveType.Cube,new Vector3(0,.64f,1.80f),new Vector3(4.25f,.09f,.07f),Straw);
            for(int i=0;i<5;i++)Shape(transform,"Garden rear fence post",PrimitiveType.Cube,new Vector3(-2.10f+i*1.05f,.52f,1.80f),new Vector3(.085f,.62f,.085f),WoodLight);
        }

        void LeafBlade(Transform p,string name,Vector3 at,Vector3 scale,Vector3 rotation)=>Shape(p,name,PrimitiveType.Sphere,at,scale,LeafLight,rotation);

        Transform Shape(Transform parent,string name,PrimitiveType type,Vector3 at,Vector3 scale,Color color,Vector3 rotation=default(Vector3))
        {
            var item=GameObject.CreatePrimitive(type);item.name=name;item.transform.SetParent(parent,false);
            item.transform.localPosition=at;item.transform.localScale=scale;item.transform.localRotation=Quaternion.Euler(rotation);
            var collider=item.GetComponent<Collider>();if(collider){collider.enabled=false;Destroy(collider);}
            item.GetComponent<Renderer>().sharedMaterial=MaterialFor(color);
            return item.transform;
        }
        Material MaterialFor(Color color)
        {
            if(materials.TryGetValue(color,out var existing))return existing;
            var shader=Shader.Find("Universal Render Pipeline/Lit");if(!shader)shader=Shader.Find("Standard");
            var material=new Material(shader){name="Garden / "+ColorUtility.ToHtmlStringRGB(color),color=color};
            if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.12f);
            materials.Add(color,material);return material;
        }
    }
}
