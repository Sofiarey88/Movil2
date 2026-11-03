using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ARMobileLab
{
    /// <summary>
    /// Controlador de la interfaz gráfica táctil para la experiencia AR.
    /// Presenta avisos de estado de seguimiento (Búsqueda, Tracking Activo, Limitado, Perdido),
    /// contadores de detecciones por marcador, panel de ayuda y controles de manipulación 3D.
    /// </summary>
    public class ARUIManager : MonoBehaviour
    {
        [Header("Referencia al Gestor de Targets")]
        [SerializeField] private ARTrackedImageMultiTargetManager targetManager;

        [Header("Banner Superior de Estado")]
        [SerializeField] private Image bannerBackground;
        [SerializeField] private TMP_Text statusTitleText;
        [SerializeField] private TMP_Text statusSubtitleText;

        [Header("Colores de Estado")]
        [SerializeField] private Color searchingColor = new Color(0.12f, 0.16f, 0.23f, 0.92f); // Azul oscuro
        [SerializeField] private Color trackingColor = new Color(0.05f, 0.58f, 0.38f, 0.92f);  // Verde esmeralda
        [SerializeField] private Color limitedColor = new Color(0.85f, 0.50f, 0.05f, 0.92f);   // Naranja / Ámbar
        [SerializeField] private Color lostColor = new Color(0.80f, 0.20f, 0.20f, 0.92f);      // Rojo

        [Header("HUD de Contadores")]
        [SerializeField] private TMP_Text targetACounterText;
        [SerializeField] private TMP_Text targetBCounterText;

        [Header("Controles de Manipulación 3D")]
        [SerializeField] private Button rotateLeftButton;
        [SerializeField] private Button rotateRightButton;
        [SerializeField] private Button toggleAutoRotateButton;
        [SerializeField] private TMP_Text autoRotateButtonText;
        [SerializeField] private Button scaleUpButton;
        [SerializeField] private Button scaleDownButton;
        [SerializeField] private Button cycleColorButton;
        [SerializeField] private Button resetButton;

        [Header("Panel Modal de Ayuda / Instrucciones")]
        [SerializeField] private GameObject helpModalPanel;
        [SerializeField] private Button helpOpenButton;
        [SerializeField] private Button helpCloseButton;

        private bool isAutoRotateActive = true;

        private void Awake()
        {
            if (targetManager == null)
            {
                targetManager = FindFirstObjectByType<ARTrackedImageMultiTargetManager>();
            }

            SetupButtons();
        }

        private void SetupButtons()
        {
            if (rotateLeftButton != null)
                rotateLeftButton.onClick.AddListener(() => OnRotateClicked(-30f));

            if (rotateRightButton != null)
                rotateRightButton.onClick.AddListener(() => OnRotateClicked(30f));

            if (toggleAutoRotateButton != null)
                toggleAutoRotateButton.onClick.AddListener(OnToggleAutoRotateClicked);

            if (scaleUpButton != null)
                scaleUpButton.onClick.AddListener(() => OnScaleClicked(0.15f));

            if (scaleDownButton != null)
                scaleDownButton.onClick.AddListener(() => OnScaleClicked(-0.15f));

            if (cycleColorButton != null)
                cycleColorButton.onClick.AddListener(OnCycleColorClicked);

            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetClicked);

            if (helpOpenButton != null)
                helpOpenButton.onClick.AddListener(() => SetHelpModalVisible(true));

            if (helpCloseButton != null)
                helpCloseButton.onClick.AddListener(() => SetHelpModalVisible(false));
        }

        // ==========================================
        // ACTUALIZACIÓN DE ESTADOS DE SEGUIMIENTO
        // ==========================================

        public void UpdateStateSearching()
        {
            if (bannerBackground != null) bannerBackground.color = searchingColor;
            if (statusTitleText != null) statusTitleText.text = "Buscando marcadores...";
            if (statusSubtitleText != null) statusSubtitleText.text = "Apunta la cámara a Target Alpha o Target Beta (15-30 cm)";
        }

        public void UpdateStateTracking(string displayName, int count)
        {
            if (bannerBackground != null) bannerBackground.color = trackingColor;
            if (statusTitleText != null) statusTitleText.text = $"✓ {displayName} detectado";
            if (statusSubtitleText != null) statusSubtitleText.text = $"Seguimiento activo | Total detecciones: {count}";
        }

        public void UpdateStateLimited(string displayName)
        {
            if (bannerBackground != null) bannerBackground.color = limitedColor;
            if (statusTitleText != null) statusTitleText.text = $"⚠ Seguimiento parcial: {displayName}";
            if (statusSubtitleText != null) statusSubtitleText.text = "Movimiento rápido o iluminación baja. Mantén el teléfono estable";
        }

        public void UpdateStateLost()
        {
            if (bannerBackground != null) bannerBackground.color = lostColor;
            if (statusTitleText != null) statusTitleText.text = "✗ Seguimiento perdido";
            if (statusSubtitleText != null) statusSubtitleText.text = "El marcador salió del encuadre. Reenfócalo para continuar";
        }

        public void UpdateCounters(IReadOnlyDictionary<string, int> counts)
        {
            int countA = 0;
            int countB = 0;

            if (counts != null)
            {
                if (counts.TryGetValue("Target_Alpha", out int a)) countA = a;
                if (counts.TryGetValue("Target_Beta", out int b)) countB = b;
            }

            if (targetACounterText != null)
                targetACounterText.text = $"Target Alpha: {countA} veces";

            if (targetBCounterText != null)
                targetBCounterText.text = $"Target Beta: {countB} veces";
        }

        // ==========================================
        // HANDLERS DE INTERACCIÓN
        // ==========================================

        private void OnRotateClicked(float degrees)
        {
            if (targetManager != null)
            {
                targetManager.RotateActiveObject(degrees);
            }
        }

        private void OnToggleAutoRotateClicked()
        {
            isAutoRotateActive = !isAutoRotateActive;

            if (targetManager != null)
            {
                targetManager.ToggleAutoRotateActiveObject();
            }

            if (autoRotateButtonText != null)
            {
                autoRotateButtonText.text = isAutoRotateActive ? "Giro: ON" : "Giro: OFF";
            }
        }

        private void OnScaleClicked(float delta)
        {
            if (targetManager != null)
            {
                targetManager.ScaleActiveObject(delta);
            }
        }

        private void OnCycleColorClicked()
        {
            if (targetManager != null)
            {
                targetManager.CycleColorActiveObject();
            }
        }

        private void OnResetClicked()
        {
            if (targetManager != null)
            {
                targetManager.ResetActiveObject();
            }
        }

        public void SetHelpModalVisible(bool visible)
        {
            if (helpModalPanel != null)
            {
                helpModalPanel.SetActive(visible);
            }
        }
    }
}
