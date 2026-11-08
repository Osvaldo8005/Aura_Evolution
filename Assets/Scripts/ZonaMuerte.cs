using UnityEngine;

/// <summary>
/// Representa una porción peligrosa del nivel (DeathPart / Zona de Muerte).
/// Basado en la mecánica de niveles y DeathPart de la Actividad 4.3:
/// Al entrar en contacto con el jugador, notifica al GameManager para activar
/// el fin de partida y reiniciar el bucle de juego (Game Loop).
/// </summary>
public class ZonaMuerte : MonoBehaviour
{
    [Header("Configuración de Zona de Peligro (DeathPart)")]
    [Tooltip("Color distintivo para identificar visualmente la porción letal del módulo")]
    public Color colorPeligro = new Color(0.90f, 0.16f, 0.16f, 1f);

    [Tooltip("Aplicar color rojo automáticamente al inicializar la pieza")]
    public bool aplicarColorAutomaticamente = true;

    private void Start()
    {
        if (aplicarColorAutomaticamente)
        {
            AplicarColorPeligro(colorPeligro);
        }
    }

    /// <summary>
    /// Asigna el color de peligro al Renderer de esta porción del nivel compatible con URP y Standard.
    /// </summary>
    public void AplicarColorPeligro(Color color)
    {
        colorPeligro = color;
        Renderer ren = GetComponent<Renderer>();
        if (ren != null)
        {
            Material mat = ren.material;
            mat.color = colorPeligro;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", colorPeligro);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        VerificarContactoJugador(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        VerificarContactoJugador(other.gameObject);
    }

    private void VerificarContactoJugador(GameObject obj)
    {
        if (obj == null) return;

        if (obj.CompareTag("Player") || obj.name.Contains("Jugador") || obj.GetComponent<PlayerController3D>() != null)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PerderJuego("La corrupción drenó tu poder. Elévate de nuevo.");
            }
        }
    }
}
