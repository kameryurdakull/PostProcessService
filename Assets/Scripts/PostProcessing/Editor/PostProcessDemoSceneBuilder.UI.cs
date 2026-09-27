using Game.Core.PostProcessing.Demo;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.Core.PostProcessing.Editor
{
    public static partial class PostProcessDemoSceneBuilder
    {
        private static readonly Color PanelColor = new(0.035f, 0.065f, 0.105f, 0.96f);
        private static readonly Color CardColor = new(0.035f, 0.07f, 0.115f, 0.88f);
        private static readonly Color AccentColor = new(0.14f, 0.82f, 0.79f);
        private static readonly Color MutedTextColor = new(0.57f, 0.69f, 0.74f);
        private static readonly Color ButtonColor = new(0.10f, 0.15f, 0.22f, 0.95f);

        private static void CreateCanvas(
            PostProcessDemoController controller,
            Camera sceneCamera,
            PostProcessPreset[] presets)
        {
            var canvasObject = new GameObject("Volume Lab Canvas", typeof(RectTransform));
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var panel = CreatePanel("Control Panel", canvas.transform, PanelColor);
            SetRect(panel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(520f, 0f));

            var accent = CreatePanel("Top Accent", panel.transform, AccentColor);
            SetTopLeft(accent.rectTransform, 0f, 0f, 520f, 6f);

            CreateText("Brand", panel.transform, "POST / PROCESS", 36, FontStyle.Bold,
                Color.white, TextAnchor.MiddleLeft, 38f, 45f, 445f, 58f);
            CreateText("Edition", panel.transform, "VOLUME LAB     /     URP 17", 17, FontStyle.Bold,
                AccentColor, TextAnchor.MiddleLeft, 40f, 108f, 435f, 30f);
            var divider = CreatePanel("Divider", panel.transform, new Color(0.18f, 0.29f, 0.34f));
            SetTopLeft(divider.rectTransform, 40f, 157f, 440f, 2f);

            CreateText("Introduction", panel.transform,
                "A live study of cinematic image treatments.\nSelect several effects to build your own mix.",
                17, FontStyle.Normal, MutedTextColor, TextAnchor.UpperLeft,
                40f, 182f, 440f, 64f);

            var gridObject = new GameObject("Effect Buttons", typeof(RectTransform));
            gridObject.transform.SetParent(panel.transform, false);
            var gridRect = gridObject.GetComponent<RectTransform>();
            SetTopLeft(gridRect, 40f, 280f, 440f, 462f);
            var grid = gridObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(212f, 64f);
            grid.spacing = new Vector2(16f, 14f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperLeft;

            var entries = new PostProcessDemoEntry[Definitions.Length];
            for (var index = 0; index < Definitions.Length; index++)
            {
                var definition = Definitions[index];
                var button = CreateButton(definition.Label, gridObject.transform, ButtonColor);
                entries[index] = new PostProcessDemoEntry(
                    presets[index], button, definition.Label, definition.Description, definition.AnimateCamera);
            }

            var resetButton = CreateButton("RESET  /  BASELINE", panel.transform,
                new Color(0.53f, 0.17f, 0.15f, 1f));
            SetTopLeft(resetButton.GetComponent<RectTransform>(), 40f, 814f, 440f, 70f);

            CreateText("Footer", panel.transform,
                "MULTI-SELECT / CLICK AGAIN TO DISABLE\nBUILT WITH URP VOLUMES + PROBUILDER",
                14, FontStyle.Normal, MutedTextColor, TextAnchor.UpperLeft,
                40f, 927f, 440f, 70f);

            var statusCard = CreatePanel("Status Card", canvas.transform, CardColor);
            SetRect(statusCard.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-48f, -48f), new Vector2(355f, 190f));
            CreateText("Status Heading", statusCard.transform, "NOW SHOWING", 15, FontStyle.Bold,
                AccentColor, TextAnchor.MiddleLeft, 25f, 18f, 305f, 29f);
            var statusText = CreateText("Status", statusCard.transform, "BASELINE", 26,
                FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, 25f, 53f, 305f, 44f);
            var descriptionText = CreateText("Description", statusCard.transform,
                "Select effects to build a live mix.", 16,
                FontStyle.Normal, MutedTextColor, TextAnchor.UpperLeft,
                25f, 110f, 305f, 65f);

            var noteCard = CreatePanel("Instructions Card", canvas.transform, CardColor);
            SetRect(noteCard.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(-48f, 48f), new Vector2(355f, 145f));
            CreateText("Instructions Heading", noteCard.transform, "INTERACTIVE TEST", 15,
                FontStyle.Bold, AccentColor, TextAnchor.MiddleLeft,
                25f, 17f, 305f, 28f);
            CreateText("Instructions", noteCard.transform,
                "Combine any of 12 looks\nToggle each effect independently\nMotion blur includes camera movement",
                16, FontStyle.Normal, MutedTextColor, TextAnchor.UpperLeft,
                25f, 52f, 305f, 80f);

            var eventSystem = new GameObject("Event System", typeof(UnityEngine.EventSystems.EventSystem));
            eventSystem.AddComponent<InputSystemUIInputModule>();
            controller.Configure(entries, resetButton, statusText, descriptionText, sceneCamera);
        }

        private static Image CreatePanel(string name, Transform parent, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var image = panel.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Button CreateButton(string label, Transform parent, Color color)
        {
            var buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.GetComponent<Image>();
            image.color = Color.white;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.colors = new ColorBlock
            {
                normalColor = color,
                highlightedColor = new Color(0.15f, 0.43f, 0.47f, 1f),
                pressedColor = new Color(0.10f, 0.57f, 0.57f, 1f),
                selectedColor = color,
                disabledColor = new Color(0.06f, 0.09f, 0.12f, 0.5f),
                colorMultiplier = 1f,
                fadeDuration = 0.12f
            };

            var text = CreateText("Label", buttonObject.transform, label, 16,
                FontStyle.Bold, Color.white, TextAnchor.MiddleCenter,
                0f, 0f, 212f, 64f);
            SetRect(text.rectTransform, Vector2.zero, Vector2.one,
                new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            return button;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            string content,
            int fontSize,
            FontStyle style,
            Color color,
            TextAnchor alignment,
            float left,
            float top,
            float width,
            float height)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var label = textObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.text = content;
            label.raycastTarget = false;
            SetTopLeft(label.rectTransform, left, top, width, height);
            return label;
        }

        private static void SetTopLeft(
            RectTransform rect,
            float left,
            float top,
            float width,
            float height)
        {
            SetRect(rect, Vector2.up, Vector2.up, Vector2.up,
                new Vector2(left, -top), new Vector2(width, height));
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
