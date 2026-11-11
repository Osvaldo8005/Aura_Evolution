using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controlador de menús e interfaces de navegación principal (Actividades 4.1 y 4.5).
/// Permite navegar fluidamente entre el Menú Principal, el Nivel 1 (SampleScene), el Nivel 2 (Nivel2) y salir de la aplicación
/// utilizando SceneManager sin arrojar errores en la consola.
/// </summary>
public class MenuController : MonoBehaviour
{
    [Header("Nombres de Escenas (Build Settings)")]
    [Tooltip("Nombre exacto de la escena del Nivel 1")]
    public string nombreEscenaJuego = "SampleScene";

    [Tooltip("Nombre exacto de la escena del Nivel 2")]
    public string nombreEscenaNivel2 = "Nivel2";

    [Tooltip("Nombre exacto de la escena del Menú Principal")]
    public string nombreEscenaMenu = "MenuPrincipal";

    private const string KEY_RECORD = "Aura_PuntajeMaximo";

    private void Start()
    {
        Time.timeScale = 1f;
        BotonTactil.ReiniciarEstados();

        if (SceneManager.GetActiveScene().name == nombreEscenaMenu)
        {
            ConfigurarInterfazMenuPrincipal();
        }
    }

    private void ConfigurarInterfazMenuPrincipal()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.15f, 0.26f, 1f);
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer < 0) uiLayer = 5;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // 1. Título e historia de Aura Evolution
        if (canvas.transform.Find("Texto_Historia_Aura") == null && canvas.transform.Find("Titulo_Juego") == null)
        {
            GameObject txtObj = new GameObject("Texto_Historia_Aura", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.layer = uiLayer;
            txtObj.transform.SetParent(canvas.transform, false);

            RectTransform rect = txtObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.78f);
            rect.anchorMax = new Vector2(0.5f, 0.78f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1000f, 150f);

            TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = "AURA EVOLUTION\n<size=52%>Despierta tu Aura. Avanza y evoluciona a través de los niveles.</size>";
            tmp.fontSize = 54;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.78f, 0.93f, 1f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
        }

        // 2. Mostrar el récord guardado en el Menú Principal
        if (canvas.transform.Find("Texto_Record_Menu") == null)
        {
            int record = PlayerPrefs.GetInt(KEY_RECORD, 0);
            GameObject recObj = new GameObject("Texto_Record_Menu", typeof(RectTransform), typeof(TextMeshProUGUI));
            recObj.layer = uiLayer;
            recObj.transform.SetParent(canvas.transform, false);

            RectTransform rectR = recObj.GetComponent<RectTransform>();
            rectR.anchorMin = new Vector2(0.5f, 0.62f);
            rectR.anchorMax = new Vector2(0.5f, 0.62f);
            rectR.pivot = new Vector2(0.5f, 0.5f);
            rectR.anchoredPosition = Vector2.zero;
            rectR.sizeDelta = new Vector2(600f, 60f);

            TextMeshProUGUI tmpR = recObj.GetComponent<TextMeshProUGUI>();
            tmpR.text = $"Mejor Récord Guardado: {record} pts";
            tmpR.fontSize = 28;
            tmpR.fontStyle = FontStyles.Bold;
            tmpR.color = new Color(1f, 0.88f, 0.28f, 1f);
            tmpR.alignment = TextAlignmentOptions.Center;
        }

        // 3. Estilizar el botón principal de Jugar Nivel 1 si ya existe en la escena
        Transform btnExistente = canvas.transform.Find("Button") ?? canvas.transform.Find("Btn_Jugar");
        if (btnExistente != null)
        {
            RectTransform bRect = btnExistente.GetComponent<RectTransform>();
            if (bRect != null)
            {
                bRect.anchorMin = new Vector2(0.5f, 0.5f);
                bRect.anchorMax = new Vector2(0.5f, 0.5f);
                bRect.pivot = new Vector2(0.5f, 0.5f);
                bRect.anchoredPosition = new Vector2(0f, 20f);
                bRect.sizeDelta = new Vector2(420f, 80f);
            }

            Image bImg = btnExistente.GetComponent<Image>();
            if (bImg != null)
            {
                bImg.color = new Color(0.16f, 0.66f, 0.36f, 0.96f);
            }

            TMP_Text bTmp = btnExistente.GetComponentInChildren<TMP_Text>();
            if (bTmp != null)
            {
                bTmp.text = "JUGAR NIVEL 1";
                bTmp.fontSize = 30;
                bTmp.fontStyle = FontStyles.Bold;
                bTmp.color = Color.white;
            }
        }
        else
        {
            CrearBotonMenu(canvas.transform, "Btn_JugarNivel1", uiLayer,
                new Vector2(0f, 20f), new Vector2(420f, 80f),
                "JUGAR NIVEL 1", new Color(0.16f, 0.66f, 0.36f, 0.96f), IniciarJuego);
        }

        // 4. Botón para ir directo al Nivel 2
        if (canvas.transform.Find("Btn_JugarNivel2") == null)
        {
            CrearBotonMenu(canvas.transform, "Btn_JugarNivel2", uiLayer,
                new Vector2(0f, -85f), new Vector2(420f, 80f),
                "JUGAR NIVEL 2", new Color(0.24f, 0.44f, 0.84f, 0.96f), IniciarNivel2);
        }

        // 5. Botón para Salir del Juego
        if (canvas.transform.Find("Btn_Salir") == null)
        {
            CrearBotonMenu(canvas.transform, "Btn_Salir", uiLayer,
                new Vector2(0f, -190f), new Vector2(420f, 80f),
                "SALIR DEL JUEGO", new Color(0.78f, 0.24f, 0.26f, 0.96f), SalirDelJuego);
        }
    }

    private void CrearBotonMenu(Transform parent, string nombre, int uiLayer,
        Vector2 pos, Vector2 size, string texto, Color colorFondo, UnityEngine.Events.UnityAction accion)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.layer = uiLayer;
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image img = obj.GetComponent<Image>();
        img.color = colorFondo;

        Button btn = obj.GetComponent<Button>();
        btn.onClick.AddListener(accion);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.layer = uiLayer;
        txtObj.transform.SetParent(obj.transform, false);

        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = 28;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    public void IniciarJuego()
    {
        Time.timeScale = 1f;
        BotonTactil.ReiniciarEstados();
        SceneManager.LoadScene(nombreEscenaJuego);
    }

    public void IniciarNivel2()
    {
        Time.timeScale = 1f;
        BotonTactil.ReiniciarEstados();
        SceneManager.LoadScene(nombreEscenaNivel2);
    }

    public void IrAlMenu()
    {
        Time.timeScale = 1f;
        BotonTactil.ReiniciarEstados();
        SceneManager.LoadScene(nombreEscenaMenu);
    }

    public void ReiniciarNivel()
    {
        Time.timeScale = 1f;
        BotonTactil.ReiniciarEstados();
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
