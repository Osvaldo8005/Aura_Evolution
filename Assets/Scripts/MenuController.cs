using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

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

    private void Start()
    {
        // Si estamos en el Menú Principal, asegurar el texto inspirador de la historia de Aura Evolution
        if (SceneManager.GetActiveScene().name == nombreEscenaMenu)
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null && canvas.transform.Find("Texto_Historia_Aura") == null)
            {
                Transform subtitulo = canvas.transform.Find("Subtitulo_Juego");
                if (subtitulo != null)
                {
                    TMP_Text tmpSub = subtitulo.GetComponent<TMP_Text>();
                    if (tmpSub != null)
                    {
                        tmpSub.text = "Despierta tu Aura. Avanza y evoluciona.";
                    }
                }
                else
                {
                    int uiLayer = LayerMask.NameToLayer("UI");
                    if (uiLayer < 0) uiLayer = 5;

                    GameObject txtObj = new GameObject("Texto_Historia_Aura", typeof(RectTransform), typeof(TextMeshProUGUI));
                    txtObj.layer = uiLayer;
                    txtObj.transform.SetParent(canvas.transform, false);

                    RectTransform rect = txtObj.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.5f, 0.72f);
                    rect.anchorMax = new Vector2(0.5f, 0.72f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.sizeDelta = new Vector2(850f, 80f);

                    TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
                    tmp.text = "AURA EVOLUTION\n<size=65%>Despierta tu Aura. Avanza y evoluciona.</size>";
                    tmp.fontSize = 42;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = new Color(0.75f, 0.92f, 1f, 1f);
                    tmp.alignment = TextAlignmentOptions.Center;
                }
            }
        }
    }

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
