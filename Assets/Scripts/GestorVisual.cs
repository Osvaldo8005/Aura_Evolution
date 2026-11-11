using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Gestiona la iluminación del escenario y las mejoras visuales dinámicas (Actividad 4.4).
/// Cumple con los criterios de evaluación:
/// 1. Modifica de manera dinámica y lógica el color de fondo del entorno de juego según el nivel de dificultad.
/// 2. Permite alterar y personalizar el color y las propiedades visuales del personaje principal manteniendo su nitidez y contraste.
/// 3. Implementa una gestión de iluminación adecuada (Directional Light, ambiente RenderSettings, niebla lineal y Global Volume)
///    para evitar sobreexposición y lograr que el caballero luzca nítido tanto en fondos claros como oscuros.
/// </summary>
public class GestorVisual : MonoBehaviour
{
    public static GestorVisual Instance { get; private set; }

    [Header("1. Fondo Dinámico del Entorno")]
    [Tooltip("Cámara principal cuyo color de fondo cambia dinámicamente (se detecta automáticamente si está vacío)")]
    public Camera camaraPrincipal;

    [Tooltip("Velocidad de transición suave al cambiar el color de fondo y de iluminación entre niveles")]
    [Range(0.5f, 10f)]
    public float velocidadTransicionColor = 2.5f;

    [Tooltip("Paleta de colores de fondo que cambian dinámicamente cada vez que sube el nivel de dificultad")]
    public Color[] coloresFondoPorNivel = new Color[]
    {
        new Color(0.14f, 0.21f, 0.34f, 1f), // Nivel 1: Azul Acero Profundo
        new Color(0.10f, 0.28f, 0.36f, 1f), // Nivel 2: Cian Oceánico
        new Color(0.12f, 0.29f, 0.24f, 1f), // Nivel 3: Verde Bosque Esmeralda
        new Color(0.32f, 0.22f, 0.14f, 1f), // Nivel 4: Ámbar Terracota
        new Color(0.24f, 0.15f, 0.34f, 1f), // Nivel 5: Púrpura Imperial
        new Color(0.34f, 0.14f, 0.20f, 1f), // Nivel 6: Carmesí Oscuro
        new Color(0.18f, 0.24f, 0.32f, 1f)  // Nivel 7+: Azul Pizarra Estelar
    };

    [Header("2. Color y Propiedades Visuales del Personaje (Sin Sobreexposición)")]
    [Tooltip("Objeto raíz del jugador principal (se busca automáticamente si no se asigna)")]
    public Transform jugador;

    [Tooltip("Colores de personalización del caballero según el nivel (calibrados para no quemar la textura original)")]
    public Color[] coloresAuraPersonaje = new Color[]
    {
        new Color(0.92f, 0.96f, 1.00f, 1f), // Nivel 1: Armadura Natural / Plata Celeste
        new Color(0.62f, 0.90f, 1.00f, 1f), // Nivel 2: Aura Cian Templada
        new Color(0.65f, 0.95f, 0.72f, 1f), // Nivel 3: Aura Esmeralda
        new Color(1.00f, 0.85f, 0.52f, 1f), // Nivel 4: Aura Dorada Solar
        new Color(0.84f, 0.68f, 1.00f, 1f), // Nivel 5: Aura Amatista
        new Color(1.00f, 0.62f, 0.62f, 1f), // Nivel 6: Aura Carmesí
        new Color(0.75f, 0.92f, 0.98f, 1f)  // Nivel 7+: Aura Diamante
    };

    [Tooltip("Intensidad del tinte de color sobre la textura original del caballero (0 = textura original pura, 1 = tinte completo)")]
    [Range(0.2f, 1f)]
    public float intensidadTintePersonaje = 0.75f;

    [Tooltip("Propiedad visual: nivel de acabado metálico (Metallic) moderado para conservar los colores reales de la textura")]
    [Range(0f, 1f)]
    public float acabadoMetalicoPersonaje = 0.15f;

    [Tooltip("Propiedad visual: nivel de suavidad (Smoothness) controlado para evitar reflejos blancos excesivos")]
    [Range(0f, 1f)]
    public float suavidadMaterialPersonaje = 0.25f;

    [Tooltip("Activar luz puntual suave (Luz_Aura_Caballero) con baja intensidad para no quemar la textura")]
    public bool activarLuzDeAuraEnPersonaje = true;

    [Tooltip("Intensidad baja de la Point Light del caballero para iluminar sutilmente sin sobreexponer")]
    [Range(0f, 1.2f)]
    public float intensidadLuzAura = 0.32f;

    [Tooltip("Tecla opcional para cambiar y personalizar manualmente el color del personaje y fondo en cualquier momento")]
    public KeyCode teclaCambiarAspectoManual = KeyCode.C;

    [Header("3. Gestión de Iluminación, Ambiente y Global Volume")]
    [Tooltip("Luz direccional principal de la escena (se detecta automáticamente si está vacío)")]
    public Light luzDireccional;

    [Tooltip("Intensidad balanceada de la Directional Light para que el personaje se vea nítido y no sobreexpuesto")]
    [Range(0.5f, 2.5f)]
    public float intensidadLuzDireccional = 1.05f;

    [Tooltip("Ángulo de rotación de la Directional Light para dar buen contraste y volumen 3D al caballero")]
    public Vector3 rotacionLuzDireccional = new Vector3(45f, -35f, 0f);

    [Tooltip("Color neutro cálido de la luz direccional principal")]
    public Color colorLuzDireccionalBase = new Color(0.98f, 0.95f, 0.90f, 1f);

    [Tooltip("Intensidad de la iluminación ambiental controlada para mantener sombras con buen contraste")]
    [Range(0.2f, 1.5f)]
    public float intensidadAmbiental = 0.75f;

    [Tooltip("Activar niebla lineal al fondo del escenario (empieza lejos de la cámara para no empañar al personaje)")]
    public bool activarNieblaAtmosferica = true;

    [Tooltip("Distancia donde empieza la niebla (detrás del jugador para que el caballero se vea 100% nítido)")]
    public float inicioNieblaLineal = 38f;

    [Tooltip("Distancia donde la niebla cubre el horizonte lejano")]
    public float finNieblaLineal = 135f;

    [Tooltip("Exposición en el Global Volume de URP (valores ligeramente negativos evitan que el personaje se vea blancoso)")]
    [Range(-1f, 0.5f)]
    public float exposicionGlobalVolume = -0.15f;

    [Tooltip("Contraste adicional en el Global Volume para resaltar los colores reales del caballero")]
    [Range(0f, 25f)]
    public float contrasteGlobalVolume = 10f;

    // Estado interno para transiciones suaves
    private Color colorFondoObjetivo;
    private Color colorAuraObjetivo;
    private Color colorLuzObjetivo;
    private Color colorAuraActual;
    private Renderer[] renderersPersonaje;
    private Light luzAuraPunto;
    private int indicePersonalizacionManual = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        AsegurarPaletasDeColores();
    }

    private void AsegurarPaletasDeColores()
    {
        if (coloresFondoPorNivel == null || coloresFondoPorNivel.Length == 0)
        {
            coloresFondoPorNivel = new Color[]
            {
                new Color(0.14f, 0.21f, 0.34f, 1f),
                new Color(0.10f, 0.28f, 0.36f, 1f),
                new Color(0.12f, 0.29f, 0.24f, 1f),
                new Color(0.32f, 0.22f, 0.14f, 1f),
                new Color(0.24f, 0.15f, 0.34f, 1f),
                new Color(0.34f, 0.14f, 0.20f, 1f),
                new Color(0.18f, 0.24f, 0.32f, 1f)
            };
        }

        if (coloresAuraPersonaje == null || coloresAuraPersonaje.Length == 0)
        {
            coloresAuraPersonaje = new Color[]
            {
                new Color(0.92f, 0.96f, 1.00f, 1f),
                new Color(0.62f, 0.90f, 1.00f, 1f),
                new Color(0.65f, 0.95f, 0.72f, 1f),
                new Color(1.00f, 0.85f, 0.52f, 1f),
                new Color(0.84f, 0.68f, 1.00f, 1f),
                new Color(1.00f, 0.62f, 0.62f, 1f),
                new Color(0.75f, 0.92f, 0.98f, 1f)
            };
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        InicializarReferencias();
        ConfigurarIluminacionBase();
        ConfigurarGlobalVolumeURP();
        ActualizarAspectoPorNivel(1, true);
    }

    private void Update()
    {
        // Permitir personalizar/alternar el color manualmente con la tecla C para pruebas rápidas
        if (Input.GetKeyDown(teclaCambiarAspectoManual))
        {
            SiguienteEstiloVisualManual();
        }

        // Transición suave y continua del color de fondo de la cámara y la niebla lejana
        if (camaraPrincipal != null)
        {
            camaraPrincipal.backgroundColor = Color.Lerp(
                camaraPrincipal.backgroundColor,
                colorFondoObjetivo,
                velocidadTransicionColor * Time.deltaTime
            );

            if (activarNieblaAtmosferica)
            {
                RenderSettings.fogColor = camaraPrincipal.backgroundColor;
            }

            ActualizarColoresAmbientales(camaraPrincipal.backgroundColor);
        }

        // Transición suave del color de la luz direccional manteniendo intensidad balanceada
        if (luzDireccional != null)
        {
            luzDireccional.color = Color.Lerp(
                luzDireccional.color,
                colorLuzObjetivo,
                velocidadTransicionColor * Time.deltaTime
            );
            luzDireccional.intensity = intensidadLuzDireccional;
        }

        // Transición suave del color y propiedades del personaje principal
        colorAuraActual = Color.Lerp(colorAuraActual, colorAuraObjetivo, velocidadTransicionColor * Time.deltaTime);
        AplicarColorYPropiedadesAlMaterialPersonaje(colorAuraActual);
    }

    /// <summary>
    /// Localiza la cámara principal, la Directional Light y los Renderers del caballero.
    /// </summary>
    private void InicializarReferencias()
    {
        if (camaraPrincipal == null)
        {
            camaraPrincipal = Camera.main;
            if (camaraPrincipal == null)
            {
                camaraPrincipal = FindAnyObjectByType<Camera>();
            }
        }

        if (camaraPrincipal != null)
        {
            camaraPrincipal.clearFlags = CameraClearFlags.SolidColor;
        }

        if (luzDireccional == null)
        {
            Light[] luces = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in luces)
            {
                if (l.type == LightType.Directional)
                {
                    luzDireccional = l;
                    break;
                }
            }
        }

        if (jugador == null)
        {
            PlayerController3D pc = FindAnyObjectByType<PlayerController3D>();
            if (pc != null)
            {
                jugador = pc.transform;
            }
            else
            {
                GameObject objJugador = GameObject.Find("Jugador");
                if (objJugador != null) jugador = objJugador.transform;
            }
        }

        if (jugador != null)
        {
            renderersPersonaje = jugador.GetComponentsInChildren<Renderer>(true);
            ConfigurarLuzDeAuraEnPersonaje();
        }
    }

    /// <summary>
    /// Configura los parámetros de la Directional Light, iluminación ambiental y niebla lineal
    /// para garantizar alto contraste y que el personaje nunca se vea empañado ni sobreexpuesto.
    /// </summary>
    public void ConfigurarIluminacionBase()
    {
        if (luzDireccional != null)
        {
            luzDireccional.transform.rotation = Quaternion.Euler(rotacionLuzDireccional);
            luzDireccional.intensity = intensidadLuzDireccional;
            luzDireccional.color = colorLuzDireccionalBase;
            luzDireccional.shadows = LightShadows.Soft;
            luzDireccional.shadowStrength = 0.82f;
        }

        // Iluminación ambiental Trilight controlada para que las sombras del caballero tengan volumen real
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientIntensity = intensidadAmbiental;

        // Niebla Lineal que inicia a 38m: así entre la cámara y el personaje (8m) hay 0% de niebla
        // y el caballero se ve completamente nítido con sus colores reales
        RenderSettings.fog = activarNieblaAtmosferica;
        if (activarNieblaAtmosferica)
        {
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = inicioNieblaLineal;
            RenderSettings.fogEndDistance = finNieblaLineal;
        }
    }

    /// <summary>
    /// Ajusta la exposición, el contraste y el Bloom en el Global Volume de URP para evitar sobreexposición.
    /// </summary>
    private void ConfigurarGlobalVolumeURP()
    {
        Volume volumenGlobal = FindAnyObjectByType<Volume>();
        if (volumenGlobal == null || volumenGlobal.profile == null) return;

        // Reducir Bloom para que no blanquee la armadura del caballero en fondos oscuros
        if (volumenGlobal.profile.TryGet(out Bloom bloom))
        {
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 1.45f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.10f;
        }

        // Ajustar exposición y contraste para que los colores del caballero se vean vivos y definidos
        if (!volumenGlobal.profile.TryGet(out ColorAdjustments ajustesColor))
        {
            ajustesColor = volumenGlobal.profile.Add<ColorAdjustments>(true);
        }

        if (ajustesColor != null)
        {
            ajustesColor.active = true;
            ajustesColor.postExposure.overrideState = true;
            ajustesColor.postExposure.value = exposicionGlobalVolume;
            ajustesColor.contrast.overrideState = true;
            ajustesColor.contrast.value = contrasteGlobalVolume;
            ajustesColor.saturation.overrideState = true;
            ajustesColor.saturation.value = 6f;
        }
    }

    /// <summary>
    /// Configura una luz puntual muy suave en el personaje para no quemar su textura.
    /// </summary>
    private void ConfigurarLuzDeAuraEnPersonaje()
    {
        if (jugador == null) return;

        Transform trLuz = jugador.Find("Luz_Aura_Caballero");
        if (trLuz == null)
        {
            GameObject objLuz = new GameObject("Luz_Aura_Caballero");
            objLuz.transform.SetParent(jugador, false);
            objLuz.transform.localPosition = new Vector3(0f, 0.1f, 0.1f);
            luzAuraPunto = objLuz.AddComponent<Light>();
        }
        else
        {
            luzAuraPunto = trLuz.GetComponent<Light>();
            if (luzAuraPunto == null) luzAuraPunto = trLuz.gameObject.AddComponent<Light>();
        }

        luzAuraPunto.type = LightType.Point;
        luzAuraPunto.range = 3.5f;
        luzAuraPunto.intensity = intensidadLuzAura;
        luzAuraPunto.shadows = LightShadows.None;
        luzAuraPunto.enabled = activarLuzDeAuraEnPersonaje;
    }

    /// <summary>
    /// Actualiza dinámicamente el color de fondo, el color del personaje y la iluminación según el nivel actual.
    /// </summary>
    public void ActualizarAspectoPorNivel(int nivelDificultad, bool aplicarInmediato = false)
    {
        int indice = Mathf.Max(0, nivelDificultad - 1);
        indicePersonalizacionManual = indice;

        if (coloresFondoPorNivel != null && coloresFondoPorNivel.Length > 0)
        {
            colorFondoObjetivo = coloresFondoPorNivel[indice % coloresFondoPorNivel.Length];
        }

        if (coloresAuraPersonaje != null && coloresAuraPersonaje.Length > 0)
        {
            colorAuraObjetivo = coloresAuraPersonaje[indice % coloresAuraPersonaje.Length];
        }

        // Mantener la luz direccional limpia y equilibrada sin saturar el blanco
        colorLuzObjetivo = Color.Lerp(colorLuzDireccionalBase, colorAuraObjetivo, 0.12f);

        if (aplicarInmediato)
        {
            if (camaraPrincipal != null)
            {
                camaraPrincipal.clearFlags = CameraClearFlags.SolidColor;
                camaraPrincipal.backgroundColor = colorFondoObjetivo;
            }

            if (activarNieblaAtmosferica)
            {
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = inicioNieblaLineal;
                RenderSettings.fogEndDistance = finNieblaLineal;
                RenderSettings.fogColor = colorFondoObjetivo;
            }

            ActualizarColoresAmbientales(colorFondoObjetivo);

            if (luzDireccional != null)
            {
                luzDireccional.color = colorLuzObjetivo;
                luzDireccional.intensity = intensidadLuzDireccional;
            }

            colorAuraActual = colorAuraObjetivo;
            AplicarColorYPropiedadesAlMaterialPersonaje(colorAuraActual);
        }
    }

    /// <summary>
    /// Permite cambiar manualmente al siguiente estilo visual de fondo y color del personaje (Tecla C o Inspector).
    /// </summary>
    [ContextMenu("Probar Siguiente Color de Fondo y Personaje")]
    public void SiguienteEstiloVisualManual()
    {
        indicePersonalizacionManual++;
        ActualizarAspectoPorNivel(indicePersonalizacionManual + 1, false);
    }

    /// <summary>
    /// Modifica el tinte de color y propiedades de superficie en el material del caballero
    /// desactivando la emisión plana para conservar el 100% de la nitidez y contraste de su textura original.
    /// </summary>
    private void AplicarColorYPropiedadesAlMaterialPersonaje(Color colorAura)
    {
        if (jugador == null) return;
        if (renderersPersonaje == null || renderersPersonaje.Length == 0)
        {
            renderersPersonaje = jugador.GetComponentsInChildren<Renderer>(true);
        }

        // Mezclar con blanco neutro según intensidadTintePersonaje y limitar el brillo máximo a 0.95
        // para que nunca sobreexponga la textura BaseMap del caballero
        Color tinteAplicado = Color.Lerp(new Color(0.90f, 0.90f, 0.92f, 1f), colorAura, intensidadTintePersonaje);
        tinteAplicado.r = Mathf.Clamp(tinteAplicado.r, 0.25f, 0.95f);
        tinteAplicado.g = Mathf.Clamp(tinteAplicado.g, 0.25f, 0.95f);
        tinteAplicado.b = Mathf.Clamp(tinteAplicado.b, 0.25f, 0.95f);
        tinteAplicado.a = 1f;

        for (int i = 0; i < renderersPersonaje.Length; i++)
        {
            Renderer ren = renderersPersonaje[i];
            if (ren == null || !ren.enabled) continue;

            Material mat = ren.material;
            if (mat == null) continue;

            mat.color = tinteAplicado;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", tinteAplicado);
            }

            if (mat.HasProperty("_Metallic"))
            {
                mat.SetFloat("_Metallic", acabadoMetalicoPersonaje);
            }
            if (mat.HasProperty("_Smoothness"))
            {
                mat.SetFloat("_Smoothness", suavidadMaterialPersonaje);
            }

            // Desactivar emisión plana sobre la textura para que no blanquee ni lave los detalles del caballero
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", Color.black);
                mat.DisableKeyword("_EMISSION");
            }
        }

        if (luzAuraPunto != null)
        {
            luzAuraPunto.enabled = activarLuzDeAuraEnPersonaje;
            luzAuraPunto.color = colorAura;
            luzAuraPunto.intensity = intensidadLuzAura;
        }
    }

    /// <summary>
    /// Ajusta la iluminación ambiental con niveles equilibrados para que el personaje tenga buen contraste
    /// tanto cuando el fondo es oscuro como cuando el fondo es más claro.
    /// </summary>
    private void ActualizarColoresAmbientales(Color colorFondo)
    {
        RenderSettings.ambientIntensity = intensidadAmbiental;

        // Mantener la luz ambiental en un rango medio estable (ni muy brillante ni completamente negra)
        Color cieloBase = new Color(0.38f, 0.42f, 0.48f, 1f);
        Color ecuadorBase = new Color(0.26f, 0.29f, 0.34f, 1f);
        Color sueloBase = new Color(0.14f, 0.16f, 0.20f, 1f);

        RenderSettings.ambientSkyColor = Color.Lerp(cieloBase, colorFondo, 0.25f);
        RenderSettings.ambientEquatorColor = Color.Lerp(ecuadorBase, colorFondo, 0.25f);
        RenderSettings.ambientGroundColor = Color.Lerp(sueloBase, colorFondo * 0.5f, 0.25f);
    }
}
