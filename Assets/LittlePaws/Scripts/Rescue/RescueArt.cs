using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittlePaws
{
    /// <summary>Original layered woodland illustrations. Every sprite is generated locally, with no external assets.</summary>
    public static class RescueArt
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        static readonly Color Ink = C("293238"), Steel = C("99B9C0"), Light = C("D8E6DF"), Red = C("C8543E"), Gold = C("EAB956");
        static Color C(string hex) { Color c; ColorUtility.TryParseHtmlString("#" + hex, out c); return c; }
        static Sprite Get(string name, Action<Paint> draw, int w = 192, int h = 192)
        {
            Sprite found;
            if (Cache.TryGetValue(name, out found) && found != null) return found;
            var p = new Paint(w,h); draw(p);
            var texture = new Texture2D(w,h,TextureFormat.RGBA32,false) { name = "Rescue • " + name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(p.pixels); texture.Apply(false,false);
            found = Sprite.Create(texture,new Rect(0,0,w,h),new Vector2(.5f,.5f),100); found.name = name; Cache[name] = found; return found;
        }
        public static Sprite Head(bool hen) { return Get(hen ? "Hen helmet" : "Rooster helmet", p => DrawHead(p,hen)); }
        static void DrawHead(Paint p, bool hen)
        {
            var feather=hen?C("FFF1D7"):C("F5D39A");
            var crest=hen?C("D87879"):C("CE4C39");
            // A broad, friendly profile, with a raised visor and readable eye.
            p.P(Ink,17,27,10,40,16,50,11,58,25,70,71,69,82,48,73,20,46,13);
            p.P(feather,21,31,16,40,24,46,18,55,30,64,68,63,76,47,68,25,46,19);
            p.P(hen?C("E4CBAD"):C("DCA766"),19,40,35,31,59,30,68,39,68,25,47,19,26,28);
            p.E(48,44,26,24,feather);p.E(42,49,18,14,hen?C("FFFAEA"):C("FFE7BE"));
            p.P(Ink,67,47,96,38,78,28,65,32);p.P(C("F3B74F"),69,43,90,38,76,33,68,35);
            p.L(73,36,85,37,1.3f,C("BB7336"));
            p.E(62,48,8,11,Ink);p.E(63,50,5,8,C("3C453E"));p.E(65,53,3,4,Color.white);p.E(61,46,1.6f,2,C("8EAB82"));
            p.L(54,60,68,58,3,Ink);if(hen){p.L(68,54,74,57,1.5f,Ink);p.E(61,35,6,3,C("EAA18B"));}
            p.E(70,24,5,9,Ink);p.E(70,25,3.5f,7,crest);
            // Crest passes through a reinforced slot above the helmet.
            p.P(Ink,24,77,25,90,34,99,44,93,50,99,60,94,67,80,62,73);
            p.P(crest,28,79,29,89,35,94,43,88,50,94,56,90,62,80,58,76);
            if(!hen){p.P(C("EB7752"),33,84,35,91,41,86,48,92,52,86,57,83);}
            p.P(Ink,12,57,15,73,27,84,50,88,69,80,79,65,78,56,62,56,50,61,31,58);
            p.P(C("78959E"),17,59,20,72,30,80,50,84,66,77,73,65,72,61,59,61,49,66,30,62);
            p.P(C("B9D2D0"),21,70,32,78,49,81,61,76,48,73,30,68);
            p.P(C("E8EEE0"),28,74,36,78,47,79,43,76);
            p.L(17,61,30,63,3,Gold);p.L(31,63,49,67,2,Gold);
            p.P(Ink,15,58,27,56,29,32,22,24,15,34);p.P(Steel,19,55,24,54,25,34,22,30,19,36);
            p.E(22,48,2.5f,3,Gold);p.E(51,80,2,2,C("EDF4E7"));
            p.P(Ink,29,21,54,17,58,10,45,5,26,12);p.P(hen?C("3E9290"):C("BA4940"),30,17,52,14,52,11,44,9,30,14);
        }
        public static Sprite Cape(bool hen) { return Get(hen?"Hen teal cape":"Rooster red cape",p=>{
            p.P(Ink,29,95,66,95,76,71,83,24,68,10,54,20,36,8,14,18,23,63);
            p.P(hen?C("397F80"):C("A53E36"),32,90,62,90,70,67,77,26,67,18,54,27,37,16,22,23,29,63);
            p.P(hen?C("55ACAA"):C("D5684B"),32,87,40,82,34,43,29,25,24,28);
            p.L(37,17,54,28,2,Gold);p.L(55,28,68,18,2,Gold);
        }); }
        public static Sprite Tail(bool hen) { return Get(hen?"Hen feather fan":"Rooster feather fan",p=>{
            if(hen){p.E(45,43,27,25,Ink);p.E(45,44,23,21,C("E9D6B6"));p.E(31,53,14,23,Ink);p.E(32,54,10,19,C("FFF0D0"));}
            else{p.P(Ink,70,15,30,22,7,47,9,84,24,94,35,72,41,89,57,92,61,71,81,68,87,49,74,30);
            p.P(C("326D69"),66,23,34,30,16,51,17,81,23,86,30,64,39,44,42,78,50,84,54,64,72,61,78,49,69,35);
            p.L(24,36,26,70,3,C("64A58F"));p.L(41,36,47,68,3,C("64A58F"));}
        }); }
        public static Sprite Torso(bool hen) { return Get(hen ? "Hen cuirass" : "Rooster cuirass", p => {
            var cloth=hen?C("4B9995"):C("49734F");
            p.P(Ink,27,90,69,90,84,74,80,30,72,14,54,8,28,14,17,32,14,72);
            p.P(cloth,29,84,65,84,77,71,74,32,66,19,53,14,32,20,24,34,22,69);
            // Small curved breastplate, gold egg crest and leather waist belt.
            p.P(Ink,25,80,40,86,66,81,75,65,67,35,48,27,29,36,20,61);
            p.P(C("78959E"),28,75,42,80,62,76,68,64,62,40,48,33,35,41,27,61);
            p.P(C("BDD4D2"),30,71,42,76,47,72,43,43,36,47,30,61);
            p.P(C("E1EBDF"),33,69,40,72,41,68,37,49,34,52);
            p.P(C("506F7A"),54,72,63,68,61,47,49,38,50,56);
            p.L(29,75,43,80,2,Gold);p.L(43,80,64,75,2,Gold);p.L(35,40,48,33,2,Gold);p.L(48,33,62,40,2,Gold);
            p.E(49,59,7,10,Ink);p.E(49,60,5,7,Gold);p.E(47,63,2,3,C("FFF1B8"));
            p.P(Ink,23,32,73,32,76,19,23,18);p.P(C("78513E"),27,28,70,28,71,22,27,22);
            p.P(Gold,44,31,57,31,57,18,44,18);p.P(Ink,48,27,53,27,53,22,48,22);
            p.P(cloth,29,18,45,17,46,6,24,10);p.P(cloth,54,17,68,18,75,9,53,5);
            p.L(29,12,42,10,2,Gold);p.L(57,10,69,12,2,Gold);
        }); }
        public static Sprite Arm() { return Get("Armored wing", p => {
            p.P(Ink,26,77,37,93,62,93,76,78,70,60,62,50,65,27,57,8,35,8,26,25,31,53);
            p.P(C("89A9B0"),31,77,40,87,59,87,69,77,65,65,38,63);
            p.P(C("D9E4D9"),36,78,43,83,56,83,54,78);
            p.P(C("E9CDA3"),38,61,61,61,56,44,38,40);p.P(C("C69C73"),51,58,61,59,56,44,49,43);
            p.P(Ink,32,42,61,45,66,27,59,20,33,21);
            p.P(Steel,36,39,58,40,61,28,56,25,37,26);p.L(40,35,55,36,2,Light);
            p.E(47,15,14,12,Ink);p.E(47,16,10,9,C("715443"));p.L(40,18,52,18,2,C("A48361"));
        }); }
        public static Sprite Boot() { return Get("Protected greave and boot", p => {
            p.P(Ink,23,94,69,94,72,51,85,33,88,10,72,4,16,4,13,20,23,46);
            p.P(C("66838C"),28,88,63,88,65,49,78,30,81,14,70,10,23,10,20,21,29,47);
            p.P(Steel,30,82,61,82,60,52,46,43,27,49);p.P(Light,33,77,42,78,39,54,31,54);
            p.P(C("A9C1C0"),28,38,63,40,73,28,74,20,25,20);p.L(27,35,62,36,3,Ink);
            p.L(23,16,78,16,4,Ink);p.L(27,23,70,23,2,C("D2DFD3"));p.E(59,70,2.5f,2.5f,Gold);
        }); }
        public static Sprite Sword() { return Get("Iron sword", p => {
            p.P(Ink,39,35,40,83,50,99,61,83,61,35);p.P(C("A6BDC4"),44,39,44,81,50,91,56,81,56,39);
            p.P(C("EEF3E8"),45,42,45,80,50,89,50,43);p.L(52,45,52,79,2,C("73949F"));
            p.P(Ink,20,38,27,46,40,42,60,42,73,46,80,38,71,29,58,33,42,33,28,29);
            p.P(Gold,26,38,29,41,42,38,58,38,71,41,74,38,69,34,57,36,43,36,30,34);
            p.L(50,11,50,32,13,Ink);p.L(50,14,50,31,8,C("76503D"));p.L(47,18,53,21,2,Gold);p.L(47,25,53,28,2,Gold);
            p.E(50,8,8,6,Ink);p.E(50,9,5,4,Gold);p.E(49,10,2,2,C("FFF0B3"));
        }); }
        public static Sprite RoachHead() { return Get("Roach swordsman head", p => {
            p.L(37,70,18,94,4,Ink); p.L(62,71,81,94,4,Ink); p.E(18,94,4,3,Gold); p.E(81,94,4,3,Gold);
            p.E(50,49,32,33,Ink); p.E(50,50,28,29,C("98633F")); p.P(C("BC8550"),29,62,49,74,63,64,48,56);
            p.E(35,54,12,15,Ink); p.E(65,54,12,15,Ink); p.E(37,56,8,10,C("F6D98C")); p.E(63,56,8,10,C("F6D98C"));
            p.E(40,55,3,7,Ink); p.E(60,55,3,7,Ink); p.L(23,70,44,65,5,Ink); p.L(56,65,77,70,5,Ink);
            p.P(Ink,33,26,40,36,49,29,58,36,68,26,58,17,42,17); p.L(40,26,59,26,3,C("E4BC85"));
        }); }
        public static Sprite RoachTorso() { return Get("Roach swordsman shell", p => {
            p.E(49,49,34,44,Ink); p.E(49,50,29,39,C("784836"));
            p.P(C("AD784A"),24,72,43,86,43,15,30,24); p.P(C("925E3C"),53,86,73,71,67,25,53,14);
            p.L(49,85,49,13,4,Ink); p.L(26,64,72,64,3,C("5F3C31")); p.L(23,45,75,45,3,C("5F3C31")); p.L(28,27,70,27,3,C("5F3C31"));
            p.P(Ink,19,77,77,45,73,34,17,66); p.P(C("68858B"),20,72,74,44,72,39,20,68); p.E(48,56,7,7,Gold);
        }); }
        public static Sprite Egg() { return Get("Rescued golden egg", p => { p.E(50,44,29,39,Ink); p.E(50,46,26,35,C("F7D990")); p.E(40,55,9,17,C("FFF3C5")); p.E(58,28,10,3,C("DBAE63")); }); }
        public static Sprite Disc() { return Get("Soft circle",p=>p.E(50,50,49,49,Color.white)); }
        public static Sprite Rounded() { return Get("Rounded panel",p=> {p.P(Color.white,12,2,88,2,98,12,98,88,88,98,12,98,2,88,2,12);}); }
        public static Sprite Arrow() { return Get("Go arrow",p=> {p.P(Ink,5,33,58,33,58,10,96,50,58,90,58,67,5,67);p.P(Gold,10,39,64,39,64,25,87,50,64,75,64,61,10,61);}); }
        public static Sprite Slash() { return Get("Sword slash",p=> {p.P(C("FFF1B2"),8,18,27,58,60,84,94,91,67,77,42,55,26,24);p.P(Color.white,21,32,46,66,76,84,56,65,36,40);}); }
        public static Sprite Forest() { return Get("Layered woodland",p=> {
            p.Rect(0,0,100,100,C("B9D0B2")); p.E(76,76,15,18,C("E9DFA7"));
            for(int layer=0;layer<3;layer++) { var leaf=layer==0?C("88AC94"):layer==1?C("537F70"):C("315C50"); var trunk=layer==0?C("839F89"):layer==1?C("6A7962"):C("635744");
                for(int i=0;i<8;i++) { float x=i*16-4+layer*5; p.P(trunk,x-2,0,x+3,0,x+2,73-layer*8,x-1,86-layer*8); p.L(x,46,x-8,65,2,trunk); p.L(x,56,x+9,72,2,trunk);p.E(x,86-layer*8,13,20,leaf);p.E(x-8,73-layer*7,11,14,leaf);p.E(x+9,76-layer*6,13,16,leaf); }
            }
            p.P(C("40634A"),0,4,0,19,20,24,40,14,65,21,83,15,100,24,100,0);
        },1024,384); }
        public static Sprite Dirt() { return Get("Warm dirt path", p=> {
            p.Rect(0,0,100,100,C("B68A5A")); p.P(C("C79A66"),0,10,0,92,23,85,48,94,76,86,100,95,100,4,70,12,44,5,22,13);
            var random=new System.Random(817); for(int i=0;i<180;i++){float x=(float)random.NextDouble()*100,y=(float)random.NextDouble()*100;p.E(x,y,.12f+(float)random.NextDouble()*.35f,.18f+(float)random.NextDouble()*.5f,i%3==0?C("D8B47D"):C("A27851"));}
            for(int i=0;i<20;i++){float x=i*5.7f;p.L(x,94,x-1,99, .3f,C("64804D"));p.L(x,96,x+1,100,.3f,C("64804D"));}
        },1024,512); }
        public static Sprite Hero(bool hen) { var approved=LittlePaws.Rescue.RescueHeroAssets.Load(hen);if(approved)return approved;return Get(hen?"Hen hero preview":"Rooster hero preview",p=> {
            p.Blit(Tail(hen),1,24,40,36);p.Blit(Cape(hen),15,17,63,61);
            p.Blit(Boot(),22,0,31,31);p.Blit(Boot(),49,0,31,31);
            p.Blit(Arm(),11,30,30,41);p.Blit(Torso(hen),23,27,58,43);
            p.Blit(Head(hen),17,58,66,42);p.Blit(Arm(),65,30,30,41);p.Blit(Sword(),78,23,22,59);
        },512,768); }
        public static Sprite Roach() { return Get("Roach swordsman preview",p=> {p.Blit(Boot(),25,0,25,31);p.Blit(Boot(),52,0,25,31);p.Blit(RoachTorso(),24,24,53,47);p.Blit(RoachHead(),24,64,53,35);p.Blit(Arm(),65,29,23,36);p.Blit(Sword(),76,25,19,56);},256,384); }
        sealed class Paint
        {
            public readonly Color32[] pixels; readonly int w,h;
            public Paint(int width,int height){w=width;h=height;pixels=new Color32[w*h];}
            public void Rect(float x,float y,float width,float height,Color c){P(c,x,y,x+width,y,x+width,y+height,x,y+height);}
            public void E(float x,float y,float rx,float ry,Color c){int x0=Mathf.Max(0,(int)((x-rx)*w/100)),x1=Mathf.Min(w-1,(int)((x+rx)*w/100)+1),y0=Mathf.Max(0,(int)((y-ry)*h/100)),y1=Mathf.Min(h-1,(int)((y+ry)*h/100)+1);for(int j=y0;j<=y1;j++)for(int i=x0;i<=x1;i++){float a=((i+.5f)*100/w-x)/rx,b=((j+.5f)*100/h-y)/ry;if(a*a+b*b<=1)pixels[j*w+i]=c;}}
            public void L(float x1,float y1,float x2,float y2,float width,Color c){float dx=x2-x1,dy=y2-y1,len=Mathf.Sqrt(dx*dx+dy*dy);if(len<.01f)return;float ox=-dy/len*width*.5f,oy=dx/len*width*.5f;P(c,x1+ox,y1+oy,x2+ox,y2+oy,x2-ox,y2-oy,x1-ox,y1-oy);E(x1,y1,width/2,width/2,c);E(x2,y2,width/2,width/2,c);}
            public void P(Color c,params float[] xy){float minX=100,maxX=0,minY=100,maxY=0;for(int n=0;n<xy.Length;n+=2){minX=Mathf.Min(minX,xy[n]);maxX=Mathf.Max(maxX,xy[n]);minY=Mathf.Min(minY,xy[n+1]);maxY=Mathf.Max(maxY,xy[n+1]);}for(int j=Mathf.Max(0,(int)(minY*h/100));j<=Mathf.Min(h-1,(int)(maxY*h/100)+1);j++)for(int i=Mathf.Max(0,(int)(minX*w/100));i<=Mathf.Min(w-1,(int)(maxX*w/100)+1);i++){float x=(i+.5f)*100/w,y=(j+.5f)*100/h;bool inside=false;for(int a=0,b=xy.Length-2;a<xy.Length;b=a,a+=2){if((xy[a+1]>y)!=(xy[b+1]>y)&&x<(xy[b]-xy[a])*(y-xy[a+1])/(xy[b+1]-xy[a+1])+xy[a])inside=!inside;}if(inside)pixels[j*w+i]=c;}}
            public void Blit(Sprite sprite,float x,float y,float width,float height){
                var source=sprite.texture; var data=source.GetPixels32();
                int left=(int)(x*w/100),bottom=(int)(y*h/100),ww=(int)(width*w/100),hh=(int)(height*h/100);for(int j=0;j<hh;j++)for(int i=0;i<ww;i++){int xx=left+i,yy=bottom+j;if(xx<0||xx>=w||yy<0||yy>=h)continue;var color=data[Mathf.Min(source.height-1,j*source.height/hh)*source.width+Mathf.Min(source.width-1,i*source.width/ww)];if(color.a>0)pixels[yy*w+xx]=color;}
            }
        }
    }
}
