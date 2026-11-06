using UnityEngine;

/// <summary>
/// Generador de obstáculos con dificultad progresiva.
/// Conforme la dificultad sube, los obstáculos aparecen con mayor frecuencia
/// y se desplazan a mayor velocidad.
/// </summary>
public class GeneradorObstaculos : MonoBehaviour
{
    [Header("Prefab de Obstáculo (Opcional)")]
    [Tooltip("Prefab a instanciar. Si está vacío, creará automáticamente cubos con físicas.")]
    public GameObject prefabObstaculo;

    [Header("Frecuencia de Generación")]
    [Tooltip("Intervalo base en segundos entre cada obstáculo generado")]
    public float intervaloBase = 3.0f;

    [Tooltip("Intervalo mínimo posible (límite de dificultad máxima)")]
    public float intervaloMinimo = 0.8f;

    [Header("Velocidad de Obstáculos")]
    public float velocidadBaseObstaculo = 7f;
    public float velocidadMaximaObstaculo = 16f;

    [Header("Área de Generación (Límites de la Arena)")]
    public float rangoX = 20f;
    public float posicionZGeneracion = 45f;
    public float alturaY = 1.0f;

    private float temporizador = 0f;
    private float intervaloActual;
    private float velocidadActualObstaculo;

    private void Start()
    {
        intervaloActual = intervaloBase;
        velocidadActualObstaculo = velocidadBaseObstaculo;
    }

    private void Update()
    {
        temporizador += Time.deltaTime;

        if (temporizador >= intervaloActual)
        {
            temporizador = 0f;
            GenerarObstaculo();
        }
    }

    /// <summary>
    /// Genera un obstáculo en el escenario. Si no hay prefab, crea un cubo rojo proceduralmente.
    /// </summary>
    private void GenerarObstaculo()
    {
        float posX = Random.Range(-rangoX, rangoX);
        Vector3 posSpawn = new Vector3(posX, alturaY, posicionZGeneracion);

        GameObject nuevoObstaculo;

        if (prefabObstaculo != null)
        {
            nuevoObstaculo = Instantiate(prefabObstaculo, posSpawn, Quaternion.identity);
        }
        else
        {
            // Generación automática: Crea un cubo físico con collider y color rojo
            nuevoObstaculo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nuevoObstaculo.name = "Obstaculo_Auto";
            nuevoObstaculo.transform.position = posSpawn;
            nuevoObstaculo.transform.localScale = new Vector3(2f, 2f, 2f);

            Renderer ren = nuevoObstaculo.GetComponent<Renderer>();
            if (ren != null)
            {
                ren.material.color = new Color(0.9f, 0.2f, 0.2f); // Rojo peligro
            }
        }

        // Asegurar tag o componente
        nuevoObstaculo.tag = "Untagged"; // Funciona con el script y por nombre
        Obstaculo scriptObs = nuevoObstaculo.GetComponent<Obstaculo>();
        if (scriptObs == null)
        {
            scriptObs = nuevoObstaculo.AddComponent<Obstaculo>();
        }

        scriptObs.velocidad = velocidadActualObstaculo;
        scriptObs.direccion = new Vector3(0f, 0f, -1f); // Viaja de Norte (+Z) a Sur (-Z)
    }

    /// <summary>
    /// Actualiza la frecuencia y velocidad de los obstáculos según la curva de dificultad.
    /// </summary>
    public void ActualizarDificultad(float multiplicador)
    {
        // El intervalo se reduce (más frecuente)
        intervaloActual = Mathf.Max(intervaloMinimo, intervaloBase / multiplicador);

        // La velocidad aumenta
        velocidadActualObstaculo = Mathf.Min(velocidadMaximaObstaculo, velocidadBaseObstaculo * multiplicador);
    }
}
