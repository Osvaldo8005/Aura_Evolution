using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Controlador principal del juego.
/// Gestiona la Interfaz de Usuario (UI), el Guardado Persistente (PlayerPrefs)
/// y la Curva de Dificultad Progresiva.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referencias de UI (TextMeshPro)")]
    [Tooltip("Texto para mostrar el puntaje / avance del jugador")]
    public TMP_Text textoPuntaje;

    [Tooltip("Texto para mostrar el récord persistente cargado de PlayerPrefs")]
    public TMP_Text textoRecord;

    [Tooltip("Texto opcional para mostrar el nivel de dificultad actual")]
    public TMP_Text textoDificultad;

    [Tooltip("Panel de Game Over (opcional)")]
    public GameObject panelGameOver;

    [Header("Contador y Avance Uniforme")]
    [Tooltip("Puntos ganados de forma continua por segundo")]
    public float puntosPorSegundo = 10f;

    [Tooltip("Puntos ganados por cada metro que el jugador se desplaza")]
    public float puntosPorMetro = 5f;

    [Header("Curva de Dificultad Progresiva")]
    [Tooltip("Segundos necesarios para que suba un nivel de dificultad")]
    public float tiempoPorNivelDificultad = 12f;

    [Tooltip("Porcentaje de incremento de dificultad por nivel (0.15 = +15%)")]
    public float incrementoDificultadPorNivel = 0.15f;

    [Tooltip("Nivel máximo de dificultad")]
    public int nivelMaximo = 10;

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

    private PlayerController3D jugador;
    private GeneradorObstaculos spawner;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        jugador = FindAnyObjectByType<PlayerController3D>();
        spawner = FindAnyObjectByType<GeneradorObstaculos>();

        CargarDatosPlayerPrefs();
        ActualizarTextosUI();

        if (panelGameOver != null)
        {
            panelGameOver.SetActive(false);
        }
    }

    private void Update()
    {
        if (juegoTerminado) return;

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
    /// Suma distancia cuando el personaje camina, incrementando el puntaje uniformemente.
    /// </summary>
    public void RegistrarAvance(float distanciaDelta)
    {
        if (juegoTerminado) return;

        distanciaRecorrida += distanciaDelta;
        puntajeAcumuladoFloat += distanciaDelta * puntosPorMetro;
    }

    /// <summary>
    /// Suma puntos directos por interacción (ej. recoger monedas/gemas).
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
    /// Curva de dificultad progresiva basada en el tiempo y puntaje acumulado.
    /// Incrementa la velocidad del jugador y la frecuencia/velocidad de obstáculos.
    /// </summary>
    private void ActualizarCurvaDificultad()
    {
        int nuevoNivel = 1 + Mathf.FloorToInt(tiempoJugado / tiempoPorNivelDificultad);
        nuevoNivel = Mathf.Min(nuevoNivel, nivelMaximo);

        if (nuevoNivel != nivelActual)
        {
            nivelActual = nuevoNivel;
            multiplicadorDificultad = 1f + ((nivelActual - 1) * incrementoDificultadPorNivel);

            // Aplica dificultad al personaje (se mueve más rápido)
            if (jugador != null)
            {
                jugador.ModificarVelocidadPorDificultad(multiplicadorDificultad);
            }

            // Aplica dificultad al generador de obstáculos (aparecen más seguido y más rápido)
            if (spawner != null)
            {
                spawner.ActualizarDificultad(multiplicadorDificultad);
            }

            ActualizarTextosUI();
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
            float velActual = (jugador != null) ? jugador.ObtenerVelocidadActual() : 6f;
            textoDificultad.text = $"Dificultad: Nivel {nivelActual} (Vel: {velActual:F1})";
        }
    }

    #region Guardado de Datos con PlayerPrefs

    /// <summary>
    /// Carga los datos persistentes guardados con PlayerPrefs.
    /// </summary>
    public void CargarDatosPlayerPrefs()
    {
        recordGuardado = PlayerPrefs.GetInt(KEY_RECORD, 0);
    }

    /// <summary>
    /// Comprueba si se ha roto el récord y lo guarda automáticamente en PlayerPrefs.
    /// </summary>
    private void VerificarYGuardarRecord()
    {
        if (puntajeActual > recordGuardado)
        {
            recordGuardado = puntajeActual;
            PlayerPrefs.SetInt(KEY_RECORD, recordGuardado);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Guarda el último puntaje alcanzado.
    /// </summary>
    public void GuardarProgresoActual()
    {
        PlayerPrefs.SetInt(KEY_ULTIMO_PUNTAJE, puntajeActual);
        if (puntajeActual > recordGuardado)
        {
            PlayerPrefs.SetInt(KEY_RECORD, puntajeActual);
        }
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Permite borrar los datos guardados en PlayerPrefs desde el Inspector para pruebas del profesor.
    /// </summary>
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

    /// <summary>
    /// Maneja el impacto con obstáculos (penalización o fin de juego).
    /// </summary>
    public void RecibirImpactoObstaculo()
    {
        GuardarProgresoActual();

        // Opcional: mostrar panel de Game Over o restar puntaje
        if (panelGameOver != null)
        {
            juegoTerminado = true;
            panelGameOver.SetActive(true);
        }
        else
        {
            // Pequeña penalización si no hay panel de Game Over
            puntajeAcumuladoFloat = Mathf.Max(0, puntajeAcumuladoFloat - 50);
            puntajeActual = Mathf.FloorToInt(puntajeAcumuladoFloat);
            ActualizarTextosUI();
        }
    }

    public void ReiniciarJuego()
    {
        GuardarProgresoActual();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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
