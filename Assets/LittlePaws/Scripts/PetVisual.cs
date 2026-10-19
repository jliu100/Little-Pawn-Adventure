using System.Collections.Generic;
using UnityEngine;

namespace LittlePaws
{
    /// <summary>Original toy-like pets built from local geometry. Faces local +Z.</summary>
    public sealed class PetVisual : MonoBehaviour
    {
        private Transform visualRoot, head, tail, leftEar, rightEar;
        private readonly List<Transform> legs = new List<Transform>();
        private readonly List<Vector3> legRestPositions = new List<Vector3>();
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Mesh> generatedMeshes = new List<Mesh>();
        private bool isPuppy;
        private float movement, runPhase;
        private bool wasRunning;

        public void Build(bool puppy)
        {
            isPuppy = puppy;
            movement = 0f; runPhase = 0f; wasRunning = false;
            if (visualRoot != null) { visualRoot.gameObject.SetActive(false); Destroy(visualRoot.gameObject); }
            foreach (Material material in materials) if (material != null) Destroy(material);
            materials.Clear(); legs.Clear(); legRestPositions.Clear();
            foreach (Mesh mesh in generatedMeshes) if (mesh != null) Destroy(mesh);
            generatedMeshes.Clear();
            visualRoot = Group("Pet model", transform, Vector3.zero);
            Material fur = Mat(puppy ? new Color(.73f,.45f,.25f) : new Color(1f,.64f,.29f));
            Material light = Mat(new Color(1f,.91f,.74f));
            Material darkFur = Mat(puppy ? new Color(.32f,.17f,.12f) : new Color(.78f,.34f,.13f));
            Material pink = Mat(new Color(1f,.57f,.59f));
            Material nose = Mat(puppy ? new Color(.16f,.12f,.14f) : new Color(.72f,.29f,.32f));
            Material eyes = Mat(new Color(.06f,.09f,.13f));
            Material white = Mat(new Color(1f,.99f,.92f));
            Material collar = Mat(puppy ? new Color(.81f,.25f,.25f) : new Color(.12f,.58f,.62f));
            Material gold = Mat(new Color(1f,.78f,.26f));
            Ball("Round body", visualRoot, new Vector3(0,.64f,-.16f), new Vector3(.72f,.70f,.99f), fur);
            Ball("Cream chest", visualRoot, new Vector3(0,.67f,.245f), new Vector3(.49f,.52f,.14f), light);
            if (puppy)
            {
                Ball("Back patch", visualRoot, new Vector3(.20f,.84f,-.31f), new Vector3(.34f,.17f,.41f), darkFur);
                Ball("Side patch", visualRoot, new Vector3(-.324f,.66f,-.28f), new Vector3(.055f,.30f,.33f), darkFur);
            }
            else for (int i=0; i<3; i++)
                Ball("Tabby stripe", visualRoot, new Vector3(0,.969f-i*.012f,-.43f+i*.17f), new Vector3(.40f,.06f,.065f), darkFur);
            for (int i=0; i<4; i++)
            {
                Transform leg = Group("Leg "+i, visualRoot, new Vector3((i%2==0?-1:1)*.235f,.43f,i<2?.19f:-.47f));
                legs.Add(leg); legRestPositions.Add(leg.localPosition);
                Ball("Leg", leg, new Vector3(0,-.13f,0), new Vector3(.22f,.40f,.25f), fur);
                Ball("Soft cream paw", leg, new Vector3(0,-.325f,.045f), new Vector3(.26f,.20f,.32f), light);
                for (int toe=0; toe<2; toe++)
                    Ball("Toe seam", leg, new Vector3(toe==0?-.045f:.045f,-.32f,.197f), new Vector3(.012f,.05f,.012f), darkFur);
            }
            head = Group("Head", visualRoot, new Vector3(0,1.04f,.36f));
            Ball("Head fur", head, Vector3.zero, new Vector3(.82f,.76f,.73f), fur);
            Ball("Lower face", head, new Vector3(0,-.15f,.22f), new Vector3(.61f,.34f,.29f), light);
            if (puppy)
            {
                Ball("Eye patch", head, new Vector3(-.208f,.04f,.277f), new Vector3(.32f,.39f,.14f), darkFur);
                Ball("Muzzle left", head, new Vector3(-.105f,-.105f,.34f), new Vector3(.28f,.23f,.22f), light);
                Ball("Muzzle right", head, new Vector3(.105f,-.105f,.34f), new Vector3(.28f,.23f,.22f), light);
                Ball("Little tongue", head, new Vector3(.035f,-.255f,.345f), new Vector3(.10f,.10f,.04f), pink);
            }
            else
            {
                for(int i=-1;i<=1;i++)
                    Ball("Forehead marking", head, new Vector3(i*.105f,.242f+(i==0?.015f:0),.248f), new Vector3(.055f,.15f,.045f), darkFur);
                Ball("Muzzle left", head, new Vector3(-.085f,-.12f,.342f), new Vector3(.23f,.18f,.12f), light);
                Ball("Muzzle right", head, new Vector3(.085f,-.12f,.342f), new Vector3(.23f,.18f,.12f), light);
                for(int side=-1; side<=1; side+=2) for(int w=0;w<3;w++)
                    Rod("Whisker",head,new Vector3(side*.17f,-.10f-.05f*w,.337f),new Vector3(side*.47f,-.055f-.095f*w,.31f),.008f,light);
            }
            for(int side=-1;side<=1;side+=2)
            {
                Ball("Eye",head,new Vector3(side*.183f,.035f,.337f),new Vector3(.148f,.189f,.075f),eyes);
                Ball("Eye sparkle",head,new Vector3(side*.183f-.023f,.076f,.376f),new Vector3(.045f,.055f,.022f),white);
                Ball("Small sparkle",head,new Vector3(side*.183f+.026f,.007f,.377f),new Vector3(.020f,.023f,.012f),white);
                Ball("Cheek blush",head,new Vector3(side*.29f,-.115f,.29f),new Vector3(.10f,.055f,.025f),pink);
                Rod("Smile",head,new Vector3(0,-.16f,.402f),new Vector3(side*.065f,-.195f,.387f),.012f,nose);
            }
            Ball("Button nose",head,new Vector3(0,-.09f,puppy?.455f:.408f),new Vector3(puppy?.15f:.10f,.085f,.065f),nose);
            Ball("Nose shine",head,new Vector3(-.02f,-.07f,puppy?.483f:.438f),new Vector3(.035f,.018f,.008f),white);
            leftEar=Group("Left ear",head,new Vector3(-.30f,.23f,-.015f));
            rightEar=Group("Right ear",head,new Vector3(.30f,.23f,-.015f));
            foreach(Transform ear in new[]{leftEar,rightEar})
            {
                float sign=ear==leftEar?-1:1;
                if(puppy)
                {
                    ear.localRotation=Quaternion.Euler(0,0,sign*18);
                    Ball("Floppy ear",ear,new Vector3(sign*.075f,-.19f,-.015f),new Vector3(.25f,.56f,.24f),darkFur);
                    Ball("Ear inner",ear,new Vector3(sign*.08f,-.23f,.10f),new Vector3(.13f,.30f,.035f),pink);
                }
                else
                {
                    Ear("Pointed ear",ear,.30f,.40f,.20f,fur);
                    Ear("Pink inner ear",Group("Inner ear",ear,new Vector3(0,.055f,.071f)),.17f,.25f,.035f,pink);
                }
            }
            Ball("Collar",visualRoot,new Vector3(0,.79f,.28f),new Vector3(.56f,.12f,.29f),collar);
            Ball("Gold tag",visualRoot,new Vector3(0,.69f,.435f),new Vector3(.13f,.15f,.045f),gold);
            Ball("Tag paw center",visualRoot,new Vector3(0,.676f,.46f),new Vector3(.049f,.04f,.01f),light);
            for(int i=0;i<3;i++)
                Ball("Tag toe",visualRoot,new Vector3((i-1)*.026f,.716f+(i==1?.009f:0),.461f),Vector3.one*.02f,light);
            tail=Group("Tail",visualRoot,new Vector3(0,.72f,-.59f));
            if(puppy)
            {
                Rod("Tail base",tail,Vector3.zero,new Vector3(.02f,.23f,-.12f),.15f,fur);
                Rod("Tail tip",tail,new Vector3(.02f,.23f,-.12f),new Vector3(.10f,.36f,-.07f),.14f,light);
            }
            else
            {
                Vector3[] points={Vector3.zero,new Vector3(0,.17f,-.10f),new Vector3(0,.36f,-.14f),new Vector3(0,.54f,-.10f),new Vector3(.06f,.66f,-.02f),new Vector3(.16f,.64f,.04f)};
                for(int i=1;i<points.Length;i++)
                {
                    Material segment=i==points.Length-1?light:(i%2==0?darkFur:fur);
                    Rod("Curved tail",tail,points[i-1],points[i],.13f-i*.005f,segment);
                    Ball("Tail joint",tail,points[i],Vector3.one*(.13f-i*.005f),segment);
                }
            }
            Animate(0,0);
        }

        /// <param name="speed">Movement intensity: zero idle, one normal walk.</param>
        public void Animate(float speed,float time)
        {
            if(visualRoot==null)return;
            movement=Mathf.Lerp(movement,Mathf.Clamp01(speed),.18f);
            wasRunning=false;
            float phase=time*10f;
            visualRoot.localRotation=Quaternion.identity;
            visualRoot.localScale=Vector3.one;
            visualRoot.localPosition=new Vector3(0,Mathf.Sin(time*2.8f)*.009f+Mathf.Abs(Mathf.Sin(phase))*.025f*movement,0);
            for(int i=0;i<legs.Count;i++)
            {
                legs[i].localPosition=legRestPositions[i];
                legs[i].localRotation=Quaternion.Euler(Mathf.Sin(phase+((i==0||i==3)?0:Mathf.PI))*25f*movement,0,0);
            }
            head.localRotation=Quaternion.Euler(Mathf.Sin(time*2.5f)*2f+Mathf.Sin(phase)*movement*1.5f,Mathf.Sin(time*.7f)*4f*(1f-movement),Mathf.Sin(time*1.4f)*1.5f);
            tail.localRotation=Quaternion.Euler(0,Mathf.Sin(time*(isPuppy?7f:2.6f))*(isPuppy?27f:13f),Mathf.Sin(time*2f)*3f);
            if(isPuppy)
            {
                leftEar.localRotation=Quaternion.Euler(Mathf.Sin(phase)*movement*10f,0,-18f);
                rightEar.localRotation=Quaternion.Euler(-Mathf.Sin(phase)*movement*10f,0,18f);
            }
            else {leftEar.localRotation=Quaternion.identity;rightEar.localRotation=Quaternion.identity;}
        }

        /// <summary>A visible four-paw run that responds even to a single maze step.</summary>
        public void AnimateRun(float speed, float time)
        {
            if(visualRoot==null)return;
            float target=Mathf.Clamp01(speed);
            bool running=target>.01f;
            if(running && !wasRunning && movement<.05f) runPhase=0f;
            wasRunning=running;
            float dt=Mathf.Max(0f,Time.deltaTime);
            // Short taps need a fast take-off. Deceleration still settles smoothly.
            float blend=1f-Mathf.Exp(-(target>movement?36f:20f)*dt);
            movement=Mathf.Lerp(movement,target,blend);
            if(movement<.001f && !running)movement=0f;
            runPhase+=dt*23f*movement;
            float phase=runPhase;
            float stride=Mathf.Sin(phase);
            float bound=Mathf.Abs(Mathf.Sin(phase));
            float idle=1f-movement;
            visualRoot.localPosition=new Vector3(0,Mathf.Sin(time*2.8f)*.009f*idle+bound*.09f*movement,0);
            visualRoot.localRotation=Quaternion.Euler(stride*6f*movement,0,Mathf.Sin(phase)*3f*movement);
            float stretch=Mathf.Cos(phase*2f)*.035f*movement;
            visualRoot.localScale=new Vector3(1f-stretch*.4f,1f+stretch,1f-stretch*.6f);
            for(int i=0;i<legs.Count;i++)
            {
                // Diagonal pairs alternate: both front paws visibly take turns.
                float legPhase=phase+((i==0||i==3)?0:Mathf.PI);
                float swing=Mathf.Sin(legPhase);
                float lift=Mathf.Max(0f,swing)*.12f*movement;
                legs[i].localPosition=legRestPositions[i]+new Vector3(0,lift,swing*.10f*movement);
                legs[i].localRotation=Quaternion.Euler(swing*55f*movement,0,(i%2==0?-1:1)*bound*5f*movement);
            }
            head.localRotation=Quaternion.Euler(
                Mathf.Sin(time*2.5f)*2f*idle-stride*4f*movement,
                Mathf.Sin(time*.7f)*4f*idle,
                Mathf.Sin(time*1.4f)*1.5f*idle);
            tail.localRotation=Quaternion.Euler(
                Mathf.Sin(phase-.6f)*12f*movement,
                Mathf.Sin(time*(isPuppy?10f:4f))*(isPuppy?30f:16f),
                Mathf.Sin(time*2f)*3f);
            float flop=Mathf.Sin(phase-.65f)*(isPuppy?24f:8f)*movement;
            float spread=bound*(isPuppy?8f:3f)*movement;
            leftEar.localRotation=Quaternion.Euler(flop,0,-(isPuppy?18f:0f)-spread);
            rightEar.localRotation=Quaternion.Euler(flop,0,(isPuppy?18f:0f)+spread);
        }

        /// <summary>Tuck the paws in flight; the maze moves the image along its jump arc.</summary>
        public void AnimateJump(float progress,float time)
        {
            AnimateRun(0,time);
            float tuck=Mathf.Sin(Mathf.Clamp01(progress)*Mathf.PI);
            visualRoot.localPosition=Vector3.zero;
            visualRoot.localRotation=Quaternion.Euler(Mathf.Sin(progress*Mathf.PI*2)*-10f,0,0);
            visualRoot.localScale=new Vector3(1f,1f-tuck*.06f,1f+tuck*.04f);
            for(int i=0;i<legs.Count;i++)
            {
                legs[i].localPosition=legRestPositions[i]+Vector3.up*(tuck*.08f);
                legs[i].localRotation=Quaternion.Euler((i<2?-50f:45f)*tuck,0,0);
            }
            head.localRotation=Quaternion.Euler(-tuck*5f,0,0);
        }

        /// <summary>Raise the face gently toward the viewer when resting or being washed.</summary>
        public void LookToward(Vector3 worldTarget, float weight)
        {
            if(head==null || weight<=0)return;
            Vector3 local=head.parent.InverseTransformDirection(worldTarget-head.position);
            float horizontal=Mathf.Sqrt(local.x*local.x+local.z*local.z);
            float pitch=Mathf.Clamp(-Mathf.Atan2(local.y,horizontal)*Mathf.Rad2Deg,-22,8);
            float yaw=Mathf.Clamp(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg,-20,20);
            head.localRotation=Quaternion.Slerp(head.localRotation,Quaternion.Euler(pitch,yaw,0),Mathf.Clamp01(weight)*.9f);
        }

        private Material Mat(Color color)
        {
            Shader shader=Shader.Find("Universal Render Pipeline/Lit");
            if(shader==null)shader=Shader.Find("Standard");
            Material material=new Material(shader){name="Pet "+materials.Count};
            if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);
            material.color=color;
            if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.24f);
            materials.Add(material);return material;
        }
        private static Transform Group(string label,Transform parent,Vector3 position)
        {
            Transform result=new GameObject(label).transform;
            result.SetParent(parent,false);result.localPosition=position;return result;
        }
        private static Transform Ball(string label,Transform parent,Vector3 position,Vector3 scale,Material material)
        {
            GameObject part=GameObject.CreatePrimitive(PrimitiveType.Sphere);part.name=label;
            part.transform.SetParent(parent,false);part.transform.localPosition=position;part.transform.localScale=scale;
            part.GetComponent<Renderer>().sharedMaterial=material;
            Collider collider=part.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
            return part.transform;
        }
        private static void Rod(string label,Transform parent,Vector3 start,Vector3 end,float width,Material material)
        {
            Vector3 difference=end-start;
            Transform part=Ball(label,parent,(start+end)*.5f,new Vector3(width,difference.magnitude+width*.3f,width),material);
            part.localRotation=Quaternion.FromToRotation(Vector3.up,difference);
        }
        private void Ear(string label,Transform parent,float width,float height,float depth,Material material)
        {
            Transform part=Group(label,parent,Vector3.zero);
            Mesh mesh=new Mesh{name=label};
            generatedMeshes.Add(mesh);
            mesh.vertices=new[]{new Vector3(-width*.5f,0,depth*.5f),new Vector3(width*.5f,0,depth*.5f),new Vector3(0,height,0),new Vector3(-width*.5f,0,-depth*.5f),new Vector3(width*.5f,0,-depth*.5f)};
            mesh.triangles=new[]{0,1,2,4,3,2,3,0,2,1,4,2,3,4,1,3,1,0};mesh.RecalculateNormals();
            part.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;
            part.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        private void OnDestroy()
        {
            foreach(Material material in materials)if(material!=null)Destroy(material);
            foreach(Mesh mesh in generatedMeshes)if(mesh!=null)Destroy(mesh);
        }
    }
}
