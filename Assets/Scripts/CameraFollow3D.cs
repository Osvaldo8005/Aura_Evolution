using UnityEngine;

public class CameraFollow3D : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] private Transform objetivo;

    [Header("Configuracion")]
    [SerializeField] private float offsetY = 8f;
    [SerializeField] private float offsetZ = -10f;
    [SerializeField] private float suavidad = 5f;

    private float zFija;

    void Start()
    {
        // Guardamos la posición inicial de la Z para que nunca cambie
        zFija = transform.position.z;
    }

    private void LateUpdate()
    {
        if (objetivo == null) return;

        // La cámara solo calcula el movimiento en X e Y
        float xDeseada = objetivo.position.x;
        float yDeseada = objetivo.position.y + offsetY;

        // Creamos la posición deseada manteniendo la Z fija
        Vector3 posicionDeseada = new Vector3(xDeseada, yDeseada, zFija);

        // Suavizar el movimiento
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, suavidad * Time.deltaTime);

        // Mantener la rotación fija mirando al frente (opcional, para evitar giros raros)
        transform.rotation = Quaternion.Euler(20f, 0f, 0f); 
    }
}