using UnityEngine;

/// <summary>
/// Cumple con los Criterios de Evaluación 1 y 2 de la Actividad 4.1:
/// - Control mediante interfaz táctil móvil (BotonTactil) y teclado (PC).
/// - Desplazamiento real en los 3 ejes tridimensionales:
///     * Eje X: Desplazamiento lateral (izquierda / derecha).
///     * Eje Z: Desplazamiento longitudinal (adelante / atrás).
///     * Eje Y: Desplazamiento vertical mediante física de salto (Rigidbody) y gravedad.
/// - Sistema de colisiones integrado (Rigidbody + Collider + Detección de superficie).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PlayerController3D : MonoBehaviour
{
    [Header("Parámetros de Movimiento (Ejes X y Z)")]
    [Tooltip("Velocidad actual de traslación horizontal")]
    public float velocidad = 6f;

    [Tooltip("Velocidad base sobre la que se aplica la curva de dificultad")]
    public float velocidadBase = 6f;

    [Tooltip("Velocidad de rotación al girar hacia la dirección de movimiento")]
    public float velocidadGiro = 12f;

    [Header("Parámetros de Salto (Eje Y)")]
    [Tooltip("Fuerza del impulso vertical en el eje Y")]
    public float fuerzaSalto = 6f;

    [Header("Estado")]
    [SerializeField] private bool enElSuelo = true;

    private Rigidbody rb;
    private Animator animator;
    private Vector3 posicionAnterior;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        velocidadBase = velocidad;
        posicionAnterior = transform.position;

        // Asegurar que el Rigidbody no se caiga ni ruede de lado por las físicas
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void Update()
    {
        // 1. Obtener entradas combinadas de Teclado (PC) y Botones Táctiles en pantalla (Móvil)
        float inputX = Input.GetAxisRaw("Horizontal") + BotonTactil.ObtenerHorizontal();
        float inputZ = Input.GetAxisRaw("Vertical") + BotonTactil.ObtenerVertical();

        // Limitar valores entre -1 y 1
        inputX = Mathf.Clamp(inputX, -1f, 1f);
        inputZ = Mathf.Clamp(inputZ, -1f, 1f);

        Vector3 direccion = new Vector3(inputX, 0f, inputZ);

        // 2. Desplazamiento en los ejes X y Z
        if (direccion.sqrMagnitude > 0.01f)
        {
            direccion.Normalize();

            // Aplicar velocidad horizontal preservando la velocidad en Y (gravedad o salto)
            rb.linearVelocity = new Vector3(direccion.x * velocidad, rb.linearVelocity.y, direccion.z * velocidad);

            // Girar suavemente al personaje hacia la dirección hacia donde se desplaza
            Quaternion rotacionObjetivo = Quaternion.LookRotation(direccion, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, velocidadGiro * Time.deltaTime);

            if (animator != null)
            {
                animator.SetBool("isWalking", true);
            }
        }
        else
        {
            // Detener el desplazamiento horizontal si no hay controles activos (evita patinajes)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

            if (animator != null)
            {
                animator.SetBool("isWalking", false);
            }
        }

        // 3. Desplazamiento en el eje Y (Salto con tecla Espacio o Botón Táctil de Salto)
        bool solicitarSalto = Input.GetKeyDown(KeyCode.Space) || BotonTactil.ConsumirSalto();
        if (solicitarSalto && enElSuelo)
        {
            EjecutarSalto();
        }

        // 4. Registro de avance tridimensional para el contador uniforme de la Actividad 4.2
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

    private void EjecutarSalto()
    {
        // Reiniciar velocidad Y antes de aplicar el impulso para un salto consistente
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * fuerzaSalto, ForceMode.Impulse);
        enElSuelo = false;
    }

    // --- SISTEMA DE COLISIONES ROBUSTO (Criterio 2) ---
    private void OnCollisionStay(Collision collision)
    {
        // Verifica si la superficie de contacto está por debajo del personaje (es suelo)
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

    // --- MÉTODOS DE COMPATIBILIDAD CON GAMEMANAGER Y EDITOR ---
    public void ModificarVelocidadPorDificultad(float multiplicador)
    {
        velocidad = velocidadBase * multiplicador;
    }

    public float ObtenerVelocidadActual()
    {
        return velocidad;
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
