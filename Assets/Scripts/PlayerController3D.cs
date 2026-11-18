using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Controlador tridimensional del jugador (Actividades 4.1 a 4.6):
/// - Control mediante interfaz táctil móvil (BotonTactil) y teclado (PC).
/// - Desplazamiento real en los 3 ejes tridimensionales (X, Y, Z).
/// - Integrado con la Curva de Dificultad Progresiva y el Bucle de Juego (Game Loop) al caer o colisionar.
/// - Incluye el efecto de Potenciador de Velocidad (Speed Boost) con TrailRenderer y duración máxima de 3 segundos.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PlayerController3D : MonoBehaviour
{
    [Header("Parámetros de Movimiento (Ejes X y Z)")]
    [Tooltip("Velocidad actual de traslación horizontal")]
    public float velocidad = 8f;

    [Tooltip("Velocidad base sobre la que se aplica la curva de dificultad")]
    public float velocidadBase = 8f;

    [Tooltip("Velocidad máxima permitida al escalar la dificultad normal")]
    public float velocidadMaxima = 14.5f;

    [Tooltip("Velocidad de rotación al girar hacia la dirección de movimiento")]
    public float velocidadGiro = 12f;

    [Header("Potenciador de Velocidad y TrailRenderer (Actividad 4.6)")]
    [Tooltip("TrailRenderer que se enciende únicamente cuando el potenciador de velocidad está activo")]
    public TrailRenderer estelaVelocidad;

    [Tooltip("Multiplicador de velocidad al tocar el cubo amarillo (por defecto x3)")]
    public float multiplicadorPotenciador = 3f;

    [Tooltip("Duración máxima del potenciador en segundos (no debe superar los 3 segundos)")]
    [Range(0.5f, 3f)]
    public float duracionPotenciador = 3f;

    [Tooltip("Incremento pequeño de velocidad base cada 100 puntos (el cubo x3 sigue siendo el protagonista)")]
    public float incrementoCada100Puntos = 0.25f;

    [Header("Parámetros de Salto (Eje Y)")]
    [Tooltip("Fuerza del impulso vertical en el eje Y para superar huecos y obstáculos")]
    public float fuerzaSalto = 8.5f;

    [Header("Game Loop (Posición Inicial y Caída)")]
    [Tooltip("Posición inicial segura a la que se teletransporta el jugador al iniciar o reiniciar la partida")]
    public Vector3 posicionInicialSegura = new Vector3(0f, 3.25f, 0f);

    [Tooltip("Altura Y por debajo de la cual se considera caída al vacío y se reinicia el Game Loop")]
    public float alturaCaidaMuerte = -4.5f;

    [Header("Personalización Visual del Personaje (Actividad 4.4)")]
    [Tooltip("Color actual del Aura y del material del caballero")]
    public Color colorPersonaje = new Color(0.92f, 0.96f, 1.00f, 1f);

    [Tooltip("Propiedad visual de acabado metálico de la armadura")]
    [Range(0f, 1f)]
    public float acabadoMetalico = 0.15f;

    [Tooltip("Propiedad visual de suavidad y brillo del material")]
    [Range(0f, 1f)]
    public float suavidadBrillo = 0.25f;

    [Header("Estado")]
    [SerializeField] private bool enElSuelo = true;
    [SerializeField] private bool potenciadorActivo = false;
    [SerializeField] private float tiempoRestanteBoost = 0f;

    public bool PotenciadorActivo => potenciadorActivo;
    public float TiempoRestanteBoost => tiempoRestanteBoost;
    public float MultiplicadorBoostActual => multiplicadorBoostActual;

    private Rigidbody rb;
    private Animator animator;
    private Vector3 posicionAnterior;
    private Quaternion rotacionInicial;
    private float tiempoProteccionReinicio = 0f;

    private float velocidadBaseInicial = 8f;
    private float multiplicadorDificultadActual = 1f;
    private float bonoVelocidadPorPuntos = 0f;
    private float multiplicadorBoostActual = 1f;
    private Coroutine corrutinaPotenciador;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();

        // Calcular altura segura sobre el origen (X = 0, Z = 0) considerando la escala del personaje
        float mitadAltura = Mathf.Max(0.5f, transform.localScale.y * 0.5f);
        posicionInicialSegura = new Vector3(0f, mitadAltura + 0.25f, 0f);
        rotacionInicial = Quaternion.identity;
    }

    void Start()
    {
        if (velocidad < 7f) velocidad = 8f;
        if (fuerzaSalto < 7.5f) fuerzaSalto = 8.5f;
        velocidadBase = velocidad;
        velocidadBaseInicial = velocidadBase;

        // Ajustar el BoxCollider al cuerpo real del caballero si tiene el tamaño de cubo por defecto
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null && box.size.x > 0.6f)
        {
            box.size = new Vector3(0.35f, 1.0f, 0.35f);
        }

        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        ConfigurarEstelaVelocidad();
        ApagarEstelaVelocidad();

        // Colocar en la posición inicial segura al arrancar
        ReiniciarJugador();
    }

    void Update()
    {
        if (tiempoProteccionReinicio > 0f)
        {
            tiempoProteccionReinicio -= Time.deltaTime;
        }

        // Si el juego terminó (Game Over en transición de reinicio), mantener al jugador detenido sin seguir cayendo
        if (GameManager.Instance != null && GameManager.Instance.JuegoTerminado)
        {
            if (animator != null)
            {
                animator.SetBool("isWalking", false);
            }
            return;
        }

        // 0. Verificar si el jugador cayó al vacío entre plataformas para activar el Game Loop
        if (tiempoProteccionReinicio <= 0f && transform.position.y < alturaCaidaMuerte)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PerderJuego("Has caído en el vacío. Elévate de nuevo.");
            }
            return;
        }

        // 1. Obtener entradas combinadas de Teclado (PC) y Botones Táctiles en pantalla (Móvil)
        float inputX = Input.GetAxisRaw("Horizontal") + BotonTactil.ObtenerHorizontal();
        float inputZ = Input.GetAxisRaw("Vertical") + BotonTactil.ObtenerVertical();

        inputX = Mathf.Clamp(inputX, -1f, 1f);
        inputZ = Mathf.Clamp(inputZ, -1f, 1f);

        Vector3 direccion = new Vector3(inputX, 0f, inputZ);

        // 2. Desplazamiento en los ejes X y Z
        if (direccion.sqrMagnitude > 0.01f)
        {
            direccion.Normalize();

            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = new Vector3(direccion.x * velocidad, rb.linearVelocity.y, direccion.z * velocidad);
            }

            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadGiro * Time.deltaTime);

            if (animator != null)
            {
                animator.SetBool("isWalking", true);
            }
        }
        else
        {
            if (rb != null && !rb.isKinematic)
            {
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            }

            if (animator != null)
            {
                animator.SetBool("isWalking", false);
            }
        }

        // 3. Desplazamiento en el eje Y (Salto con tecla Espacio o Botón Táctil de Salto)
        bool solicitarSalto = Input.GetKeyDown(KeyCode.Space) || BotonTactil.ConsumirSalto();
        if (solicitarSalto && EstaEnElSuelo())
        {
            EjecutarSalto();
        }

        // 4. Registro de avance para el contador uniforme y progresión de nivel
        float distanciaDelta = Vector3.Distance(transform.position, posicionAnterior);
        if (distanciaDelta > 0.001f)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegistrarAvance(distanciaDelta);
            }
            posicionAnterior = transform.position;
        }
    }

    private bool EstaEnElSuelo()
    {
        if (enElSuelo) return true;

        float distanciaRayo = (transform.localScale.y * 0.5f) + 0.25f;
        return Physics.Raycast(transform.position, Vector3.down, distanciaRayo);
    }

    private void EjecutarSalto()
    {
        if (rb == null || rb.isKinematic) return;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * fuerzaSalto, ForceMode.Impulse);
        enElSuelo = false;
    }

    private void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contacto in collision.contacts)
        {
            if (contacto.normal.y > 0.45f)
            {
                enElSuelo = true;
                return;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        enElSuelo = false;
    }

    /// <summary>
    /// Congela el movimiento y la caída del jugador inmediatamente al perder.
    /// </summary>
    public void DetenerPorMuerte()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
        }

        if (animator != null)
        {
            animator.SetBool("isWalking", false);
        }

        CancelarPotenciadorVelocidad();
    }

    /// <summary>
    /// Ajusta la velocidad del jugador según el multiplicador de la Curva de Dificultad Progresiva.
    /// </summary>
    public void ModificarVelocidadPorDificultad(float multiplicador)
    {
        multiplicadorDificultadActual = Mathf.Max(1f, multiplicador);
        RecalcularVelocidadActual();
    }

    /// <summary>
    /// Extra (Actividad 4.6): Aumenta ligeramente la velocidad base cada 100 puntos obtenidos,
    /// manteniendo el efecto del cubo amarillo (x3) como el protagonista principal.
    /// </summary>
    public void ActualizarVelocidadPorPuntos(int puntajeActual)
    {
        int escalonesDe100 = Mathf.Max(0, puntajeActual / 100);
        bonoVelocidadPorPuntos = escalonesDe100 * incrementoCada100Puntos;
        velocidadBase = velocidadBaseInicial + bonoVelocidadPorPuntos;
        RecalcularVelocidadActual();
    }

    /// <summary>
    /// Recalcula la velocidad efectiva del personaje combinando la velocidad base, la dificultad y el potenciador x3.
    /// </summary>
    private void RecalcularVelocidadActual()
    {
        float velocidadNormal = Mathf.Min(velocidadMaxima, velocidadBase * multiplicadorDificultadActual);

        if (potenciadorActivo)
        {
            velocidad = velocidadNormal * multiplicadorBoostActual;
        }
        else
        {
            velocidad = velocidadNormal;
        }
    }

    /// <summary>
    /// Activa el Potenciador de Velocidad (Speed Boost) al recoger un cubo amarillo (Actividad 4.6).
    /// Multiplica la velocidad (por ejemplo x3), enciende el TrailRenderer y dura máximo 3 segundos.
    /// </summary>
    public void ActivarPotenciadorVelocidad(float multiplicador = 3f, float duracion = 3f)
    {
        multiplicadorPotenciador = Mathf.Max(1.2f, multiplicador);
        // Asegurar que el efecto nunca dure más de 3 segundos
        duracionPotenciador = Mathf.Clamp(duracion, 0.5f, 3f);

        if (corrutinaPotenciador != null)
        {
            StopCoroutine(corrutinaPotenciador);
        }

        corrutinaPotenciador = StartCoroutine(CorrutinaPotenciadorVelocidad(multiplicadorPotenciador, duracionPotenciador));
    }

    private IEnumerator CorrutinaPotenciadorVelocidad(float multiplicador, float duracion)
    {
        potenciadorActivo = true;
        multiplicadorBoostActual = multiplicador;
        tiempoRestanteBoost = Mathf.Clamp(duracion, 0.5f, 3f);
        RecalcularVelocidadActual();

        if (animator != null)
        {
            animator.speed = 1.8f;
        }

        EncenderEstelaVelocidad();

        while (tiempoRestanteBoost > 0f)
        {
            yield return null;
            tiempoRestanteBoost = Mathf.Max(0f, tiempoRestanteBoost - Time.deltaTime);
        }

        potenciadorActivo = false;
        multiplicadorBoostActual = 1f;
        tiempoRestanteBoost = 0f;
        RecalcularVelocidadActual();

        if (animator != null)
        {
            animator.speed = 1f;
        }

        ApagarEstelaVelocidad();
        corrutinaPotenciador = null;
    }

    private void CancelarPotenciadorVelocidad()
    {
        if (corrutinaPotenciador != null)
        {
            StopCoroutine(corrutinaPotenciador);
            corrutinaPotenciador = null;
        }

        potenciadorActivo = false;
        multiplicadorBoostActual = 1f;
        tiempoRestanteBoost = 0f;
        RecalcularVelocidadActual();

        if (animator != null)
        {
            animator.speed = 1f;
        }

        ApagarEstelaVelocidad();
    }

    /// <summary>
    /// Crea o configura el TrailRenderer llamativo del personaje para el potenciador de velocidad.
    /// </summary>
    private void ConfigurarEstelaVelocidad()
    {
        if (estelaVelocidad == null)
        {
            Transform trEstela = transform.Find("Estela_Aura_Trail");
            if (trEstela != null)
            {
                estelaVelocidad = trEstela.GetComponent<TrailRenderer>();
            }
        }

        if (estelaVelocidad == null)
        {
            estelaVelocidad = GetComponentInChildren<TrailRenderer>(true);
        }

        if (estelaVelocidad == null)
        {
            GameObject objEstela = new GameObject("Estela_Aura_Trail");
            objEstela.transform.SetParent(transform, false);
            objEstela.transform.localPosition = new Vector3(0f, -0.08f, -0.08f);
            estelaVelocidad = objEstela.AddComponent<TrailRenderer>();
        }

        estelaVelocidad.time = 0.50f;
        estelaVelocidad.minVertexDistance = 0.06f;
        estelaVelocidad.numCapVertices = 4;
        estelaVelocidad.numCornerVertices = 4;
        estelaVelocidad.shadowCastingMode = ShadowCastingMode.Off;
        estelaVelocidad.receiveShadows = false;

        AnimationCurve curvaAncho = new AnimationCurve();
        curvaAncho.AddKey(0.0f, 0.78f);
        curvaAncho.AddKey(0.45f, 0.48f);
        curvaAncho.AddKey(1.0f, 0.0f);
        estelaVelocidad.widthCurve = curvaAncho;

        Shader shaderSprites = Shader.Find("Sprites/Default");
        if (shaderSprites != null && estelaVelocidad.sharedMaterial == null)
        {
            estelaVelocidad.material = new Material(shaderSprites);
        }

        // Gradiente tricolor llamativo: Amarillo solar -> Naranja energético -> Dorado transparente
        Gradient gradiente = new Gradient();
        gradiente.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(1.0f, 0.96f, 0.15f), 0.0f),
                new GradientColorKey(new Color(1.0f, 0.58f, 0.04f), 0.55f),
                new GradientColorKey(new Color(1.0f, 0.25f, 0.0f), 1.0f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.95f, 0.0f),
                new GradientAlphaKey(0.75f, 0.50f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        estelaVelocidad.colorGradient = gradiente;
    }

    private void EncenderEstelaVelocidad()
    {
        ConfigurarEstelaVelocidad();
        if (estelaVelocidad != null)
        {
            estelaVelocidad.Clear();
            estelaVelocidad.enabled = true;
            estelaVelocidad.emitting = true;
        }
    }

    private void ApagarEstelaVelocidad()
    {
        if (estelaVelocidad == null)
        {
            estelaVelocidad = GetComponentInChildren<TrailRenderer>(true);
        }

        if (estelaVelocidad != null)
        {
            estelaVelocidad.emitting = false;
            estelaVelocidad.Clear();
            estelaVelocidad.enabled = false;
        }
    }

    public float ObtenerVelocidadActual()
    {
        return velocidad;
    }

    /// <summary>
    /// Permite alterar y personalizar el color y las propiedades visuales del material del personaje principal (Actividad 4.4).
    /// </summary>
    public void CambiarColorYPropiedadesPersonaje(Color nuevoColor, float metalico, float suavidad)
    {
        colorPersonaje = nuevoColor;
        acabadoMetalico = metalico;
        suavidadBrillo = suavidad;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null || !renderers[i].enabled) continue;

            Material mat = renderers[i].material;
            if (mat == null) continue;

            mat.color = nuevoColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", nuevoColor);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metalico);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", suavidad);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", Color.black);
                mat.DisableKeyword("_EMISSION");
            }
        }
    }

    /// <summary>
    /// Teletransporta al jugador a la posición inicial segura (X=0, Z=0 sobre la plataforma inicial)
    /// y elimina por completo toda velocidad o inercia de caída del Rigidbody.
    /// </summary>
    public void ReiniciarJugador()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();

        float mitadAltura = Mathf.Max(0.5f, transform.localScale.y * 0.5f);
        posicionInicialSegura = new Vector3(0f, mitadAltura + 0.25f, 0f);

        if (rb != null)
        {
            // Desactivar interpolación y activar modo cinemático temporalmente para que PhysX
            // no sobreescriba el teletransporte con la posición donde murió el jugador
            rb.interpolation = RigidbodyInterpolation.None;
            rb.isKinematic = true;
        }

        transform.position = posicionInicialSegura;
        transform.rotation = rotacionInicial;

        if (rb != null)
        {
            rb.position = posicionInicialSegura;
            rb.rotation = rotacionInicial;
        }

        // Sincronizar inmediatamente el motor de físicas con el nuevo Transform
        Physics.SyncTransforms();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.WakeUp();
        }

        posicionAnterior = posicionInicialSegura;
        multiplicadorDificultadActual = 1f;
        bonoVelocidadPorPuntos = 0f;
        velocidadBase = velocidadBaseInicial;
        CancelarPotenciadorVelocidad();
        enElSuelo = true;
        tiempoProteccionReinicio = 0.35f;

        if (animator != null)
        {
            animator.SetBool("isWalking", false);
        }
    }

    public void ActualizarBotonesTactiles()
    {
        // Los botones táctiles se autorregistran en tiempo de ejecución de manera desacoplada
    }

    void OnApplicationQuit()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GuardarProgresoActual();
        }
    }
}
