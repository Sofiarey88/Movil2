# Prototipo de Realidad Aumentada Móvil (Image Tracking)

**Cátedra de Simulaciones — UAI**  
*Proyecto desarrollado sobre Unity 6 utilizando el AR Mobile Template y AR Foundation 6.*

---

## 📋 Resumen del Proyecto

Este proyecto implementa una aplicación de **Realidad Aumentada (AR)** para dispositivos móviles orientada al reconocimiento y seguimiento de imágenes 2D (**Image Tracking**). Utiliza la infraestructura oficial de **AR Foundation 6** y **ARCore XR Plugin**, reconociendo de forma simultánea e independiente múltiples objetivos físicos (*Target Alpha* y *Target Beta*), instanciando sobre cada uno un modelo 3D diferenciado con animación y controles de manipulación interactiva desde la interfaz táctil.

### ✨ Características Implementadas

- **Detección y Seguimiento de Múltiples Imágenes (Image Tracking):**
  - Librería de referencia (`TargetsLibrary.asset`) con dos targets de alto contraste y esquinas definidas:
    - **Target Alpha:** Genera el objeto 3D *Cyber Core* (núcleo energético con anillos orbitales y satélites).
    - **Target Beta:** Genera el objeto 3D *Quantum Beacon* (obelisco cuántico con fragmentos flotantes y cúspide de energía).
  - Escala métrica real configurada a **15 cm × 15 cm (0.15 m)**.
- **Gestión Robusta de Estados de Tracking (Tracking, Limited, None):**
  - Migración a la API moderna de **AR Foundation 6.x** utilizando `trackablesChanged` (UnityEvent).
  - Los objetos se instancian una única vez por trackable y quedan anclados a su pose.
  - Al perder el encuadre o degradarse la señal (*Limited* / *None*), el objeto se oculta sin destruir el GameObject de forma innecesaria en cada frame, reapareciendo sin duplicados al reanudar el seguimiento (*Tracking*).
- **Interfaz de Usuario Interactiva (UI / HUD en Pantalla):**
  - **Banner Superior:** Notifica el estado en tiempo real (Búsqueda inicial, Target detectado con seguimiento activo, y avisos de pérdida o degradación de señal).
  - **Contador de Detecciones (Bonus):** Registra y muestra en pantalla cuántas veces ha sido detectado cada marcador de forma independiente.
  - **Panel de Ayuda Modal:** Explicación emergente accesible mediante botón `?` con indicaciones de distancia y luz.
- **Manipulación de Objetos 3D en Tiempo Real:**
  - Barra de control táctil inferior:
    - `↺ -30°` / `↻ +30°`: Rotación incremental manual.
    - `Giro: ON/OFF`: Alternar rotación continua automática.
    - `🔍 +` / `🔍 -`: Ajuste interactivo de escala con límites de seguridad.
    - `🎨 Color`: Ciclo de materiales y paleta de colores reactivos.
    - `⟲ Reset`: Restablecer rotación, posición y escala original.
  - Animación sutil de levitación / flotación vertical senoidal para dar sensación de holograma AR.

---

## 🛠️ Especificaciones Técnicas

| Componente | Versión / Detalle |
| :--- | :--- |
| **Motor de Desarrollo** | Unity 6 (`6000.0.69f1`) |
| **Render Pipeline** | Universal Render Pipeline (URP `17.0.4`) |
| **AR Framework** | AR Foundation (`6.5.0`) |
| **AR Provider (Android)** | ARCore XR Plugin (`6.5.0`) |
| **Input System** | New Input System Package (`1.19.0`) |
| **XR Management** | OpenXR / ARCore Loader habilitado |
| **Scripting Backend** | IL2CPP |
| **Arquitectura de Build** | ARM64 |
| **API Mínima de Android** | Android 7.0 / API Level 24 (*ARCore Required*) |
| **API Objetivo de Android** | Automatic / Highest Installed |

---

## 📂 Estructura del Repositorio

```text
Movil/
├── Assets/
│   ├── AR/
│   │   ├── Editor/
│   │   │   └── ARProjectSetupEditor.cs       # Herramientas de automatización y build
│   │   ├── Images/
│   │   │   ├── Target_Alpha.png              # Imagen objetivo 1 (Cyber Core)
│   │   │   └── Target_Beta.png               # Imagen objetivo 2 (Quantum Beacon)
│   │   ├── Materials/                        # Materiales URP (Cyan, Naranja, Metal, Oro)
│   │   ├── Prefabs/
│   │   │   ├── Prefab_TargetA_CyberCore.prefab
│   │   │   └── Prefab_TargetB_QuantumBeacon.prefab
│   │   ├── Scripts/
│   │   │   ├── ARInteractiveObject.cs        # Lógica de manipulación 3D
│   │   │   ├── ARTrackedImageMultiTargetManager.cs # Gestor AR Foundation 6.x
│   │   │   └── ARUIManager.cs                # Controlador de UI / HUD táctil
│   │   └── TargetsLibrary.asset              # Reference Image Library (2 targets a 0.15m)
│   ├── Scenes/
│   │   ├── ImageTargetLab.unity              # Escena principal de la práctica
│   │   └── SampleScene.unity                 # Escena base del AR Mobile Template
│   └── MobileARTemplateAssets/               # Assets del template AR
├── PrintableTargets/
│   ├── Target_Alpha.png                      # Target 1 listo para imprimir / visualizar
│   ├── Target_Beta.png                       # Target 2 listo para imprimir / visualizar
│   └── Targets_Printable_Sheet.html          # Lámina web interactiva e imprimible a escala 15x15cm
├── ProjectSettings/                          # Configuración de Player, Android, XR y Build
└── Tools/
    └── generate_targets.ps1                  # Script generador de marcadores con alta entropía
```

---

## 🖨️ Marcadores para Pruebas (Targets)

Los archivos de imagen para imprimir o visualizar en una segunda pantalla se encuentran en la carpeta `PrintableTargets/`:

1. **Opción Recomendada:** Abre en tu navegador el archivo [`PrintableTargets/Targets_Printable_Sheet.html`](PrintableTargets/Targets_Printable_Sheet.html). Permite ver ambos targets en pantalla o imprimirlos directamente en hoja A4 con sus medidas calibradas (15 cm × 15 cm).
2. **Opción Pantalla Móvil/Monitor:** Puedes abrir directamente `PrintableTargets/Target_Alpha.png` o `PrintableTargets/Target_Beta.png` a pantalla completa en otro teléfono o monitor.

> **Consejo de Iluminación:** Evitar reflejos directos en la pantalla o papel. Una iluminación uniforme garantiza un tracking instantáneo y continuo.

---

## 🚀 Guía Paso a Paso para Generar la Build (Android APK)

Dado que tú realizas la compilación final en Unity, sigue estos sencillos pasos:

### Opción A — Desde el Menú Automatizado de Unity (Recomendada)
1. Abre el proyecto en **Unity 6**.
2. En la barra superior de menú de Unity, ve a:
   **`AR Lab` ➔ `★ Compilar APK Android Directo`**
3. Unity compilará automáticamente la escena `ImageTargetLab.unity` y generará el instalable en:
   `Builds/Android/AR_Prototype_UAI.apk`

---

### Opción B — Flujo Estándar de Unity
1. Conecta tu teléfono Android mediante cable USB con la **Depuración USB** habilitada.
2. En Unity, dirígete a **File ➔ Build Profiles** (o **File ➔ Build Settings**).
3. Asegúrate de que la plataforma seleccionada sea **Android** (si no lo está, haz clic en **Switch Platform**).
4. Verifica que la escena **`Assets/Scenes/ImageTargetLab.unity`** esté marcada con el índice **0**.
5. En **Run Device**, selecciona tu teléfono conectado (por ejemplo, *Samsung Galaxy A34 5G* o tu modelo).
6. Haz clic en **Build and Run** (o **Build** para guardar el archivo `.apk` manualmente).
7. Al iniciar la aplicación en el dispositivo:
   - Acepta el permiso de cámara.
   - Apunta la cámara a cualquiera de los dos marcadores.

---

## 🎥 Guión Recomendado para el Video de YouTube

Para la entrega del video demostrativo de funcionamiento real, se sugiere una duración de **1:30 a 2:30 minutos** con la siguiente secuencia:

| Tiempo | Acción en el Video | Elemento a Mostrar / Explicar |
| :---: | :--- | :--- |
| **0:00 - 0:15** | Apertura de la aplicación en el teléfono real. | Concesión de permiso de cámara. Se aprecia la UI inicial con el banner superior: *"Buscando marcadores... Apunta la cámara a Target Alpha o Target Beta"*. |
| **0:15 - 0:45** | Enfocar **Target Alpha** (impreso o en pantalla). | El modelo 3D (*Cyber Core*) aparece instantáneamente sobre el papel. El banner cambia a verde: *"Target Alpha detectado | Seguimiento Activo"*. El contador sube a `1`. |
| **0:45 - 1:05** | Demostración de interacción y controles. | Usar los botones inferiores: pulsar `Giro` (activar/pausar auto-rotación), `🔍 +` (agrandar objeto), y `🎨 Color` para alternar la apariencia del material. |
| **1:05 - 1:25** | Prueba de pérdida y recuperación de tracking (*Limited / None*). | Tapar con la mano el marcador o retirar la cámara. El banner cambia a advertencia (*Seguimiento perdido*) y el objeto se oculta limpiamente. Al destapar o reenfocar, el objeto reaparece de inmediato sin crear clones. |
| **1:25 - 1:55** | Enfocar **Target Beta**. | Se detecta el segundo marcador. Se instancia el *Quantum Beacon* (modelo 3D distinto). El banner confirma *"Target Beta detectado"* y el contador de Beta se incrementa. |
| **1:55 - 2:15** | Demostración de ambos marcadores y panel de ayuda. | Mostrar ambos marcadores en la misma toma. Abrir el modal de ayuda con el botón `?` y cerrarlo. Breve conclusión. |

---

## 🔍 Dificultades Encontradas y Soluciones Técnicas

1. **Versión de AR Foundation 6.x vs 5.x:**
   - *Problema:* Las versiones antiguas de AR Foundation utilizaban el evento C# `trackedImagesChanged += OnTrackedImagesChanged`. En AR Foundation 6.x este delegado es obsoleto o inaccesible.
   - *Solución:* Se implementó `trackablesChanged.AddListener(...)` tipado con `ARTrackablesChangedEventArgs<ARTrackedImage>`, respetando la API recomendada en la cátedra.

2. **Gestión de ciclo de vida del trackable:**
   - *Problema:* Si se instancia un objeto en cada frame de actualización o si se destruye el propio `ARTrackedImage`, el subsistema entra en error y la aplicación parpadea.
   - *Solución:* Los prefabs se instancian una sola vez por `TrackableId` como hijos del transform del trackable (`image.transform`). Solo se conmuta su `SetActive` de acuerdo a `TrackingState.Tracking`, limpiando únicamente en el evento `removed`.

3. **Compatibilidad de niveles de API en Android:**
   - *Problema:* Ciertas plantillas asignan Android API 34 (Android 14) como mínimo, impidiendo la instalación en teléfonos con versiones anteriores (Android 10 a 13).
   - *Solución:* Se ajustó `AndroidMinSdkVersion: 24` (Android 7.0), cumpliendo con el estándar mínimo oficial de ARCore Required sin excluir dispositivos compatibles.

4. **Conflicto de Input System (Both vs New):**
   - *Problema:* Android puede emitir advertencias de despliegue si `Active Input Handling` está configurado en `Both`.
   - *Solución:* Se estableció `Input System Package (New)` de forma exclusiva, garantizando compatibilidad con los componentes táctiles de URP y XR Interaction Toolkit.

---

## 👥 Criterios de Evaluación Cubiertos

- **Configuración técnica y ejecución real (30%):** URP configurado, ARCore Provider activo, IL2CPP ARM64, compatibilidad verificado.
- **Reconocimiento y seguimiento de dos targets (35%):** Dos marcadores físicos distintos con modelos 3D diferenciados instanciados sobre sus poses.
- **Gestión de Tracking / Limited / None (20%):** Control estricto de visibilidad, ausencia de duplicados y transiciones limpias ante oclusión.
- **Organización y explicación de decisiones (15%):** Código modular y documentado, UI responsiva con contadores y controles, repositorio Git ordenado con `.gitignore` profesional y láminas imprimibles.
