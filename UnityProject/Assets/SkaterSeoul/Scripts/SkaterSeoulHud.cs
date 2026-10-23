using UnityEngine;
using UnityEngine.UI;

namespace SkaterSeoul
{
    public sealed class SkaterSeoulHud : MonoBehaviour
    {
        private static Sprite squareSprite;
        private static Sprite roundedSprite;
        private static Sprite discSprite;
        private Canvas canvas;
        private RectTransform safeRoot;
        private Text locationText;
        private Text battleTitle;
        private Text playerLettersText;
        private Text rivalLettersText;
        private Text turnText;
        private Text challengeText;
        private Text feedbackText;
        private Image feedbackPlate;
        private Text skillText;
        private Image skillFill;
        private Image pushImage;
        private Text pushText;
        private Image gestureRing;
        private Image gestureVector;
        private Text gestureLabel;
        private Image pauseShade;
        private GameObject pauseCard;
        private GameObject playerPanel;
        private GameObject rivalPanel;
        private GameObject battleCard;
        private Text pauseTitle;
        private Text pauseMode;
        private bool isPractice;
        private string feedback = "";
        private string trick = "OLLIE";
        private int skill;
        private bool pushing;
        private bool showingGesture;
        private bool built;
        private float feedbackChangedAt;

        private void Awake() { EnsureBuilt(); }
        private void Start() { EnsureBuilt(); }

        private void EnsureBuilt()
        {
            if (!built) Build();
        }

        private void Build()
        {
            if (built) return;
            EnsureSprites();
            GameObject canvasObject = new GameObject("Gameplay Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            safeRoot = CreateRect("Safe Area", canvas.transform, Vector2.zero, Vector2.one);

            Image topChip = CreateImage("Context Chip", safeRoot, new Vector2(0.02f, 0.91f),
                new Vector2(0.25f, 0.985f), new Color(0.055f, 0.095f, 0.105f, 0.72f));
            RoundImage(topChip);
            locationText = CreateText("Context", topChip.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(22f, 0f), 24, TextAnchor.MiddleLeft, new Color(0.95f, 0.93f, 0.86f));
            locationText.text = "HANGANG  /  PRACTICE";

            locationText.text = "HANGANG  /  PRACTICE";
            locationText.fontSize = 18;

            battleTitle = CreateText("Battle Title", safeRoot, new Vector2(0.40f, 0.94f),
                new Vector2(0.60f, 0.99f), Vector2.zero, 18, TextAnchor.MiddleCenter,
                new Color(0.97f, 0.95f, 0.88f));
            battleTitle.text = "S.K.A.T.E. BATTLE";

            Image playerBackground = CreateImage("Player Match Panel", safeRoot, new Vector2(0.025f, 0.89f),
                new Vector2(0.245f, 0.985f), new Color(0.055f, 0.095f, 0.105f, 0.78f));
            RoundImage(playerBackground);
            playerPanel = playerBackground.gameObject;
            CreateText("Player Name", playerBackground.rectTransform, new Vector2(0.07f, 0.53f),
                new Vector2(0.95f, 0.94f), Vector2.zero, 18, TextAnchor.MiddleLeft,
                new Color(0.97f, 0.95f, 0.88f)).text = "JUNSIK";
            playerLettersText = CreateText("Player Letters", playerBackground.rectTransform, new Vector2(0.07f, 0.06f),
                new Vector2(0.95f, 0.56f), Vector2.zero, 19, TextAnchor.MiddleLeft,
                new Color(0.96f, 0.83f, 0.30f));
            playerLettersText.fontSize = 27;
            playerLettersText.fontStyle = FontStyle.Bold;
            playerLettersText.text = "_ _ _ _ _";
            Image playerAccent = CreateImage("Player Accent", playerBackground.rectTransform,
                new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.055f), new Color(0.31f, 0.70f, 0.65f));
            playerAccent.raycastTarget = false;

            Image rivalBackground = CreateImage("Rival Match Panel", safeRoot, new Vector2(0.755f, 0.89f),
                new Vector2(0.975f, 0.985f), new Color(0.055f, 0.095f, 0.105f, 0.78f));
            RoundImage(rivalBackground);
            rivalPanel = rivalBackground.gameObject;
            CreateText("Rival Name", rivalBackground.rectTransform, new Vector2(0.05f, 0.53f),
                new Vector2(0.93f, 0.94f), Vector2.zero, 18, TextAnchor.MiddleRight,
                new Color(0.97f, 0.95f, 0.88f)).text = "RIVAL";
            rivalLettersText = CreateText("Rival Letters", rivalBackground.rectTransform, new Vector2(0.05f, 0.06f),
                new Vector2(0.93f, 0.56f), Vector2.zero, 19, TextAnchor.MiddleRight,
                new Color(0.96f, 0.83f, 0.30f));
            rivalLettersText.fontSize = 27;
            rivalLettersText.fontStyle = FontStyle.Bold;
            rivalLettersText.text = "_ _ _ _ _";
            Image rivalAccent = CreateImage("Rival Accent", rivalBackground.rectTransform,
                new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.055f), new Color(0.83f, 0.37f, 0.29f));
            rivalAccent.raycastTarget = false;

            Image challenge = CreateImage("Battle Challenge Card", safeRoot, new Vector2(0.35f, 0.79f),
                new Vector2(0.65f, 0.895f), new Color(0.055f, 0.095f, 0.105f, 0.80f));
            RoundImage(challenge);
            battleCard = challenge.gameObject;
            turnText = CreateText("Battle Turn", challenge.rectTransform, new Vector2(0.06f, 0.63f),
                new Vector2(0.94f, 0.94f), Vector2.zero, 16, TextAnchor.MiddleCenter,
                new Color(0.72f, 0.87f, 0.84f));
            challengeText = CreateText("Battle Challenge", challenge.rectTransform, new Vector2(0.04f, 0.07f),
                new Vector2(0.96f, 0.69f), Vector2.zero, 27, TextAnchor.MiddleCenter,
                new Color(0.98f, 0.95f, 0.87f));
            challengeText.fontStyle = FontStyle.Bold;

            Image pause = CreateImage("Pause Button", safeRoot, new Vector2(0.94f, 0.82f),
                new Vector2(0.99f, 0.88f), new Color(0.055f, 0.095f, 0.105f, 0.75f));
            RoundImage(pause);
            CreateText("Pause Icon", pause.rectTransform, Vector2.zero, Vector2.one, Vector2.zero,
                26, TextAnchor.MiddleCenter, Color.white).text = "Ⅱ";

            feedbackPlate = CreateImage("Trick Feedback Chip", safeRoot, new Vector2(0.39f, 0.045f),
                new Vector2(0.61f, 0.11f), new Color(0.055f, 0.095f, 0.105f, 0.58f));
            RoundImage(feedbackPlate);
            feedbackText = CreateText("Trick Feedback", feedbackPlate.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(12f, 0f), 22, TextAnchor.MiddleCenter, new Color(0.98f, 0.95f, 0.87f));
            feedbackPlate.gameObject.SetActive(false);

            skillText = CreateText("Practice Skill", safeRoot, new Vector2(0.33f, 0.92f),
                new Vector2(0.67f, 0.955f), Vector2.zero, 17, TextAnchor.MiddleCenter,
                new Color(0.92f, 0.89f, 0.80f, 0.9f));
            Image skillBack = CreateImage("Skill Track", safeRoot, new Vector2(0.44f, 0.91f),
                new Vector2(0.56f, 0.918f), new Color(0.08f, 0.12f, 0.13f, 0.55f));
            RoundImage(skillBack);
            skillFill = CreateImage("Skill Fill", skillBack.rectTransform, Vector2.zero,
                new Vector2(0.7f, 1f), new Color(0.31f, 0.70f, 0.65f, 0.92f));
            skillFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            skillFill.rectTransform.anchorMin = new Vector2(0f, 0f);

            gestureRing = CreateImage("Gesture Preview", safeRoot, new Vector2(0.17f, 0.15f),
                new Vector2(0.17f, 0.15f), new Color(0.79f, 0.91f, 0.87f, 0.07f));
            gestureRing.rectTransform.sizeDelta = new Vector2(200f, 200f);
            gestureRing.sprite = discSprite;
            gestureRing.raycastTarget = false;
            gestureVector = CreateImage("Gesture Direction", gestureRing.rectTransform,
                new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Color(.98f, .83f, .30f, .94f));
            gestureVector.rectTransform.pivot = new Vector2(0f, .5f);
            gestureVector.rectTransform.sizeDelta = new Vector2(0f, 8f);
            gestureVector.raycastTarget = false;
            gestureVector.gameObject.SetActive(false);
            gestureLabel = CreateText("Gesture Label", gestureRing.rectTransform, Vector2.zero, Vector2.one,
                Vector2.zero, 17, TextAnchor.MiddleCenter, new Color(0.95f, 0.96f, 0.91f, 0.82f));
            gestureLabel.text = "SWIPE";

            pushImage = CreateImage("Push Button", safeRoot, new Vector2(0.895f, 0.13f),
                new Vector2(0.895f, 0.13f), new Color(0.055f, 0.095f, 0.105f, 0.48f));
            pushImage.rectTransform.sizeDelta = new Vector2(140f, 140f);
            pushImage.sprite = discSprite;
            pushText = CreateText("Push Label", pushImage.rectTransform, Vector2.zero, Vector2.one,
                Vector2.zero, 19, TextAnchor.MiddleCenter, new Color(0.98f, 0.95f, 0.87f));
            pushText.text = "PUSH";

            pauseShade = CreateImage("Pause Shade", safeRoot, Vector2.zero, Vector2.one,
                new Color(0.018f, 0.028f, 0.03f, 0.65f));
            pauseShade.gameObject.SetActive(false);
            pauseCard = new GameObject("Pause Panel", typeof(RectTransform), typeof(Image));
            pauseCard.transform.SetParent(safeRoot, false);
            RectTransform cardRect = pauseCard.GetComponent<RectTransform>();
            cardRect.anchorMin = cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(560f, 410f);
            pauseCard.GetComponent<Image>().color = new Color(0.10f, 0.145f, 0.15f, 0.97f);
            Image cardImage = pauseCard.GetComponent<Image>();
            RoundImage(cardImage);
            pauseTitle = CreateText("Pause Title", cardRect, new Vector2(0.08f, 0.70f),
                new Vector2(0.92f, 0.92f), Vector2.zero, 38, TextAnchor.MiddleCenter,
                new Color(0.97f, 0.93f, 0.84f));
            pauseTitle.text = "PAUSED";
            CreatePanelButton(cardRect, "Resume", new Vector2(0.14f, 0.49f), new Vector2(0.86f, 0.65f));
            pauseMode = CreatePanelButton(cardRect, "Mode", new Vector2(0.14f, 0.29f), new Vector2(0.86f, 0.45f));
            pauseMode.text = "SWITCH TO BATTLE";
            CreatePanelButton(cardRect, "Reset", new Vector2(0.14f, 0.09f), new Vector2(0.86f, 0.25f)).text = "RESET MATCH";
            pauseCard.SetActive(false);
            ApplySafeArea();
            built = true;
        }

        private Text CreatePanelButton(RectTransform parent, string name, Vector2 min, Vector2 max)
        {
            Image image = CreateImage(name + " Button", parent, min, max,
                new Color(0.15f, 0.24f, 0.24f, 1f));
            RoundImage(image);
            return CreateText(name + " Label", image.rectTransform, Vector2.zero, Vector2.one,
                Vector2.zero, 22, TextAnchor.MiddleCenter, Color.white);
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            GameObject g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            RectTransform r = g.GetComponent<RectTransform>();
            r.anchorMin = min;
            r.anchorMax = max;
            r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }

        private static Image CreateImage(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            GameObject g = new GameObject(name, typeof(RectTransform), typeof(Image));
            g.transform.SetParent(parent, false);
            RectTransform r = g.GetComponent<RectTransform>();
            r.anchorMin = min;
            r.anchorMax = max;
            r.offsetMin = r.offsetMax = Vector2.zero;
            Image image = g.GetComponent<Image>();
            image.sprite = squareSprite;
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, Transform parent, Vector2 min, Vector2 max,
            Vector2 inset, int size, TextAnchor anchor, Color color)
        {
            GameObject g = new GameObject(name, typeof(RectTransform), typeof(Text));
            g.transform.SetParent(parent, false);
            RectTransform r = g.GetComponent<RectTransform>();
            r.anchorMin = min;
            r.anchorMax = max;
            r.offsetMin = -inset;
            r.offsetMax = inset;
            Text text = g.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (text.font == null) text.font = Font.CreateDynamicFontFromOSFont("Arial", size);
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static void RoundImage(Image image)
        {
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
        }

        private static void EnsureSprites()
        {
            if (squareSprite != null) return;
            Texture2D square = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            square.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            square.Apply();
            squareSprite = Sprite.Create(square, new Rect(0f, 0f, 2f, 2f), new Vector2(.5f, .5f), 100f);

            Texture2D rounded = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
            Texture2D disc = new Texture2D(64, 64, TextureFormat.RGBA32, false, true);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float cornerX = Mathf.Max(0f, 8f - Mathf.Min(x, 63 - x));
                    float cornerY = Mathf.Max(0f, 8f - Mathf.Min(y, 63 - y));
                    float cornerDistance = Mathf.Sqrt(cornerX * cornerX + cornerY * cornerY);
                    float roundedAlpha = Mathf.Clamp01(8.5f - cornerDistance);
                    float radius = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                    float discAlpha = Mathf.Clamp01(32f - radius);
                    rounded.SetPixel(x, y, new Color(1f, 1f, 1f, roundedAlpha));
                    disc.SetPixel(x, y, new Color(1f, 1f, 1f, discAlpha));
                }
            rounded.Apply(); disc.Apply();
            roundedSprite = Sprite.Create(rounded, new Rect(0f, 0f, 64f, 64f), new Vector2(.5f, .5f), 100f,
                0u, SpriteMeshType.FullRect, new Vector4(16f, 16f, 16f, 16f));
            discSprite = Sprite.Create(disc, new Rect(0f, 0f, 64f, 64f), new Vector2(.5f, .5f), 100f);
        }

        private void ApplySafeArea()
        {
            if (safeRoot == null || Screen.width == 0 || Screen.height == 0) return;
            Rect safe = Screen.safeArea;
            safeRoot.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        }

        private void Update() { ApplySafeArea(); }

        public void SetState(bool practice, string playerLetters, string rivalLetters, string turn,
            string challenge, string latestFeedback, string currentTrick, int currentMastery,
            bool isPushing, bool showGesture, Vector2 gestureDelta, bool isPaused)
        {
            EnsureBuilt();
            isPractice = practice;
            string nextFeedback = latestFeedback ?? "";
            if (feedback != nextFeedback)
            {
                feedback = nextFeedback;
                feedbackChangedAt = Time.unscaledTime;
            }
            trick = currentTrick ?? "OLLIE";
            skill = Mathf.Clamp(currentMastery, 0, 100);
            pushing = isPushing;
            showingGesture = showGesture;
            locationText.text = "HANGANG  /  PRACTICE";
            locationText.gameObject.SetActive(practice);
            battleTitle.gameObject.SetActive(!practice);
            playerPanel.SetActive(!practice);
            rivalPanel.SetActive(!practice);
            battleCard.SetActive(!practice);
            turnText.text = turn ?? "";
            challengeText.text = challenge ?? "";
            playerLettersText.text = FormatLetters(playerLetters);
            rivalLettersText.text = FormatLetters(rivalLetters);
            float feedbackAge = Time.unscaledTime - feedbackChangedAt;
            bool showFeedback = feedback.Length > 0 && feedback != "SWIPE TO TRICK" && feedbackAge < 2.2f;
            feedbackPlate.gameObject.SetActive(showFeedback);
            if (showFeedback)
            {
                float alpha = Mathf.Clamp01((2.2f - feedbackAge) / .35f);
                feedbackPlate.color = new Color(.055f, .095f, .105f, .58f * alpha);
                feedbackText.color = new Color(.98f, .95f, .87f, alpha);
                feedbackText.text = feedback;
            }
            skillText.text = practice ? trick + "  ·  MASTERY " + skill + "%" : "";
            skillText.gameObject.SetActive(practice);
            skillFill.transform.parent.gameObject.SetActive(practice);
            skillFill.rectTransform.anchorMax = new Vector2(skill / 100f, 1f);
            pushImage.color = pushing ? new Color(0.12f, 0.42f, 0.40f, 0.86f) : new Color(0.055f, 0.095f, 0.105f, 0.48f);
            pushText.text = "PUSH";
            gestureRing.color = showingGesture ? new Color(0.31f, 0.70f, 0.65f, 0.28f) : new Color(0.79f, 0.91f, 0.87f, 0.07f);
            gestureLabel.text = showingGesture ? "TRICK" : "SWIPE";
            float vectorLength = Mathf.Clamp01(gestureDelta.magnitude / Mathf.Max(1f, Screen.safeArea.height * .18f)) * 90f;
            gestureVector.rectTransform.sizeDelta = new Vector2(vectorLength, 8f);
            gestureVector.rectTransform.localEulerAngles = new Vector3(0f, 0f,
                Mathf.Atan2(gestureDelta.y, gestureDelta.x) * Mathf.Rad2Deg);
            gestureVector.gameObject.SetActive(showingGesture && vectorLength >= 8f);
            pauseShade.gameObject.SetActive(isPaused);
            pauseCard.SetActive(isPaused);
            pauseMode.text = isPractice ? "SWITCH TO BATTLE" : "SWITCH TO PRACTICE";
        }

        private static string FormatLetters(string value)
        {
            if (string.IsNullOrEmpty(value)) return "_ _ _ _ _";
            int lost = 0;
            for (int i = 0; i < value.Length; i++) if (value[i] == '×') lost++;
            int count = Mathf.Min(value.Length, 5);
            System.Text.StringBuilder result = new System.Text.StringBuilder(count * 30);
            const string letters = "SKATE";
            for (int i = 0; i < count; i++)
            {
                if (result.Length > 0) result.Append(' ');
                if (i < lost) result.Append("<color=#F47E72>×</color>");
                else result.Append("<color=#F2D26B>").Append(letters[i]).Append("</color>");
            }
            return result.ToString();
        }
    }
}
