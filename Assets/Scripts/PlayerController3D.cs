using UnityEngine;

public class PlayerController3D : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 6f;
    public float fuerzaSalto = 5f;

    private Rigidbody rb;
    private bool enElSuelo = true;

    void Start()
    {
        // Obtenemos el componente Rigidbody del jugador
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Movimiento en los ejes X y Z usando las flechas del teclado
        float movimientoX = Input.GetAxis("Horizontal");
        float movimientoZ = Input.GetAxis("Vertical");

        Vector3 movimiento = new Vector3(movimientoX, 0f, movimientoZ) * velocidad;
        
        // Aplicamos la velocidad manteniendo la gravedad (Y)
        rb.linearVelocity = new Vector3(movimiento.x, rb.linearVelocity.y, movimiento.z);

        // Salto con la barra espaciadora
        if (Input.GetKeyDown(KeyCode.Space) && enElSuelo)
        {
            rb.AddForce(Vector3.up * fuerzaSalto, ForceMode.Impulse);
            enElSuelo = false;
        }
    }

    // Detectar si el jugador toca el suelo para volver a saltar
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Suelo"))
        {
            enElSuelo = true;
        }
    }
}