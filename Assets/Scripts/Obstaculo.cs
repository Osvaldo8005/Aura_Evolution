using UnityEngine;

/// <summary>
/// Controla el comportamiento de los obstáculos del escenario.
/// Soporta desplazamiento lineal (oleadas hacia el jugador) y patrullaje lateral
/// dentro de cada módulo procedural del nivel, escalando su velocidad con la curva de dificultad.
/// </summary>
public class Obstaculo : MonoBehaviour
{
    public enum ModoMovimiento
    {
        Lineal,
        PatrullaLateral
    }

    [Header("Configuración de Movimiento")]
    [Tooltip("Modo de desplazamiento del obstáculo en el nivel")]
    public ModoMovimiento modo = ModoMovimiento.Lineal;

    [Tooltip("Velocidad de desplazamiento en unidades por segundo")]
    public float velocidad = 8f;

    [Tooltip("Dirección de avance para el modo Lineal")]
    public Vector3 direccion = new Vector3(0f, 0f, -1f);

    [Header("Parámetros de Patrulla en Plataforma")]
    [Tooltip("Amplitud de movimiento lateral (Eje X) cuando está en modo PatrullaLateral")]
    public float rangoPatrullaX = 7f;

    [Header("Destrucción Automática")]
    [Tooltip("Si está activo, el obstáculo se autodestruye después de tiempoVida segundos")]
    public bool destruirPorTiempo = true;

    [Tooltip("Segundos antes de autodestruirse (para modo Lineal)")]
    public float tiempoVida = 12f;

    private Vector3 posicionInicialLocal;
    private float desfaseTiempo;

    private void Start()
    {
        posicionInicialLocal = transform.localPosition;
        desfaseTiempo = Random.Range(0f, Mathf.PI * 2f);

        if (destruirPorTiempo && modo == ModoMovimiento.Lineal)
        {
            Destroy(gameObject, tiempoVida);
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.JuegoTerminado)
        {
            return;
        }

        if (modo == ModoMovimiento.Lineal)
        {
            transform.position += direccion.normalized * velocidad * Time.deltaTime;
        }
        else if (modo == ModoMovimiento.PatrullaLateral)
        {
            float desplazamientoX = Mathf.Sin((Time.time * (velocidad * 0.35f)) + desfaseTiempo) * rangoPatrullaX;
            transform.localPosition = new Vector3(
                posicionInicialLocal.x + desplazamientoX,
                posicionInicialLocal.y,
                posicionInicialLocal.z
            );
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        VerificarImpactoJugador(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        VerificarImpactoJugador(other.gameObject);
    }

    private void VerificarImpactoJugador(GameObject obj)
    {
        if (obj == null) return;

        if (obj.CompareTag("Player") || obj.name.Contains("Jugador") || obj.GetComponent<PlayerController3D>() != null)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RecibirImpactoObstaculo();
            }

            if (modo == ModoMovimiento.Lineal)
            {
                Destroy(gameObject);
            }
        }
    }
}
