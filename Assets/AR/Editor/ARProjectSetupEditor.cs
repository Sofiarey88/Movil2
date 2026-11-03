using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

namespace ARMobileLab.Editor
{
    [InitializeOnLoad]
    public static class ARProjectSetupEditor
    {
        private const string ImagesFolder = "Assets/AR/Images";
        private const string PrefabsFolder = "Assets/AR/Prefabs";
        private const string MaterialsFolder = "Assets/AR/Materials";
        private const string ScenesFolder = "Assets/Scenes";
        private const string LibraryPath = "Assets/AR/TargetsLibrary.asset";
        private const string ScenePath = "Assets/Scenes/ImageTargetLab.unity";

        static ARProjectSetupEditor()
        {
            EditorApplication.delayCall += AutoSetupIfNeeded;
        }

        private static void AutoSetupIfNeeded()
        {
            if (!System.IO.File.Exists(ScenePath) || !System.IO.File.Exists(LibraryPath))
            {
                Debug.Log("[AR Lab] Configurando escena...");
                BuildImageTargetLabScene();
            }
        }

        [MenuItem("AR Lab/1. Configurar Reference Image Library", false, 10)]
        public static XRReferenceImageLibrary SetupReferenceImageLibrary()
        {
            EnsureDirectories();

            string alphaTexPath = $"{ImagesFolder}/Target_Alpha.png";
            string betaTexPath = $"{ImagesFolder}/Target_Beta.png";

            // Asegurar que las texturas estén importadas con lectura y sin compresión agresiva
            ConfigureTextureImporter(alphaTexPath);
            ConfigureTextureImporter(betaTexPath);
            AssetDatabase.Refresh();

            Texture2D texAlpha = AssetDatabase.LoadAssetAtPath<Texture2D>(alphaTexPath);
            Texture2D texBeta = AssetDatabase.LoadAssetAtPath<Texture2D>(betaTexPath);

            if (texAlpha == null || texBeta == null)
            {
                Debug.LogError("[AR Lab] Error: No se encontraron las texturas de los targets en Assets/AR/Images.");
                return null;
            }

            XRReferenceImageLibrary library = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<XRReferenceImageLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            // Limpiar y poblar la librería
            while (library.count > 0)
            {
                // XRReferenceImageLibrary no tiene RemoveAt público directo, pero si ya existe se sobrescriben entradas
                break;
            }

            // Si está vacía o tiene menos de 2 entradas
            if (library.count < 1) library.Add();
            if (library.count < 2) library.Add();

            // Configurar Target Alpha (0.15m x 0.15m)
            library.SetName(0, "Target_Alpha");
            library.SetTexture(0, texAlpha, false);
            library.SetSpecifySize(0, true);
            library.SetSize(0, new Vector2(0.15f, 0.15f));

            // Configurar Target Beta (0.15m x 0.15m)
            library.SetName(1, "Target_Beta");
            library.SetTexture(1, texBeta, false);
            library.SetSpecifySize(1, true);
            library.SetSize(1, new Vector2(0.15f, 0.15f));

            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            Debug.Log($"[AR Lab] ✓ Reference Image Library creada exitosamente en {LibraryPath} con 2 targets.");
            return library;
        }

        private static void ConfigureTextureImporter(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool modified = false;
                if (!importer.isReadable)
                {
                    importer.isReadable = true;
                    modified = true;
                }
                if (importer.textureType != TextureImporterType.Default)
                {
                    importer.textureType = TextureImporterType.Default;
                    modified = true;
                }
                if (modified)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        [MenuItem("AR Lab/2. Crear Materiales y Prefabs 3D", false, 20)]
        public static (GameObject prefabA, GameObject prefabB) CreateMaterialsAndPrefabs()
        {
            EnsureDirectories();

            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null) urpLitShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (urpLitShader == null) urpLitShader = Shader.Find("Standard");

            // 1. Materiales
            Material matCyan = CreateOrGetMaterial($"{MaterialsFolder}/Mat_CyberCore_Cyan.mat", urpLitShader, new Color(0.0f, 0.85f, 1.0f), 0.7f, 0.85f, true);
            Material matOrange = CreateOrGetMaterial($"{MaterialsFolder}/Mat_QuantumBeacon_Orange.mat", urpLitShader, new Color(1.0f, 0.45f, 0.0f), 0.7f, 0.85f, true);
            Material matDarkMetal = CreateOrGetMaterial($"{MaterialsFolder}/Mat_SciFiMetal.mat", urpLitShader, new Color(0.15f, 0.17f, 0.20f), 0.9f, 0.65f, false);
            Material matGold = CreateOrGetMaterial($"{MaterialsFolder}/Mat_GoldAccent.mat", urpLitShader, new Color(1.0f, 0.80f, 0.15f), 0.85f, 0.9f, false);

            AssetDatabase.SaveAssets();

            // 2. Prefab A: Cyber Core
            string prefabAPath = $"{PrefabsFolder}/Prefab_TargetA_CyberCore.prefab";
            GameObject rootA = new GameObject("Prefab_TargetA_CyberCore");
            var interactiveA = rootA.AddComponent<ARInteractiveObject>();

            // Pedestal base
            GameObject pedestalA = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestalA.name = "PedestalBase";
            pedestalA.transform.SetParent(rootA.transform, false);
            pedestalA.transform.localPosition = new Vector3(0f, 0.003f, 0f);
            pedestalA.transform.localScale = new Vector3(0.11f, 0.004f, 0.11f);
            pedestalA.GetComponent<Renderer>().sharedMaterial = matDarkMetal;
            RemoveCollider(pedestalA);

            // Anillo base dorado
            GameObject ringBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ringBase.name = "GoldRingBase";
            ringBase.transform.SetParent(rootA.transform, false);
            ringBase.transform.localPosition = new Vector3(0f, 0.007f, 0f);
            ringBase.transform.localScale = new Vector3(0.085f, 0.003f, 0.085f);
            ringBase.GetComponent<Renderer>().sharedMaterial = matGold;
            RemoveCollider(ringBase);

            // Núcleo energético central
            GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "EnergyOrb";
            orb.transform.SetParent(rootA.transform, false);
            orb.transform.localPosition = new Vector3(0f, 0.050f, 0f);
            orb.transform.localScale = new Vector3(0.045f, 0.045f, 0.045f);
            orb.GetComponent<Renderer>().sharedMaterial = matCyan;
            RemoveCollider(orb);

            // Anillo orbital inclinado
            GameObject orbitRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            orbitRing.name = "OrbitRing";
            orbitRing.transform.SetParent(rootA.transform, false);
            orbitRing.transform.localPosition = new Vector3(0f, 0.050f, 0f);
            orbitRing.transform.localRotation = Quaternion.Euler(30f, 25f, 15f);
            orbitRing.transform.localScale = new Vector3(0.075f, 0.002f, 0.075f);
            orbitRing.GetComponent<Renderer>().sharedMaterial = matCyan;
            RemoveCollider(orbitRing);

            // Satélites orbitales
            for (int i = 0; i < 3; i++)
            {
                float angle = i * (360f / 3f) * Mathf.Deg2Rad;
                GameObject sat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sat.name = $"Satellite_{i}";
                sat.transform.SetParent(rootA.transform, false);
                sat.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.048f, 0.050f, Mathf.Sin(angle) * 0.048f);
                sat.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);
                sat.transform.localScale = new Vector3(0.012f, 0.012f, 0.012f);
                sat.GetComponent<Renderer>().sharedMaterial = matGold;
                RemoveCollider(sat);
            }

            GameObject savedPrefabA = PrefabUtility.SaveAsPrefabAsset(rootA, prefabAPath);
            GameObject.DestroyImmediate(rootA);

            // 3. Prefab B: Quantum Beacon
            string prefabBPath = $"{PrefabsFolder}/Prefab_TargetB_QuantumBeacon.prefab";
            GameObject rootB = new GameObject("Prefab_TargetB_QuantumBeacon");
            var interactiveB = rootB.AddComponent<ARInteractiveObject>();

            // Plataforma base
            GameObject pedestalB = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestalB.name = "BasePlatform";
            pedestalB.transform.SetParent(rootB.transform, false);
            pedestalB.transform.localPosition = new Vector3(0f, 0.004f, 0f);
            pedestalB.transform.localScale = new Vector3(0.10f, 0.006f, 0.10f);
            pedestalB.GetComponent<Renderer>().sharedMaterial = matDarkMetal;
            RemoveCollider(pedestalB);

            // Corona exterior dorada
            GameObject outerRim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            outerRim.name = "OuterRim";
            outerRim.transform.SetParent(rootB.transform, false);
            outerRim.transform.localPosition = new Vector3(0f, 0.008f, 0f);
            outerRim.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            outerRim.transform.localScale = new Vector3(0.075f, 0.004f, 0.075f);
            outerRim.GetComponent<Renderer>().sharedMaterial = matGold;
            RemoveCollider(outerRim);

            // Obelisco Cuántico central (Cubo alargado y rotado 45°)
            GameObject spire = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spire.name = "QuantumSpire";
            spire.transform.SetParent(rootB.transform, false);
            spire.transform.localPosition = new Vector3(0f, 0.055f, 0f);
            spire.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            spire.transform.localScale = new Vector3(0.035f, 0.085f, 0.035f);
            spire.GetComponent<Renderer>().sharedMaterial = matOrange;
            RemoveCollider(spire);

            // Fragmentos cuánticos flotantes
            for (int k = 0; k < 4; k++)
            {
                float angle = (k * 90f + 25f) * Mathf.Deg2Rad;
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = $"QuantumShard_{k}";
                shard.transform.SetParent(rootB.transform, false);
                shard.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.042f, 0.055f + (k % 2 == 0 ? 0.015f : -0.015f), Mathf.Sin(angle) * 0.042f);
                shard.transform.localRotation = Quaternion.Euler(30f * k, 45f + (k * 20f), 15f);
                shard.transform.localScale = new Vector3(0.014f, 0.024f, 0.014f);
                shard.GetComponent<Renderer>().sharedMaterial = (k % 2 == 0) ? matGold : matOrange;
                RemoveCollider(shard);
            }

            // Diamante superior (Cúspide de energía)
            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crown.name = "CrownApex";
            crown.transform.SetParent(rootB.transform, false);
            crown.transform.localPosition = new Vector3(0f, 0.110f, 0f);
            crown.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);
            crown.transform.localScale = new Vector3(0.022f, 0.022f, 0.022f);
            crown.GetComponent<Renderer>().sharedMaterial = matOrange;
            RemoveCollider(crown);

            GameObject savedPrefabB = PrefabUtility.SaveAsPrefabAsset(rootB, prefabBPath);
            GameObject.DestroyImmediate(rootB);

            AssetDatabase.SaveAssets();
            Debug.Log($"[AR Lab] ✓ Prefabs creados exitosamente:\n  1. {prefabAPath}\n  2. {prefabBPath}");

            return (savedPrefabA, savedPrefabB);
        }

        private static Material CreateOrGetMaterial(string path, Shader shader, Color baseColor, float metallic, float smoothness, bool emission)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetColor("_BaseColor", baseColor);
            mat.SetColor("_Color", baseColor);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);

            if (emission)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", baseColor * 0.8f);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void RemoveCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) GameObject.DestroyImmediate(col);
        }

        [MenuItem("AR Lab/3. Construir Escena ImageTargetLab", false, 30)]
        public static void BuildImageTargetLabScene()
        {
            // 1. Asegurar librería y prefabs
            var library = SetupReferenceImageLibrary();
            var (prefabA, prefabB) = CreateMaterialsAndPrefabs();

            string sampleScenePath = $"{ScenesFolder}/SampleScene.unity";

            // Duplicar SampleScene si ImageTargetLab no existe aún
            if (!AssetDatabase.CopyAsset(sampleScenePath, ScenePath))
            {
                // Si ya existe, podemos continuar sobreescribiendo o abriendo
                Debug.Log($"[AR Lab] La escena {ScenePath} ya existe o se reutilizará.");
            }

            AssetDatabase.Refresh();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // 2. Desactivar / Retirar objetos propios de la demo de planos para no mezclar conductas
            string[] objectsToDisable = new string[]
            {
                "Object Spawner",
                "Object Menu",
                "Coaching UI",
                "Greeting Prompt",
                "Options Modal",
                "UI",
                "Affordance UI"
            };

            foreach (var rootObj in scene.GetRootGameObjects())
            {
                foreach (var name in objectsToDisable)
                {
                    if (rootObj.name.Contains(name))
                    {
                        rootObj.SetActive(false);
                    }
                }
            }

            // 3. Localizar XR Origin
            GameObject xrOriginGo = null;
            foreach (var rootObj in scene.GetRootGameObjects())
            {
                if (rootObj.name.Contains("XR Origin"))
                {
                    xrOriginGo = rootObj;
                    break;
                }
            }

            if (xrOriginGo == null)
            {
                Debug.LogError("[AR Lab] Error: No se encontró XR Origin en la escena.");
                return;
            }

            // 4. Configurar ARTrackedImageManager en XR Origin
            var trackedImageManager = xrOriginGo.GetComponent<ARTrackedImageManager>();
            if (trackedImageManager == null)
            {
                trackedImageManager = xrOriginGo.AddComponent<ARTrackedImageManager>();
            }

            trackedImageManager.referenceLibrary = library;
            trackedImageManager.requestedMaxNumberOfMovingImages = 2;

            // Desactivar el ARPlaneManager en XR Origin para concentrarse en Image Tracking según el PDF
            var planeManager = xrOriginGo.GetComponent<ARPlaneManager>();
            if (planeManager != null)
            {
                planeManager.enabled = false;
            }

            // 5. Configurar ARTrackedImageMultiTargetManager en XR Origin
            var multiTargetManager = xrOriginGo.GetComponent<ARTrackedImageMultiTargetManager>();
            if (multiTargetManager == null)
            {
                multiTargetManager = xrOriginGo.AddComponent<ARTrackedImageMultiTargetManager>();
            }

            // Asignar los mapeos por SerializedObject
            SerializedObject soManager = new SerializedObject(multiTargetManager);
            SerializedProperty mappingsProp = soManager.FindProperty("targetMappings");
            mappingsProp.ClearArray();

            mappingsProp.InsertArrayElementAtIndex(0);
            SerializedProperty elem0 = mappingsProp.GetArrayElementAtIndex(0);
            elem0.FindPropertyRelative("imageName").stringValue = "Target_Alpha";
            elem0.FindPropertyRelative("displayName").stringValue = "Target Alpha (Cyber Core)";
            elem0.FindPropertyRelative("prefab").objectReferenceValue = prefabA;

            mappingsProp.InsertArrayElementAtIndex(1);
            SerializedProperty elem1 = mappingsProp.GetArrayElementAtIndex(1);
            elem1.FindPropertyRelative("imageName").stringValue = "Target_Beta";
            elem1.FindPropertyRelative("displayName").stringValue = "Target Beta (Quantum Beacon)";
            elem1.FindPropertyRelative("prefab").objectReferenceValue = prefabB;

            soManager.ApplyModifiedProperties();

            // 6. Construir Canvas y UI Interactiva
            GameObject canvasGo = CreateARCanvasUI(multiTargetManager);

            // Vincular UI Manager con MultiTargetManager
            var uiManager = canvasGo.GetComponent<ARUIManager>();
            SerializedObject soMulti = new SerializedObject(multiTargetManager);
            soMulti.FindProperty("uiManager").objectReferenceValue = uiManager;
            soMulti.ApplyModifiedProperties();

            // 7. Crear vista previa de los targets con sus objetos 3D visibles en la Escena
            CreateOrRefreshEditorPreview();

            // 8. Guardar Escena
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // 8. Registrar escena en EditorBuildSettings
            RegisterSceneInBuildSettings(ScenePath);

            Debug.Log($"[AR Lab] ✓ ¡Escena {ScenePath} configurada y guardada con éxito!");
        }

        private static GameObject CreateARCanvasUI(ARTrackedImageMultiTargetManager multiManager)
        {
            // Eliminar canvas previo si existía
            GameObject existingCanvas = GameObject.Find("AR_Interactive_Canvas");
            if (existingCanvas != null)
            {
                GameObject.DestroyImmediate(existingCanvas);
            }

            GameObject canvasGo = new GameObject("AR_Interactive_Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            ARUIManager uiManager = canvasGo.AddComponent<ARUIManager>();

            // Fuente predeterminada TMP
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

            // --- 1. BANNER SUPERIOR DE ESTADO ---
            GameObject bannerGo = CreateUIPanel(canvasGo.transform, "TopStatusBanner",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -80f), new Vector2(980f, 180f),
                new Color(0.12f, 0.16f, 0.23f, 0.94f));

            var bannerImg = bannerGo.GetComponent<Image>();

            GameObject titleTextGo = CreateUIText(bannerGo.transform, "StatusTitle",
                new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), new Vector2(920f, 60f),
                "Buscando marcadores...", 34, FontStyles.Bold, Color.white, font);

            GameObject subTextGo = CreateUIText(bannerGo.transform, "StatusSubtitle",
                new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 0f), new Vector2(920f, 50f),
                "Apunta la cámara a Target Alpha o Target Beta (15-30 cm)", 24, FontStyles.Normal, new Color(0.85f, 0.88f, 0.92f), font);

            // --- 2. CONTADORES HUD (Pill bajo el banner) ---
            GameObject countersPill = CreateUIPanel(canvasGo.transform, "CountersHUD",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -280f), new Vector2(860f, 70f),
                new Color(0.08f, 0.11f, 0.16f, 0.85f));

            GameObject countAGo = CreateUIText(countersPill.transform, "CounterAlphaText",
                new Vector2(0.28f, 0.5f), new Vector2(0.28f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(400f, 50f),
                "Target Alpha: 0 veces", 24, FontStyles.Bold, new Color(0.0f, 0.85f, 1.0f), font);

            GameObject countBGo = CreateUIText(countersPill.transform, "CounterBetaText",
                new Vector2(0.72f, 0.5f), new Vector2(0.72f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(400f, 50f),
                "Target Beta: 0 veces", 24, FontStyles.Bold, new Color(1.0f, 0.65f, 0.15f), font);

            // --- 3. BOTÓN DE CONEXIÓN WEBCAM (TOP-RIGHT) ---
            GameObject webcamBtnGo = CreateUIButton(canvasGo.transform, "WebCamConnectButton",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-220f, -90f), new Vector2(210f, 90f),
                "CAM: WebCam\n(iOS / PC)", 22, new Color(0.08f, 0.45f, 0.55f, 0.95f), font);

            // --- 4. BOTÓN DE AYUDA (TOP-RIGHT) ---
            GameObject helpBtnGo = CreateUIButton(canvasGo.transform, "HelpOpenButton",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-60f, -90f), new Vector2(90f, 90f),
                "?", 44, new Color(0.20f, 0.28f, 0.38f, 0.95f), font);

            // --- 5. BARRA DE SIMULACIÓN Y TEST EN EDITOR (BAJO EL HUD) ---
            GameObject simToolbar = CreateUIPanel(canvasGo.transform, "SimulationToolbar",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -370f), new Vector2(960f, 65f),
                new Color(0.06f, 0.08f, 0.12f, 0.90f));

            float simX = -360f;
            float simStep = 240f;

            GameObject btnSimAlpha = CreateUIButton(simToolbar.transform, "BtnSimAlpha",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(simX + (simStep * 0), 0f), new Vector2(220f, 52f),
                "Target Alpha (1)", 20, new Color(0.05f, 0.48f, 0.55f), font);

            GameObject btnSimBeta = CreateUIButton(simToolbar.transform, "BtnSimBeta",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(simX + (simStep * 1), 0f), new Vector2(220f, 52f),
                "Target Beta (2)", 20, new Color(0.70f, 0.35f, 0.05f), font);

            GameObject btnSimLost = CreateUIButton(simToolbar.transform, "BtnSimLost",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(simX + (simStep * 2), 0f), new Vector2(220f, 52f),
                "Perder (3)", 20, new Color(0.70f, 0.15f, 0.15f), font);

            GameObject btnDrag = CreateUIButton(simToolbar.transform, "BtnToggleDrag",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(simX + (simStep * 3), 0f), new Vector2(220f, 52f),
                "Mover: OFF (M)", 20, new Color(0.20f, 0.25f, 0.35f), font);

            // --- 6. BARRA INFERIOR DE CONTROLES 3D ---
            GameObject bottomBar = CreateUIPanel(canvasGo.transform, "BottomControlsBar",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 120f), new Vector2(1000f, 180f),
                new Color(0.10f, 0.13f, 0.18f, 0.92f));

            // Botones de control horizontal
            float startX = -400f;
            float stepX = 133f;

            GameObject btnRotL = CreateUIButton(bottomBar.transform, "BtnRotateLeft",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(startX + (stepX * 0), 0f), new Vector2(115f, 120f),
                "<\n-30", 28, new Color(0.18f, 0.24f, 0.34f), font);

            GameObject btnRotR = CreateUIButton(bottomBar.transform, "BtnRotateRight",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(startX + (stepX * 1), 0f), new Vector2(115f, 120f),
                ">\n+30", 28, new Color(0.18f, 0.24f, 0.34f), font);

            GameObject btnAutoRot = CreateUIButton(bottomBar.transform, "BtnToggleAutoRotate",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(startX + (stepX * 2), 0f), new Vector2(130f, 120f),
                "Giro:\nON", 26, new Color(0.05f, 0.50f, 0.35f), font);

            GameObject btnScaleUp = CreateUIButton(bottomBar.transform, "BtnScaleUp",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(startX + (stepX * 3), 0f), new Vector2(115f, 120f),
                "Zoom\n+", 26, new Color(0.18f, 0.24f, 0.34f), font);

            GameObject btnScaleDown = CreateUIButton(bottomBar.transform, "BtnScaleDown",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(startX + (stepX * 4), 0f), new Vector2(115f, 120f),
                "Zoom\n-", 26, new Color(0.18f, 0.24f, 0.34f), font);

            GameObject btnColor = CreateUIButton(bottomBar.transform, "BtnCycleColor",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(startX + (stepX * 5), 0f), new Vector2(115f, 120f),
                "Color\nModo", 24, new Color(0.55f, 0.20f, 0.70f), font);

            GameObject btnReset = CreateUIButton(bottomBar.transform, "BtnReset",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(startX + (stepX * 6), 0f), new Vector2(115f, 120f),
                "Reset\nPose", 24, new Color(0.70f, 0.20f, 0.20f), font);

            // --- 5. PANEL MODAL DE AYUDA ---
            GameObject modalGo = CreateUIPanel(canvasGo.transform, "HelpModalPanel",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 850f),
                new Color(0.08f, 0.11f, 0.16f, 0.98f));

            CreateUIText(modalGo.transform, "ModalTitle",
                new Vector2(0.5f, 0.90f), new Vector2(0.5f, 0.90f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(800f, 80f),
                "Guía de Uso — AR Image Tracking", 34, FontStyles.Bold, new Color(0.2f, 0.75f, 1.0f), font);

            string helpBody = "1. Enfoca la cámara hacia los marcadores:\n" +
                              "   • Target Alpha (Cyber Core)\n" +
                              "   • Target Beta (Quantum Beacon)\n\n" +
                              "2. Distancia recomendada: 15 cm a 40 cm con buena iluminación.\n\n" +
                              "3. Cada target genera su propio objeto 3D independiente con seguimiento continuo.\n\n" +
                              "4. Utiliza la barra inferior para rotar, activar giro automático, cambiar escala o alternar colores.\n\n" +
                              "5. Al tapar el marcador, se ilustra la pérdida de tracking (Limited/None). Al enfocarlo de nuevo, se reanuda sin duplicados.";

            CreateUIText(modalGo.transform, "ModalBody",
                new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.50f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(780f, 480f),
                helpBody, 24, FontStyles.Normal, Color.white, font, TextAlignmentOptions.TopLeft);

            GameObject closeBtnGo = CreateUIButton(modalGo.transform, "HelpCloseButton",
                new Vector2(0.5f, 0.10f), new Vector2(0.5f, 0.10f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(300f, 80f),
                "Entendido", 30, new Color(0.05f, 0.58f, 0.38f), font);

            modalGo.SetActive(false); // Inicia oculto

            // Serializar referencias en ARUIManager
            SerializedObject soUI = new SerializedObject(uiManager);
            soUI.FindProperty("targetManager").objectReferenceValue = multiManager;
            soUI.FindProperty("bannerBackground").objectReferenceValue = bannerImg;
            soUI.FindProperty("statusTitleText").objectReferenceValue = titleTextGo.GetComponent<TMP_Text>();
            soUI.FindProperty("statusSubtitleText").objectReferenceValue = subTextGo.GetComponent<TMP_Text>();
            soUI.FindProperty("targetACounterText").objectReferenceValue = countAGo.GetComponent<TMP_Text>();
            soUI.FindProperty("targetBCounterText").objectReferenceValue = countBGo.GetComponent<TMP_Text>();

            soUI.FindProperty("rotateLeftButton").objectReferenceValue = btnRotL.GetComponent<Button>();
            soUI.FindProperty("rotateRightButton").objectReferenceValue = btnRotR.GetComponent<Button>();
            soUI.FindProperty("toggleAutoRotateButton").objectReferenceValue = btnAutoRot.GetComponent<Button>();
            soUI.FindProperty("autoRotateButtonText").objectReferenceValue = btnAutoRot.GetComponentInChildren<TMP_Text>();
            soUI.FindProperty("scaleUpButton").objectReferenceValue = btnScaleUp.GetComponent<Button>();
            soUI.FindProperty("scaleDownButton").objectReferenceValue = btnScaleDown.GetComponent<Button>();
            soUI.FindProperty("cycleColorButton").objectReferenceValue = btnColor.GetComponent<Button>();
            soUI.FindProperty("resetButton").objectReferenceValue = btnReset.GetComponent<Button>();

            soUI.FindProperty("helpModalPanel").objectReferenceValue = modalGo;
            soUI.FindProperty("helpOpenButton").objectReferenceValue = helpBtnGo.GetComponent<Button>();
            soUI.FindProperty("helpCloseButton").objectReferenceValue = closeBtnGo.GetComponent<Button>();

            soUI.ApplyModifiedProperties();

            // Configurar ARWebCamTestManager
            var webcamMgr = canvasGo.AddComponent<ARWebCamTestManager>();
            SerializedObject soCam = new SerializedObject(webcamMgr);
            soCam.FindProperty("targetManager").objectReferenceValue = multiManager;
            soCam.FindProperty("arCamera").objectReferenceValue = Camera.main;
            soCam.FindProperty("canvas").objectReferenceValue = canvas;
            soCam.FindProperty("webcamToggleButton").objectReferenceValue = webcamBtnGo.GetComponent<Button>();
            soCam.FindProperty("webcamButtonText").objectReferenceValue = webcamBtnGo.GetComponentInChildren<TMP_Text>();
            soCam.FindProperty("simulationToolbar").objectReferenceValue = simToolbar;
            soCam.FindProperty("btnSimulateAlpha").objectReferenceValue = btnSimAlpha.GetComponent<Button>();
            soCam.FindProperty("btnSimulateBeta").objectReferenceValue = btnSimBeta.GetComponent<Button>();
            soCam.FindProperty("btnSimulateLost").objectReferenceValue = btnSimLost.GetComponent<Button>();
            soCam.FindProperty("btnToggleDrag").objectReferenceValue = btnDrag.GetComponent<Button>();
            soCam.FindProperty("dragButtonText").objectReferenceValue = btnDrag.GetComponentInChildren<TMP_Text>();
            soCam.ApplyModifiedProperties();

            return canvasGo;
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            img.color = color;
            return go;
        }

        private static GameObject CreateUIText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, string text, float fontSize, FontStyles style, Color color, TMP_FontAsset font, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = alignment;
            if (font != null) tmp.font = font;

            return go;
        }

        private static GameObject CreateUIButton(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size, string text, float fontSize, Color btnColor, TMP_FontAsset font)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            Image img = btnGo.GetComponent<Image>();
            img.color = btnColor;

            Button btn = btnGo.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = btnColor;
            cb.highlightedColor = btnColor * 1.15f;
            cb.pressedColor = btnColor * 0.85f;
            btn.colors = cb;

            CreateUIText(btnGo.transform, "BtnText",
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                text, fontSize, FontStyles.Bold, Color.white, font);

            return btnGo;
        }

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            // Buscar si ya está
            int existingIndex = scenes.FindIndex(s => s.path == scenePath);
            if (existingIndex >= 0)
            {
                scenes.RemoveAt(existingIndex);
            }

            // Insertar ImageTargetLab en el índice 0 (primera escena al abrir la app)
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[AR Lab] ✓ Escena {scenePath} configurada como escena principal (índice 0) en EditorBuildSettings.");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/AR")) AssetDatabase.CreateFolder("Assets", "AR");
            if (!AssetDatabase.IsValidFolder(ImagesFolder)) AssetDatabase.CreateFolder("Assets/AR", "Images");
            if (!AssetDatabase.IsValidFolder(PrefabsFolder)) AssetDatabase.CreateFolder("Assets/AR", "Prefabs");
            if (!AssetDatabase.IsValidFolder(MaterialsFolder)) AssetDatabase.CreateFolder("Assets/AR", "Materials");
            if (!AssetDatabase.IsValidFolder("Assets/AR/Editor")) AssetDatabase.CreateFolder("Assets/AR", "Editor");
        }

        [MenuItem("AR Lab/★ Configurar Todo el Proyecto (All-In-One)", false, 0)]
        public static void SetupEverything()
        {
            Debug.Log("[AR Lab] Iniciando configuración integral del prototipo AR...");
            BuildImageTargetLabScene();
            Debug.Log("[AR Lab] ★ ¡Configuración integral completada con éxito!");
        }

        [MenuItem("AR Lab/★ Alternar Vista Previa 3D en Escena (Toggle Preview)", false, 1)]
        public static void ToggleEditorPreview()
        {
            const string previewRootName = "[EDITOR_PREVIEW_TARGETS]";
            GameObject existing = GameObject.Find(previewRootName);

            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
                Debug.Log("[AR Lab] Vista previa de targets retirada de la escena.");
                return;
            }

            CreateOrRefreshEditorPreview();
        }

        public static void CreateOrRefreshEditorPreview()
        {
            const string previewRootName = "[EDITOR_PREVIEW_TARGETS]";
            GameObject existing = GameObject.Find(previewRootName);
            if (existing != null)
            {
                GameObject.DestroyImmediate(existing);
            }

            GameObject previewRoot = new GameObject(previewRootName);
            previewRoot.tag = "EditorOnly";

            Texture2D texA = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ImagesFolder}/Target_Alpha.png");
            Texture2D texB = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ImagesFolder}/Target_Beta.png");
            GameObject prefabA = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsFolder}/Prefab_TargetA_CyberCore.prefab");
            GameObject prefabB = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsFolder}/Prefab_TargetB_QuantumBeacon.prefab");

            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");

            // Preview Target A (Cyber Core sobre papel de 15x15cm)
            GameObject targetCardA = GameObject.CreatePrimitive(PrimitiveType.Quad);
            targetCardA.name = "Preview_Target_Alpha_Card";
            targetCardA.transform.SetParent(previewRoot.transform, false);
            targetCardA.transform.localPosition = new Vector3(-0.25f, 0f, 0.6f);
            targetCardA.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            targetCardA.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            RemoveCollider(targetCardA);
            Material matA = new Material(unlitShader);
            matA.mainTexture = texA;
            if (matA.HasProperty("_BaseMap")) matA.SetTexture("_BaseMap", texA);
            targetCardA.GetComponent<Renderer>().material = matA;

            if (prefabA != null)
            {
                GameObject instA = (GameObject)PrefabUtility.InstantiatePrefab(prefabA, previewRoot.transform);
                instA.transform.localPosition = new Vector3(-0.25f, 0f, 0.6f);
                instA.transform.localRotation = Quaternion.identity;
            }

            // Preview Target B (Quantum Beacon sobre papel de 15x15cm)
            GameObject targetCardB = GameObject.CreatePrimitive(PrimitiveType.Quad);
            targetCardB.name = "Preview_Target_Beta_Card";
            targetCardB.transform.SetParent(previewRoot.transform, false);
            targetCardB.transform.localPosition = new Vector3(0.25f, 0f, 0.6f);
            targetCardB.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            targetCardB.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            RemoveCollider(targetCardB);
            Material matB = new Material(unlitShader);
            matB.mainTexture = texB;
            if (matB.HasProperty("_BaseMap")) matB.SetTexture("_BaseMap", texB);
            targetCardB.GetComponent<Renderer>().material = matB;

            if (prefabB != null)
            {
                GameObject instB = (GameObject)PrefabUtility.InstantiatePrefab(prefabB, previewRoot.transform);
                instB.transform.localPosition = new Vector3(0.25f, 0f, 0.6f);
                instB.transform.localRotation = Quaternion.identity;
            }

            Undo.RegisterCreatedObjectUndo(previewRoot, "Create AR Target Preview");
            Selection.activeGameObject = previewRoot;

            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }

            Debug.Log("[AR Lab] ✓ Vista previa 3D de ambos targets montada en la escena.");
        }

        [MenuItem("AR Lab/★ Compilar APK Android Directo", false, 50)]
        public static void BuildAndroidAPK()
        {
            string buildFolder = "Builds/Android";
            if (!System.IO.Directory.Exists(buildFolder))
            {
                System.IO.Directory.CreateDirectory(buildFolder);
            }
            string apkPath = $"{buildFolder}/AR_Prototype_UAI.apk";

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = apkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Debug.Log($"[AR Lab] Iniciando compilación de APK en: {apkPath}...");
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[AR Lab] ✓ ¡Build exitoso! Archivo generado: {apkPath} ({summary.totalSize / (1024 * 1024)} MB)");
                EditorUtility.RevealInFinder(apkPath);
            }
            else
            {
                Debug.LogWarning($"[AR Lab] Resultado de compilación: {summary.result}");
            }
        }
    }
}
