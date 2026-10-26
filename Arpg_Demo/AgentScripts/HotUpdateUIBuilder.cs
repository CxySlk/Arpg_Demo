using System;
using TMPro;
using UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AgentTools
{
    public static class HotUpdateUIBuilder
    {
        private const string ConfirmPath =
            "Assets/Prefabs/UI/HotUpdateScene/UI_UpdateConfirmWindow.prefab";
        private const string ProgressPath =
            "Assets/Prefabs/UI/HotUpdateScene/UI_HotUpdateWindow.prefab";
        private const string FontPath = "Assets/Res/Fonts/MSYH SDF.asset";
        private const string BackgroundGuid = "dc1e96a83a184774baac9618397f62cd";
        private const string ButtonGuid = "11c6374ed7e19f8459ea58fe4681d72b";

        private static readonly Color PanelColor = new Color(0.055f, 0.065f, 0.115f, 0.96f);
        private static readonly Color PanelBorderColor = new Color(0.38f, 0.31f, 0.95f, 0.7f);
        private static readonly Color PrimaryColor = new Color(0.46f, 0.36f, 1f, 1f);
        private static readonly Color AccentColor = new Color(1f, 0.58f, 0.12f, 1f);
        private static readonly Color TextColor = new Color(0.94f, 0.95f, 1f, 1f);
        private static readonly Color MutedColor = new Color(0.6f, 0.64f, 0.78f, 1f);
        private static readonly Color DangerColor = new Color(1f, 0.38f, 0.36f, 1f);
        private static readonly Color OverlayColor = new Color(0.01f, 0.015f, 0.04f, 0.78f);

        public static void Build()
        {
            Sprite background = LoadSpriteByGuid(BackgroundGuid);
            Sprite buttonSprite = LoadSpriteByGuid(ButtonGuid);
            Sprite rounded = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            if (font == null)
                throw new InvalidOperationException("UI font not found: " + FontPath);

            BuildConfirm(background, buttonSprite, rounded, font);
            BuildProgress(background, buttonSprite, rounded, font);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Hot update UI prefabs created.");
        }

        private static void BuildConfirm(Sprite background, Sprite buttonSprite, Sprite rounded, TMP_FontAsset font)
        {
            GameObject root = CreateWindowRoot("UI_UpdateConfirmWindow");
            AddFade(root, 0.28f);

            CreateImage("Background", root.transform, background, Color.white, true);
            CreateImage("Overlay", root.transform, null, OverlayColor, true);

            RectTransform card = CreatePanel(
                "ConfirmCard", root.transform, rounded, PanelColor, true);
            SetAnchored(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1000f, 590f));

            Image cardImage = card.GetComponent<Image>();
            Outline border = card.gameObject.AddComponent<Outline>();
            border.effectColor = PanelBorderColor;
            border.effectDistance = new Vector2(1.5f, -1.5f);

            CreateAccentLine(card, new Vector2(-460f, 235f), new Vector2(6f, 54f));
            CreateText("Title", card, font, "发现新版本", 48f, TextColor, TextAlignmentOptions.Left,
                new Vector2(-95f, 230f), new Vector2(650f, 60f), FontStyles.Bold);
            CreateText("Eyebrow", card, font, "RESOURCE UPDATE", 18f, AccentColor,
                TextAlignmentOptions.Left, new Vector2(-210f, 195f), new Vector2(420f, 28f),
                FontStyles.Bold);

            CreateText("Content", card, font, "检测到新的资源包，是否立即下载？", 30f, TextColor,
                TextAlignmentOptions.TopLeft, new Vector2(0f, 80f), new Vector2(880f, 130f),
                FontStyles.Normal);

            CreateText("Hint", card, font, "取消后将直接进入游戏，本次不下载更新内容。", 22f, MutedColor,
                TextAlignmentOptions.Left, new Vector2(0f, -10f), new Vector2(880f, 60f),
                FontStyles.Normal);

            Button confirmButton = CreateButton(
                "BtnConfirm", card, buttonSprite, font, "立即更新", PrimaryColor,
                new Vector2(-210f, -225f), new Vector2(360f, 74f), 32f);
            Button cancelButton = CreateButton(
                "BtnCancel", card, buttonSprite, font, "暂不更新", new Color(0.22f, 0.24f, 0.36f, 1f),
                new Vector2(210f, -225f), new Vector2(360f, 74f), 32f);

            Button closeButton = CreateButton(
                "BtnClose", card, null, font, "×", new Color(0f, 0f, 0f, 0f),
                new Vector2(445f, 245f), new Vector2(56f, 56f), 38f);
            SetButtonTextColor(closeButton, MutedColor);

            UI_UpdateConfirmWindow window = root.AddComponent<UI_UpdateConfirmWindow>();
            SerializedObject serialized = new SerializedObject(window);
            SetReference(serialized, "_title", card.Find("Title").GetComponent<TMP_Text>());
            SetReference(serialized, "_content", card.Find("Content").GetComponent<TMP_Text>());
            SetReference(serialized, "_btnConfirm", confirmButton);
            SetReference(serialized, "_btnCancel", cancelButton);
            SetReference(serialized, "_btnClose", closeButton);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, ConfirmPath);
        }

        private static void BuildProgress(Sprite background, Sprite buttonSprite, Sprite rounded, TMP_FontAsset font)
        {
            GameObject root = CreateWindowRoot("UI_HotUpdateWindow");
            AddFade(root, 0.28f);

            CreateImage("Background", root.transform, background, Color.white, true);
            CreateImage("Overlay", root.transform, null, OverlayColor, true);

            RectTransform brand = CreateRect("Brand", root.transform);
            SetAnchoredTopLeft(brand, new Vector2(80f, -55f), new Vector2(700f, 84f));
            CreateText("BrandTitle", brand, font, "ARPG  DEMO", 36f, TextColor,
                TextAlignmentOptions.Left, Vector2.zero, new Vector2(500f, 46f), FontStyles.Bold);
            CreateText("BrandSub", brand, font, "RESOURCE  UPDATE  SERVICE", 16f, AccentColor,
                TextAlignmentOptions.Left, new Vector2(0f, -39f), new Vector2(500f, 26f), FontStyles.Bold);

            CreateText("Version", root.transform, font, "v1.0", 20f, MutedColor,
                TextAlignmentOptions.Right, new Vector2(0f, 0f), new Vector2(300f, 30f), FontStyles.Normal,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-80f, -70f));

            RectTransform card = CreatePanel(
                "UpdateCard", root.transform, rounded, PanelColor, true);
            SetAnchored(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -20f), new Vector2(1080f, 460f));

            Outline border = card.gameObject.AddComponent<Outline>();
            border.effectColor = PanelBorderColor;
            border.effectDistance = new Vector2(1.5f, -1.5f);

            CreateAccentLine(card, new Vector2(-490f, 175f), new Vector2(6f, 54f));
            CreateText("Title", card, font, "正在更新资源", 46f, TextColor,
                TextAlignmentOptions.Left, new Vector2(-150f, 170f), new Vector2(600f, 58f), FontStyles.Bold);
            CreateText("Status", card, font, "准备更新资源", 24f, MutedColor,
                TextAlignmentOptions.Left, new Vector2(0f, 105f), new Vector2(900f, 36f), FontStyles.Normal);
            CreateText("Package", card, font, string.Empty, 20f, AccentColor,
                TextAlignmentOptions.Left, new Vector2(0f, 62f), new Vector2(900f, 30f), FontStyles.Bold);

            Image trackImage = CreateImage("ProgressTrack", card, rounded,
                new Color(0.12f, 0.14f, 0.22f, 1f), false);
            SetAnchored((RectTransform)trackImage.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(900f, 18f));

            Image glow = CreateImage("ProgressGlow", card, rounded,
                new Color(0.46f, 0.36f, 1f, 0.18f), false);
            SetAnchored((RectTransform)glow.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(920f, 34f));

            Image fill = CreateImage("ProgressFill", card, rounded, PrimaryColor, false);
            SetAnchored((RectTransform)fill.transform, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(900f, 18f));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;

            CreateText("Percent", card, font, "0%", 54f, TextColor,
                TextAlignmentOptions.Left, new Vector2(0f, -100f), new Vector2(220f, 70f), FontStyles.Bold);
            CreateText("Size", card, font, "0 B / 0 B", 22f, MutedColor,
                TextAlignmentOptions.Right, new Vector2(0f, -105f), new Vector2(620f, 36f), FontStyles.Normal,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(140f, 0f));

            RectTransform errorRoot = CreatePanel("ErrorRoot", card, rounded,
                new Color(0.28f, 0.06f, 0.07f, 0.7f), false);
            SetAnchored(errorRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 24f), new Vector2(900f, 68f));
            errorRoot.gameObject.SetActive(false);
            CreateText("ErrorText", errorRoot, font, "下载失败，请重试。", 22f, DangerColor,
                TextAlignmentOptions.Left, new Vector2(24f, 0f), new Vector2(560f, 46f), FontStyles.Normal,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f));
            Button retryButton = CreateButton("BtnRetry", errorRoot, buttonSprite, font, "重试",
                new Color(0.52f, 0.18f, 0.22f, 1f), new Vector2(350f, 0f), new Vector2(150f, 46f), 24f);

            CreateText("Footer", root.transform, font, "请保持网络连接，更新完成后将自动进入游戏", 20f, MutedColor,
                TextAlignmentOptions.Center, new Vector2(0f, 110f), new Vector2(1000f, 30f), FontStyles.Normal,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 110f));

            UI_HotUpdateWindow window = root.AddComponent<UI_HotUpdateWindow>();
            SerializedObject serialized = new SerializedObject(window);
            SetReference(serialized, "_progressFill", fill);
            SetReference(serialized, "_percentText", card.Find("Percent").GetComponent<TMP_Text>());
            SetReference(serialized, "_statusText", card.Find("Status").GetComponent<TMP_Text>());
            SetReference(serialized, "_packageText", card.Find("Package").GetComponent<TMP_Text>());
            SetReference(serialized, "_sizeText", card.Find("Size").GetComponent<TMP_Text>());
            SetReference(serialized, "_errorRoot", errorRoot.gameObject);
            SetReference(serialized, "_errorText", errorRoot.Find("ErrorText").GetComponent<TMP_Text>());
            SetReference(serialized, "_btnRetry", retryButton);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, ProgressPath);
        }

        private static GameObject CreateWindowRoot(string name)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            return root;
        }

        private static void AddFade(GameObject root, float duration)
        {
            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;

            UI_WindowFadeEffect fade = root.AddComponent<UI_WindowFadeEffect>();
            SerializedObject serialized = new SerializedObject(fade);
            SerializedProperty durationProperty = serialized.FindProperty("_duration");
            if (durationProperty != null)
                durationProperty.floatValue = duration;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return (RectTransform)gameObject.transform;
        }

        private static Image CreateImage(
            string name, Transform parent, Sprite sprite, Color color, bool raycastTarget)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycastTarget;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            return image;
        }

        private static RectTransform CreatePanel(
            string name, Transform parent, Sprite sprite, Color color, bool raycastTarget)
        {
            Image image = CreateImage(name, parent, sprite, color, raycastTarget);
            image.type = Image.Type.Sliced;
            return (RectTransform)image.transform;
        }

        private static void CreateAccentLine(Transform parent, Vector2 position, Vector2 size)
        {
            RectTransform line = CreateRect("Accent", parent);
            SetAnchored(line, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
            Image image = line.gameObject.AddComponent<Image>();
            image.color = AccentColor;
            image.raycastTarget = false;
        }

        private static TextMeshProUGUI CreateText(
            string name, Transform parent, TMP_FontAsset font, string text, float fontSize,
            Color color, TextAlignmentOptions alignment, Vector2 position, Vector2 size,
            FontStyles style, Vector2? anchorMin = null, Vector2? anchorMax = null,
            Vector2? anchoredPosition = null)
        {
            RectTransform rect = CreateRect(name, parent);
            SetAnchored(rect,
                anchorMin ?? new Vector2(0.5f, 0.5f),
                anchorMax ?? new Vector2(0.5f, 0.5f),
                anchoredPosition ?? position,
                size);

            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = style;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        private static Button CreateButton(
            string name, Transform parent, Sprite sprite, TMP_FontAsset font, string text,
            Color color, Vector2 position, Vector2 size, float fontSize)
        {
            RectTransform rect = CreateRect(name, parent);
            SetAnchored(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = true;
            image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;

            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            button.colors = colors;

            TextMeshProUGUI label = CreateText(
                "Text", rect, font, text, fontSize, Color.white, TextAlignmentOptions.Center,
                Vector2.zero, size, FontStyles.Bold,
                Vector2.zero, Vector2.one, Vector2.zero);
            label.raycastTarget = false;
            return button;
        }

        private static void SetButtonTextColor(Button button, Color color)
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.color = color;
        }

        private static void SetAnchored(
            RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void SetAnchoredTopLeft(
            RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(anchoredPosition.x, anchoredPosition.y);
            rect.sizeDelta = size;
        }

        private static Sprite LoadSpriteByGuid(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void SetReference(SerializedObject serialized, string field, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
                throw new InvalidOperationException("Serialized field not found: " + field);
            property.objectReferenceValue = value;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            if (prefab == null)
                throw new InvalidOperationException("Failed to save prefab: " + path);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
