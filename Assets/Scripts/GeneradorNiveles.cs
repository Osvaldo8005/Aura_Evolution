using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generador de niveles procedurales e infinitos en tiempo de ejecución (Actividad 4.3).
/// Cumple con todos los criterios de evaluación:
/// 1. Genera módulos de escenario de forma infinita y continua mediante código en tiempo de ejecución.
/// 2. Permite configurar de forma sencilla todos los parámetros del nivel (distancia entre plataformas,
///    dimensiones, porciones por sección, zonas de peligro DeathPart, velocidad de obstáculos, etc.) desde el Inspector.
/// 3. Se integra con la Curva de Dificultad Progresiva y el Bucle de Juego (Game Loop) de GameManager.
/// </summary>
public class GeneradorNiveles : MonoBehaviour
{
    public static GeneradorNiveles Instance { get; private set; }

    [Header("1. Referencias del Escenario")]
    [Tooltip("Transform del jugador principal (se detecta automáticamente si se deja vacío)")]
    public Transform jugador;

    [Tooltip("Prefabs opcionales de módulos de nivel. Si está vacío, se construyen proceduralmente por código")]
    public GameObject[] prefabsModulosOpcionales;

    [Tooltip("Material base para renderizar las plataformas con iluminación URP")]
    public Material materialPlataforma;

    [Header("2. Generación Procedural Infinita")]
    [Tooltip("Cantidad de módulos del escenario que se mantienen generados simultáneamente por delante")]
    [Range(4, 15)]
    public int modulosSimultaneos = 7;

    [Tooltip("Cantidad de módulos iniciales libres de peligro al comenzar la partida")]
    [Range(1, 5)]
    public int modulosSegurosAlInicio = 2;

    [Tooltip("Distancia delante del jugador a la que se instancian nuevos módulos automáticamente")]
    public float distanciaGeneracionAdelante = 85f;

    [Tooltip("Distancia detrás del jugador a la que se eliminan los módulos ya superados para optimizar memoria")]
    public float distanciaDestruccionAtras = 35f;

    [Header("3. Dimensiones y Distancia entre Plataformas")]
    [Tooltip("Tamaño de cada sección del nivel: Ancho (X), Grosor (Y) y Largo (Z)")]
    public Vector3 dimensionesPlataforma = new Vector3(22f, 1.2f, 22f);

    [Tooltip("Distancia base (hueco en el eje Z) entre plataformas consecutivas en el Nivel 1")]
    [Range(0f, 10f)]
    public float distanciaEntrePlataformasBase = 2.5f;

    [Tooltip("Distancia máxima entre plataformas al subir la curva de dificultad")]
    [Range(1f, 12f)]
    public float distanciaEntrePlataformasMaxima = 5.8f;

    [Tooltip("Desplazamiento lateral máximo (Eje X) entre una plataforma y la siguiente")]
    [Range(0f, 10f)]
    public float desplazamientoLateralMaximoX = 3.5f;

    [Tooltip("Variación vertical máxima (Eje Y) entre plataformas consecutivas")]
    [Range(0f, 2.5f)]
    public float variacionAlturaMaximaY = 0.6f;

    [Header("4. Porciones por Sección y Zonas de Peligro (DeathParts)")]
    [Tooltip("Cantidad de porciones o sub-secciones que componen cada módulo del nivel")]
    [Range(2, 8)]
    public int porcionesPorModulo = 4;

    [Tooltip("Mínimo de porciones de peligro (DeathPart) por módulo en dificultad inicial")]
    [Range(0, 4)]
    public int minZonasPeligroPorModulo = 1;

    [Tooltip("Máximo de porciones de peligro (DeathPart) por módulo en dificultad máxima")]
    [Range(1, 6)]
    public int maxZonasPeligroPorModulo = 3;

    [Tooltip("Color principal de las porciones seguras pares")]
    public Color colorPlataformaPar = new Color(0.22f, 0.30f, 0.42f, 1f);

    [Tooltip("Color secundario de las porciones seguras impares")]
    public Color colorPlataformaImpar = new Color(0.17f, 0.24f, 0.34f, 1f);

    [Tooltip("Color rojo de las Zonas de Peligro (DeathPart)")]
    public Color colorZonaPeligro = new Color(0.90f, 0.16f, 0.16f, 1f);

    [Header("5. Velocidad y Frecuencia de Obstáculos del Nivel")]
    [Tooltip("Probabilidad inicial (0 a 1) de que un módulo incluya un obstáculo en patrulla")]
    [Range(0f, 1f)]
    public float probabilidadObstaculoBase = 0.40f;

    [Tooltip("Probabilidad máxima de obstáculo al aumentar la dificultad")]
    [Range(0f, 1f)]
    public float probabilidadObstaculoMaxima = 0.85f;

    [Tooltip("Velocidad inicial de los obstáculos del nivel")]
    public float velocidadBaseObstaculo = 6.5f;

    [Tooltip("Velocidad máxima de los obstáculos con la curva de dificultad")]
    public float velocidadMaximaObstaculo = 15.0f;

    [Header("6. Coleccionables por Sección")]
    [Tooltip("Probabilidad de generar fila de monedas en cada sección")]
    [Range(0f, 1f)]
    public float probabilidadColeccionables = 0.80f;

    [Tooltip("Cantidad máxima de monedas generadas por sección")]
    [Range(1, 5)]
    public int maxMonedasPorModulo = 3;

    [Tooltip("Puntos otorgados por cada moneda recolectada")]
    public int puntosPorMoneda = 100;

    [Header("Estado en Tiempo de Ejecución (Solo Lectura)")]
    [SerializeField] private int totalModulosGenerados = 0;
    [SerializeField] private float distanciaActualEntrePlataformas;
    [SerializeField] private float velocidadActualObstaculos;
    [SerializeField] private int zonasPeligroActuales;

    private readonly List<ModuloNivel> modulosActivos = new List<ModuloNivel>();
    private float siguientePosicionZ = -6f;
    private float ultimaPosicionX = 0f;
    private float ultimaAlturaY = 0f;
    private int nivelDificultadActual = 1;
    private float multiplicadorDificultadActual = 1f;

    private void Awake()
    {
        Instance = this;
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
        InicializarReferenciasYMaterial();
        PrepararEscenarioParaNivelProcedural();
        ReiniciarYGenerarNivelInicial();
    }

    private void Update()
    {
        if (jugador == null) return;
        if (GameManager.Instance != null && GameManager.Instance.JuegoTerminado) return;

        // 1. GENERACIÓN INFINITA ADELANTE DEL JUGADOR
        while (siguientePosicionZ < jugador.position.z + distanciaGeneracionAdelante || modulosActivos.Count < modulosSimultaneos)
        {
            GenerarSiguienteModulo();
        }

        // 2. RECICLAJE / DESTRUCCIÓN DE MÓDULOS SUPERADOS DETRÁS DEL JUGADOR
        LimpiarModulosAntiguos();
    }

    private void InicializarReferenciasYMaterial()
    {
        if (jugador == null)
        {
            PlayerController3D pc = FindAnyObjectByType<PlayerController3D>();
            if (pc != null)
            {
                jugador = pc.transform;
            }
            else
            {
                GameObject objJugador = GameObject.FindWithTag("Player");
                if (objJugador != null) jugador = objJugador.transform;
            }
        }

        // Extraer el material URP del Suelo original si no se asignó uno manualmente
        if (materialPlataforma == null)
        {
            GameObject sueloOriginal = GameObject.Find("Suelo");
            if (sueloOriginal != null)
            {
                Renderer ren = sueloOriginal.GetComponent<Renderer>();
                if (ren != null && ren.sharedMaterial != null)
                {
                    materialPlataforma = ren.sharedMaterial;
                }
            }
        }
    }

    /// <summary>
    /// Desactiva el plano estático antiguo y las paredes cerradas para permitir
    /// el avance infinito a través de las plataformas generadas proceduralmente.
    /// </summary>
    private void PrepararEscenarioParaNivelProcedural()
    {
        GameObject sueloEstatico = GameObject.Find("Suelo");
        if (sueloEstatico != null && sueloEstatico.activeSelf)
        {
            sueloEstatico.SetActive(false);
        }

        GameObject limitesCerrados = GameObject.Find("LimitesDelSuelo");
        if (limitesCerrados != null && limitesCerrados.activeSelf)
        {
            limitesCerrados.SetActive(false);
        }

        GameObject coleccionablesEstaticos = GameObject.Find("Coleccionables_Grupo");
        if (coleccionablesEstaticos != null && coleccionablesEstaticos.activeSelf)
        {
            coleccionablesEstaticos.SetActive(false);
        }
    }

    /// <summary>
    /// Destruye todas las plataformas existentes y genera un nivel completamente nuevo desde la posición inicial (X=0, Y=0, Z=-6).
    /// Se invoca al iniciar la partida y al reiniciar el Game Loop tras perder.
    /// </summary>
    [ContextMenu("Regenerar Nivel Procedural Ahora")]
    public void ReiniciarYGenerarNivelInicial()
    {
        // 1. Destruir todas las plataformas registradas en la lista activa
        for (int i = modulosActivos.Count - 1; i >= 0; i--)
        {
            if (modulosActivos[i] != null)
            {
                modulosActivos[i].gameObject.SetActive(false);
                Destroy(modulosActivos[i].gameObject);
            }
        }
        modulosActivos.Clear();

        // Asegurar limpieza total de cualquier hijo remanente del contenedor
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform hijo = transform.GetChild(i);
            if (hijo != null)
            {
                hijo.gameObject.SetActive(false);
                Destroy(hijo.gameObject);
            }
        }

        // 2. Reiniciar coordenadas de generación exactamente al punto de partida
        totalModulosGenerados = 0;
        siguientePosicionZ = -6f;
        ultimaPosicionX = 0f;
        ultimaAlturaY = 0f;

        // 3. Restablecer parámetros de dificultad a Nivel 1
        ActualizarDificultad(1, 1f);

        // 4. Generar el lote inicial de módulos desde el origen
        for (int i = 0; i < modulosSimultaneos; i++)
        {
            GenerarSiguienteModulo();
        }

        // Sincronizar colisionadores con el motor de físicas inmediatamente
        Physics.SyncTransforms();
    }

    /// <summary>
    /// Instancia y configura un nuevo módulo del nivel en tiempo de ejecución.
    /// </summary>
    private void GenerarSiguienteModulo()
    {
        int indice = totalModulosGenerados;
        bool esSeguro = indice < modulosSegurosAlInicio;

        // Calcular desplazamiento lateral (X) y altura (Y) de forma orgánica y alcanzable
        float posX = 0f;
        float posY = 0f;

        if (!esSeguro)
        {
            float factorProgreso = Mathf.Clamp01((nivelDificultadActual - 1) / 8f);
            float rangoLateralActual = Mathf.Lerp(desplazamientoLateralMaximoX * 0.4f, desplazamientoLateralMaximoX, factorProgreso);
            posX = Mathf.Clamp(ultimaPosicionX + Random.Range(-rangoLateralActual, rangoLateralActual), -desplazamientoLateralMaximoX, desplazamientoLateralMaximoX);

            if (variacionAlturaMaximaY > 0.01f)
            {
                float deltaY = Random.Range(-variacionAlturaMaximaY, variacionAlturaMaximaY);
                posY = Mathf.Clamp(ultimaAlturaY + deltaY, -1.0f, 2.0f);
            }
        }

        Vector3 posicionSpawn = new Vector3(posX, posY, siguientePosicionZ);

        GameObject objModulo;
        if (prefabsModulosOpcionales != null && prefabsModulosOpcionales.Length > 0)
        {
            GameObject prefabElegido = prefabsModulosOpcionales[Random.Range(0, prefabsModulosOpcionales.Length)];
            objModulo = Instantiate(prefabElegido, posicionSpawn, Quaternion.identity, transform);
            objModulo.name = $"Modulo_Nivel_{indice + 1}";
        }
        else
        {
            objModulo = new GameObject($"Modulo_Nivel_{indice + 1}");
            objModulo.transform.SetParent(transform, false);
            objModulo.transform.position = posicionSpawn;
        }

        ModuloNivel modulo = objModulo.GetComponent<ModuloNivel>();
        if (modulo == null)
        {
            modulo = objModulo.AddComponent<ModuloNivel>();
        }

        float probObstaculo = Mathf.Lerp(
            probabilidadObstaculoBase,
            probabilidadObstaculoMaxima,
            Mathf.Clamp01((multiplicadorDificultadActual - 1f) / 1.35f)
        );

        modulo.ConfigurarSeccion(
            indice,
            dimensionesPlataforma,
            porcionesPorModulo,
            zonasPeligroActuales,
            esSeguro,
            velocidadActualObstaculos,
            probObstaculo,
            probabilidadColeccionables,
            maxMonedasPorModulo,
            puntosPorMoneda,
            materialPlataforma,
            colorPlataformaPar,
            colorPlataformaImpar,
            colorZonaPeligro
        );

        modulosActivos.Add(modulo);
        totalModulosGenerados++;

        // Calcular la posición Z donde comenzará la siguiente plataforma incluyendo la distancia entre plataformas
        float huecoZ = esSeguro ? (distanciaEntrePlataformasBase * 0.5f) : Random.Range(distanciaEntrePlataformasBase, distanciaActualEntrePlataformas);
        siguientePosicionZ += dimensionesPlataforma.z + huecoZ;
        ultimaPosicionX = posX;
        ultimaAlturaY = posY;
    }

    private void LimpiarModulosAntiguos()
    {
        if (modulosActivos.Count <= modulosSimultaneos) return;

        ModuloNivel masAntiguo = modulosActivos[0];
        if (masAntiguo != null && jugador != null)
        {
            if (masAntiguo.FinZ < jugador.position.z - distanciaDestruccionAtras)
            {
                Destroy(masAntiguo.gameObject);
                modulosActivos.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// Ajusta dinámicamente los parámetros de generación según la Curva de Dificultad Progresiva.
    /// A mayor nivel de dificultad: mayor distancia entre plataformas, más porciones de peligro (DeathParts)
    /// y mayor velocidad de obstáculos.
    /// </summary>
    public void ActualizarDificultad(int nuevoNivel, float multiplicador)
    {
        nivelDificultadActual = Mathf.Max(1, nuevoNivel);
        multiplicadorDificultadActual = Mathf.Max(1f, multiplicador);

        // 1. Escalar distancia entre plataformas respetando el máximo configurado en el Inspector
        distanciaActualEntrePlataformas = Mathf.Min(
            distanciaEntrePlataformasMaxima,
            distanciaEntrePlataformasBase * multiplicadorDificultadActual
        );

        // 2. Escalar velocidad de obstáculos en cada módulo
        velocidadActualObstaculos = Mathf.Min(
            velocidadMaximaObstaculo,
            velocidadBaseObstaculo * multiplicadorDificultadActual
        );

        // 3. Incrementar cantidad de Zonas de Peligro (DeathParts) por módulo conforme sube el nivel
        int incrementoPeligro = (nivelDificultadActual - 1) / 2;
        zonasPeligroActuales = Mathf.Clamp(
            minZonasPeligroPorModulo + incrementoPeligro,
            minZonasPeligroPorModulo,
            Mathf.Min(maxZonasPeligroPorModulo, porcionesPorModulo - 1)
        );
    }

    public int ObtenerTotalModulosGenerados()
    {
        return totalModulosGenerados;
    }
}
