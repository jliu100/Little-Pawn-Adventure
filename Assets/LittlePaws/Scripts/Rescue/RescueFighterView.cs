using UnityEngine;
using UnityEngine.UI;

namespace LittlePaws.Rescue
{
    /// <summary>Independent paper-doll limbs keep both armor and boots animated in 2D.</summary>
    public sealed class RescueFighterView
    {
        public RectTransform Root { get; private set; }
        readonly RectTransform puppet, torso, head, leftBoot, rightBoot, leftArm, rightArm, sword, slashArc;
        readonly Image[] images;
        readonly bool roach;
        readonly RescueHeroGraphic approvedHero;
        public RescueFighterView(Transform parent, bool hen, bool isRoach)
        {
            roach = isRoach;
            Root = Rect("Fighter feet", parent, Vector2.zero, Vector2.zero);
            var shadow = Part("Ground shadow", Root, RescueArt.Disc(), new Vector2(0, 3), new Vector2(93, 20));
            shadow.GetComponent<Image>().color = new Color(.12f, .14f, .08f, .25f);
            puppet = Rect("Animated character", Root, Vector2.zero, Vector2.zero);
            var approved = roach ? null : RescueHeroAssets.Load(hen);
            if (approved)
            {
                var visual=Rect("Approved pixel-art hero",puppet,Vector2.zero,Vector2.zero);
                approvedHero=visual.gameObject.AddComponent<RescueHeroGraphic>();approvedHero.Initialize(approved,hen);
                slashArc=Part("Sword trail",puppet,RescueArt.Slash(),new Vector2(55,84),new Vector2(89,89));
                slashArc.gameObject.SetActive(false);
                return;
            }
            leftBoot = Part("Left armored boot", puppet, RescueArt.Boot(), new Vector2(-21, 29), new Vector2(30, 58));
            rightBoot = Part("Right armored boot", puppet, RescueArt.Boot(), new Vector2(21, 29), new Vector2(30, 58));
            if (roach)
            {
                for (int i = 0; i < 4; i++)
                {
                    var leg = Part("Roach side leg", puppet, RescueArt.Arm(), new Vector2(i < 2 ? -38 : 38, 32 + i % 2 * 23), new Vector2(17, 48));
                    leg.localRotation = Quaternion.Euler(0, 0, i < 2 ? -55 : 55);
                    leg.GetComponent<Image>().color = new Color(.57f, .36f, .19f);
                }
            }
            leftArm = Part("Rear arm", puppet, RescueArt.Arm(), new Vector2(-37, 77), new Vector2(25, 60));
            torso = Part("Armored body", puppet, roach ? RescueArt.RoachTorso() : RescueArt.Torso(hen), new Vector2(0, 77), new Vector2(82, 77));
            head = Part("Helmet and face", puppet, roach ? RescueArt.RoachHead() : RescueArt.Head(hen), new Vector2(0, 128), new Vector2(82, 78));
            rightArm = Part("Sword arm", puppet, RescueArt.Arm(), new Vector2(37, 77), new Vector2(25, 60));
            sword = Rect("Swinging iron sword", puppet, new Vector2(37, 50), Vector2.zero);
            Part("Iron blade", sword, RescueArt.Sword(), new Vector2(0, 40), new Vector2(30, 100));
            slashArc = Part("Sword trail", puppet, RescueArt.Slash(), new Vector2(57, 93), new Vector2(105, 100));
            slashArc.gameObject.SetActive(false);
            images = puppet.GetComponentsInChildren<Image>();
        }
        public void Animate(float time, bool walking, int facing, float attackAge, int slash, bool prone, bool hurt)
        {
            float stride = walking ? Mathf.Sin(time * 14) : 0;
            puppet.localScale = new Vector3(facing < 0 ? -1 : 1, 1, 1);
            puppet.localRotation = Quaternion.Euler(0, 0, prone ? -85 : stride * 2);
            puppet.anchoredPosition = new Vector2(0, prone ? 15 : Mathf.Abs(stride) * 3);
            if(approvedHero)
            {
                bool swinging=attackAge>=0&&attackAge<.25f;
                approvedHero.Pose(time,walking,attackAge,slash,hurt?new Color(1,.65f,.53f):prone?new Color(.72f,.70f,.62f):Color.white);
                slashArc.gameObject.SetActive(swinging);slashArc.localScale=new Vector3(slash<0?-1:1,1,1);
                return;
            }
            leftBoot.localRotation = Quaternion.Euler(0, 0, stride * 24);
            rightBoot.localRotation = Quaternion.Euler(0, 0, -stride * 24);
            leftBoot.anchoredPosition = new Vector2(-21 + stride * 7, 29 + Mathf.Max(0, stride) * 8);
            rightBoot.anchoredPosition = new Vector2(21 - stride * 7, 29 + Mathf.Max(0, -stride) * 8);
            leftArm.localRotation = Quaternion.Euler(0, 0, -stride * 14);
            rightArm.localRotation = Quaternion.Euler(0, 0, stride * 14);
            torso.anchoredPosition = new Vector2(0, 77 + Mathf.Abs(stride) * 2);
            head.localRotation = Quaternion.Euler(0, 0, -stride * 2);
            bool attacking = attackAge >= 0 && attackAge < .25f;
            float swing = attacking ? Mathf.Lerp(slash < 0 ? -110 : 125, slash < 0 ? 125 : -110, attackAge / .25f) : -25;
            sword.localRotation = Quaternion.Euler(0, 0, swing);
            slashArc.gameObject.SetActive(attacking);
            slashArc.localScale=new Vector3(slash<0?-1:1,1,1);
            slashArc.localRotation=Quaternion.Euler(0,0,swing*.25f);
            if (attacking) rightArm.localRotation = Quaternion.Euler(0, 0, swing * .4f);
            foreach (var image in images) image.color = hurt ? new Color(1, .65f, .53f) : prone ? new Color(.72f, .70f, .62f) : Color.white;
        }
        static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = new Vector2(.5f, 0);
            r.pivot = new Vector2(.5f, .5f); r.anchoredPosition = position; r.sizeDelta = size; return r;
        }
        static RectTransform Part(string name, Transform parent, Sprite sprite, Vector2 position, Vector2 size)
        {
            var r = Rect(name, parent, position, size); var image = r.gameObject.AddComponent<Image>();
            image.sprite = sprite; image.preserveAspect = false; image.raycastTarget = false; return r;
        }
    }
}
