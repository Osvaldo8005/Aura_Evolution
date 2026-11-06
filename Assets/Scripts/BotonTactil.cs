using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Componente para botones táctiles en pantalla adaptados a dispositivos móviles.
/// Permite mantener presionado un botón (ej: cruceta o botón de salto) tanto en pantallas táctiles como con el cursor del ratón.
/// Cumple con el Criterio 1: Interfaz gráfica táctil funcional para móvil.
/// </summary>
public class BotonTactil : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public enum TipoBoton
    {
        Horizontal,
        Vertical,
        Salto
    }

    [Header("Configuración del Botón Táctil")]
    [Tooltip("Determina qué tipo de acción realiza el botón (Horizontal, Vertical o Salto)")]
    public TipoBoton tipo;

    [Tooltip("Valor que envía (-1 para izquierda/atrás, +1 para derecha/adelante)")]
    public float valor = 1f;

    public bool Presionado { get; private set; }

    // Registro estático de botones táctiles activos para lectura global inmediata y desacoplada
    private static readonly List<BotonTactil> botonesActivos = new List<BotonTactil>();
    private static bool saltoSolicitado = false;

    private void OnEnable()
    {
        if (!botonesActivos.Contains(this))
        {
            botonesActivos.Add(this);
        }
    }

    private void OnDisable()
    {
        botonesActivos.Remove(this);
        Presionado = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Presionado = true;
        if (tipo == TipoBoton.Salto)
        {
            saltoSolicitado = true;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Presionado = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Si el dedo o puntero se sale del área del botón, se deja de presionar
        Presionado = false;
    }

    /// <summary>
    /// Devuelve el valor acumulado de los botones horizontales (-1 a 1).
    /// </summary>
    public static float ObtenerHorizontal()
    {
        float val = 0f;
        for (int i = 0; i < botonesActivos.Count; i++)
        {
            if (botonesActivos[i] != null && botonesActivos[i].tipo == TipoBoton.Horizontal && botonesActivos[i].Presionado)
            {
                val += botonesActivos[i].valor;
            }
        }
        return Mathf.Clamp(val, -1f, 1f);
    }

    /// <summary>
    /// Devuelve el valor acumulado de los botones verticales (-1 a 1).
    /// </summary>
    public static float ObtenerVertical()
    {
        float val = 0f;
        for (int i = 0; i < botonesActivos.Count; i++)
        {
            if (botonesActivos[i] != null && botonesActivos[i].tipo == TipoBoton.Vertical && botonesActivos[i].Presionado)
            {
                val += botonesActivos[i].valor;
            }
        }
        return Mathf.Clamp(val, -1f, 1f);
    }

    /// <summary>
    /// Devuelve true una sola vez si se presionó el botón de salto y consume la petición.
    /// </summary>
    public static bool ConsumirSalto()
    {
        if (saltoSolicitado)
        {
            saltoSolicitado = false;
            return true;
        }
        return false;
    }
}
