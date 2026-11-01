using UnityEngine;
using UnityEngine.SceneManagement; // Necesario para cambiar de escena

public class MenuController : MonoBehaviour
{
    public void IniciarJuego()
    {
        // Carga la escena de tu juego. Asegúrate de que el nombre sea exacto.
        SceneManager.LoadScene("SampleScene"); 
    }
}