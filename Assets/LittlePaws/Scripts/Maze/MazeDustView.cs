using UnityEngine;
using UnityEngine.UI;

namespace LittlePaws.Maze
{
    /// <summary>A white billowing cloud curtain with a clear circle around the pet's floor position.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MazeDustView : MaskableGraphic
    {
        private Vector2 center;
        private float cellSize=1f, opacity, clock;
        public Vector2 ClearCenter => center;
        public float Opacity => opacity;

        protected override void Awake() { base.Awake();raycastTarget=false; }
        public void SetState(Vector2 clearCenter,float size,float amount,float time)
        {
            if(center==clearCenter && cellSize==size && opacity==amount && clock==time)return;
            center=clearCenter;cellSize=Mathf.Max(.001f,size);opacity=Mathf.Clamp01(amount);clock=time;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();if(opacity<=0)return;
            Rect bounds=rectTransform.rect;
            // A small fixed mesh avoids allocating and uploading a new texture every frame.
            const int columns=80,rows=48;
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
            {
                Vector2 point=new Vector2(Mathf.Lerp(bounds.xMin,bounds.xMax,(float)x/columns),Mathf.Lerp(bounds.yMin,bounds.yMax,(float)y/rows));
                Vector2 offset=point-center;
                float distance=offset.magnitude/cellSize;
                float edge=MazeCloudPattern.EdgeExpansion(Mathf.Atan2(offset.y,offset.x),clock);
                float billow=MazeCloudPattern.Brightness(point.x,point.y,clock);
                Color shade=Color.Lerp(new Color(.80f,.85f,.92f),Color.white,billow);
                shade.a=MazeDustSequence.Obscurity(distance-edge)*opacity;
                var vertex=UIVertex.simpleVert;vertex.position=point;vertex.color=shade;vertex.uv0=Vector2.zero;
                mesh.AddVert(vertex);
            }
            for(int y=0;y<rows;y++)for(int x=0;x<columns;x++)
            {
                int a=y*(columns+1)+x,b=a+1,c=a+columns+1,d=c+1;
                mesh.AddTriangle(a,c,b);mesh.AddTriangle(b,c,d);
            }
        }
    }
}
