using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ARMobileLab
{
    /// <summary>
    /// Sistema de activación y desactivación con 1 solo clic de Iriun Webcam (cámara de iPhone conectada por Wi-Fi)
    /// y simulador de Image Tracking interactivo para pruebas en Unity Editor.
    /// Compatible al 100% con el nuevo Input System Package de Unity.
    /// </summary>
    public class ARWebCamTestManager : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private ARTrackedImageMultiTargetManager targetManager;
        [SerializeField] private Camera arCamera;
        [SerializeField] private Canvas canvas;

        [Header("Botón de Activación / Desactivación Iriun")]
        [SerializeField] private Button webcamToggleButton;
        [SerializeField] private TMP_Text webcamButtonText;

        [Header("Panel de Simulación de Tracking")]
        [SerializeField] private GameObject simulationToolbar;
        [SerializeField] private Button btnSimulateAlpha;
        [SerializeField] private Button btnSimulateBeta;
        [SerializeField] private Button btnSimulateLost;
        [SerializeField] private Button btnToggleDrag;
        [SerializeField] private TMP_Text dragButtonText;

        private WebCamTexture webcamTexture;
        private GameObject bgCanvasGo;
        private RawImage bgRawImage;
        private UnityEngine.XR.ARFoundation.ARCameraBackground cachedArCamBg;
        private bool isDraggingWithMouse = false;
        private string lastSimulatedTarget = string.Empty;

        public bool IsWebCamPlaying => webcamTexture != null && webcamTexture.isPlaying;

        private void Awake()
        {
            if (targetManager == null)
            {
                targetManager = FindFirstObjectByType<ARTrackedImageMultiTargetManager>();
            }

            if (arCamera == null)
            {
                arCamera = Camera.main;
            }

            if (arCamera != null)
            {
                cachedArCamBg = arCamera.GetComponent<UnityEngine.XR.ARFoundation.ARCameraBackground>();
            }

            SetupUI();
        }

        private void Start()
        {
            LogAvailableCameras();
            UpdateWebCamButtonLabel();

            // En el editor de Unity, mostrar la barra de simulación para pruebas inmediatas
            #if UNITY_EDITOR
            if (simulationToolbar != null)
            {
                simulationToolbar.SetActive(true);
            }
            #endif
        }

        private void SetupUI()
        {
            if (webcamToggleButton != null)
            {
                webcamToggleButton.onClick.AddListener(OnWebCamButtonClicked);
            }

            if (btnSimulateAlpha != null)
            {
                btnSimulateAlpha.onClick.AddListener(() => TriggerSimulation("Target_Alpha"));
            }

            if (btnSimulateBeta != null)
            {
                btnSimulateBeta.onClick.AddListener(() => TriggerSimulation("Target_Beta"));
            }

            if (btnSimulateLost != null)
            {
                btnSimulateLost.onClick.AddListener(TriggerSimulateLost);
            }

            if (btnToggleDrag != null)
            {
                btnToggleDrag.onClick.AddListener(ToggleMouseDrag);
            }
        }

        private void LogAvailableCameras()
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
            {
                Debug.LogWarning("[AR WebCam] No se detectaron cámaras en Windows. Abre la app Iriun Webcam en tu iPhone y PC conectados al mismo Wi-Fi.");
                return;
            }

            Debug.Log($"[AR WebCam] Se encontraron {devices.Length} cámaras disponibles en el sistema:");
            for (int i = 0; i < devices.Length; i++)
            {
                Debug.Log($"   [{i}] {devices[i].name} (Frontal: {devices[i].isFrontFacing})");
            }
        }

        private void Update()
        {
            // Atajos de teclado utilizando el New Input System
            #if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
                {
                    TriggerSimulation("Target_Alpha");
                }
                else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
                {
                    TriggerSimulation("Target_Beta");
                }
                else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
                {
                    TriggerSimulateLost();
                }
                else if (keyboard.cKey.wasPressedThisFrame || keyboard.iKey.wasPressedThisFrame)
                {
                    OnWebCamButtonClicked();
                }
                else if (keyboard.mKey.wasPressedThisFrame)
                {
                    ToggleMouseDrag();
                }
            }

            // Arrastre con el mouse para posicionar el holograma sobre el papel en el video de la cámara
            if (isDraggingWithMouse && mouse != null && mouse.leftButton.isPressed && arCamera != null)
            {
                Vector2 mousePos = mouse.position.ReadValue();
                Ray ray = arCamera.ScreenPointToRay(mousePos);
                Vector3 newPos = ray.GetPoint(2.5f);
                if (targetManager != null)
                {
                    targetManager.UpdateSimulatedObjectPosition(newPos);
                }
            }
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && arCamera != null)
            {
                // Clic en la pantalla para posicionar / spawnear el objeto en esa coordenada del video
                if (UnityEngine.EventSystems.EventSystem.current == null || !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    Vector2 mousePos = mouse.position.ReadValue();
                    Ray ray = arCamera.ScreenPointToRay(mousePos);
                    Vector3 placePos = ray.GetPoint(2.5f);
                    string targetToSpawn = string.IsNullOrEmpty(lastSimulatedTarget) ? "Target_Alpha" : lastSimulatedTarget;
                    TriggerSimulation(targetToSpawn, placePos);
                }
            }
            #endif

            // Ajustar orientación y aspect ratio de la imagen de la cámara si está activa
            UpdateBackgroundRawImageTransform();
        }

        // ==========================================
        // BOTÓN: ACTIVAR / DESACTIVAR IRIUN WEBCAM
        // ==========================================

        public void OnWebCamButtonClicked()
        {
            // 1. Si la cámara ya está activa -> DESACTIVAR
            if (IsWebCamPlaying)
            {
                StopWebCam();
                Debug.Log("[AR WebCam] Iriun Webcam desactivada por el usuario.");
                return;
            }

            // 2. Si está apagada -> ACTIVAR IRIUN
            WebCamDevice[] devices = WebCamTexture.devices;

            if (devices == null || devices.Length == 0)
            {
                Debug.LogWarning("[AR WebCam] No se detectó ninguna cámara en Windows. Asegúrate de tener Iriun Webcam instalado en tu PC y abierto en tu iPhone conectados al mismo Wi-Fi.");
                if (webcamButtonText != null)
                {
                    webcamButtonText.text = "Iriun: No Detectada\n(Abrir en iPhone)";
                }
                if (webcamToggleButton != null)
                {
                    webcamToggleButton.image.color = new Color(0.85f, 0.45f, 0.05f, 0.95f);
                }
                return;
            }

            // Buscar prioritariamente la cámara de Iriun
            string targetDevice = devices[0].name;
            bool foundIriun = false;

            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i].name.IndexOf("iriun", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    targetDevice = devices[i].name;
                    foundIriun = true;
                    break;
                }
            }

            // Si no tiene 'iriun' en el nombre pero hay EpocCam o Camo
            if (!foundIriun)
            {
                for (int i = 0; i < devices.Length; i++)
                {
                    string dName = devices[i].name;
                    if (dName.IndexOf("epoc", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        dName.IndexOf("camo", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        targetDevice = dName;
                        break;
                    }
                }
            }

            StartWebCamDevice(targetDevice);
        }

        private void StartWebCamDevice(string deviceName)
        {
            StopWebCam();

            try
            {
                // Iniciar la textura con la resolución de Iriun
                webcamTexture = new WebCamTexture(deviceName, 1280, 720, 30);
                webcamTexture.Play();

                // Desactivar temporalmente ARCameraBackground para que no opaque el video en el Editor
                if (cachedArCamBg == null && arCamera != null)
                {
                    cachedArCamBg = arCamera.GetComponent<UnityEngine.XR.ARFoundation.ARCameraBackground>();
                }
                if (cachedArCamBg != null)
                {
                    cachedArCamBg.enabled = false;
                }

                CreateOrUpdateBackgroundCanvas();

                Debug.Log($"[AR WebCam] ✓ ¡Iriun Webcam '{deviceName}' activada exitosamente!");
                UpdateWebCamButtonLabel();

                if (simulationToolbar != null)
                {
                    simulationToolbar.SetActive(true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AR WebCam] Error al activar Iriun Webcam ({deviceName}): {ex.Message}");
            }
        }

        public void StopWebCam()
        {
            if (webcamTexture != null)
            {
                if (webcamTexture.isPlaying) webcamTexture.Stop();
                Destroy(webcamTexture);
                webcamTexture = null;
            }

            if (bgCanvasGo != null)
            {
                bgCanvasGo.SetActive(false);
            }

            // Reactivar ARCameraBackground
            if (cachedArCamBg != null)
            {
                cachedArCamBg.enabled = true;
            }

            UpdateWebCamButtonLabel();
        }

        private void CreateOrUpdateBackgroundCanvas()
        {
            if (arCamera == null) arCamera = Camera.main;
            if (arCamera == null) return;

            if (bgCanvasGo == null)
            {
                bgCanvasGo = new GameObject("WebCam_Background_Canvas");
                Canvas c = bgCanvasGo.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = arCamera;
                c.planeDistance = 5.0f; // Detrás de los hologramas 3D (que están a 0.55m)
                c.sortingOrder = -100;  // Detrás de todo el HUD

                bgCanvasGo.AddComponent<CanvasScaler>();

                GameObject rawGo = new GameObject("WebCam_RawImage", typeof(RectTransform), typeof(RawImage));
                rawGo.transform.SetParent(bgCanvasGo.transform, false);

                RectTransform rt = rawGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                bgRawImage = rawGo.GetComponent<RawImage>();
            }

            bgCanvasGo.SetActive(true);

            if (bgRawImage != null && webcamTexture != null)
            {
                bgRawImage.texture = webcamTexture;
                bgRawImage.color = Color.white;
            }

            UpdateBackgroundRawImageTransform();
        }

        private void UpdateBackgroundRawImageTransform()
        {
            if (bgRawImage == null || webcamTexture == null || !webcamTexture.isPlaying)
                return;

            // Inversión vertical si la webcam viene invertida
            float scaleY = webcamTexture.videoVerticallyMirrored ? -1f : 1f;
            bgRawImage.transform.localScale = new Vector3(1f, scaleY, 1f);

            // Ajustar rotación si el celular está rotado
            int angle = webcamTexture.videoRotationAngle;
            bgRawImage.transform.localEulerAngles = new Vector3(0f, 0f, -angle);
        }

        private void UpdateWebCamButtonLabel()
        {
            if (webcamToggleButton != null)
            {
                if (IsWebCamPlaying && webcamTexture != null)
                {
                    // VERDE cuando está ACTIVADA
                    webcamToggleButton.image.color = new Color(0.05f, 0.65f, 0.35f, 0.95f);
                }
                else
                {
                    // AZUL / GRIS cuando está APAGADA
                    webcamToggleButton.image.color = new Color(0.18f, 0.24f, 0.34f, 0.95f);
                }
            }

            if (webcamButtonText == null) return;

            if (IsWebCamPlaying && webcamTexture != null)
            {
                webcamButtonText.text = "Iriun: ON\n[Clic: Apagar]";
            }
            else
            {
                webcamButtonText.text = "Iriun: OFF\n[Clic: Encender]";
            }
        }

        // ==========================================
        // SIMULADOR DE IMAGE TRACKING
        // ==========================================

        public void TriggerSimulation(string targetName, Vector3? customPos = null)
        {
            if (targetManager == null) return;

            lastSimulatedTarget = targetName;

            Vector3 spawnPos;
            if (customPos.HasValue)
            {
                spawnPos = customPos.Value;
            }
            else
            {
                if (arCamera != null)
                {
                    // Spawnear a 2.5m frente a la cámara para que sea visible en el Editor
                    spawnPos = arCamera.transform.position + (arCamera.transform.forward * 2.5f);
                    Debug.Log($"[AR WebCam] Spawn en {spawnPos} (cámara: {arCamera.transform.position}, forward: {arCamera.transform.forward})");
                }
                else
                {
                    spawnPos = new Vector3(0f, 0f, 2.5f);
                    Debug.LogWarning("[AR WebCam] arCamera es null — usando posición de fallback (0,0,2.5)");
                }
            }

            targetManager.SimulateTargetTracking(targetName, spawnPos);
        }

        public void TriggerSimulateLost()
        {
            if (targetManager == null) return;
            targetManager.SimulateTrackingLost();
        }

        public void ToggleMouseDrag()
        {
            isDraggingWithMouse = !isDraggingWithMouse;
            if (dragButtonText != null)
            {
                dragButtonText.text = isDraggingWithMouse ? "Mover: ON" : "Mover: OFF";
            }
        }

        private void OnDestroy()
        {
            StopWebCam();
            if (bgCanvasGo != null)
            {
                Destroy(bgCanvasGo);
            }
        }
    }
}
