using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generador de oleadas de obstáculos con dificultad progresiva.
/// Conforme la dificultad sube, los obstáculos aparecen con mayor frecuencia
/// y se desplazan a mayor velocidad, acompañando el avance infinito del jugador.
/// </summary>
public class GeneradorObstaculos : MonoBehaviour
{
    [Header("Prefab de Obstáculo (Opcional)")]
    [Tooltip("Prefab a instanciar. Si está vacío, creará automáticamente cubos con físicas.")]
    public GameObject prefabObstaculo;

    [Header("Frecuencia de Generación")]
    [Tooltip("Intervalo base en segundos entre cada obstáculo generado")]
    public float intervaloBase = 3.2f;

    [Tooltip("Intervalo mínimo posible (límite de dificultad máxima)")]
    public float intervaloMinimo = 0.85f;

    [Header("Velocidad de Obstáculos")]
    public float velocidadBaseObstaculo = 7f;
    public float velocidadMaximaObstaculo = 16f;

    [Header("Área de Generación Relativa al Avance")]
    [Tooltip("Rango lateral X sobre las plataformas donde pueden aparecer obstáculos")]
    public float rangoX = 9.5f;

    [Tooltip("Distancia por delante del jugador en el eje Z donde se generan los obstáculos")]
    public float posicionZGeneracion = 45f;

    [Tooltip("Altura Y sobre la plataforma a la que aparecen los obstáculos")]
    public float alturaY = 1.1f;

    private float temporizador = 0f;
    private float intervaloActual;
    private float velocidadActualObstaculo;
    private Transform jugador;
    private Material materialObstaculo;
    private readonly List<GameObject> obstaculosActivos = new List<GameObject>();

    private void Start()
    {
        intervaloActual = intervaloBase;
        velocidadActualObstaculo = velocidadBaseObstaculo;

        PlayerController3D pc = FindAnyObjectByType<PlayerController3D>();
        if (pc != null)
        {
            jugador = pc.transform;
        }

        if (GeneradorNiveles.Instance != null && GeneradorNiveles.Instance.materialPlataforma != null)
        {
            materialObstaculo = GeneradorNiveles.Instance.materialPlataforma;
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.JuegoTerminado)
        {
            return;
        }

        temporizador += Time.deltaTime;

        if (temporizador >= intervaloActual)
        {
            temporizador = 0f;
            GenerarObstaculo();
        }
    }

    /// <summary>
    /// Genera un obstáculo por delante del jugador en el escenario infinito.
    /// </summary>
    private void GenerarObstaculo()
    {
        float baseX = (jugador != null) ? jugador.position.x : 0f;
        float baseY = (jugador != null) ? Mathf.Max(0f, jugador.position.y - 2.5f) : 0f;
        float baseZ = (jugador != null) ? jugador.position.z : 0f;

        float posX = baseX + Random.Range(-rangoX, rangoX);
        Vector3 posSpawn = new Vector3(posX, baseY + alturaY, baseZ + posicionZGeneracion);

        GameObject nuevoObstaculo;

        if (prefabObstaculo != null)
        {
            nuevoObstaculo = Instantiate(prefabObstaculo, posSpawn, Quaternion.identity);
        }
        else
        {
            nuevoObstaculo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nuevoObstaculo.name = "Obstaculo_Auto";
            nuevoObstaculo.transform.position = posSpawn;
            nuevoObstaculo.transform.localScale = new Vector3(2f, 2f, 2f);

            Renderer ren = nuevoObstaculo.GetComponent<Renderer>();
            if (ren != null)
            {
                if (materialObstaculo != null)
                {
                    ren.sharedMaterial = materialObstaculo;
                }
                Color colorPeligro = new Color(0.95f, 0.25f, 0.15f, 1f);
                Material mat = ren.material;
                mat.color = colorPeligro;
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", colorPeligro);
                }
            }
        }

        Obstaculo scriptObs = nuevoObstaculo.GetComponent<Obstaculo>();
        if (scriptObs == null)
        {
            scriptObs = nuevoObstaculo.AddComponent<Obstaculo>();
        }

        scriptObs.modo = Obstaculo.ModoMovimiento.Lineal;
        scriptObs.velocidad = velocidadActualObstaculo;
        scriptObs.direccion = new Vector3(0f, 0f, -1f);
        scriptObs.destruirPorTiempo = true;

        obstaculosActivos.Add(nuevoObstaculo);
    }

    /// <summary>
    /// Actualiza la frecuencia y velocidad de los obstáculos según la curva de dificultad.
    /// </summary>
    public void ActualizarDificultad(float multiplicador)
    {
        intervaloActual = Mathf.Max(intervaloMinimo, intervaloBase / multiplicador);
        velocidadActualObstaculo = Mathf.Min(velocidadMaximaObstaculo, velocidadBaseObstaculo * multiplicador);
    }

    /// <summary>
    /// Elimina todos los obstáculos activos al reiniciar el Game Loop.
    /// </summary>
    public void LimpiarObstaculos()
    {
        for (int i = obstaculosActivos.Count - 1; i >= 0; i--)
        {
            if (obstaculosActivos[i] != null)
            {
                Destroy(obstaculosActivos[i]);
            }
        }
        obstaculosActivos.Clear();
        temporizador = 0f;
    }
}
