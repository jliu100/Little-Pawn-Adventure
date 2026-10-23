using UnityEngine;

namespace LittlePaws.Rescue
{
    public static class RescueHeroAssets
    {
        static Sprite rooster,hen;
        public static Sprite Load(bool isHen)
        {
            var cached=isHen?hen:rooster;if(cached)return cached;
            var original=Resources.Load<Sprite>("LittlePaws/Rescue/Heroes/"+(isHen?"HenKnight":"RoosterKnight"));
            if(!original)return null;
            var texture=original.texture;var pixels=texture.GetPixels32();
            int left=texture.width,right=0,bottom=texture.height,top=0;
            for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)
            {
                if(pixels[y*texture.width+x].a<24)continue;
                left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);
            }
            if(right<=left||top<=bottom)return null;
            var bounds=new Rect(left,bottom,right-left+1,top-bottom+1);
            // The feet sit right of the full silhouette center because of the tail and shield.
            float footX=texture.width*(isHen?.56f:.57f);
            var pivot=new Vector2(Mathf.Clamp01((footX-left)/bounds.width),0);
            var sprite=Sprite.Create(texture,bounds,pivot,100,0,SpriteMeshType.FullRect);
            sprite.name=isHen?"Approved Hen Knight":"Approved Rooster Knight";
            if(isHen)hen=sprite;else rooster=sprite;return sprite;
        }
    }
}
