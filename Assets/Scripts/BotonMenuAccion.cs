using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Componente para el botón de navegación hacia el Menú Principal.
/// Se conecta automáticamente al Button para que funcione siempre sin fallos.
/// </summary>
public class BotonMenuAccion : MonoBehaviour
{
    public string escenaMenu = "MenuPrincipal";

    private void Start()
    {
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveListener(CargarMenu);
            btn.onClick.AddListener(CargarMenu);
        }
    }

    public void CargarMenu()
    {
        SceneManager.LoadScene(escenaMenu);
    }
}
