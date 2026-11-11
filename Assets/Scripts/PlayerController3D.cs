using UnityEngine;

/// <summary>
/// Controlador tridimensional del jugador (Actividades 4.1, 4.2 y 4.3):
/// - Control mediante interfaz táctil móvil (BotonTactil) y teclado (PC).
/// - Desplazamiento real en los 3 ejes tridimensionales (X, Y, Z).
/// - Integrado con la Curva de Dificultad Progresiva y el Bucle de Juego (Game Loop) al caer o colisionar.
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

    [Tooltip("Velocidad máxima permitida al escalar la dificultad")]
    public float velocidadMaxima = 14.5f;

    [Tooltip("Velocidad de rotación al girar hacia la dirección de movimiento")]
    public float velocidadGiro = 12f;

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

    private Rigidbody rb;
    private Animator animator;
    private Vector3 posicionAnterior;
    private Quaternion rotacionInicial;
    private float tiempoProteccionReinicio = 0f;

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
    }

    /// <summary>
    /// Ajusta la velocidad del jugador según el multiplicador de la Curva de Dificultad Progresiva.
    /// </summary>
    public void ModificarVelocidadPorDificultad(float multiplicador)
    {
        velocidad = Mathf.Min(velocidadMaxima, velocidadBase * multiplicador);
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
        velocidad = velocidadBase;
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
