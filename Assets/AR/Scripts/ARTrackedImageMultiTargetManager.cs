using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARMobileLab
{
    /// <summary>
    /// Configuración para asociar el nombre de una imagen de referencia con un prefab y nombre visible.
    /// </summary>
    [Serializable]
    public class TargetMapping
    {
        [Tooltip("Nombre exacto en la Reference Image Library")]
        public string imageName;

        [Tooltip("Nombre para mostrar en la interfaz de usuario")]
        public string displayName;

        [Tooltip("Prefab 3D que se instanciará al detectar esta imagen")]
        public GameObject prefab;
    }

    /// <summary>
    /// Gestor de seguimiento de múltiples imágenes para AR Foundation 6.x.
    /// Implementa la arquitectura recomendada por la cátedra (trackablesChanged de AR Foundation 6),
    /// manejo de estados Tracking/Limited/None, registro de cantidad de detecciones
    /// y puente de interacción táctil con la UI.
    /// </summary>
    [RequireComponent(typeof(ARTrackedImageManager))]
    public class ARTrackedImageMultiTargetManager : MonoBehaviour
    {
        [Header("Configuración de Targets")]
        [SerializeField] private List<TargetMapping> targetMappings = new List<TargetMapping>();

        [Header("Prefab de respaldo si no hay coincidencia")]
        [SerializeField] private GameObject fallbackPrefab;

        [Header("Referencia al Gestor de UI")]
        [SerializeField] private ARUIManager uiManager;

        private ARTrackedImageManager manager;

        // Diccionario de instancias creadas asociadas a cada TrackableId
        private readonly Dictionary<TrackableId, GameObject> spawnedObjects = new Dictionary<TrackableId, GameObject>();

        // Diccionario de scripts interactivos en las instancias
        private readonly Dictionary<TrackableId, ARInteractiveObject> interactiveObjects = new Dictionary<TrackableId, ARInteractiveObject>();

        // Estado previo de cada imagen para detectar transiciones a Tracking
        private readonly Dictionary<TrackableId, TrackingState> previousStates = new Dictionary<TrackableId, TrackingState>();

        // Contador de detecciones por nombre de target
        private readonly Dictionary<string, int> detectionCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Target actualmente enfocado y activo
        private TrackableId lastActiveTrackableId;
        private string currentTrackedTargetName = string.Empty;

        public ARUIManager UIManager => uiManager;
        public IReadOnlyDictionary<string, int> DetectionCounts => detectionCounts;

        private void Awake()
        {
            manager = GetComponent<ARTrackedImageManager>();

            // Inicializar contadores en 0
            foreach (var mapping in targetMappings)
            {
                if (!string.IsNullOrEmpty(mapping.imageName))
                {
                    detectionCounts[mapping.imageName] = 0;
                }
            }
        }

        private void Start()
        {
            if (uiManager != null)
            {
                uiManager.UpdateStateSearching();
                uiManager.UpdateCounters(detectionCounts);
            }
        }

        private void OnEnable()
        {
            if (manager != null)
            {
                // AR Foundation 6.x usa UnityEvent trackablesChanged
                manager.trackablesChanged.AddListener(OnTrackablesChanged);
            }
        }

        private void OnDisable()
        {
            if (manager != null)
            {
                manager.trackablesChanged.RemoveListener(OnTrackablesChanged);
            }
        }

        private void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
        {
            // 1. Nuevos targets detectados
            foreach (ARTrackedImage image in args.added)
            {
                ProcessImage(image, isNew: true);
            }

            // 2. Targets actualizados (pose, estado de seguimiento)
            foreach (ARTrackedImage image in args.updated)
            {
                ProcessImage(image, isNew: false);
            }

            // 3. Targets eliminados del subsistema
            foreach (var kvp in args.removed)
            {
                RemoveImage(kvp.Key);
            }

            // Actualizar si no hay ningún target actualmente visible
            CheckIfAnyTracked();
        }

        private void ProcessImage(ARTrackedImage image, bool isNew)
        {
            TrackableId id = image.trackableId;
            string refName = image.referenceImage.name;
            TrackingState state = image.trackingState;

            // Instanciar contenido si aún no existe para este trackableId
            if (!spawnedObjects.TryGetValue(id, out GameObject go))
            {
                GameObject prefabToInstantiate = FindPrefabForImage(refName);
                if (prefabToInstantiate != null)
                {
                    go = Instantiate(prefabToInstantiate, image.transform);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;

                    spawnedObjects.Add(id, go);

                    var interactive = go.GetComponent<ARInteractiveObject>();
                    if (interactive == null)
                    {
                        interactive = go.GetComponentInChildren<ARInteractiveObject>();
                    }
                    if (interactive != null)
                    {
                        interactiveObjects[id] = interactive;
                    }
                }
            }

            // Obtener estado anterior
            previousStates.TryGetValue(id, out TrackingState prevState);
            previousStates[id] = state;

            // Manejo de Estados según la consigna: Tracking, Limited, None
            switch (state)
            {
                case TrackingState.Tracking:
                    if (go != null) go.SetActive(true);

                    // Si pasa a Tracking desde otro estado o es nuevo, registrar detección
                    if (isNew || prevState != TrackingState.Tracking)
                    {
                        RegisterDetection(refName);
                    }

                    lastActiveTrackableId = id;
                    currentTrackedTargetName = refName;

                    if (uiManager != null)
                    {
                        string displayName = GetDisplayName(refName);
                        int count = GetDetectionCount(refName);
                        uiManager.UpdateStateTracking(displayName, count);
                        uiManager.UpdateCounters(detectionCounts);
                    }
                    break;

                case TrackingState.Limited:
                    // En estado Limited, el seguimiento está degradado (movimiento rápido, oclusión parcial).
                    // Mantenemos visible o informamos en UI.
                    if (uiManager != null && currentTrackedTargetName == refName)
                    {
                        string displayName = GetDisplayName(refName);
                        uiManager.UpdateStateLimited(displayName);
                    }
                    break;

                case TrackingState.None:
                    // Ausencia total de seguimiento: ocultar contenido para evitar posiciones fantasma
                    if (go != null) go.SetActive(false);

                    if (currentTrackedTargetName == refName)
                    {
                        currentTrackedTargetName = string.Empty;
                        if (uiManager != null)
                        {
                            uiManager.UpdateStateLost();
                        }
                    }
                    break;
            }
        }

        private void RemoveImage(TrackableId id)
        {
            if (spawnedObjects.TryGetValue(id, out GameObject go))
            {
                if (go != null)
                {
                    Destroy(go);
                }
                spawnedObjects.Remove(id);
            }

            interactiveObjects.Remove(id);
            previousStates.Remove(id);

            if (lastActiveTrackableId == id)
            {
                lastActiveTrackableId = TrackableId.invalidId;
                currentTrackedTargetName = string.Empty;
                if (uiManager != null)
                {
                    uiManager.UpdateStateSearching();
                }
            }
        }

        private void RegisterDetection(string refName)
        {
            if (string.IsNullOrEmpty(refName)) return;

            if (detectionCounts.ContainsKey(refName))
            {
                detectionCounts[refName]++;
            }
            else
            {
                detectionCounts[refName] = 1;
            }
        }

        private void CheckIfAnyTracked()
        {
            bool hasActive = false;
            foreach (var kvp in previousStates)
            {
                if (kvp.Value == TrackingState.Tracking)
                {
                    hasActive = true;
                    break;
                }
            }

            if (!hasActive && string.IsNullOrEmpty(currentTrackedTargetName) && uiManager != null)
            {
                uiManager.UpdateStateSearching();
            }
        }

        private GameObject FindPrefabForImage(string imageName)
        {
            foreach (var mapping in targetMappings)
            {
                if (string.Equals(mapping.imageName, imageName, StringComparison.OrdinalIgnoreCase))
                {
                    return mapping.prefab;
                }
            }
            return fallbackPrefab;
        }

        private string GetDisplayName(string imageName)
        {
            foreach (var mapping in targetMappings)
            {
                if (string.Equals(mapping.imageName, imageName, StringComparison.OrdinalIgnoreCase))
                {
                    return string.IsNullOrEmpty(mapping.displayName) ? mapping.imageName : mapping.displayName;
                }
            }
            return imageName;
        }

        public int GetDetectionCount(string imageName)
        {
            if (!string.IsNullOrEmpty(imageName) && detectionCounts.TryGetValue(imageName, out int count))
            {
                return count;
            }
            return 0;
        }

        // ==========================================
        // MÉTODOS DE MANIPULACIÓN PARA CONTROLES UI
        // ==========================================

        private ARInteractiveObject GetCurrentInteractiveObject()
        {
            if (interactiveObjects.TryGetValue(lastActiveTrackableId, out var obj) && obj != null)
            {
                return obj;
            }

            // Si no está el último activo, retornar el primer objeto válido
            foreach (var item in interactiveObjects.Values)
            {
                if (item != null && item.gameObject.activeInHierarchy)
                    return item;
            }
            return null;
        }

        public void RotateActiveObject(float degrees)
        {
            var obj = GetCurrentInteractiveObject();
            if (obj != null)
            {
                obj.RotateManual(degrees);
            }
        }

        public void ToggleAutoRotateActiveObject()
        {
            var obj = GetCurrentInteractiveObject();
            if (obj != null)
            {
                obj.ToggleAutoRotate();
            }
            else
            {
                // Si no hay ninguno enfocado, alternar en todos los registrados
                foreach (var item in interactiveObjects.Values)
                {
                    if (item != null) item.ToggleAutoRotate();
                }
            }
        }

        public void ScaleActiveObject(float delta)
        {
            var obj = GetCurrentInteractiveObject();
            if (obj != null)
            {
                obj.AdjustScale(delta);
            }
        }

        public void CycleColorActiveObject()
        {
            var obj = GetCurrentInteractiveObject();
            if (obj != null)
            {
                obj.CycleColor();
            }
        }

        public void ResetActiveObject()
        {
            var obj = GetCurrentInteractiveObject();
            if (obj != null)
            {
                obj.ResetTransform();
            }
        }
    }
}
