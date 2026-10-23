using UnityEngine;
using UnityEngine.UI;

namespace LittlePaws.Rescue
{
    /// <summary>A lightweight 2D mesh rig animates the approved full-body sprite without replacing its artwork.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RescueHeroGraphic : MaskableGraphic
    {
        Sprite sprite;
        bool hen,walking;
        float clock,attackAge=10;
        int slash;
        public bool HasArtwork=>sprite;
        public override Texture mainTexture=>sprite?sprite.texture:Texture2D.whiteTexture;
        public void Initialize(Sprite artwork,bool isHen)
        {
            sprite=artwork;hen=isHen;raycastTarget=false;
            rectTransform.pivot=new Vector2(sprite.pivot.x/sprite.rect.width,0);
            rectTransform.sizeDelta=new Vector2(167*sprite.rect.width/sprite.rect.height,167);
            SetAllDirty();
        }
        public void Pose(float time,bool isWalking,float swingAge,int direction,Color tint)
        {
            clock=time;walking=isWalking;attackAge=swingAge;slash=direction;color=tint;SetVerticesDirty();
        }
        static float Edge(float from,float to,float value)
        {
            float t=Mathf.InverseLerp(from,to,value);return t*t*(3-2*t);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(!sprite)return;
            const int columns=40,rows=44;
            Rect box=GetPixelAdjustedRect(),uv=sprite.rect;
            uv=new Rect(uv.x/sprite.texture.width,uv.y/sprite.texture.height,uv.width/sprite.texture.width,uv.height/sprite.texture.height);
            float stride=walking?Mathf.Sin(clock*14):0;
            bool attacking=attackAge>=0&&attackAge<.25f;
            float swing=attacking?Mathf.Sin(attackAge/.25f*Mathf.PI)*(slash<0?1:-1)*70*Mathf.Deg2Rad:0;
            Vector2 wristUv=new Vector2(hen?.80f:.81f,hen?.35f:.36f);
            Vector2 wrist=new Vector2(box.xMin+(wristUv.x-uv.xMin)/uv.width*box.width,box.yMin+(wristUv.y-uv.yMin)/uv.height*box.height);
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
            {
                float u=x/(float)columns,v=y/(float)rows;
                Vector2 source=new Vector2(uv.x+u*uv.width,uv.y+v*uv.height);
                Vector2 p=new Vector2(box.xMin+u*box.width,box.yMin+v*box.height);
                float legWeight=1-Edge(.10f,.26f,source.y);
                float legSide=source.x<.56f?1:-1;
                p.x+=stride*legSide*box.width*.035f*legWeight;
                p.y+=Mathf.Max(0,stride*legSide)*box.height*.025f*legWeight;
                // Vertices in the sword and gauntlet region pivot around the wrist.
                float swordWeight=Edge(.735f,.84f,source.x)*Edge(.25f,.34f,source.y);
                swordWeight*=1-Edge(.69f,.75f,source.y);
                if(attacking&&swordWeight>0)
                {
                    Vector2 delta=p-wrist;
                    Vector2 rotated=new Vector2(delta.x*Mathf.Cos(swing)-delta.y*Mathf.Sin(swing),delta.x*Mathf.Sin(swing)+delta.y*Mathf.Cos(swing));
                    p=Vector2.Lerp(p,wrist+rotated,swordWeight);
                }
                p.y+=Mathf.Abs(stride)*box.height*.008f*(1-legWeight);
                vh.AddVert(p,color,source);
            }
            for(int y=0;y<rows;y++)for(int x=0;x<columns;x++)
            {
                int a=y*(columns+1)+x,b=a+columns+1;
                vh.AddTriangle(a,b,a+1);vh.AddTriangle(a+1,b,b+1);
            }
        }
    }
}
