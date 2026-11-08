using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controlador principal del juego y del Bucle de Juego (Game Loop).
/// Gestiona la Interfaz de Usuario (UI), el Guardado Persistente (PlayerPrefs),
/// la Curva de Dificultad Progresiva y el Reinicio del Flujo al Perder (Actividad 4.3).
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
    public float tiempoEsperaReinicio = 1.2f;

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

    public bool JuegoTerminado => juegoTerminado;

    private PlayerController3D jugador;
    private GeneradorObstaculos spawner;
    private GeneradorNiveles generadorNiveles;
    private CameraFollow3D camaraFollow;
    private TMP_Text textoDetalleGameOver;
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

        AsegurarElementosUI();
        CargarDatosPlayerPrefs();
        ActualizarTextosUI();

        if (panelGameOver != null)
        {
            panelGameOver.SetActive(false);
        }
    }

    private void Update()
    {
        if (juegoTerminado)
        {
            // Permitir reinicio inmediato con tecla R durante la pantalla de derrota
            if (Input.GetKeyDown(KeyCode.R))
            {
                ReiniciarJuego();
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
            VerificarYGuardarRecord();
        }

        ActualizarTextosUI();

        // 2. CURVA DE DIFICULTAD PROGRESIVA
        ActualizarCurvaDificultad();
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
        VerificarYGuardarRecord();
        ActualizarTextosUI();
    }

    /// <summary>
    /// Curva de dificultad progresiva basada en el tiempo jugado y la distancia recorrida.
    /// Incrementa la velocidad del jugador, la generación de obstáculos y la complejidad de los niveles procedurales.
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
            textoPuntaje.text = $"Puntaje: {puntajeActual}  |  Avance: {distanciaRecorrida:F0}m";
        }

        if (textoRecord != null)
        {
            textoRecord.text = $"Récord: {recordGuardado}";
        }

        if (textoDificultad != null)
        {
            float velActual = (jugador != null) ? jugador.ObtenerVelocidadActual() : 8f;
            textoDificultad.text = $"Dificultad: Nivel {nivelActual} (Vel: {velActual:F1})";
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

    #region Bucle de Juego (Game Loop: Perder y Reiniciar)

    /// <summary>
    /// Maneja el impacto con obstáculos activando el flujo de fin de partida y reinicio del Game Loop.
    /// </summary>
    public void RecibirImpactoObstaculo()
    {
        PerderJuego("Un fragmento oscuro fracturó tu Aura. Elévate de nuevo.");
    }

    /// <summary>
    /// Ejecuta la lógica de derrota (por obstáculo, DeathPart o caída al vacío),
    /// congela la caída del jugador e inicia el reinicio del bucle de juego (Game Loop).
    /// </summary>
    public void PerderJuego(string motivo = "Fin de partida")
    {
        if (juegoTerminado) return;

        juegoTerminado = true;

        // Detener inmediatamente al jugador para que no siga acumulando velocidad de caída en el vacío
        if (jugador == null) jugador = FindAnyObjectByType<PlayerController3D>();
        if (jugador != null)
        {
            jugador.DetenerPorMuerte();
        }

        GuardarProgresoActual();
        ActualizarTextosUI();

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
    /// Reinicia el flujo completo del juego (Game Loop):
    /// 1. Vuelve el puntaje a 0 y la dificultad al Nivel 1.
    /// 2. Destruye todas las plataformas existentes y genera un nivel nuevo desde la posición inicial.
    /// 3. Teletransporta al jugador a la posición inicial segura con velocidad e inercia en cero.
    /// </summary>
    public void ReiniciarJuego()
    {
        if (corrutinaReinicio != null)
        {
            StopCoroutine(corrutinaReinicio);
            corrutinaReinicio = null;
        }

        GuardarProgresoActual();
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

        // 6. Ocultar el panel de Game Over, actualizar textos UI y reanudar el bucle de juego
        if (panelGameOver != null)
        {
            panelGameOver.SetActive(false);
        }

        ActualizarTextosUI();
        juegoTerminado = false;
    }

    #endregion

    /// <summary>
    /// Garantiza que todos los elementos visuales del HUD (Texto_Record y PanelGameOver funcional)
    /// estén presentes y vinculados correctamente en el Canvas.
    /// </summary>
    private void AsegurarElementosUI()
    {
        GameObject canvasObj = GameObject.Find("Canvas_HUD");
        if (canvasObj == null) return;

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) uiLayer = 5;

        // 1. Asegurar Texto_Record si no estaba vinculado en la escena
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

        // 1.5. Asegurar texto narrativo inspirador en la UI al empezar a jugar
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
            rectN.sizeDelta = new Vector2(680f, 45f);

            TextMeshProUGUI tmpN = objNarrativa.GetComponent<TextMeshProUGUI>();
            tmpN.text = "Despierta tu Aura. Avanza y evoluciona.";
            tmpN.fontSize = 21;
            tmpN.fontStyle = FontStyles.Italic;
            tmpN.color = new Color(0.65f, 0.90f, 1f, 0.95f);
            tmpN.alignment = TextAlignmentOptions.Top;
        }

        // 2. Asegurar que panelGameOver tenga interfaz gráfica real dentro del Canvas
        bool necesitaConstruirPanel = (panelGameOver == null) ||
                                      (panelGameOver.GetComponent<RectTransform>() == null) ||
                                      (panelGameOver.GetComponentInChildren<TMP_Text>(true) == null);

        if (necesitaConstruirPanel)
        {
            Transform existenteEnCanvas = canvasObj.transform.Find("Panel_GameOver_UI");
            if (existenteEnCanvas != null)
            {
                panelGameOver = existenteEnCanvas.gameObject;
                Transform det = panelGameOver.transform.Find("Texto_Detalle_GameOver");
                if (det != null) textoDetalleGameOver = det.GetComponent<TMP_Text>();
            }
            else
            {
                ConstruirPanelGameOverUI(canvasObj.transform, uiLayer);
            }
        }
        else
        {
            Transform det = panelGameOver.transform.Find("Texto_Detalle_GameOver");
            if (det != null) textoDetalleGameOver = det.GetComponent<TMP_Text>();
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
        panelRect.sizeDelta = new Vector2(720f, 360f);

        Image bgImg = panelObj.GetComponent<Image>();
        bgImg.color = new Color(0.08f, 0.09f, 0.14f, 0.92f);

        // Título
        GameObject tituloObj = new GameObject("Titulo_GameOver", typeof(RectTransform), typeof(TextMeshProUGUI));
        tituloObj.layer = uiLayer;
        tituloObj.transform.SetParent(panelObj.transform, false);
        RectTransform tRect = tituloObj.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.5f, 1f);
        tRect.anchorMax = new Vector2(0.5f, 1f);
        tRect.pivot = new Vector2(0.5f, 1f);
        tRect.anchoredPosition = new Vector2(0f, -25f);
        tRect.sizeDelta = new Vector2(660f, 65f);

        TextMeshProUGUI tmpTitulo = tituloObj.GetComponent<TextMeshProUGUI>();
        tmpTitulo.text = "¡TU AURA SE HA AGOTADO!";
        tmpTitulo.fontSize = 36;
        tmpTitulo.fontStyle = FontStyles.Bold;
        tmpTitulo.color = new Color(1f, 0.32f, 0.32f, 1f);
        tmpTitulo.alignment = TextAlignmentOptions.Center;

        // Texto Detalle / Cuenta regresiva
        GameObject detalleObj = new GameObject("Texto_Detalle_GameOver", typeof(RectTransform), typeof(TextMeshProUGUI));
        detalleObj.layer = uiLayer;
        detalleObj.transform.SetParent(panelObj.transform, false);
        RectTransform dRect = detalleObj.GetComponent<RectTransform>();
        dRect.anchorMin = new Vector2(0.5f, 0.5f);
        dRect.anchorMax = new Vector2(0.5f, 0.5f);
        dRect.pivot = new Vector2(0.5f, 0.5f);
        dRect.anchoredPosition = new Vector2(0f, 10f);
        dRect.sizeDelta = new Vector2(660f, 130f);

        textoDetalleGameOver = detalleObj.GetComponent<TextMeshProUGUI>();
        textoDetalleGameOver.text = "Reiniciando nivel...";
        textoDetalleGameOver.fontSize = 26;
        textoDetalleGameOver.color = Color.white;
        textoDetalleGameOver.alignment = TextAlignmentOptions.Center;

        // Botón Reintentar Ahora
        GameObject btnObj = new GameObject("Btn_Reintentar", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.layer = uiLayer;
        btnObj.transform.SetParent(panelObj.transform, false);
        RectTransform bRect = btnObj.GetComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0.5f, 0f);
        bRect.anchorMax = new Vector2(0.5f, 0f);
        bRect.pivot = new Vector2(0.5f, 0f);
        bRect.anchoredPosition = new Vector2(0f, 25f);
        bRect.sizeDelta = new Vector2(280f, 65f);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.18f, 0.65f, 0.35f, 0.95f);

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(ReiniciarJuego);

        GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTxtObj.layer = uiLayer;
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        RectTransform btRect = btnTxtObj.GetComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI btnTmp = btnTxtObj.GetComponent<TextMeshProUGUI>();
        btnTmp.text = "REINTENTAR";
        btnTmp.fontSize = 26;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.color = Color.white;
        btnTmp.alignment = TextAlignmentOptions.Center;

        panelGameOver = panelObj;
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
