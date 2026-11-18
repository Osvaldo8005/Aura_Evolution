using UnityEngine;

/// <summary>
/// Representa el objeto Potenciador de Velocidad (Speed Boost) de la Actividad 4.6.
/// Es un cubo amarillo que aparece en el escenario procedural; cuando el jugador lo toca,
/// multiplica su velocidad, activa el TrailRenderer durante máximo 3 segundos y se destruye.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PotenciadorVelocidad : MonoBehaviour
{
    [Header("Configuración del Potenciador (Actividad 4.6)")]
    [Tooltip("Factor por el que se multiplica la velocidad del jugador al recoger el cubo")]
    public float multiplicadorVelocidad = 3f;

    [Tooltip("Duración del efecto en segundos (máximo 3 segundos por requisito)")]
    [Range(0.5f, 3f)]
    public float duracionEfecto = 3f;

    [Tooltip("Puntos opcionales de bonificación al recoger el potenciador")]
    public int puntosExtra = 25;

    [Header("Animación Visual del Cubo Amarillo")]
    [Tooltip("Velocidad de rotación para que el cubo destaque en la plataforma")]
    public float velocidadGiro = 110f;

    [Tooltip("Amplitud de flotación vertical")]
    public float amplitudFlotacion = 0.22f;

    [Tooltip("Color amarillo llamativo del potenciador")]
    public Color colorCubo = new Color(1f, 0.92f, 0.04f, 1f);

    private Vector3 posicionInicial;
    private bool consumido = false;

    private void Start()
    {
        posicionInicial = transform.position;
        duracionEfecto = Mathf.Clamp(duracionEfecto, 0.5f, 3f);

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        AplicarColorAmarillo();
    }

    private void Update()
    {
        // Rotación y flotación suave para identificar fácilmente el cubo en el nivel
        transform.Rotate(Vector3.up, velocidadGiro * Time.deltaTime, Space.World);

        float nuevaY = posicionInicial.y + Mathf.Sin(Time.time * 3.5f) * amplitudFlotacion;
        transform.position = new Vector3(transform.position.x, nuevaY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        IntentarActivarPotenciador(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        IntentarActivarPotenciador(collision.gameObject);
    }

    private void IntentarActivarPotenciador(GameObject obj)
    {
        if (consumido || obj == null) return;

        PlayerController3D jugador = obj.GetComponent<PlayerController3D>();
        if (jugador == null)
        {
            jugador = obj.GetComponentInParent<PlayerController3D>();
        }

        if (jugador != null || obj.CompareTag("Player") || obj.name.Contains("Jugador"))
        {
            if (jugador == null)
            {
                jugador = FindAnyObjectByType<PlayerController3D>();
            }

            if (jugador != null)
            {
                consumido = true;
                float duracionSegura = Mathf.Min(3f, duracionEfecto);
                jugador.ActivarPotenciadorVelocidad(multiplicadorVelocidad, duracionSegura);

                if (puntosExtra > 0 && GameManager.Instance != null)
                {
                    GameManager.Instance.SumarPuntos(puntosExtra);
                }

                CrearDestelloRecoleccion();
                Destroy(gameObject);
            }
        }
    }

    private void CrearDestelloRecoleccion()
    {
        GameObject fxObj = new GameObject("Destello_PotenciadorVelocidad");
        fxObj.transform.position = transform.position;

        ParticleSystem ps = fxObj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = 0.45f;
        main.startSpeed = 5.5f;
        main.startSize = 0.28f;
        main.startColor = new Color(1f, 0.92f, 0.1f, 1f);
        main.maxParticles = 20;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 16) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;

        ParticleSystemRenderer psRen = fxObj.GetComponent<ParticleSystemRenderer>();
        Shader shaderSprites = Shader.Find("Sprites/Default");
        if (psRen != null && shaderSprites != null)
        {
            psRen.material = new Material(shaderSprites);
        }

        ps.Play();
        Destroy(fxObj, 0.8f);
    }

    public void AplicarColorAmarillo()
    {
        Renderer ren = GetComponent<Renderer>();
        if (ren == null) return;

        Material mat = ren.material;
        if (mat != null)
        {
            mat.color = colorCubo;
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", colorCubo);
            }
        }
    }
}
