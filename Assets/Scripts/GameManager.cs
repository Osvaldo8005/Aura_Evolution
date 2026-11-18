using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controlador principal del juego, del Bucle de Juego (Game Loop) y de la Navegación entre Niveles (Actividad 4.5).
/// Gestiona la Interfaz de Usuario (UI), el Guardado Persistente (PlayerPrefs),
/// la Curva de Dificultad Progresiva, el Reinicio al Perder y la Transición limpia entre Nivel 1 y Nivel 2 con SceneManager.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referencias de UI (TextMeshPro)")]
    [Tooltip("Texto para mostrar el puntaje y el avance en metros del jugador")]
    public TMP_Text textoPuntaje;

    [Tooltip("Texto para mostrar el récord persistente cargado de PlayerPrefs")]
    public TMP_Text textoRecord;

    [Tooltip("Texto para mostrar el nivel de dificultad actual")]
    public TMP_Text textoDificultad;

    [Tooltip("Panel de Game Over que se muestra al perder antes de reiniciar el bucle")]
    public GameObject panelGameOver;

    [Tooltip("Panel que se muestra al cumplir la condición de victoria del nivel actual (Actividad 4.5)")]
    public GameObject panelNivelCompletado;

    [Header("Flujo de Niveles y Transición con SceneManager (Actividad 4.5)")]
    [Tooltip("Número de escena de nivel actual (1 = SampleScene / Nivel 1, 2 = Nivel2)")]
    public int numeroNivelEscena = 1;

    [Tooltip("Puntaje objetivo necesario para completar el nivel actual y avanzar al siguiente")]
    public int puntajeMetaParaCompletar = 500;

    [Tooltip("Tiempo de supervivencia alternativo en segundos para completar el nivel actual")]
    public float tiempoMetaSegundos = 35f;

    [Tooltip("Nombre de la escena del siguiente nivel a cargar con SceneManager")]
    public string escenaSiguienteNivel = "Nivel2";

    [Tooltip("Nombre de la escena del Menú Principal")]
    public string escenaMenuPrincipal = "MenuPrincipal";

    [Tooltip("Tecla rápida para completar el nivel inmediatamente durante pruebas")]
    public KeyCode teclaCompletarNivelRapido = KeyCode.N;

    [Header("Contador y Avance Uniforme")]
    [Tooltip("Puntos ganados de forma continua por segundo")]
    public float puntosPorSegundo = 10f;

    [Tooltip("Puntos ganados por cada metro que el jugador se desplaza")]
    public float puntosPorMetro = 5f;

    [Header("Curva de Dificultad Progresiva")]
    [Tooltip("Segundos necesarios para que suba un nivel de dificultad")]
    public float tiempoPorNivelDificultad = 12f;

    [Tooltip("Metros de avance que también otorgan un incremento de nivel de dificultad")]
    public float distanciaPorNivelDificultad = 90f;

    [Tooltip("Porcentaje de incremento de dificultad por nivel (0.15 = +15%)")]
    public float incrementoDificultadPorNivel = 0.15f;

    [Tooltip("Nivel máximo de dificultad")]
    public int nivelMaximo = 10;

    [Header("Configuración del Game Loop (Reinicio al Perder)")]
    [Tooltip("Si está activo, el bucle de juego se reinicia automáticamente tras perder")]
    public bool reinicioAutomaticoAlPerder = true;

    [Tooltip("Segundos de espera mostrando el aviso de derrota antes de reiniciar automáticamente")]
    [Range(0f, 5f)]
    public float tiempoEsperaReinicio = 1.4f;

    [Tooltip("Si está activo, reinicia el nivel procedural, jugador y puntaje en caliente sin recargar toda la escena")]
    public bool reinicioEnCaliente = true;

    // Constantes para PlayerPrefs
    private const string KEY_RECORD = "Aura_PuntajeMaximo";
    private const string KEY_ULTIMO_PUNTAJE = "Aura_UltimoPuntaje";

    // Variables internas de estado
    private float puntajeAcumuladoFloat = 0f;
    private int puntajeActual = 0;
    private int recordGuardado = 0;
    private float distanciaRecorrida = 0f;
    private float tiempoJugado = 0f;
    private int nivelActual = 1;
    private float multiplicadorDificultad = 1.0f;
    private bool juegoTerminado = false;
    private bool nivelCompletado = false;

    public bool JuegoTerminado => juegoTerminado;

    private PlayerController3D jugador;
    private GeneradorObstaculos spawner;
    private GeneradorNiveles generadorNiveles;
    private GestorVisual gestorVisual;
    private CameraFollow3D camaraFollow;
    private TMP_Text textoDetalleGameOver;
    private TMP_Text textoTituloNivelCompletado;
    private TMP_Text textoDetalleNivelCompletado;
    private TMP_Text textoBotonSiguienteNivel;
    private TMP_Text textoNarrativaProgreso;
    private Coroutine corrutinaReinicio;

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

        ConfigurarParametrosSegunEscenaActiva();
    }

    private void ConfigurarParametrosSegunEscenaActiva()
    {
        string nombreEscena = SceneManager.GetActiveScene().name;
        if (nombreEscena == "Nivel2")
        {
            numeroNivelEscena = 2;
            if (puntajeMetaParaCompletar <= 500) puntajeMetaParaCompletar = 800;
            if (tiempoMetaSegundos <= 35f) tiempoMetaSegundos = 45f;
            escenaSiguienteNivel = "SampleScene";
        }
        else
        {
            numeroNivelEscena = 1;
            if (puntajeMetaParaCompletar <= 0) puntajeMetaParaCompletar = 500;
            if (tiempoMetaSegundos <= 0f) tiempoMetaSegundos = 35f;
            escenaSiguienteNivel = "Nivel2";
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
        Time.timeScale = 1f;
        BotonTactil.ReiniciarEstados();

        jugador = FindAnyObjectByType<PlayerController3D>();
        spawner = FindAnyObjectByType<GeneradorObstaculos>();
        generadorNiveles = FindAnyObjectByType<GeneradorNiveles>();
        camaraFollow = FindAnyObjectByType<CameraFollow3D>();

        // Garantizar que el Generador de Niveles Procedurales exista en la escena
        if (generadorNiveles == null)
        {
            GameObject genObj = new GameObject("GeneradorNiveles");
            generadorNiveles = genObj.AddComponent<GeneradorNiveles>();
        }

        // Garantizar que el Gestor Visual (Actividad 4.4) exista en la escena
        gestorVisual = FindAnyObjectByType<GestorVisual>();
        if (gestorVisual == null)
        {
            GameObject gvObj = new GameObject("GestorVisual");
            gestorVisual = gvObj.AddComponent<GestorVisual>();
        }

        AsegurarElementosUI();
        CargarDatosPlayerPrefs();
        ActualizarTextosUI();

        if (panelGameOver != null)
        {
            panelGameOver.SetActive(false);
        }

        if (panelNivelCompletado != null)
        {
            panelNivelCompletado.SetActive(false);
        }
    }

    private void Update()
    {
        if (juegoTerminado)
        {
            // Permitir reinicio inmediato con tecla R o avanzar al siguiente nivel con Enter/Espacio si completó el nivel
            if (Input.GetKeyDown(KeyCode.R))
            {
                ReiniciarJuego();
            }
            else if (nivelCompletado && Input.GetKeyDown(KeyCode.Return))
            {
                CargarSiguienteNivel();
            }
            return;
        }

        tiempoJugado += Time.deltaTime;

        // 1. INCREMENTO UNIFORME POR TIEMPO Y AVANCE
        puntajeAcumuladoFloat += puntosPorSegundo * Time.deltaTime;
        int nuevoPuntajeEntero = Mathf.FloorToInt(puntajeAcumuladoFloat);

        if (nuevoPuntajeEntero != puntajeActual)
        {
            puntajeActual = nuevoPuntajeEntero;
            if (jugador != null)
            {
                jugador.ActualizarVelocidadPorPuntos(puntajeActual);
            }
            VerificarYGuardarRecord();
        }

        ActualizarTextosUI();

        // 2. CURVA DE DIFICULTAD PROGRESIVA
        ActualizarCurvaDificultad();

        // 3. VERIFICAR CONDICIÓN DE VICTORIA DEL NIVEL (Actividad 4.5)
        if (puntajeActual >= puntajeMetaParaCompletar ||
            tiempoJugado >= tiempoMetaSegundos ||
            Input.GetKeyDown(teclaCompletarNivelRapido))
        {
            CompletarNivelActual();
        }
    }

    /// <summary>
    /// Suma distancia cuando el personaje se desplaza, incrementando el puntaje uniformemente.
    /// </summary>
    public void RegistrarAvance(float distanciaDelta)
    {
        if (juegoTerminado) return;

        distanciaRecorrida += distanciaDelta;
        puntajeAcumuladoFloat += distanciaDelta * puntosPorMetro;
    }

    /// <summary>
    /// Suma puntos directos por interacción (recoger monedas/gemas en las plataformas).
    /// </summary>
    public void SumarPuntos(int cantidad)
    {
        if (juegoTerminado) return;

        puntajeAcumuladoFloat += cantidad;
        puntajeActual = Mathf.FloorToInt(puntajeAcumuladoFloat);
        if (jugador != null)
        {
            jugador.ActualizarVelocidadPorPuntos(puntajeActual);
        }
        VerificarYGuardarRecord();
        ActualizarTextosUI();

        if (puntajeActual >= puntajeMetaParaCompletar)
        {
            CompletarNivelActual();
        }
    }

    /// <summary>
    /// Curva de dificultad progresiva basada en el tiempo jugado y la distancia recorrida.
    /// </summary>
    private void ActualizarCurvaDificultad()
    {
        int nivelesPorTiempo = Mathf.FloorToInt(tiempoJugado / Mathf.Max(1f, tiempoPorNivelDificultad));
        int nivelesPorDistancia = Mathf.FloorToInt(distanciaRecorrida / Mathf.Max(10f, distanciaPorNivelDificultad));
        int nuevoNivel = 1 + Mathf.Max(nivelesPorTiempo, nivelesPorDistancia);
        nuevoNivel = Mathf.Min(nuevoNivel, nivelMaximo);

        if (nuevoNivel != nivelActual)
        {
            nivelActual = nuevoNivel;
            multiplicadorDificultad = 1f + ((nivelActual - 1) * incrementoDificultadPorNivel);

            AplicarDificultadASistemas();
            ActualizarTextosUI();
        }
    }

    private void AplicarDificultadASistemas()
    {
        if (jugador != null)
        {
            jugador.ModificarVelocidadPorDificultad(multiplicadorDificultad);
        }

        if (spawner != null)
        {
            spawner.ActualizarDificultad(multiplicadorDificultad);
        }

        if (generadorNiveles != null)
        {
            generadorNiveles.ActualizarDificultad(nivelActual, multiplicadorDificultad);
        }

        if (gestorVisual == null) gestorVisual = FindAnyObjectByType<GestorVisual>();
        if (gestorVisual != null)
        {
            gestorVisual.ActualizarAspectoPorNivel(nivelActual, false);
        }
    }

    public float ObtenerMultiplicadorDificultad()
    {
        return multiplicadorDificultad;
    }

    /// <summary>
    /// Actualiza los elementos de texto en la interfaz visual (UI).
    /// </summary>
    private void ActualizarTextosUI()
    {
        if (textoPuntaje != null)
        {
            textoPuntaje.text = $"Puntaje: {puntajeActual}/{puntajeMetaParaCompletar}  |  Avance: {distanciaRecorrida:F0}m";
        }

        if (textoRecord != null)
        {
            textoRecord.text = $"Récord: {recordGuardado}";
        }

        if (textoDificultad != null)
        {
            float velActual = (jugador != null) ? jugador.ObtenerVelocidadActual() : 8f;
            if (jugador != null && jugador.PotenciadorActivo)
            {
                textoDificultad.text = $"Nivel {numeroNivelEscena} - Dif. {nivelActual} (Vel: {velActual:F1} | BOOST x{jugador.MultiplicadorBoostActual:F0}: {jugador.TiempoRestanteBoost:F1}s)";
            }
            else
            {
                textoDificultad.text = $"Nivel {numeroNivelEscena} - Dif. {nivelActual} (Vel: {velActual:F1})";
            }
        }

        if (textoNarrativaProgreso != null)
        {
            float segRestantes = Mathf.Max(0f, tiempoMetaSegundos - tiempoJugado);
            if (jugador != null && jugador.PotenciadorActivo)
            {
                textoNarrativaProgreso.color = new Color(1f, 0.92f, 0.15f, 1f);
                textoNarrativaProgreso.text = $"POTENCIADOR DE VELOCIDAD ACTIVO (x{jugador.MultiplicadorBoostActual:F0}) - Restan: {jugador.TiempoRestanteBoost:F1}s / 3.0s";
            }
            else
            {
                textoNarrativaProgreso.color = new Color(0.75f, 0.92f, 1f, 0.95f);
                textoNarrativaProgreso.text = $"ETAPA {numeroNivelEscena}  |  Meta: {puntajeMetaParaCompletar} pts o {segRestantes:F0}s  (Tecla N: Pasar Nivel)";
            }
        }
    }

    #region Guardado de Datos con PlayerPrefs

    public void CargarDatosPlayerPrefs()
    {
        recordGuardado = PlayerPrefs.GetInt(KEY_RECORD, 0);
    }

    private void VerificarYGuardarRecord()
    {
        if (puntajeActual > recordGuardado)
        {
            recordGuardado = puntajeActual;
            PlayerPrefs.SetInt(KEY_RECORD, recordGuardado);
            PlayerPrefs.Save();
        }
    }

    public void GuardarProgresoActual()
    {
        PlayerPrefs.SetInt(KEY_ULTIMO_PUNTAJE, puntajeActual);
        if (puntajeActual > recordGuardado)
        {
            recordGuardado = puntajeActual;
            PlayerPrefs.SetInt(KEY_RECORD, puntajeActual);
        }
        PlayerPrefs.Save();
    }

    [ContextMenu("Borrar Récord (PlayerPrefs)")]
    public void BorrarDatosGuardados()
    {
        PlayerPrefs.DeleteKey(KEY_RECORD);
        PlayerPrefs.DeleteKey(KEY_ULTIMO_PUNTAJE);
        PlayerPrefs.Save();
        recordGuardado = 0;
        ActualizarTextosUI();
        Debug.Log("[GameManager] Datos de PlayerPrefs reiniciados a 0.");
    }

    #endregion

    #region Victoria y Transición entre Niveles con SceneManager (Actividad 4.5)

    /// <summary>
    /// Se ejecuta al alcanzar la meta de puntos o tiempo del nivel actual.
    /// Muestra el panel de Nivel Completado con opciones para avanzar al siguiente nivel o ir al Menú Principal.
    /// </summary>
    public void CompletarNivelActual()
    {
        if (juegoTerminado) return;

        juegoTerminado = true;
        nivelCompletado = true;

        if (corrutinaReinicio != null)
        {
            StopCoroutine(corrutinaReinicio);
            corrutinaReinicio = null;
        }

        if (jugador == null) jugador = FindAnyObjectByType<PlayerController3D>();
        if (jugador != null)
        {
            jugador.DetenerPorMuerte();
        }

        BotonTactil.ReiniciarEstados();
        GuardarProgresoActual();
        ActualizarTextosUI();

        if (panelGameOver != null)
        {
            panelGameOver.SetActive(false);
        }

        if (panelNivelCompletado != null)
        {
            panelNivelCompletado.SetActive(true);
        }

        if (numeroNivelEscena == 1)
        {
            if (textoTituloNivelCompletado != null)
                textoTituloNivelCompletado.text = "¡NIVEL 1 COMPLETADO!";

            if (textoDetalleNivelCompletado != null)
                textoDetalleNivelCompletado.text = $"¡Has despertado el poder de tu Aura!\nPuntaje obtenido: {puntajeActual}  |  Mejor Récord: {recordGuardado}";

            if (textoBotonSiguienteNivel != null)
                textoBotonSiguienteNivel.text = "SIGUIENTE NIVEL (NIVEL 2)";
        }
        else
        {
            if (textoTituloNivelCompletado != null)
                textoTituloNivelCompletado.text = "¡NIVEL 2 COMPLETADO! ¡VICTORIA!";

            if (textoDetalleNivelCompletado != null)
                textoDetalleNivelCompletado.text = $"¡Superaste los 2 niveles consecutivos de Aura Evolution!\nPuntaje Final: {puntajeActual}  |  Mejor Récord: {recordGuardado}";

            if (textoBotonSiguienteNivel != null)
                textoBotonSiguienteNivel.text = "JUGAR DE NUEVO (NIVEL 1)";
        }
    }

    /// <summary>
    /// Cambia limpiamente a la escena del siguiente nivel utilizando SceneManager.LoadScene sin errores.
    /// </summary>
    public void CargarSiguienteNivel()
    {
        if (corrutinaReinicio != null)
        {
            StopCoroutine(corrutinaReinicio);
            corrutinaReinicio = null;
        }

        GuardarProgresoActual();
        BotonTactil.ReiniciarEstados();
        Time.timeScale = 1f;

        string destino = string.IsNullOrEmpty(escenaSiguienteNivel) ? "Nivel2" : escenaSiguienteNivel;
        SceneManager.LoadScene(destino);
    }

    /// <summary>
    /// Regresa limpiamente a la escena del Menú Principal utilizando SceneManager.LoadScene.
    /// </summary>
    public void IrAlMenuPrincipal()
    {
        if (corrutinaReinicio != null)
        {
            StopCoroutine(corrutinaReinicio);
            corrutinaReinicio = null;
        }

        GuardarProgresoActual();
        BotonTactil.ReiniciarEstados();
        Time.timeScale = 1f;

        string destinoMenu = string.IsNullOrEmpty(escenaMenuPrincipal) ? "MenuPrincipal" : escenaMenuPrincipal;
        SceneManager.LoadScene(destinoMenu);
    }

    #endregion

    #region Bucle de Juego (Game Loop: Perder y Reiniciar)

    public void RecibirImpactoObstaculo()
    {
        PerderJuego("Un fragmento oscuro fracturó tu Aura. Elévate de nuevo.");
    }

    public void PerderJuego(string motivo = "Fin de partida")
    {
        if (juegoTerminado) return;

        juegoTerminado = true;
        nivelCompletado = false;

        if (jugador == null) jugador = FindAnyObjectByType<PlayerController3D>();
        if (jugador != null)
        {
            jugador.DetenerPorMuerte();
        }

        BotonTactil.ReiniciarEstados();
        GuardarProgresoActual();
        ActualizarTextosUI();

        if (panelNivelCompletado != null)
        {
            panelNivelCompletado.SetActive(false);
        }

        if (panelGameOver != null)
        {
            panelGameOver.SetActive(true);
        }

        if (reinicioAutomaticoAlPerder)
        {
            if (corrutinaReinicio != null)
            {
                StopCoroutine(corrutinaReinicio);
            }
            corrutinaReinicio = StartCoroutine(CorrutinaReiniciarGameLoop(motivo));
        }
        else if (textoDetalleGameOver != null)
        {
            textoDetalleGameOver.text = $"{motivo}\nPuntaje: {puntajeActual}  |  Récord: {recordGuardado}";
        }
    }

    private IEnumerator CorrutinaReiniciarGameLoop(string motivo)
    {
        float tiempoRestante = tiempoEsperaReinicio;

        while (tiempoRestante > 0f)
        {
            if (textoDetalleGameOver != null)
            {
                textoDetalleGameOver.text = $"{motivo}\nPuntaje Final: {puntajeActual}  |  Mejor Récord: {recordGuardado}\nReiniciando en {tiempoRestante:F1}s...";
            }
            yield return null;
            tiempoRestante -= Time.unscaledDeltaTime;
        }

        ReiniciarJuego();
    }

    /// <summary>
    /// Reinicia el flujo completo del nivel actual (Game Loop).
    /// </summary>
    public void ReiniciarJuego()
    {
        if (corrutinaReinicio != null)
        {
            StopCoroutine(corrutinaReinicio);
            corrutinaReinicio = null;
        }

        GuardarProgresoActual();
        BotonTactil.ReiniciarEstados();
        Time.timeScale = 1f;

        if (!reinicioEnCaliente)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        // 1. Reiniciar puntaje a 0 y dificultad al Nivel 1
        puntajeAcumuladoFloat = 0f;
        puntajeActual = 0;
        distanciaRecorrida = 0f;
        tiempoJugado = 0f;
        nivelActual = 1;
        multiplicadorDificultad = 1.0f;
        nivelCompletado = false;

        // 2. Destruir todas las plataformas anteriores y generar el nivel nuevo desde la posición inicial
        if (generadorNiveles == null) generadorNiveles = FindAnyObjectByType<GeneradorNiveles>();
        if (generadorNiveles != null)
        {
            generadorNiveles.ReiniciarYGenerarNivelInicial();
        }

        // 3. Limpiar obstáculos activos y restablecer su dificultad al Nivel 1
        if (spawner == null) spawner = FindAnyObjectByType<GeneradorObstaculos>();
        if (spawner != null)
        {
            spawner.LimpiarObstaculos();
            spawner.ActualizarDificultad(1.0f);
        }

        // 4. Teletransportar al jugador a la posición inicial segura y reiniciar su velocidad a cero
        if (jugador == null) jugador = FindAnyObjectByType<PlayerController3D>();
        if (jugador != null)
        {
            jugador.ReiniciarJugador();
        }

        // 5. Reposicionar la cámara inmediatamente en el punto de inicio
        if (camaraFollow == null) camaraFollow = FindAnyObjectByType<CameraFollow3D>();
        if (camaraFollow != null)
        {
            camaraFollow.ReposicionarInmediato();
        }

        // 5.5. Restablecer color de fondo, color del personaje e iluminación al Nivel 1 de la escena
        if (gestorVisual == null) gestorVisual = FindAnyObjectByType<GestorVisual>();
        if (gestorVisual != null)
        {
            gestorVisual.ActualizarAspectoPorNivel(1, true);
        }

        // 6. Ocultar paneles de fin de partida, actualizar textos UI y reanudar el bucle de juego
        if (panelGameOver != null)
        {
            panelGameOver.SetActive(false);
        }

        if (panelNivelCompletado != null)
        {
            panelNivelCompletado.SetActive(false);
        }

        ActualizarTextosUI();
        juegoTerminado = false;
    }

    #endregion

    /// <summary>
    /// Garantiza que todos los elementos visuales del HUD, PanelGameOver y PanelNivelCompletado
    /// estén presentes y vinculados correctamente en el Canvas.
    /// </summary>
    private void AsegurarElementosUI()
    {
        GameObject canvasObj = GameObject.Find("Canvas_HUD");
        if (canvasObj == null) return;

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) uiLayer = 5;

        // 1. Asegurar Texto_Record
        if (textoRecord == null)
        {
            Transform trRecord = canvasObj.transform.Find("Texto_Record");
            if (trRecord != null)
            {
                textoRecord = trRecord.GetComponent<TMP_Text>();
            }
            else
            {
                GameObject objRecord = new GameObject("Texto_Record", typeof(RectTransform), typeof(TextMeshProUGUI));
                objRecord.layer = uiLayer;
                objRecord.transform.SetParent(canvasObj.transform, false);

                RectTransform rect = objRecord.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -30f);
                rect.sizeDelta = new Vector2(360f, 60f);

                TextMeshProUGUI tmp = objRecord.GetComponent<TextMeshProUGUI>();
                tmp.fontSize = 26;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color = new Color(1f, 0.88f, 0.25f, 1f);
                tmp.alignment = TextAlignmentOptions.Top;
                textoRecord = tmp;
            }
        }

        // 1.5. Asegurar texto de etapa y meta del nivel en la UI
        Transform trNarrativa = canvasObj.transform.Find("Texto_Narrativa_Aura");
        if (trNarrativa == null)
        {
            GameObject objNarrativa = new GameObject("Texto_Narrativa_Aura", typeof(RectTransform), typeof(TextMeshProUGUI));
            objNarrativa.layer = uiLayer;
            objNarrativa.transform.SetParent(canvasObj.transform, false);

            RectTransform rectN = objNarrativa.GetComponent<RectTransform>();
            rectN.anchorMin = new Vector2(0.5f, 1f);
            rectN.anchorMax = new Vector2(0.5f, 1f);
            rectN.pivot = new Vector2(0.5f, 1f);
            rectN.anchoredPosition = new Vector2(0f, -68f);
            rectN.sizeDelta = new Vector2(820f, 45f);

            TextMeshProUGUI tmpN = objNarrativa.GetComponent<TextMeshProUGUI>();
            tmpN.fontSize = 20;
            tmpN.fontStyle = FontStyles.Italic;
            tmpN.color = new Color(0.75f, 0.92f, 1f, 0.95f);
            tmpN.alignment = TextAlignmentOptions.Top;
            textoNarrativaProgreso = tmpN;
        }
        else
        {
            textoNarrativaProgreso = trNarrativa.GetComponent<TMP_Text>();
        }

        // 2. Asegurar Panel de Game Over con botones de Reintentar y Menú Principal
        Transform existenteGameOver = canvasObj.transform.Find("Panel_GameOver_UI");
        if (existenteGameOver != null)
        {
            panelGameOver = existenteGameOver.gameObject;
            Transform det = panelGameOver.transform.Find("Texto_Detalle_GameOver");
            if (det != null) textoDetalleGameOver = det.GetComponent<TMP_Text>();
        }
        else
        {
            ConstruirPanelGameOverUI(canvasObj.transform, uiLayer);
        }

        // 3. Asegurar Panel de Nivel Completado (Actividad 4.5) con botones de Siguiente Nivel y Menú Principal
        Transform existenteCompletado = canvasObj.transform.Find("Panel_NivelCompletado_UI");
        if (existenteCompletado != null)
        {
            panelNivelCompletado = existenteCompletado.gameObject;
            Transform tit = panelNivelCompletado.transform.Find("Titulo_NivelCompletado");
            if (tit != null) textoTituloNivelCompletado = tit.GetComponent<TMP_Text>();
            Transform det = panelNivelCompletado.transform.Find("Texto_Detalle_NivelCompletado");
            if (det != null) textoDetalleNivelCompletado = det.GetComponent<TMP_Text>();
            Transform btnSig = panelNivelCompletado.transform.Find("Btn_SiguienteNivel/Text");
            if (btnSig != null) textoBotonSiguienteNivel = btnSig.GetComponent<TMP_Text>();
        }
        else
        {
            ConstruirPanelNivelCompletadoUI(canvasObj.transform, uiLayer);
        }
    }

    private void ConstruirPanelGameOverUI(Transform canvasTransform, int uiLayer)
    {
        GameObject panelObj = new GameObject("Panel_GameOver_UI", typeof(RectTransform), typeof(Image));
        panelObj.layer = uiLayer;
        panelObj.transform.SetParent(canvasTransform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(760f, 380f);

        Image bgImg = panelObj.GetComponent<Image>();
        bgImg.color = new Color(0.08f, 0.09f, 0.14f, 0.94f);

        // Título
        GameObject tituloObj = new GameObject("Titulo_GameOver", typeof(RectTransform), typeof(TextMeshProUGUI));
        tituloObj.layer = uiLayer;
        tituloObj.transform.SetParent(panelObj.transform, false);
        RectTransform tRect = tituloObj.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.5f, 1f);
        tRect.anchorMax = new Vector2(0.5f, 1f);
        tRect.pivot = new Vector2(0.5f, 1f);
        tRect.anchoredPosition = new Vector2(0f, -25f);
        tRect.sizeDelta = new Vector2(700f, 65f);

        TextMeshProUGUI tmpTitulo = tituloObj.GetComponent<TextMeshProUGUI>();
        tmpTitulo.text = "¡TU AURA SE HA AGOTADO!";
        tmpTitulo.fontSize = 36;
        tmpTitulo.fontStyle = FontStyles.Bold;
        tmpTitulo.color = new Color(1f, 0.35f, 0.35f, 1f);
        tmpTitulo.alignment = TextAlignmentOptions.Center;

        // Texto Detalle / Cuenta regresiva
        GameObject detalleObj = new GameObject("Texto_Detalle_GameOver", typeof(RectTransform), typeof(TextMeshProUGUI));
        detalleObj.layer = uiLayer;
        detalleObj.transform.SetParent(panelObj.transform, false);
        RectTransform dRect = detalleObj.GetComponent<RectTransform>();
        dRect.anchorMin = new Vector2(0.5f, 0.5f);
        dRect.anchorMax = new Vector2(0.5f, 0.5f);
        dRect.pivot = new Vector2(0.5f, 0.5f);
        dRect.anchoredPosition = new Vector2(0f, 15f);
        dRect.sizeDelta = new Vector2(700f, 130f);

        textoDetalleGameOver = detalleObj.GetComponent<TextMeshProUGUI>();
        textoDetalleGameOver.text = "Reiniciando nivel...";
        textoDetalleGameOver.fontSize = 25;
        textoDetalleGameOver.color = Color.white;
        textoDetalleGameOver.alignment = TextAlignmentOptions.Center;

        // Botón Reintentar Ahora
        CrearBotonPanel(panelObj.transform, "Btn_Reintentar", uiLayer,
            new Vector2(-165f, 28f), new Vector2(290f, 65f),
            "REINTENTAR", new Color(0.18f, 0.65f, 0.35f, 0.95f), ReiniciarJuego);

        // Botón Ir al Menú Principal
        CrearBotonPanel(panelObj.transform, "Btn_MenuDesdeGameOver", uiLayer,
            new Vector2(165f, 28f), new Vector2(290f, 65f),
            "MENÚ PRINCIPAL", new Color(0.20f, 0.45f, 0.82f, 0.95f), IrAlMenuPrincipal);

        panelGameOver = panelObj;
    }

    private void ConstruirPanelNivelCompletadoUI(Transform canvasTransform, int uiLayer)
    {
        GameObject panelObj = new GameObject("Panel_NivelCompletado_UI", typeof(RectTransform), typeof(Image));
        panelObj.layer = uiLayer;
        panelObj.transform.SetParent(canvasTransform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(780f, 390f);

        Image bgImg = panelObj.GetComponent<Image>();
        bgImg.color = new Color(0.07f, 0.14f, 0.22f, 0.95f);

        // Título de Victoria
        GameObject tituloObj = new GameObject("Titulo_NivelCompletado", typeof(RectTransform), typeof(TextMeshProUGUI));
        tituloObj.layer = uiLayer;
        tituloObj.transform.SetParent(panelObj.transform, false);
        RectTransform tRect = tituloObj.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.5f, 1f);
        tRect.anchorMax = new Vector2(0.5f, 1f);
        tRect.pivot = new Vector2(0.5f, 1f);
        tRect.anchoredPosition = new Vector2(0f, -25f);
        tRect.sizeDelta = new Vector2(720f, 65f);

        textoTituloNivelCompletado = tituloObj.GetComponent<TextMeshProUGUI>();
        textoTituloNivelCompletado.text = $"¡NIVEL {numeroNivelEscena} COMPLETADO!";
        textoTituloNivelCompletado.fontSize = 36;
        textoTituloNivelCompletado.fontStyle = FontStyles.Bold;
        textoTituloNivelCompletado.color = new Color(1f, 0.88f, 0.28f, 1f);
        textoTituloNivelCompletado.alignment = TextAlignmentOptions.Center;

        // Texto Detalle
        GameObject detalleObj = new GameObject("Texto_Detalle_NivelCompletado", typeof(RectTransform), typeof(TextMeshProUGUI));
        detalleObj.layer = uiLayer;
        detalleObj.transform.SetParent(panelObj.transform, false);
        RectTransform dRect = detalleObj.GetComponent<RectTransform>();
        dRect.anchorMin = new Vector2(0.5f, 0.5f);
        dRect.anchorMax = new Vector2(0.5f, 0.5f);
        dRect.pivot = new Vector2(0.5f, 0.5f);
        dRect.anchoredPosition = new Vector2(0f, 15f);
        dRect.sizeDelta = new Vector2(720f, 130f);

        textoDetalleNivelCompletado = detalleObj.GetComponent<TextMeshProUGUI>();
        textoDetalleNivelCompletado.text = "¡Meta alcanzada! Avanza al siguiente nivel.";
        textoDetalleNivelCompletado.fontSize = 25;
        textoDetalleNivelCompletado.color = Color.white;
        textoDetalleNivelCompletado.alignment = TextAlignmentOptions.Center;

        // Botón Siguiente Nivel
        GameObject btnSig = CrearBotonPanel(panelObj.transform, "Btn_SiguienteNivel", uiLayer,
            new Vector2(-175f, 28f), new Vector2(330f, 68f),
            "SIGUIENTE NIVEL", new Color(0.16f, 0.68f, 0.38f, 0.98f), CargarSiguienteNivel);

        Transform trTxtSig = btnSig.transform.Find("Text");
        if (trTxtSig != null) textoBotonSiguienteNivel = trTxtSig.GetComponent<TMP_Text>();

        // Botón Menú Principal
        CrearBotonPanel(panelObj.transform, "Btn_MenuPrincipal", uiLayer,
            new Vector2(185f, 28f), new Vector2(290f, 68f),
            "MENÚ PRINCIPAL", new Color(0.20f, 0.45f, 0.82f, 0.95f), IrAlMenuPrincipal);

        panelNivelCompletado = panelObj;
    }

    private GameObject CrearBotonPanel(Transform parent, string nombre, int uiLayer,
        Vector2 anchoredPos, Vector2 sizeDelta, string texto, Color colorFondo, UnityEngine.Events.UnityAction accion)
    {
        GameObject btnObj = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.layer = uiLayer;
        btnObj.transform.SetParent(parent, false);

        RectTransform bRect = btnObj.GetComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0.5f, 0f);
        bRect.anchorMax = new Vector2(0.5f, 0f);
        bRect.pivot = new Vector2(0.5f, 0f);
        bRect.anchoredPosition = anchoredPos;
        bRect.sizeDelta = sizeDelta;

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = colorFondo;

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(accion);

        GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTxtObj.layer = uiLayer;
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        RectTransform btRect = btnTxtObj.GetComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI btnTmp = btnTxtObj.GetComponent<TextMeshProUGUI>();
        btnTmp.text = texto;
        btnTmp.fontSize = 23;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.color = Color.white;
        btnTmp.alignment = TextAlignmentOptions.Center;

        return btnObj;
    }

    private void OnApplicationQuit()
    {
        GuardarProgresoActual();
    }

    private void OnDisable()
    {
        GuardarProgresoActual();
    }
}
