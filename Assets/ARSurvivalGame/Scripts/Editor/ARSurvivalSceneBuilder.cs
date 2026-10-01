using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using ARSurvivalGame.Core;
using ARSurvivalGame.Input;
using ARSurvivalGame.UI;
using ARSurvivalGame.VFX;
using ARSurvivalGame.Enemies;

namespace ARSurvivalGame.Editor
{
    [InitializeOnLoad]
    public static class ARSurvivalSceneBuilder
    {
        private const string TexturePath = "Assets/ARSurvivalGame/Materials/";
        private const string ScenePath = "Assets/Scenes/ARSurvivalScene.unity";
        private const string BaseScenePath = "Assets/Scenes/SampleScene.unity";

        static ARSurvivalSceneBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath))
                {
                    BuildScene();
                }
            };
        }

        [MenuItem("AR Survival/1. Generate UI Sprites", false, 1)]
        public static void GenerateSprites()
        {
            if (!Directory.Exists(TexturePath))
            {
                Directory.CreateDirectory(TexturePath);
            }

            CreateCircleTexture("JoystickBg.png", 256, new Color(0.1f, 0.15f, 0.25f, 0.45f), new Color(0f, 0.8f, 1f, 0.85f), 10);
            CreateCircleTexture("JoystickHandle.png", 128, new Color(0f, 0.85f, 1f, 0.9f), Color.white, 4);
            CreateCircleTexture("FireButton.png", 160, new Color(1f, 0.25f, 0.1f, 0.85f), new Color(1f, 0.7f, 0.3f, 1f), 8);
            CreateVignetteTexture("DamageVignette.png", 512, new Color(0.9f, 0.05f, 0.05f, 0.75f));
            CreateRoundedBarTexture("BarBg.png", 256, 32, new Color(0.08f, 0.1f, 0.14f, 0.75f));
            CreateRoundedBarTexture("BarFill.png", 256, 32, new Color(0.1f, 0.95f, 0.4f, 1f));

            AssetDatabase.Refresh();
            Debug.Log("[AR Survival] Texturas y sprites UI generados con éxito en " + TexturePath);
        }

        private static void CreateCircleTexture(string fileName, int size, Color fillColor, Color rimColor, int rimWidth)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.48f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    if (dist > radius)
                    {
                        float alpha = Mathf.Clamp01(1f - (dist - radius) * 1.5f);
                        tex.SetPixel(x, y, new Color(rimColor.r, rimColor.g, rimColor.b, rimColor.a * alpha));
                    }
                    else if (dist > radius - rimWidth)
                    {
                        float t = (dist - (radius - rimWidth)) / rimWidth;
                        tex.SetPixel(x, y, Color.Lerp(fillColor, rimColor, t));
                    }
                    else
                    {
                        float innerGlow = 1f - (dist / radius) * 0.3f;
                        tex.SetPixel(x, y, fillColor * innerGlow);
                    }
                }
            }

            tex.Apply();
            SaveTextureAndSetSprite(tex, TexturePath + fileName);
        }

        private static void CreateVignetteTexture(string fileName, int size, Color vignetteColor)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxDist = size * 0.5f * 1.414f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float normalized = dist / maxDist;
                    float alpha = Mathf.Pow(Mathf.Clamp01((normalized - 0.25f) / 0.75f), 1.8f);
                    tex.SetPixel(x, y, new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, vignetteColor.a * alpha));
                }
            }

            tex.Apply();
            SaveTextureAndSetSprite(tex, TexturePath + fileName);
        }

        private static void CreateRoundedBarTexture(string fileName, int width, int height, Color color)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, color);
                }
            }

            tex.Apply();
            SaveTextureAndSetSprite(tex, TexturePath + fileName);
        }

        private static void SaveTextureAndSetSprite(Texture2D tex, string path)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        [MenuItem("AR Survival/2. Setup AR Survival Scene", false, 2)]
        public static void BuildScene()
        {
            GenerateSprites();

            // Abrir o crear escena basada en SampleScene
            if (!File.Exists(ScenePath))
            {
                if (File.Exists(BaseScenePath))
                {
                    File.Copy(BaseScenePath, ScenePath, true);
                    AssetDatabase.Refresh();
                }
            }

            var scene = EditorSceneManager.OpenScene(File.Exists(ScenePath) ? ScenePath : BaseScenePath);

            // Desactivar UI de la plantilla para evitar superposiciones
            GameObject templateUI = GameObject.Find("UI");
            if (templateUI != null)
            {
                templateUI.SetActive(false);
            }

            // Crear GameController
            GameObject controllerObj = GameObject.Find("GameController");
            if (controllerObj == null)
            {
                controllerObj = new GameObject("GameController");
            }

            var gameManager = controllerObj.GetComponent<GameManager>() ?? controllerObj.AddComponent<GameManager>();
            var placement = controllerObj.GetComponent<ARPlacementController>() ?? controllerObj.AddComponent<ARPlacementController>();
            var spawner = controllerObj.GetComponent<EnemySpawner>() ?? controllerObj.AddComponent<EnemySpawner>();
            var vfx = controllerObj.GetComponent<VFXManager>() ?? controllerObj.AddComponent<VFXManager>();
            var uiMgr = controllerObj.GetComponent<UIManager>() ?? controllerObj.AddComponent<UIManager>();

            // Crear Canvas UI
            GameObject canvasObj = GameObject.Find("ARSurvival_Canvas");
            if (canvasObj != null)
            {
                Object.DestroyImmediate(canvasObj);
            }

            canvasObj = new GameObject("ARSurvival_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Cargar sprites generados
            Sprite joystickBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath + "JoystickBg.png");
            Sprite joystickHandleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath + "JoystickHandle.png");
            Sprite fireBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath + "FireButton.png");
            Sprite vignetteSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath + "DamageVignette.png");
            Sprite barBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath + "BarBg.png");
            Sprite barFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath + "BarFill.png");

            // 1. Placement Panel
            GameObject placementPanel = CreateUIElement("PlacementPanel", canvasObj.transform);
            SetStretch(placementPanel.GetComponent<RectTransform>());

            GameObject bannerBg = CreateUIElement("BannerBg", placementPanel.transform);
            RectTransform bannerRect = bannerBg.GetComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.15f, 0.78f);
            bannerRect.anchorMax = new Vector2(0.85f, 0.92f);
            bannerRect.offsetMin = Vector2.zero;
            bannerRect.offsetMax = Vector2.zero;
            Image bannerImg = bannerBg.AddComponent<Image>();
            bannerImg.color = new Color(0.05f, 0.1f, 0.18f, 0.85f);

            GameObject bannerTextObj = CreateUIElement("Text", bannerBg.transform);
            SetStretch(bannerTextObj.GetComponent<RectTransform>());
            TextMeshProUGUI bannerText = bannerTextObj.AddComponent<TextMeshProUGUI>();
            bannerText.text = "<b>ESCANEANDO SUPERFICIE AR</b>\n<size=22><color=#00E5FF>Apunta al suelo o mesa y toca la pantalla para iniciar</color></size>";
            bannerText.alignment = TextAlignmentOptions.Center;
            bannerText.fontSize = 28;
            bannerText.color = Color.white;

            // 2. HUD Panel
            GameObject hudPanel = CreateUIElement("HUDPanel", canvasObj.transform);
            SetStretch(hudPanel.GetComponent<RectTransform>());

            // Top Bar
            GameObject topBar = CreateUIElement("TopBar", hudPanel.transform);
            RectTransform topBarRect = topBar.GetComponent<RectTransform>();
            topBarRect.anchorMin = new Vector2(0f, 0.88f);
            topBarRect.anchorMax = new Vector2(1f, 1f);
            topBarRect.offsetMin = new Vector2(40f, 0f);
            topBarRect.offsetMax = new Vector2(-40f, -20f);

            // Health Bar Background
            GameObject hpBg = CreateUIElement("HealthBarBg", topBar.transform);
            RectTransform hpBgRect = hpBg.GetComponent<RectTransform>();
            hpBgRect.anchorMin = new Vector2(0f, 0.3f);
            hpBgRect.anchorMax = new Vector2(0.35f, 0.85f);
            hpBgRect.offsetMin = Vector2.zero;
            hpBgRect.offsetMax = Vector2.zero;
            Image hpBgImg = hpBg.AddComponent<Image>();
            if (barBgSprite != null) hpBgImg.sprite = barBgSprite;
            hpBgImg.color = new Color(0.12f, 0.15f, 0.2f, 0.85f);

            // Health Bar Fill
            GameObject hpFill = CreateUIElement("HealthBarFill", hpBg.transform);
            SetStretch(hpFill.GetComponent<RectTransform>());
            Image hpFillImg = hpFill.AddComponent<Image>();
            if (barFillSprite != null) hpFillImg.sprite = barFillSprite;
            hpFillImg.type = Image.Type.Filled;
            hpFillImg.fillMethod = Image.FillMethod.Horizontal;
            hpFillImg.fillAmount = 1f;
            hpFillImg.color = new Color(0.1f, 0.95f, 0.4f, 1f);

            // Health Text
            GameObject hpTextObj = CreateUIElement("HealthText", hpBg.transform);
            SetStretch(hpTextObj.GetComponent<RectTransform>());
            TextMeshProUGUI hpText = hpTextObj.AddComponent<TextMeshProUGUI>();
            hpText.text = "100 / 100";
            hpText.fontSize = 20;
            hpText.alignment = TextAlignmentOptions.Center;
            hpText.color = Color.white;

            // Timer Text
            GameObject timerObj = CreateUIElement("TimerText", topBar.transform);
            RectTransform timerRect = timerObj.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.4f, 0.2f);
            timerRect.anchorMax = new Vector2(0.6f, 0.9f);
            timerRect.offsetMin = Vector2.zero;
            timerRect.offsetMax = Vector2.zero;
            TextMeshProUGUI timerText = timerObj.AddComponent<TextMeshProUGUI>();
            timerText.text = "00:00";
            timerText.fontSize = 36;
            timerText.fontStyle = FontStyles.Bold;
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.color = new Color(0f, 0.9f, 1f);

            // Kills Text
            GameObject killsObj = CreateUIElement("KillsText", topBar.transform);
            RectTransform killsRect = killsObj.GetComponent<RectTransform>();
            killsRect.anchorMin = new Vector2(0.75f, 0.2f);
            killsRect.anchorMax = new Vector2(1f, 0.9f);
            killsRect.offsetMin = Vector2.zero;
            killsRect.offsetMax = Vector2.zero;
            TextMeshProUGUI killsText = killsObj.AddComponent<TextMeshProUGUI>();
            killsText.text = "Bajas: 0";
            killsText.fontSize = 28;
            killsText.alignment = TextAlignmentOptions.Right;
            killsText.color = new Color(1f, 0.8f, 0.2f);

            // Virtual Joystick
            GameObject joystickObj = CreateUIElement("VirtualJoystick", hudPanel.transform);
            RectTransform joyRect = joystickObj.GetComponent<RectTransform>();
            joyRect.anchorMin = new Vector2(0f, 0f);
            joyRect.anchorMax = new Vector2(0f, 0f);
            joyRect.pivot = new Vector2(0.5f, 0.5f);
            joyRect.anchoredPosition = new Vector2(230f, 230f);
            joyRect.sizeDelta = new Vector2(220f, 220f);
            Image joyBgImg = joystickObj.AddComponent<Image>();
            if (joystickBgSprite != null) joyBgImg.sprite = joystickBgSprite;
            else joyBgImg.color = new Color(0.1f, 0.2f, 0.3f, 0.5f);

            GameObject handleObj = CreateUIElement("Handle", joystickObj.transform);
            RectTransform handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(90f, 90f);
            Image handleImg = handleObj.AddComponent<Image>();
            if (joystickHandleSprite != null) handleImg.sprite = joystickHandleSprite;
            else handleImg.color = new Color(0f, 0.8f, 1f, 0.9f);

            VirtualJoystick joystick = joystickObj.AddComponent<VirtualJoystick>();
            SetSerializedField(joystick, "backgroundRect", joyRect);
            SetSerializedField(joystick, "handleRect", handleRect);
            SetSerializedField(joystick, "handleRange", 70f);

            // Fire Button
            GameObject fireBtnObj = CreateUIElement("FireButton", hudPanel.transform);
            RectTransform fireRect = fireBtnObj.GetComponent<RectTransform>();
            fireRect.anchorMin = new Vector2(1f, 0f);
            fireRect.anchorMax = new Vector2(1f, 0f);
            fireRect.pivot = new Vector2(0.5f, 0.5f);
            fireRect.anchoredPosition = new Vector2(-210f, 210f);
            fireRect.sizeDelta = new Vector2(150f, 150f);
            Image fireImg = fireBtnObj.AddComponent<Image>();
            if (fireBtnSprite != null) fireImg.sprite = fireBtnSprite;
            else fireImg.color = new Color(1f, 0.3f, 0.1f, 0.85f);
            Button fireBtn = fireBtnObj.AddComponent<Button>();

            GameObject fireLabelObj = CreateUIElement("Label", fireBtnObj.transform);
            SetStretch(fireLabelObj.GetComponent<RectTransform>());
            TextMeshProUGUI fireLabel = fireLabelObj.AddComponent<TextMeshProUGUI>();
            fireLabel.text = "DISPARO";
            fireLabel.fontSize = 20;
            fireLabel.fontStyle = FontStyles.Bold;
            fireLabel.alignment = TextAlignmentOptions.Center;
            fireLabel.color = Color.white;

            // 3. Damage Overlay Vignette
            GameObject vignetteObj = CreateUIElement("DamageVignette", canvasObj.transform);
            SetStretch(vignetteObj.GetComponent<RectTransform>());
            Image vigImg = vignetteObj.AddComponent<Image>();
            if (vignetteSprite != null) vigImg.sprite = vignetteSprite;
            else vigImg.color = new Color(0.9f, 0f, 0f, 0.5f);
            vigImg.raycastTarget = false;
            CanvasGroup vigGroup = vignetteObj.AddComponent<CanvasGroup>();
            vigGroup.alpha = 0f;
            vigGroup.blocksRaycasts = false;

            // 4. Game Over Panel
            GameObject gameOverPanel = CreateUIElement("GameOverPanel", canvasObj.transform);
            SetStretch(gameOverPanel.GetComponent<RectTransform>());
            Image goBgImg = gameOverPanel.AddComponent<Image>();
            goBgImg.color = new Color(0.04f, 0.06f, 0.1f, 0.92f);

            GameObject goDialog = CreateUIElement("DialogBox", gameOverPanel.transform);
            RectTransform dlgRect = goDialog.GetComponent<RectTransform>();
            dlgRect.anchorMin = new Vector2(0.25f, 0.2f);
            dlgRect.anchorMax = new Vector2(0.75f, 0.82f);
            dlgRect.offsetMin = Vector2.zero;
            dlgRect.offsetMax = Vector2.zero;
            Image dlgImg = goDialog.AddComponent<Image>();
            dlgImg.color = new Color(0.08f, 0.12f, 0.2f, 0.95f);

            // Title
            GameObject goTitle = CreateUIElement("Title", goDialog.transform);
            RectTransform titleRect = goTitle.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.78f);
            titleRect.anchorMax = new Vector2(1f, 0.96f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            TextMeshProUGUI titleText = goTitle.AddComponent<TextMeshProUGUI>();
            titleText.text = "¡JUEGO TERMINADO!";
            titleText.fontSize = 42;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.2f, 0.15f);

            // Final Stats
            GameObject statsBox = CreateUIElement("StatsBox", goDialog.transform);
            RectTransform statsRect = statsBox.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.1f, 0.42f);
            statsRect.anchorMax = new Vector2(0.9f, 0.74f);
            statsRect.offsetMin = Vector2.zero;
            statsRect.offsetMax = Vector2.zero;

            GameObject fTimeObj = CreateUIElement("FinalTime", statsBox.transform);
            RectTransform fTimeRect = fTimeObj.GetComponent<RectTransform>();
            fTimeRect.anchorMin = new Vector2(0f, 0.66f);
            fTimeRect.anchorMax = new Vector2(1f, 1f);
            fTimeRect.offsetMin = Vector2.zero;
            fTimeRect.offsetMax = Vector2.zero;
            TextMeshProUGUI fTimeText = fTimeObj.AddComponent<TextMeshProUGUI>();
            fTimeText.text = "Tiempo Sobrevivido: 00:00";
            fTimeText.fontSize = 24;
            fTimeText.alignment = TextAlignmentOptions.Center;
            fTimeText.color = Color.white;

            GameObject fKillsObj = CreateUIElement("FinalKills", statsBox.transform);
            RectTransform fKillsRect = fKillsObj.GetComponent<RectTransform>();
            fKillsRect.anchorMin = new Vector2(0f, 0.33f);
            fKillsRect.anchorMax = new Vector2(1f, 0.66f);
            fKillsRect.offsetMin = Vector2.zero;
            fKillsRect.offsetMax = Vector2.zero;
            TextMeshProUGUI fKillsText = fKillsObj.AddComponent<TextMeshProUGUI>();
            fKillsText.text = "Enemigos Eliminados: 0";
            fKillsText.fontSize = 24;
            fKillsText.alignment = TextAlignmentOptions.Center;
            fKillsText.color = new Color(1f, 0.85f, 0.3f);

            GameObject fScoreObj = CreateUIElement("FinalScore", statsBox.transform);
            RectTransform fScoreRect = fScoreObj.GetComponent<RectTransform>();
            fScoreRect.anchorMin = new Vector2(0f, 0f);
            fScoreRect.anchorMax = new Vector2(1f, 0.33f);
            fScoreRect.offsetMin = Vector2.zero;
            fScoreRect.offsetMax = Vector2.zero;
            TextMeshProUGUI fScoreText = fScoreObj.AddComponent<TextMeshProUGUI>();
            fScoreText.text = "Puntuación Total: 0";
            fScoreText.fontSize = 26;
            fScoreText.fontStyle = FontStyles.Bold;
            fScoreText.alignment = TextAlignmentOptions.Center;
            fScoreText.color = new Color(0f, 0.9f, 1f);

            // Retry Button
            GameObject retryBtnObj = CreateUIElement("RetryButton", goDialog.transform);
            RectTransform retryRect = retryBtnObj.GetComponent<RectTransform>();
            retryRect.anchorMin = new Vector2(0.12f, 0.18f);
            retryRect.anchorMax = new Vector2(0.48f, 0.34f);
            retryRect.offsetMin = Vector2.zero;
            retryRect.offsetMax = Vector2.zero;
            Image retryImg = retryBtnObj.AddComponent<Image>();
            retryImg.color = new Color(0f, 0.6f, 0.9f);
            Button retryBtn = retryBtnObj.AddComponent<Button>();

            GameObject retryTextObj = CreateUIElement("Text", retryBtnObj.transform);
            SetStretch(retryTextObj.GetComponent<RectTransform>());
            TextMeshProUGUI retryText = retryTextObj.AddComponent<TextMeshProUGUI>();
            retryText.text = "REINTENTAR";
            retryText.fontSize = 22;
            retryText.fontStyle = FontStyles.Bold;
            retryText.alignment = TextAlignmentOptions.Center;
            retryText.color = Color.white;

            // Re-anchor Button
            GameObject reanchorBtnObj = CreateUIElement("ReanchorButton", goDialog.transform);
            RectTransform reanchorRect = reanchorBtnObj.GetComponent<RectTransform>();
            reanchorRect.anchorMin = new Vector2(0.52f, 0.18f);
            reanchorRect.anchorMax = new Vector2(0.88f, 0.34f);
            reanchorRect.offsetMin = Vector2.zero;
            reanchorRect.offsetMax = Vector2.zero;
            Image reanchorImg = reanchorBtnObj.AddComponent<Image>();
            reanchorImg.color = new Color(0.2f, 0.3f, 0.45f);
            Button reanchorBtn = reanchorBtnObj.AddComponent<Button>();

            GameObject reanchorTextObj = CreateUIElement("Text", reanchorBtnObj.transform);
            SetStretch(reanchorTextObj.GetComponent<RectTransform>());
            TextMeshProUGUI reanchorText = reanchorTextObj.AddComponent<TextMeshProUGUI>();
            reanchorText.text = "REPOSICIONAR";
            reanchorText.fontSize = 20;
            reanchorText.fontStyle = FontStyles.Bold;
            reanchorText.alignment = TextAlignmentOptions.Center;
            reanchorText.color = Color.white;

            // Conectar referencias en UIManager
            SetSerializedField(uiMgr, "placementPanel", placementPanel);
            SetSerializedField(uiMgr, "hudPanel", hudPanel);
            SetSerializedField(uiMgr, "gameOverPanel", gameOverPanel);
            SetSerializedField(uiMgr, "healthBarFill", hpFillImg);
            SetSerializedField(uiMgr, "healthText", hpText);
            SetSerializedField(uiMgr, "timerText", timerText);
            SetSerializedField(uiMgr, "killsText", killsText);
            SetSerializedField(uiMgr, "damageVignette", vigGroup);
            SetSerializedField(uiMgr, "finalTimeText", fTimeText);
            SetSerializedField(uiMgr, "finalKillsText", fKillsText);
            SetSerializedField(uiMgr, "finalScoreText", fScoreText);
            SetSerializedField(uiMgr, "retryButton", retryBtn);
            SetSerializedField(uiMgr, "reanchorButton", reanchorBtn);
            SetSerializedField(uiMgr, "fireButton", fireBtn);

            // Conectar referencias en GameManager
            SetSerializedField(gameManager, "placementController", placement);
            SetSerializedField(gameManager, "enemySpawner", spawner);
            SetSerializedField(gameManager, "uiManager", uiMgr);

            // Configurar paneles iniciales
            placementPanel.SetActive(true);
            hudPanel.SetActive(false);
            gameOverPanel.SetActive(false);

            // Guardar escena
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log("[AR Survival] ¡Escena ARSurvivalScene configurada y guardada con éxito!");
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetSerializedField(Object target, string fieldName, object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                if (value is Object unityObj)
                {
                    prop.objectReferenceValue = unityObj;
                }
                else if (value is float floatVal)
                {
                    prop.floatValue = floatVal;
                }
                else if (value is int intVal)
                {
                    prop.intValue = intVal;
                }
                else if (value is bool boolVal)
                {
                    prop.boolValue = boolVal;
                }
                else if (value is string strVal)
                {
                    prop.stringValue = strVal;
                }
                so.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning($"[AR Survival] No se encontró el campo serializado '{fieldName}' en {target.name}");
            }
        }
    }
}
