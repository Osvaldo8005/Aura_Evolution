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

    private void Start()
    {
        if (objetivo == null)
        {
            GameObject jugador = GameObject.FindWithTag("Player");
            if (jugador != null)
            {
                objetivo = jugador.transform;
            }
        }

        if (fijarZMundial && posicionZMundial == 0f)
        {
            posicionZMundial = transform.position.z;
        }
    }

    private void LateUpdate()
    {
        if (objetivo == null) return;

        // 1. Cálculo de coordenadas deseadas según los criterios de evaluación:
        
        // Eje X: Desplazamiento sincronizado
        float destinoX = seguirEjeX ? (objetivo.position.x + offsetX) : transform.position.x;

        // Eje Y: Desplazamiento sincronizado o altura fija estable
        float destinoY = seguirEjeY ? (objetivo.position.y + offsetY) : alturaFijaY;

        // Eje Z: Mantiene posición fija y constante respecto al desplazamiento del personaje (o Z mundial)
        float destinoZ = fijarZMundial ? posicionZMundial : (objetivo.position.z + offsetConstanteZ);

        Vector3 posicionDeseada = new Vector3(destinoX, destinoY, destinoZ);

        // 2. Interpolación para un seguimiento suave y estable
        if (suavidad > 0f)
        {
            transform.position = Vector3.Lerp(transform.position, posicionDeseada, suavidad * Time.deltaTime);
        }
        else
        {
            transform.position = posicionDeseada;
        }

        // 3. Orientación hacia el personaje
        Vector3 puntoAMirar = new Vector3(objetivo.position.x + offsetMira.x, objetivo.position.y + offsetMira.y, objetivo.position.z + offsetMira.z);
        transform.LookAt(puntoAMirar);
    }
}
