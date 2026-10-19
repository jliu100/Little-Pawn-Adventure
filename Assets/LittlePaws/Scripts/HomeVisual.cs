using UnityEngine;

namespace LittlePaws
{
    /// <summary>A small, well-loved home built from lightweight geometry.</summary>
    public static class HomeVisual
    {
        public static GameObject Build()
        {
            var home = new GameObject("Little Paws • Home");
            var wood = Material("Aged honey wood", "#977457");
            var woodLight = Material("Worn floorboard", "#B08A66");
            var woodDark = Material("Dark wood grain", "#72563F");
            var wall = Material("Warm worn plaster", "#D7C7AA");
            var patch = Material("Plaster patches", "#BCA987");
            var cream = Material("Faded cream fabric", "#E7DCC4");
            var green = Material("Old sage blanket", "#8DAB94");
            var blue = Material("Clear blue morning", "#9FCFD1");
            var cardboard = Material("Cardboard", "#B99163");
            var dark = Material("Box interior", "#695644");
            var ceramic = Material("Mismatched ceramic bowls", "#A1B4A9");
            var water = Material("Water", "#75B5C1");
            var food = Material("Kibble", "#A77649");

            Part(home.transform, "Foundation", PrimitiveType.Cube, new Vector3(0,-.16f,0), new Vector3(9.08f,.24f,6.08f), woodDark);
            // Tiny gaps and alternating shades make the inexpensive plank floor legible.
            for (int row = 0; row < 15; row++)
            {
                float z = -2.8f + row * .4f;
                for (int column = 0; column < 3; column++)
                {
                    float x = -3f + column * 3f;
                    Part(home.transform, "Floor plank", PrimitiveType.Cube, new Vector3(x,-.045f,z), new Vector3(2.975f,.09f,.382f), (row+column)%3 == 0 ? woodLight : wood);
                }
            }
            Part(home.transform, "Back plaster wall", PrimitiveType.Cube, new Vector3(0,1.4f,3.06f), new Vector3(9.12f,2.8f,.12f), wall);
            Part(home.transform, "Left plaster wall", PrimitiveType.Cube, new Vector3(-4.56f,1.4f,0), new Vector3(.12f,2.8f,6f), wall);
            Part(home.transform, "Back skirting", PrimitiveType.Cube, new Vector3(0,.12f,2.94f), new Vector3(9,.24f,.1f), woodDark);
            Part(home.transform, "Left skirting", PrimitiveType.Cube, new Vector3(-4.44f,.12f,0), new Vector3(.1f,.24f,6f), woodDark);
            Part(home.transform, "Left wall top", PrimitiveType.Cube, new Vector3(-4.56f,2.83f,0), new Vector3(.19f,.08f,6.1f), wood);
            Part(home.transform, "Back wall top", PrimitiveType.Cube, new Vector3(0,2.83f,3.06f), new Vector3(9.15f,.08f,.19f), wood);
            // Visible repairs suggest humble circumstances without making the room gloomy.
            Part(home.transform, "Repaired plaster A", PrimitiveType.Cube, new Vector3(-2.4f,.67f,2.987f), new Vector3(.72f,.35f,.012f), patch);
            Part(home.transform, "Repaired plaster B", PrimitiveType.Cube, new Vector3(3.95f,1.13f,2.987f), new Vector3(.4f,.62f,.012f), patch);
            Part(home.transform, "Left plaster repair", PrimitiveType.Cube, new Vector3(-4.487f,.65f,-.7f), new Vector3(.012f,.42f,.65f), patch);

            // Window is a shallow inset representation, avoiding costly wall meshes.
            Part(home.transform, "Window recess", PrimitiveType.Cube, new Vector3(.55f,1.85f,2.96f), new Vector3(1.95f,1.28f,.09f), woodDark);
            Part(home.transform, "Sky through window", PrimitiveType.Cube, new Vector3(.55f,1.85f,2.9f), new Vector3(1.73f,1.08f,.035f), blue);
            Part(home.transform, "Window vertical mullion", PrimitiveType.Cube, new Vector3(.55f,1.85f,2.865f), new Vector3(.065f,1.16f,.065f), cream);
            Part(home.transform, "Window horizontal mullion", PrimitiveType.Cube, new Vector3(.55f,1.85f,2.865f), new Vector3(1.82f,.065f,.065f), cream);
            Part(home.transform, "Window sill", PrimitiveType.Cube, new Vector3(.55f,1.24f,2.78f), new Vector3(2.13f,.1f,.38f), woodLight);
            Part(home.transform, "Curtain rod", PrimitiveType.Cube, new Vector3(.55f,2.57f,2.77f), new Vector3(2.42f,.055f,.055f), woodDark);
            for (int side = -1; side <= 1; side += 2)
                for (int fold = 0; fold < 4; fold++)
                    Part(home.transform, "Simple curtain fold", PrimitiveType.Cube, new Vector3(.55f+side*(.98f+fold*.065f),1.96f,2.76f+(fold%2)*.025f), new Vector3(.08f,1.15f,.045f), cream);

            Crate(home.transform,new Vector3(3.64f,.32f,1.1f),wood,woodDark);

            // An open cardboard box doubles as a tiny pet hideaway.
            var box = new GameObject("Well-loved cardboard box"); box.transform.SetParent(home.transform,false);
            Part(box.transform,"Box bottom",PrimitiveType.Cube,new Vector3(-3.69f,.07f,-.22f),new Vector3(1.08f,.14f,1f),dark);
            Part(box.transform,"Box back",PrimitiveType.Cube,new Vector3(-3.69f,.37f,.25f),new Vector3(1.08f,.6f,.06f),cardboard);
            Part(box.transform,"Box left",PrimitiveType.Cube,new Vector3(-4.2f,.37f,-.22f),new Vector3(.06f,.6f,1f),cardboard);
            Part(box.transform,"Box right",PrimitiveType.Cube,new Vector3(-3.18f,.37f,-.22f),new Vector3(.06f,.6f,1f),cardboard);
            Part(box.transform,"Low box front",PrimitiveType.Cube,new Vector3(-3.69f,.16f,-.69f),new Vector3(1.08f,.18f,.06f),cardboard);
            var flap=Part(box.transform,"Bent box flap",PrimitiveType.Cube,new Vector3(-3.69f,.74f,.39f),new Vector3(1.08f,.05f,.45f),cardboard);
            flap.transform.localRotation=Quaternion.Euler(-32,0,0);

            // Flat woven mat can be walked on; bowls sit beyond the walk boundary.
            Part(home.transform,"Frayed entrance mat",PrimitiveType.Cube,new Vector3(.05f,.008f,-2.53f),new Vector3(2.05f,.016f,.64f),green);
            for(int stripe=0;stripe<9;stripe++)
                Part(home.transform,"Mat weave",PrimitiveType.Cube,new Vector3(-.82f+stripe*.21f,.018f,-2.53f),new Vector3(.025f,.004f,.62f),cream);
            Bowl(home.transform,new Vector3(3.25f,.02f,-1.14f),ceramic,water);
            Bowl(home.transform,new Vector3(3.91f,.02f,-1.14f),cream,food);
            for(int kibble=0;kibble<6;kibble++)
            {
                float angle=kibble*Mathf.PI/3f;
                Part(home.transform,"Kibble piece",PrimitiveType.Sphere,new Vector3(3.91f+Mathf.Cos(angle)*.11f,.115f,-1.14f+Mathf.Sin(angle)*.11f),new Vector3(.066f,.05f,.066f),woodDark);
            }
            var floor = new GameObject("Walkable floor", typeof(BoxCollider), typeof(HomeFloorSurface));
            floor.transform.SetParent(home.transform, false);
            var surface = floor.GetComponent<BoxCollider>();
            surface.center = new Vector3(0,-.05f,0); surface.size = new Vector3(9,.1f,6);
            home.AddComponent<RuntimeVisualResources>().CaptureMaterials();
            return home;
        }

        private static void Bowl(Transform parent,Vector3 p,Material ceramic,Material contents)
        {
            Part(parent,"Bowl base",PrimitiveType.Cylinder,p+Vector3.up*.055f,new Vector3(.43f,.055f,.43f),ceramic);
            Part(parent,"Bowl contents",PrimitiveType.Cylinder,p+Vector3.up*.081f,new Vector3(.32f,.012f,.32f),contents);
            for(int i=0;i<16;i++)
            {
                float angle=i*360f/16f;
                var rim=Part(parent,"Bowl rim",PrimitiveType.Cube,p+new Vector3(Mathf.Sin(angle*Mathf.Deg2Rad)*.2f,.11f,Mathf.Cos(angle*Mathf.Deg2Rad)*.2f),new Vector3(.09f,.11f,.045f),ceramic);
                rim.transform.localRotation=Quaternion.Euler(0,angle,0);
            }
        }

        private static void Crate(Transform parent,Vector3 p,Material wood,Material dark)
        {
            Part(parent,"Crate shadow",PrimitiveType.Cube,p,new Vector3(.8f,.6f,.6f),dark);
            for(int row=0;row<3;row++)
                Part(parent,"Crate slat",PrimitiveType.Cube,p+new Vector3(0,-.2f+row*.2f,-.31f),new Vector3(.86f,.16f,.055f),wood);
            var brace=Part(parent,"Crate diagonal brace",PrimitiveType.Cube,p+new Vector3(0,0,-.35f),new Vector3(.1f,.85f,.045f),wood);
            brace.transform.localRotation=Quaternion.Euler(0,0,-51);
        }

        private static GameObject Part(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
        {
            var part=GameObject.CreatePrimitive(type);
            part.name=name;
            part.transform.SetParent(parent,false);
            part.transform.localPosition=position;
            part.transform.localScale=scale;
            part.GetComponent<Renderer>().sharedMaterial=material;
            var collider=part.GetComponent<Collider>();
            bool flatSurface = name == "Foundation" || name == "Floor plank" || name == "Frayed entrance mat" || name == "Mat weave";
            if (flatSurface)
            {
                collider.enabled = false;
                if(Application.isPlaying) Object.Destroy(collider); else Object.DestroyImmediate(collider);
            }
            // All furnishings and walls retain colliders so taps cannot pass through them.
            return part;
        }

        private static Material Material(string name,string hex)
        {
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            if(shader==null) shader=Shader.Find("Standard");
            var material=new Material(shader){name=name};
            if(ColorUtility.TryParseHtmlString(hex,out var color)) material.color=color;
            if(material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",.12f);
            return material;
        }
    }
}
