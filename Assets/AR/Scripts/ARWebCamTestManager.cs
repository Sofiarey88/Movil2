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
    /// Sistema de conexión con WebCam (cámaras de PC o iPhone conectado por Wi-Fi mediante Iriun, EpocCam, Camo, etc.)
    /// y simulador de Image Tracking interactivo para pruebas en Unity Editor sin necesidad de compilar a móvil cada vez.
    /// Compatible al 100% con el nuevo Input System Package de Unity.
    /// </summary>
    public class ARWebCamTestManager : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private ARTrackedImageMultiTargetManager targetManager;
        [SerializeField] private Camera arCamera;
        [SerializeField] private Canvas canvas;

        [Header("Botón de Conexión WebCam en UI")]
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
        private int currentDeviceIndex = -1;
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
                Debug.LogWarning("[AR WebCam] No se detectaron cámaras en Windows. Si vas a usar tu iPhone por Wi-Fi, abre la app Iriun Webcam o EpocCam en el teléfono.");
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
                else if (keyboard.cKey.wasPressedThisFrame)
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
                Vector3 newPos = ray.GetPoint(0.55f); // a 55 cm de la cámara
                if (targetManager != null)
                {
                    targetManager.UpdateSimulatedObjectPosition(newPos);
                }
            }
            #endif

            // Ajustar orientación y aspect ratio de la imagen de la cámara si está activa
            UpdateBackgroundRawImageTransform();
        }

        // ==========================================
        // GESTIÓN DE WEBCAM (PC / iOS VÍA WI-FI)
        // ==========================================

        public void OnWebCamButtonClicked()
        {
            WebCamDevice[] devices = WebCamTexture.devices;

            if (devices == null || devices.Length == 0)
            {
                Debug.LogWarning("[AR WebCam] No se detectaron cámaras en el sistema. Asegúrate de instalar y abrir Iriun Webcam (o EpocCam) en tu iPhone y PC conectados al mismo Wi-Fi.");
                if (webcamButtonText != null)
                {
                    webcamButtonText.text = "CAM: Sin Camara\n(Abrir Iriun)";
                }
                return;
            }

            // Si ya está reproduciendo, cambiar a la siguiente cámara o apagar si llegó al final
            if (IsWebCamPlaying)
            {
                currentDeviceIndex++;
                if (currentDeviceIndex >= devices.Length)
                {
                    // Apagar
                    StopWebCam();
                    currentDeviceIndex = -1;
                    UpdateWebCamButtonLabel();
                    return;
                }
            }
            else
            {
                // Buscar si hay alguna cámara de iPhone conectada (Iriun, EpocCam, Camo)
                currentDeviceIndex = 0;
                for (int i = 0; i < devices.Length; i++)
                {
                    string dName = devices[i].name;
                    if (dName.IndexOf("iriun", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        dName.IndexOf("epoc", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        dName.IndexOf("camo", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        currentDeviceIndex = i;
                        break;
                    }
                }
            }

            StartWebCamDevice(devices[currentDeviceIndex].name);
        }

        private void StartWebCamDevice(string deviceName)
        {
            StopWebCam();

            try
            {
                // Iniciar la textura de la cámara web
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

                Debug.Log($"[AR WebCam] ✓ Conectado exitosamente a la cámara: {deviceName}");
                UpdateWebCamButtonLabel();

                if (simulationToolbar != null)
                {
                    simulationToolbar.SetActive(true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AR WebCam] Error al conectar con cámara {deviceName}: {ex.Message}");
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
                c.planeDistance = 5.0f; // Detrás de los objetos 3D (que están a 0.55m)
                c.sortingOrder = -100;  // Detrás de todo el HUD

                bgCanvasGo.AddComponent<CanvasScaler>();

                GameObject rawGo = new GameObject("WebCam_RawImage", typeof(RectTransform), typeof(RawImage));
                rawGo.transform.SetParent(bgCanvasGo.transform, false);

                RectTransform rt = rawGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = Vector2.zero;

                bgRawImage = rawGo.GetComponent<RawImage>();
            }

            bgCanvasGo.SetActive(true);

            if (bgRawImage != null && webcamTexture != null)
            {
                bgRawImage.texture = webcamTexture;
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
            if (webcamButtonText == null) return;

            if (IsWebCamPlaying && webcamTexture != null)
            {
                string devName = webcamTexture.deviceName;
                if (devName.Length > 10) devName = devName.Substring(0, 9) + "..";
                webcamButtonText.text = $"CAM: {devName}\n(Activa)";
            }
            else
            {
                webcamButtonText.text = "CAM: WebCam\n(iOS / PC)";
            }
        }

        // ==========================================
        // SIMULADOR DE IMAGE TRACKING
        // ==========================================

        public void TriggerSimulation(string targetName)
        {
            if (targetManager == null) return;

            lastSimulatedTarget = targetName;

            Vector3 spawnPos = Vector3.forward * 0.55f;
            if (arCamera != null)
            {
                // Posicionar frente a la cámara en el centro de visión
                spawnPos = arCamera.transform.position + (arCamera.transform.forward * 0.55f);
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
