using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatGame.EditorTools
{
    public static class TitleScreenBuilder
    {
        const string MaterialFolder = "Assets/_Project/Art/Materials";

        [MenuItem("Cat Game/Add Title Screen To Open Scene")]
        public static void AddToOpenScene()
        {
            GameObject cat = GameObject.Find("Cat");
            Camera cam = Camera.main;
            if (cat == null || cam == null)
                throw new System.InvalidOperationException("Open the Playground scene first: it needs a 'Cat' and a main camera.");

            foreach (string existing in new[] { "Title UI", "Game Flow" })
            {
                GameObject old = GameObject.Find(existing);
                if (old != null)
                    Object.DestroyImmediate(old);
            }

            Build(cat, cam);
            EditorSceneManager.MarkSceneDirty(cat.scene);
            EditorSceneManager.SaveScene(cat.scene);
        }

        public static void Build(GameObject cat, Camera cam)
        {
            if (TMP_Settings.defaultFontAsset == null)
                throw new System.InvalidOperationException("TextMeshPro essentials are missing; import them first.");

            var input = cat.GetComponent<CatInput>();
            var rig = cam.GetComponent<ThirdPersonCamera>();
            input.enabled = false;
            rig.enabled = false;

            var orbit = cam.GetComponent<TitleOrbit>();
            if (orbit == null)
                orbit = cam.gameObject.AddComponent<TitleOrbit>();
            PlaygroundBuilder.SetReference(orbit, "focus", cat.transform);

            var canvasGo = new GameObject("Title UI", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var titleScreen = CreateStretched("TitleScreen", canvasGo.transform);
            var titleGroup = titleScreen.AddComponent<CanvasGroup>();
            titleGroup.blocksRaycasts = false;
            titleGroup.interactable = false;

            var gameTitle = CreateText("GameTitle", titleScreen.transform, "CLAWS OUT", 180f, Palette.Accent,
                TextMaterial("TitleText", outlined: true), anchorY: 0.6f, height: 260f);
            gameTitle.fontStyle = FontStyles.Bold;
            gameTitle.characterSpacing = 4f;

            var scrim = new GameObject("PromptScrim", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            scrim.transform.SetParent(titleScreen.transform, false);
            var scrimRect = (RectTransform)scrim.transform;
            scrimRect.anchorMin = scrimRect.anchorMax = new Vector2(0.5f, 0.25f);
            scrimRect.sizeDelta = new Vector2(600f, 76f);
            var scrimImage = scrim.GetComponent<UnityEngine.UI.Image>();
            scrimImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            scrimImage.type = UnityEngine.UI.Image.Type.Sliced;
            scrimImage.pixelsPerUnitMultiplier = 0.25f;
            Color scrimColor = Palette.Shadow;
            scrimColor.a = 0.6f;
            scrimImage.color = scrimColor;
            scrimImage.raycastTarget = false;

            Color promptColor = Palette.CatSecondary;
            promptColor.a = 0.7f;
            var prompt = CreateText("StartPrompt", titleScreen.transform, "PRESS ANY KEY", 40f, promptColor,
                TextMaterial("PromptText", outlined: false), anchorY: 0.25f, height: 80f);
            prompt.characterSpacing = 25f;
            prompt.gameObject.AddComponent<CanvasGroup>();
            prompt.gameObject.AddComponent<PromptPulse>();

            var fadeGo = CreateStretched("ScreenFade", canvasGo.transform);
            var fadeImage = fadeGo.AddComponent<UnityEngine.UI.Image>();
            fadeImage.color = Palette.Shadow;
            fadeImage.raycastTarget = false;
            var fadeGroup = fadeGo.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
            fadeGroup.interactable = false;
            var fader = fadeGo.AddComponent<ScreenFader>();

            var flow = new GameObject("Game Flow").AddComponent<GameFlow>();
            PlaygroundBuilder.SetReference(flow, "playerInput", input);
            PlaygroundBuilder.SetReference(flow, "gameplayCamera", rig);
            PlaygroundBuilder.SetReference(flow, "titleCamera", orbit);
            PlaygroundBuilder.SetReference(flow, "titleScreen", titleGroup);
            PlaygroundBuilder.SetReference(flow, "fader", fader);
        }

        static GameObject CreateStretched(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color,
            Material material, float anchorY, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, anchorY);
            rt.anchorMax = new Vector2(1f, anchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = Vector2.zero;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.raycastTarget = false;
            tmp.fontSharedMaterial = material;
            return tmp;
        }

        static Material TextMaterial(string name, bool outlined)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(TMP_Settings.defaultFontAsset.material) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            Color underlay = Palette.Shadow;
            underlay.a = 0.85f;
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor("_UnderlayColor", underlay);
            material.SetFloat("_UnderlayOffsetX", 0.6f);
            material.SetFloat("_UnderlayOffsetY", -0.6f);
            material.SetFloat("_UnderlaySoftness", outlined ? 0.15f : 0.5f);

            material.EnableKeyword("OUTLINE_ON");
            material.SetColor("_OutlineColor", Palette.Shadow);
            material.SetFloat("_OutlineWidth", outlined ? 0.22f : 0.12f);
            material.SetFloat("_FaceDilate", outlined ? 0.15f : 0f);

            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
