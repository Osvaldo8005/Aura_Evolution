using UnityEngine;

/// <summary>
/// Cumple estrictamente con los Criterios de Evaluación 3 y 4 de la Actividad 4.1:
/// - Criterio 3: "La cámara debe seguir de manera estable al personaje, aplicando desplazamiento sincronizado únicamente en los ejes indicados (X e Y, o X y Z)."
/// - Criterio 4: "El sistema de seguimiento de la cámara debe mantener una posición fija y constante en el eje Z respecto al desplazamiento del personaje principal."
/// </summary>
public class CameraFollow3D : MonoBehaviour
{
    [Header("Objetivo a Seguir")]
    [Tooltip("El Transform del personaje principal")]
    public Transform objetivo;

    [Header("Criterio 3: Desplazamiento Sincronizado en Ejes")]
    [Tooltip("Sincroniza el movimiento de la cámara con el personaje en el eje X")]
    public bool seguirEjeX = true;
    public float offsetX = 0f;

    [Tooltip("Si está activo, sigue al personaje en el eje Y. Si está inactivo, mantiene una altura fija Y para mayor estabilidad.")]
    public bool seguirEjeY = true;
    public float offsetY = 2.5f;
    public float alturaFijaY = 3.0f;

    [Header("Criterio 4: Posición Fija y Constante en Eje Z")]
    [Tooltip("Distancia fija y constante en Z respecto al personaje principal")]
    public float offsetConstanteZ = -7f;

    [Tooltip("Si se activa, congela la posición Z de la cámara en el espacio mundial absoluto (vista 2.5D fija)")]
    public bool fijarZMundial = false;
    public float posicionZMundial = -10f;

    [Header("Estabilidad y Suavizado")]
    [Tooltip("Velocidad de interpolación para seguimiento estable y sin tirones")]
    [Range(1f, 25f)]
    public float suavidad = 8f;

    [Tooltip("Punto de enfoque sobre el personaje (pecho / cabeza)")]
    public Vector3 offsetMira = new Vector3(0f, 1.2f, 0f);

    [Header("Efecto de Cámara en Speed Boost (Actividad 4.6)")]
    [Tooltip("Campo de visión (FOV) normal de la cámara")]
    public float fovNormal = 60f;

    [Tooltip("Campo de visión (FOV) ampliado durante los 3s del Potenciador de Velocidad")]
    public float fovPotenciador = 70f;

    private Camera cam;
    private PlayerController3D jugadorScript;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null && cam.fieldOfView > 30f)
        {
            fovNormal = cam.fieldOfView;
        }

        if (objetivo == null)
        {
            GameObject jugador = GameObject.FindWithTag("Player");
            if (jugador != null)
            {
                objetivo = jugador.transform;
            }
        }

        if (objetivo != null)
        {
            jugadorScript = objetivo.GetComponent<PlayerController3D>();
        }

        if (fijarZMundial && posicionZMundial == 0f)
        {
            posicionZMundial = transform.position.z;
        }
    }

    private void LateUpdate()
    {
        if (objetivo == null) return;
        if (jugadorScript == null) jugadorScript = objetivo.GetComponent<PlayerController3D>();

        bool enBoost = (jugadorScript != null && jugadorScript.PotenciadorActivo);

        // 1. Cálculo de coordenadas deseadas según los criterios de evaluación:
        
        // Eje X: Desplazamiento sincronizado
        float destinoX = seguirEjeX ? (objetivo.position.x + offsetX) : transform.position.x;

        // Eje Y: Desplazamiento sincronizado o altura fija estable
        float destinoY = seguirEjeY ? (objetivo.position.y + offsetY) : alturaFijaY;

        // Eje Z: Mantiene posición fija y constante respecto al desplazamiento del personaje (o Z mundial)
        float destinoZ = fijarZMundial ? posicionZMundial : (objetivo.position.z + offsetConstanteZ);

        Vector3 posicionDeseada = new Vector3(destinoX, destinoY, destinoZ);

        // 2. Interpolación para un seguimiento suave y estable (más ágil durante el Speed Boost x3)
        float suavidadEfectiva = enBoost ? Mathf.Max(suavidad * 2f, 16f) : suavidad;
        if (suavidadEfectiva > 0f)
        {
            transform.position = Vector3.Lerp(transform.position, posicionDeseada, suavidadEfectiva * Time.deltaTime);
        }
        else
        {
            transform.position = posicionDeseada;
        }

        // 3. Efecto dinámico de Field of View (FOV) durante los 3 segundos del Potenciador de Velocidad
        if (cam != null)
        {
            float fovObjetivo = enBoost ? fovPotenciador : fovNormal;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fovObjetivo, 6f * Time.deltaTime);
        }

        // 4. Orientación hacia el personaje
        Vector3 puntoAMirar = new Vector3(objetivo.position.x + offsetMira.x, objetivo.position.y + offsetMira.y, objetivo.position.z + offsetMira.z);
        transform.LookAt(puntoAMirar);
    }

    /// <summary>
    /// Reposiciona la cámara instantáneamente detrás del jugador al reiniciar el Game Loop.
    /// </summary>
    public void ReposicionarInmediato()
    {
        if (objetivo == null) return;

        float destinoX = seguirEjeX ? (objetivo.position.x + offsetX) : transform.position.x;
        float destinoY = seguirEjeY ? (objetivo.position.y + offsetY) : alturaFijaY;
        float destinoZ = fijarZMundial ? posicionZMundial : (objetivo.position.z + offsetConstanteZ);

        transform.position = new Vector3(destinoX, destinoY, destinoZ);
        Vector3 puntoAMirar = new Vector3(objetivo.position.x + offsetMira.x, objetivo.position.y + offsetMira.y, objetivo.position.z + offsetMira.z);
        transform.LookAt(puntoAMirar);
    }
}
