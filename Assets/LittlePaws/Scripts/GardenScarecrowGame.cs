using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LittlePaws
{
    public enum GardenPestKind { Bird, Squirrel }
    public enum GardenPestEntry { Top, Left, Right }

    // Pure rules are shared with the editor checks; one surviving crop earns two skill points.
    public static class GardenScarecrowRules
    {
        public const float DurationSeconds = 120f;
        public const int CropCount = 6;
        public static bool CanEnter(GardenPestKind kind, GardenPestEntry entry)
        {
            return entry == GardenPestEntry.Left || entry == GardenPestEntry.Right ||
                (kind == GardenPestKind.Bird && entry == GardenPestEntry.Top);
        }
        public static float TravelSeconds(float elapsed)
        {
            // Even the fastest pest stays on screen long enough for a deliberate touch.
            if (elapsed < 60f) return 5.2f;
            return 4.2f - Math.Min(1f, Math.Max(0f, (elapsed - 60f) / 30f)) * .8f;
        }
        public static float SpawnSeconds(float elapsed) { return elapsed < 60f ? 2.5f : 1.85f; }
        public static int RewardPoints(int survivors) { return Math.Max(0, Math.Min(CropCount, survivors)) * 2; }
    }

    /// <summary>A daily two-minute garden defense round. Ownership of the daily attempt and XP save stays with the caller.</summary>
    public sealed class GardenScarecrowGame : MonoBehaviour
    {
        public bool IsRunning { get; private set; }
        public bool IsShowing => overlay && overlay.gameObject.activeSelf;

        sealed class Pest
        {
            public RectTransform root, art;
            public Button button;
            public GardenPestKind kind;
            public Vector2 from, to, fleeDirection;
            public int target;
            public float age, travel, fleeAge;
            public bool fleeing;
        }

        RectTransform overlay, content, field, result, pausePanel;
        Text timerLabel, cropLabel, difficultyLabel, resultLabel, resultDetail;
        readonly List<Pest> pests = new List<Pest>();
        readonly RectTransform[] crops = new RectTransform[GardenScarecrowRules.CropCount];
        readonly bool[] alive = new bool[GardenScarecrowRules.CropCount];
        readonly Vector2[] cropPositions = new Vector2[GardenScarecrowRules.CropCount];
        Sprite rounded;
        Font font;
        Action<int> onFinished;
        Action onAbandoned;
        float elapsed, nextSpawn;
        int survivors;
        bool settled, paused, applicationPaused, applicationFocused = true;

        public void Initialize(RectTransform parent, Sprite roundedSprite, Font uiFont,
            Action<int> finished, Action abandoned)
        {
            Hide();
            if (overlay) Destroy(overlay.gameObject);
            rounded = roundedSprite; font = uiFont; onFinished = finished; onAbandoned = abandoned;
            overlay = Rect("Scarecrow daily game", parent, Vector2.zero, Vector2.zero);
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            var backdrop = overlay.gameObject.AddComponent<Image>();
            backdrop.color = new Color(.08f, .16f, .12f, .98f);
            content = Rect("Game layout", overlay, Vector2.zero, new Vector2(1440, 810));
            Label("Heading", content, "SCARECROW WATCH", new Vector2(-430, 360), new Vector2(470, 48), 32, Color.white);
            timerLabel = Label("Time", content, "2:00", new Vector2(0, 360), new Vector2(180, 48), 38, new Color(1f, .87f, .43f));
            cropLabel = Label("Crops remaining", content, "6 / 6 crops", new Vector2(270, 360), new Vector2(280, 48), 27, Color.white);
            MakeButton("Pause", content, "PAUSE", new Vector2(566, 358), new Vector2(190, 64), PauseRound);
            difficultyLabel = Label("Difficulty", content, "Tap birds and squirrels before they reach your crops.",
                new Vector2(0, 305), new Vector2(1270, 42), 23, new Color(.77f, .91f, .78f));

            field = Rect("Garden playfield", content, new Vector2(0, -5), new Vector2(1280, 550));
            field.gameObject.AddComponent<RectMask2D>();
            var lawn = field.gameObject.AddComponent<Image>(); lawn.color = new Color(.30f, .49f, .29f);
            // Tall soft strips make incoming pests stand apart from the center crop beds.
            for (int stripe = 0; stripe < 9; stripe++)
                Shape("Grass row", field, new Vector2(-576 + stripe * 144, 0), new Vector2(66, 550), new Color(.34f, .54f, .30f), false);
            Shape("Crop bed", field, Vector2.zero, new Vector2(460, 276), new Color(.34f, .24f, .15f), true);
            for (int i = 0; i < crops.Length; i++)
            {
                cropPositions[i] = new Vector2((i % 3 - 1) * 136, i < 3 ? 73 : -73);
                crops[i] = Rect("Protected corn " + (i + 1), field, cropPositions[i], new Vector2(90, 114));
                DrawCorn(crops[i]);
            }
            Label("Instructions", content, "Two minutes  •  Faster visitors after 1:00  •  Each saved crop = 2 gardening points",
                new Vector2(0, -326), new Vector2(1290, 45), 23, Color.white);
            Label("Farm safety", content, "This practice patch does not use your planted crops. Leaving uses today’s attempt.",
                new Vector2(0, -369), new Vector2(1310, 36), 20, new Color(.75f, .84f, .77f));
            CreatePauseAndResult();
            overlay.gameObject.SetActive(false);
        }

        public void Begin()
        {
            if (!overlay || IsRunning) return;
            ClearPests();
            elapsed = 0; nextSpawn = 1.4f; survivors = GardenScarecrowRules.CropCount;
            settled = false; paused = false; applicationPaused = false;
            applicationFocused = Application.isFocused;
            for (int i = 0; i < alive.Length; i++) { alive[i] = true; crops[i].gameObject.SetActive(true); }
            result.gameObject.SetActive(false); pausePanel.gameObject.SetActive(false);
            overlay.gameObject.SetActive(true); overlay.SetAsLastSibling();
            IsRunning = true; UpdateLabels();
            if (!applicationFocused) PauseRound();
        }

        public void Hide()
        {
            bool abandon = IsRunning && !settled;
            IsRunning = false; settled = true; paused = false;
            ClearPests();
            if (overlay) overlay.gameObject.SetActive(false);
            if (abandon) onAbandoned?.Invoke();
        }

        void OnDestroy()
        {
            Hide();
            if (overlay) Destroy(overlay.gameObject);
        }

        void OnApplicationPause(bool value)
        {
            applicationPaused = value;
            if (value && IsRunning) PauseRound();
        }
        void OnApplicationFocus(bool value)
        {
            applicationFocused = value;
            if (!value && IsRunning) PauseRound();
        }

        void LateUpdate()
        {
            if(content&&overlay)
                content.localScale=Vector3.one*Mathf.Min(overlay.rect.width/1440f,overlay.rect.height/810f);
        }

        void Update()
        {
            if (!IsRunning || paused || applicationPaused || !applicationFocused) return;
            // Unscaled time keeps the minigame duration independent of the surrounding scene's time scale.
            float dt = Time.unscaledDeltaTime;
            elapsed = Mathf.Min(GardenScarecrowRules.DurationSeconds, elapsed + dt);
            if (elapsed >= GardenScarecrowRules.DurationSeconds) { Finish(); return; }
            if (elapsed >= nextSpawn && survivors > 0)
            {
                Spawn(); nextSpawn = elapsed + GardenScarecrowRules.SpawnSeconds(elapsed);
            }
            for (int i = pests.Count - 1; i >= 0; i--)
            {
                Pest pest = pests[i];
                if (pest.fleeing)
                {
                    pest.fleeAge += dt;
                    pest.root.anchoredPosition += pest.fleeDirection * (820 * dt);
                    pest.root.localScale = Vector3.one * Mathf.Lerp(1, .55f, pest.fleeAge / .55f);
                    if (pest.fleeAge >= .55f) RemovePest(i);
                    continue;
                }
                // If another visitor already took the chosen crop, seek a surviving one instead.
                if (!alive[pest.target])
                {
                    int target = ChooseCrop();
                    if (target < 0) { Scare(pest); continue; }
                    pest.from = pest.root.anchoredPosition; pest.to = cropPositions[target];
                    pest.target = target; pest.age = 0; pest.travel = Mathf.Max(1.8f, pest.travel * .7f);
                }
                pest.age += dt;
                float t = Mathf.Clamp01(pest.age / pest.travel);
                pest.root.anchoredPosition = Vector2.Lerp(pest.from, pest.to, t);
                float swing = Mathf.Sin(pest.age * (pest.kind == GardenPestKind.Bird ? 19 : 15));
                pest.art.localEulerAngles = new Vector3(0, 0, swing * (pest.kind == GardenPestKind.Bird ? 9 : 5));
                if (t >= 1)
                {
                    alive[pest.target] = false; survivors--;
                    crops[pest.target].gameObject.SetActive(false);
                    RemovePest(i); UpdateLabels();
                }
            }
            UpdateLabels();
        }

        void Spawn()
        {
            int target = ChooseCrop(); if (target < 0) return;
            var kind = UnityEngine.Random.value < .58f ? GardenPestKind.Bird : GardenPestKind.Squirrel;
            var entry = kind == GardenPestKind.Bird
                ? (GardenPestEntry)UnityEngine.Random.Range(0, 3)
                : (UnityEngine.Random.value < .5f ? GardenPestEntry.Left : GardenPestEntry.Right);
            Vector2 from = entry == GardenPestEntry.Top
                ? new Vector2(UnityEngine.Random.Range(-470f, 470f), 236)
                : new Vector2(entry == GardenPestEntry.Left ? -588 : 588, UnityEngine.Random.Range(-205f, 205f));
            var root = Rect(kind.ToString(), field, from, new Vector2(100, 100));
            // An obvious soft ring exposes the full 100px touch target rather than tiny animal details.
            var ring = root.gameObject.AddComponent<Image>(); ring.sprite = rounded;
            ring.color = new Color(1f, .98f, .72f, .24f);
            var art = Rect("Animal artwork", root, Vector2.zero, new Vector2(100, 100));
            if (kind == GardenPestKind.Bird) DrawBird(art); else DrawSquirrel(art);
            var pest = new Pest { root = root, art = art, kind = kind, from = from,
                to = cropPositions[target], target = target, travel = GardenScarecrowRules.TravelSeconds(elapsed) };
            pest.button = root.gameObject.AddComponent<Button>(); pest.button.targetGraphic = ring;
            pest.button.onClick.AddListener(() => { if (IsRunning && !paused) Scare(pest); });
            // Flip the animal to face its approach, preserving a readable silhouette on both sides.
            art.localScale = new Vector3(from.x > cropPositions[target].x ? -1 : 1, 1, 1);
            pests.Add(pest);
        }

        void Scare(Pest pest)
        {
            if (pest.fleeing) return;
            pest.fleeing = true; pest.button.interactable = false;
            Vector2 away = pest.root.anchoredPosition - cropPositions[pest.target];
            pest.fleeDirection = away.sqrMagnitude < .01f ? Vector2.up : away.normalized;
            pest.root.GetComponent<Image>().color = new Color(1f, .94f, .47f, .6f);
        }
        int ChooseCrop()
        {
            if (survivors <= 0) return -1;
            int pick = UnityEngine.Random.Range(0, survivors);
            for (int i = 0; i < alive.Length; i++) if (alive[i] && pick-- == 0) return i;
            return -1;
        }
        void RemovePest(int index)
        {
            if (pests[index].root) Destroy(pests[index].root.gameObject);
            pests.RemoveAt(index);
        }
        void ClearPests()
        {
            for (int i = pests.Count - 1; i >= 0; i--) RemovePest(i);
        }

        void UpdateLabels()
        {
            if (!timerLabel) return;
            int left = Mathf.CeilToInt(Mathf.Max(0, GardenScarecrowRules.DurationSeconds - elapsed));
            timerLabel.text = (left / 60) + ":" + (left % 60).ToString("00");
            cropLabel.text = survivors + " / 6 crops";
            difficultyLabel.text = survivors == 0 ? "The patch is empty. The round ends when the timer reaches zero."
                : elapsed < 60 ? "Tap birds and squirrels before they reach your crops."
                : "MEDIUM • Faster visitors! Tap each glowing animal to scare it away.";
        }
        void PauseRound()
        {
            if (!IsRunning) return;
            paused = true; pausePanel.gameObject.SetActive(true); pausePanel.SetAsLastSibling();
        }
        void ResumeRound()
        {
            if (applicationPaused || !applicationFocused) return;
            paused = false; pausePanel.gameObject.SetActive(false);
        }
        void Finish()
        {
            if (settled) return;
            IsRunning = false; settled = true; ClearPests(); UpdateLabels();
            resultLabel.text = survivors == 6 ? "PERFECT GARDEN WATCH!" : "GARDEN WATCH COMPLETE";
            resultDetail.text = survivors + " of 6 crops saved\n+" + GardenScarecrowRules.RewardPoints(survivors) +
                " gardening points\n\nYour Scarecrow will have another round tomorrow.";
            result.gameObject.SetActive(true); result.SetAsLastSibling();
            onFinished?.Invoke(survivors);
        }

        void CreatePauseAndResult()
        {
            pausePanel = Panel("Paused", content, new Vector2(730, 370));
            Label("Pause title", pausePanel, "GARDEN WATCH PAUSED", new Vector2(0, 105), new Vector2(650, 50), 31, Color.white);
            Label("Pause detail", pausePanel, "The timer and visitors are paused.\nLeaving uses today’s attempt and earns no points.",
                new Vector2(0, 30), new Vector2(650, 82), 23, new Color(.83f, .91f, .83f));
            MakeButton("Resume", pausePanel, "RESUME", new Vector2(-157, -102), new Vector2(272, 66), ResumeRound);
            MakeButton("Leave", pausePanel, "LEAVE ROUND", new Vector2(157, -102), new Vector2(272, 66), Hide);
            pausePanel.gameObject.SetActive(false);
            result = Panel("Round result", content, new Vector2(800, 410));
            resultLabel = Label("Result title", result, "GARDEN WATCH COMPLETE", new Vector2(0, 137), new Vector2(740, 60), 31, Color.white);
            resultDetail = Label("Result detail", result, "", new Vector2(0, 14), new Vector2(740, 160), 27, new Color(.89f, .95f, .81f));
            MakeButton("Return", result, "BACK TO GARDEN", new Vector2(0, -140), new Vector2(365, 66), Hide);
            result.gameObject.SetActive(false);
        }

        // These UI silhouettes keep all artwork local and crisp without importing external assets.
        void DrawCorn(RectTransform parent)
        {
            Shape("Shadow", parent, new Vector2(0, -40), new Vector2(75, 16), new Color(0, 0, 0, .2f));
            Shape("Stalk", parent, new Vector2(0, -3), new Vector2(10, 95), new Color(.27f, .57f, .18f));
            Leaf(parent, new Vector2(-18, -12), new Vector2(18, 56), 40, new Color(.42f, .74f, .20f));
            Leaf(parent, new Vector2(18, -8), new Vector2(18, 57), -38, new Color(.29f, .66f, .17f));
            Shape("Corn cob", parent, new Vector2(0, 16), new Vector2(34, 64), new Color(1f, .78f, .18f));
            for (int y = 0; y < 4; y++) for (int x = 0; x < 2; x++)
                Shape("Kernel", parent, new Vector2(-7 + x * 14, -4 + y * 14), new Vector2(9, 9), new Color(1, .91f, .41f));
        }
        void DrawBird(RectTransform parent)
        {
            Color blue = new Color(.20f, .48f, .79f), wing = new Color(.31f, .65f, .90f);
            Leaf(parent, new Vector2(-30, -4), new Vector2(30, 22), 24, blue);
            Leaf(parent, new Vector2(-8, 15), new Vector2(29, 51), 38, wing);
            Shape("Bird body", parent, new Vector2(0, -4), new Vector2(61, 36), blue);
            Shape("Bird breast", parent, new Vector2(12, -10), new Vector2(30, 23), new Color(.79f, .90f, .95f));
            Shape("Bird head", parent, new Vector2(23, 14), new Vector2(32, 31), blue);
            Leaf(parent, new Vector2(43, 12), new Vector2(22, 12), -13, new Color(1, .70f, .18f));
            Shape("Eye", parent, new Vector2(30, 19), new Vector2(7, 8), new Color(.08f, .15f, .24f));
            Shape("Eye glint", parent, new Vector2(31, 21), new Vector2(3, 3), Color.white);
        }
        void DrawSquirrel(RectTransform parent)
        {
            Color brown = new Color(.67f, .34f, .16f), light = new Color(.92f, .65f, .36f);
            Leaf(parent, new Vector2(-27, 4), new Vector2(39, 67), -25, brown);
            Leaf(parent, new Vector2(-27, 6), new Vector2(22, 46), -25, light);
            Shape("Squirrel body", parent, new Vector2(1, -13), new Vector2(48, 40), brown);
            Shape("Belly", parent, new Vector2(12, -12), new Vector2(26, 29), light);
            Leaf(parent, new Vector2(16, 29), new Vector2(15, 25), 13, brown);
            Shape("Squirrel head", parent, new Vector2(23, 9), new Vector2(35, 32), brown);
            Shape("Muzzle", parent, new Vector2(36, 2), new Vector2(23, 17), light);
            Shape("Nose", parent, new Vector2(45, 6), new Vector2(8, 7), new Color(.15f, .11f, .08f));
            Shape("Eye", parent, new Vector2(29, 15), new Vector2(7, 8), new Color(.10f, .09f, .07f));
            Shape("Eye glint", parent, new Vector2(30, 17), new Vector2(3, 3), Color.white);
            Shape("Feet", parent, new Vector2(14, -34), new Vector2(48, 10), brown);
        }
        RectTransform Panel(string name, Transform parent, Vector2 size)
        {
            var rect = Rect(name, parent, Vector2.zero, size);
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = rounded;
            image.color = new Color(.12f, .24f, .18f, .99f); return rect;
        }
        static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        RectTransform Shape(string name, Transform parent, Vector2 position, Vector2 size, Color color, bool soft = true)
        {
            var rect = Rect(name, parent, position, size); var image = rect.gameObject.AddComponent<Image>();
            image.sprite = soft ? rounded : null; image.color = color; image.raycastTarget = false; return rect;
        }
        void Leaf(Transform parent, Vector2 position, Vector2 size, float angle, Color color)
        {
            Shape("Leaf or silhouette", parent, position, size, color).localEulerAngles = new Vector3(0, 0, angle);
        }
        Text Label(string name, Transform parent, string value, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var rect = Rect(name, parent, position, size); var text = rect.gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = fontSize; text.color = color;
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow; return text;
        }
        Button MakeButton(string name, Transform parent, string caption, Vector2 position, Vector2 size, Action action)
        {
            var rect = Rect(name, parent, position, size); var image = rect.gameObject.AddComponent<Image>();
            image.sprite = rounded; image.color = new Color(.95f, .76f, .32f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.onClick.AddListener(() => action());
            Label("Caption", rect, caption, Vector2.zero, size - new Vector2(15, 6), 23, new Color(.17f, .24f, .14f));
            return button;
        }
    }
}
