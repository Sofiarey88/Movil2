using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ARMobileLab
{
    /// <summary>
    /// Sistema de conexión con WebCam (cámaras de PC o iPhone conectado por Wi-Fi mediante Iriun, EpocCam, DroidCam, etc.)
    /// y simulador de Image Tracking interactivo para pruebas en Unity Editor sin necesidad de compilar a móvil cada vez.
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
        private GameObject backgroundQuad;
        private Material quadMaterial;
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

            SetupUI();
        }

        private void Start()
        {
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

        private void Update()
        {
            // Atajos de teclado en el Editor
            #if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                TriggerSimulation("Target_Alpha");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                TriggerSimulation("Target_Beta");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            {
                TriggerSimulateLost();
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
                OnWebCamButtonClicked();
            }
            else if (Input.GetKeyDown(KeyCode.M))
            {
                ToggleMouseDrag();
            }

            // Arrastre con el mouse para posicionar el holograma sobre el papel en el video de la cámara
            if (isDraggingWithMouse && Input.GetMouseButton(0) && arCamera != null)
            {
                Ray ray = arCamera.ScreenPointToRay(Input.mousePosition);
                Vector3 newPos = ray.GetPoint(0.55f); // a 55 cm de la cámara
                if (targetManager != null)
                {
                    targetManager.UpdateSimulatedObjectPosition(newPos);
                }
            }
            #endif

            // Mantener el Quad de fondo ajustado al aspecto de la cámara
            UpdateBackgroundQuadTransform();
        }

        // ==========================================
        // GESTIÓN DE WEBCAM (PC / iOS VÍA WI-FI)
        // ==========================================

        public void OnWebCamButtonClicked()
        {
            WebCamDevice[] devices = WebCamTexture.devices;

            if (devices == null || devices.Length == 0)
            {
                Debug.LogWarning("[AR WebCam] No se detectaron cámaras en el sistema. Si usas iPhone por Wi-Fi, inicia la app Iriun o EpocCam en el teléfono.");
                if (webcamButtonText != null)
                {
                    webcamButtonText.text = "📷 Sin Cámaras\n(Instalar Iriun)";
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
                // Iniciar la primera cámara
                currentDeviceIndex = 0;
            }

            StartWebCamDevice(devices[currentDeviceIndex].name);
        }

        private void StartWebCamDevice(string deviceName)
        {
            StopWebCam();

            try
            {
                webcamTexture = new WebCamTexture(deviceName, 1280, 720, 30);
                webcamTexture.Play();

                CreateOrUpdateBackgroundQuad();

                Debug.Log($"[AR WebCam] ✓ Conectado a cámara: {deviceName}");
                UpdateWebCamButtonLabel();

                if (simulationToolbar != null)
                {
                    simulationToolbar.SetActive(true);
                }
            }
            catch (System.Exception ex)
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

            if (backgroundQuad != null)
            {
                backgroundQuad.SetActive(false);
            }

            UpdateWebCamButtonLabel();
        }

        private void CreateOrUpdateBackgroundQuad()
        {
            if (arCamera == null) arCamera = Camera.main;
            if (arCamera == null) return;

            if (backgroundQuad == null)
            {
                backgroundQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                backgroundQuad.name = "WebCam_LiveFeed_Background";
                backgroundQuad.transform.SetParent(arCamera.transform, false);

                var collider = backgroundQuad.GetComponent<Collider>();
                if (collider != null) Destroy(collider);

                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
                quadMaterial = new Material(shader);
                backgroundQuad.GetComponent<Renderer>().material = quadMaterial;
            }

            backgroundQuad.SetActive(true);

            if (quadMaterial != null && webcamTexture != null)
            {
                quadMaterial.mainTexture = webcamTexture;
                if (quadMaterial.HasProperty("_BaseMap"))
                {
                    quadMaterial.SetTexture("_BaseMap", webcamTexture);
                }
            }

            UpdateBackgroundQuadTransform();
        }

        private void UpdateBackgroundQuadTransform()
        {
            if (backgroundQuad == null || !backgroundQuad.activeSelf || arCamera == null)
                return;

            float distance = 2.0f; // Detrás de los objetos 3D (que están a 0.5m)
            backgroundQuad.transform.localPosition = new Vector3(0f, 0f, distance);
            backgroundQuad.transform.localRotation = Quaternion.identity;

            float frustumHeight = 2.0f * distance * Mathf.Tan(arCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float frustumWidth = frustumHeight * arCamera.aspect;

            // Invertir verticalmente si la webcam lo requiere
            float scaleY = frustumHeight;
            if (webcamTexture != null && webcamTexture.videoVerticallyMirrored)
            {
                scaleY = -frustumHeight;
            }

            backgroundQuad.transform.localScale = new Vector3(frustumWidth, scaleY, 1f);
        }

        private void UpdateWebCamButtonLabel()
        {
            if (webcamButtonText == null) return;

            if (IsWebCamPlaying)
            {
                string devName = webcamTexture.deviceName;
                if (devName.Length > 12) devName = devName.Substring(0, 10) + "..";
                webcamButtonText.text = $"📷 {devName}\n(Conectada)";
            }
            else
            {
                webcamButtonText.text = "📷 WebCam\n(iOS / PC)";
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
            if (backgroundQuad != null)
            {
                Destroy(backgroundQuad);
            }
        }
    }
}
