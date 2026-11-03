using UnityEngine;

namespace ARMobileLab
{
    /// <summary>
    /// Componente que se adjunta al objeto 3D instanciado sobre cada target.
    /// Permite manipulación interactiva (rotación, escala, cambio de color y animación sutil de flotación).
    /// </summary>
    public class ARInteractiveObject : MonoBehaviour
    {
        [Header("Configuración de Rotación")]
        [SerializeField] private bool autoRotate = true;
        [SerializeField] private float rotationSpeed = 45f; // grados por segundo

        [Header("Configuración de Escala")]
        [SerializeField] private float minScale = 0.4f;
        [SerializeField] private float maxScale = 2.5f;
        [SerializeField] private float defaultScale = 1.0f;

        [Header("Animación de Flotación")]
        [SerializeField] private bool enableBobbing = true;
        [SerializeField] private float bobbingAmplitude = 0.015f; // en metros
        [SerializeField] private float bobbingFrequency = 1.5f;

        [Header("Renderers para cambio de color")]
        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private Color[] colorPalette = new Color[]
        {
            new Color(0.0f, 0.85f, 1.0f),  // Cyan Neón
            new Color(1.0f, 0.45f, 0.0f),  // Naranja Quantum
            new Color(0.2f, 1.0f, 0.35f),  // Verde Esmeralda
            new Color(0.9f, 0.1f, 0.85f),  // Magenta Eléctrico
            new Color(1.0f, 0.85f, 0.1f)   // Oro Brillante
        };

        private int currentColorIndex = 0;
        private float currentScaleMultiplier = 1.0f;
        private Vector3 initialLocalPosition;
        private MaterialPropertyBlock propertyBlock;

        public bool AutoRotate => autoRotate;
        public float CurrentScaleMultiplier => currentScaleMultiplier;

        private void Awake()
        {
            initialLocalPosition = transform.localPosition;
            propertyBlock = new MaterialPropertyBlock();

            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                targetRenderers = GetComponentsInChildren<Renderer>();
            }
        }

        private void Update()
        {
            // Auto rotación sobre eje Y local
            if (autoRotate)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
            }

            // Animación sutil de flotación vertical
            if (enableBobbing)
            {
                float verticalOffset = Mathf.Sin(Time.time * bobbingFrequency) * bobbingAmplitude;
                transform.localPosition = new Vector3(
                    initialLocalPosition.x,
                    initialLocalPosition.y + verticalOffset,
                    initialLocalPosition.z
                );
            }
        }

        /// <summary>
        /// Activa o desactiva la autorotación continua.
        /// </summary>
        public void ToggleAutoRotate()
        {
            autoRotate = !autoRotate;
        }

        public void SetAutoRotate(bool enable)
        {
            autoRotate = enable;
        }

        /// <summary>
        /// Rota manualmente el objeto una cantidad de grados alrededor del eje vertical.
        /// </summary>
        public void RotateManual(float angleDegrees)
        {
            transform.Rotate(Vector3.up, angleDegrees, Space.Self);
        }

        /// <summary>
        /// Modifica la escala relativa del objeto.
        /// </summary>
        public void AdjustScale(float delta)
        {
            currentScaleMultiplier = Mathf.Clamp(currentScaleMultiplier + delta, minScale, maxScale);
            transform.localScale = Vector3.one * (defaultScale * currentScaleMultiplier);
        }

        /// <summary>
        /// Restablece la rotación y la escala a sus valores iniciales.
        /// </summary>
        public void ResetTransform()
        {
            currentScaleMultiplier = 1.0f;
            transform.localScale = Vector3.one * defaultScale;
            transform.localRotation = Quaternion.identity;
            transform.localPosition = initialLocalPosition;
        }

        /// <summary>
        /// Cambia el color de acento o material ciclando por la paleta de colores.
        /// </summary>
        public void CycleColor()
        {
            if (targetRenderers == null || targetRenderers.Length == 0 || colorPalette.Length == 0)
                return;

            currentColorIndex = (currentColorIndex + 1) % colorPalette.Length;
            Color chosenColor = colorPalette[currentColorIndex];

            foreach (var rend in targetRenderers)
            {
                if (rend == null) continue;

                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", chosenColor);
                propertyBlock.SetColor("_Color", chosenColor);
                propertyBlock.SetColor("_EmissionColor", chosenColor * 0.7f);
                rend.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
