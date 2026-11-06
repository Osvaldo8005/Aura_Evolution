using UnityEngine;

/// <summary>
/// Coleccionable interactivo (moneda / gema) que rota y suma puntos uniformemente al contacto.
/// </summary>
public class Coleccionable : MonoBehaviour
{
    [Header("Puntos")]
    [Tooltip("Cantidad de puntos que otorga al jugador de forma uniforme")]
    public int puntos = 100;

    [Header("Efecto Visual")]
    [Tooltip("Velocidad de rotación")]
    public float velocidadGiro = 120f;

    [Tooltip("Amplitud de la oscilación vertical")]
    public float amplitudFlotacion = 0.25f;

    private Vector3 posicionInicial;

    private void Start()
    {
        posicionInicial = transform.position;

        // Asegurar que el collider esté configurado como Trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Update()
    {
        // Giro continuo
        transform.Rotate(Vector3.up, velocidadGiro * Time.deltaTime, Space.World);

        // Flotación arriba y abajo
        float nuevaY = posicionInicial.y + Mathf.Sin(Time.time * 3f) * amplitudFlotacion;
        transform.position = new Vector3(transform.position.x, nuevaY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.name.Contains("Jugador"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SumarPuntos(puntos);
            }
            Destroy(gameObject);
        }
    }
}
