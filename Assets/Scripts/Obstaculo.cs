using UnityEngine;

public class Obstaculo : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 8f;
    public Vector3 direccion = new Vector3(0f, 0f, -1f);

    [Header("Destrucción")]
    public float tiempoVida = 12f;

    private void Start()
    {
        Destroy(gameObject, tiempoVida);
    }

    private void Update()
    {
        transform.position += direccion.normalized * velocidad * Time.deltaTime;
    }

    // ESTA ES LA FUNCIÓN QUE FALTABA
    private void OnCollisionEnter(Collision collision)
    {
        // Si el obstáculo choca con el jugador...
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.name.Contains("Jugador")) 
        {
            // Llama al GameManager para que guarde el récord y aplique la penalización
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RecibirImpactoObstaculo();
            }
            
            // Opcional: Destruir el obstáculo al chocar
            Destroy(gameObject); 
        }
    }
}