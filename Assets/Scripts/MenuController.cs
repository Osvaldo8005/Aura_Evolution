using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cumple con el Criterio 1 de la Actividad 4.1:
/// Navegación fluida entre las escenas principales y los menús.
/// </summary>
public class MenuController : MonoBehaviour
{
    [Tooltip("Nombre exacto de la escena de juego")]
    public string nombreEscenaJuego = "SampleScene";

    [Tooltip("Nombre exacto de la escena del menú")]
    public string nombreEscenaMenu = "MenuPrincipal";

    public void IniciarJuego()
    {
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    public void IrAlMenu()
    {
        SceneManager.LoadScene(nombreEscenaMenu);
    }

    public void ReiniciarNivel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SalirDelJuego()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
