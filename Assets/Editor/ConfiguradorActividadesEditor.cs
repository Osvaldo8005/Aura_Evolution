using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Configuración automática robusta para cumplir al 100% con los criterios de la Actividad 4.1.
/// Garantiza que todos los objetos de UI se creen con RectTransform y en la capa 'UI' (Layer 5).
/// </summary>
public static class ConfiguradorActividadesEditor
{
    [MenuItem("Aura Evolution/Configurar Actividades 4.1 y 4.2 (UI, Camara, GameManager)")]
    public static void ConfigurarTodo()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "SampleScene")
        {
            EditorUtility.DisplayDialog("Aviso", "Por favor, abre la escena 'SampleScene' antes de ejecutar esta configuración.", "Entendido");
            return;
        }

        Undo.IncrementCurrentGroup();
        int groupIndex = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configurar Actividad 4.1");

        int uiLayer = LayerMask.NameToLayer("UI");

        // 1. Limpieza de objetos huérfanos anteriores si existieran
        LimpiarObjetoSiExiste("Boton_Menu");
        LimpiarObjetoSiExiste("Canvas_HUD");
        LimpiarObjetoSiExiste("GameManager");
        LimpiarObjetoSiExiste("GeneradorObstaculos");

        // 2. Configurar Jugador
        GameObject jugador = GameObject.Find("Jugador");
        PlayerController3D pc = null;
        if (jugador != null)
        {
            pc = jugador.GetComponent<PlayerController3D>();
            if (pc == null) pc = Undo.AddComponent<PlayerController3D>(jugador);
            EditorUtility.SetDirty(pc);
        }

        // 3. Configurar Cámara (Criterios 3 y 4: X y Z sincronizados, Y estable, Z constante respecto al jugador)
        Camera cam = Camera.main;
        if (cam != null)
        {
            CameraFollow3D cf = cam.GetComponent<CameraFollow3D>();
            if (cf == null) cf = Undo.AddComponent<CameraFollow3D>(cam.gameObject);

            cf.enabled = true;
            if (jugador != null) cf.objetivo = jugador.transform;
            cf.seguirEjeX = true;
            cf.offsetX = 0f;
            cf.seguirEjeY = true;
            cf.offsetY = 2.5f;
            cf.alturaFijaY = 3.5f;
            cf.offsetConstanteZ = -7.5f;
            cf.suavidad = 8f;
            cf.fijarZMundial = false;
            cf.offsetMira = new Vector3(0f, 1.2f, 0f);
            EditorUtility.SetDirty(cf);
        }

        // 4. Configurar EventSystem
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(eventSystem, "Crear EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 5. Crear Canvas adaptado a móviles
        GameObject canvasObj = new GameObject("Canvas_HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObj, "Crear Canvas_HUD");
        canvasObj.layer = uiLayer;

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 6. Botón de Regresar al Menú (Criterio 1: Navegación fluida)
        CrearBotonMenu(canvasObj.transform, uiLayer);

        // 7. Controles táctiles en pantalla para móvil (Criterio 2)
        CrearControlesTactiles(canvasObj.transform, uiLayer);

        // 8. Textos UI del HUD (Puntaje, Récord, Dificultad)
        TMP_Text textoPuntaje = CrearTextoTMP(canvasObj.transform, "Texto_Puntaje", uiLayer,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(30f, -30f), new Vector2(500f, 60f),
            "Puntaje: 0  |  Avance: 0m", TextAlignmentOptions.TopLeft);

        TMP_Text textoRecord = CrearTextoTMP(canvasObj.transform, "Texto_Record", uiLayer,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -30f), new Vector2(350f, 60f),
            "Récord: 0", TextAlignmentOptions.Top);

        TMP_Text textoDificultad = CrearTextoTMP(canvasObj.transform, "Texto_Dificultad", uiLayer,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-220f, -30f), new Vector2(350f, 60f),
            "Dificultad: Nivel 1", TextAlignmentOptions.TopRight);

        // 9. Configurar GameManager
        GameObject gmObj = new GameObject("GameManager");
        Undo.RegisterCreatedObjectUndo(gmObj, "Crear GameManager");
        GameManager gm = gmObj.AddComponent<GameManager>();
        gm.textoPuntaje = textoPuntaje;
        gm.textoRecord = textoRecord;
        gm.textoDificultad = textoDificultad;
        EditorUtility.SetDirty(gm);

        // 10. Configurar Generador de Obstáculos
        GameObject spawnerObj = new GameObject("GeneradorObstaculos");
        Undo.RegisterCreatedObjectUndo(spawnerObj, "Crear GeneradorObstaculos");
        GeneradorObstaculos spawner = spawnerObj.AddComponent<GeneradorObstaculos>();
        EditorUtility.SetDirty(spawner);

        // 11. Conectar botones táctiles al personaje
        if (pc != null)
        {
            pc.ActualizarBotonesTactiles();
        }

        // 12. Coleccionables
        CrearColeccionablesSiNoExisten();

        Undo.CollapseUndoOperations(groupIndex);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog("Actividad 4.1 Configurada",
            "¡Interfaz y Juego configurados al 100%!\n\n" +
            "✔ Botón 'Menú' visible arriba a la derecha para volver a MenuPrincipal.\n" +
            "✔ Controles táctiles móviles en pantalla (Cruceta y Salto).\n" +
            "✔ Textos de Puntaje, Récord y Dificultad vinculados.\n" +
            "✔ Cámara sincronizada en X y Z con altura Y fija.\n\n" +
            "La escena ha sido guardada automáticamente.",
            "¡Listo!");
    }

    private static void LimpiarObjetoSiExiste(string nombre)
    {
        GameObject obj = GameObject.Find(nombre);
        if (obj != null)
        {
            Undo.DestroyObjectImmediate(obj);
        }
    }

    private static void CrearBotonMenu(Transform canvas, int layer)
    {
        GameObject btnObj = new GameObject("Boton_Menu", typeof(RectTransform), typeof(Image), typeof(Button), typeof(BotonMenuAccion));
        Undo.RegisterCreatedObjectUndo(btnObj, "Crear Boton Menu");
        btnObj.layer = layer;
        btnObj.transform.SetParent(canvas, false);

        RectTransform rect = btnObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-30f, -25f);
        rect.sizeDelta = new Vector2(160f, 50f);

        Image img = btnObj.GetComponent<Image>();
        img.color = new Color(0.18f, 0.45f, 0.85f, 0.95f);

        // Texto hijo del botón
        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.layer = layer;
        txtObj.transform.SetParent(btnObj.transform, false);

        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = "Menú";
        tmp.fontSize = 26;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private static void CrearControlesTactiles(Transform canvas, int layer)
    {
        GameObject grupo = new GameObject("Controles_Moviles", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(grupo, "Crear Controles Moviles");
        grupo.layer = layer;
        grupo.transform.SetParent(canvas, false);

        RectTransform gRect = grupo.GetComponent<RectTransform>();
        gRect.anchorMin = Vector2.zero;
        gRect.anchorMax = Vector2.one;
        gRect.sizeDelta = Vector2.zero;

        // Cruceta D-Pad (Esquina inferior izquierda)
        CrearBotonTactil(grupo.transform, "Btn_Arriba", layer, new Vector2(160f, 240f), "▲", BotonTactil.TipoBoton.Vertical, 1f);
        CrearBotonTactil(grupo.transform, "Btn_Abajo", layer, new Vector2(160f, 80f), "▼", BotonTactil.TipoBoton.Vertical, -1f);
        CrearBotonTactil(grupo.transform, "Btn_Izquierda", layer, new Vector2(80f, 160f), "◀", BotonTactil.TipoBoton.Horizontal, -1f);
        CrearBotonTactil(grupo.transform, "Btn_Derecha", layer, new Vector2(240f, 160f), "▶", BotonTactil.TipoBoton.Horizontal, 1f);

        // Botón Salto (Esquina inferior derecha)
        CrearBotonTactil(grupo.transform, "Btn_Salto", layer, new Vector2(-120f, 120f), "SALTO", BotonTactil.TipoBoton.Salto, 1f, 
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(130f, 130f));
    }

    private static void CrearBotonTactil(Transform parent, string nombre, int layer, Vector2 pos, string texto,
        BotonTactil.TipoBoton tipo, float valor, Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? size = null)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(BotonTactil));
        Undo.RegisterCreatedObjectUndo(obj, "Crear " + nombre);
        obj.layer = layer;
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin ?? new Vector2(0f, 0f);
        rect.anchorMax = anchorMax ?? new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size ?? new Vector2(75f, 75f);

        Image img = obj.GetComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.18f, 0.75f);

        BotonTactil bt = obj.GetComponent<BotonTactil>();
        bt.tipo = tipo;
        bt.valor = valor;

        // Texto hijo
        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.layer = layer;
        txtObj.transform.SetParent(obj.transform, false);

        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = (texto.Length > 2) ? 22 : 36;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private static TMP_Text CrearTextoTMP(Transform parent, string nombre, int layer,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPosition, Vector2 sizeDelta, string textoInicial, TextAlignmentOptions alineacion)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(obj, "Crear " + nombre);
        obj.layer = layer;
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = textoInicial;
        tmp.fontSize = 26;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = alineacion;

        return tmp;
    }

    private static void CrearColeccionablesSiNoExisten()
    {
        if (GameObject.Find("Coleccionables_Grupo") != null) return;

        GameObject grupo = new GameObject("Coleccionables_Grupo");
        Undo.RegisterCreatedObjectUndo(grupo, "Crear Grupo Coleccionables");

        Vector3[] posiciones = new Vector3[]
        {
            new Vector3(-4f, 1f, 5f),
            new Vector3(0f, 1f, 10f),
            new Vector3(4f, 1f, 15f),
            new Vector3(-6f, 1f, 20f),
            new Vector3(6f, 1f, 25f)
        };

        for (int i = 0; i < posiciones.Length; i++)
        {
            GameObject moneda = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            moneda.name = "Moneda_" + (i + 1);
            moneda.transform.SetParent(grupo.transform);
            moneda.transform.position = posiciones[i];
            moneda.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            Renderer ren = moneda.GetComponent<Renderer>();
            if (ren != null)
            {
                ren.material.color = new Color(1f, 0.84f, 0f);
            }

            moneda.AddComponent<Coleccionable>();
        }
    }

    [MenuItem("Aura Evolution/Configurar Escena Menú Principal")]
    public static void ConfigurarMenuPrincipal()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "MenuPrincipal")
        {
            EditorUtility.DisplayDialog("Aviso", "Por favor, abre la escena 'MenuPrincipal' antes de ejecutar esta configuración.", "Entendido");
            return;
        }

        Undo.IncrementCurrentGroup();
        int groupIndex = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configurar Menú Principal");

        int uiLayer = LayerMask.NameToLayer("UI");

        // EventSystem
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(eventSystem, "Crear EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Buscar o crear MenuController
        MenuController mc = Object.FindAnyObjectByType<MenuController>();
        if (mc == null)
        {
            GameObject mcObj = new GameObject("MenuController");
            Undo.RegisterCreatedObjectUndo(mcObj, "Crear MenuController");
            mc = mcObj.AddComponent<MenuController>();
        }
        mc.nombreEscenaJuego = "SampleScene";
        mc.nombreEscenaMenu = "MenuPrincipal";

        // Limpiar Canvas previo si existe para regenerar limpio
        LimpiarObjetoSiExiste("Canvas");
        LimpiarObjetoSiExiste("Canvas_Menu");

        // Canvas Adaptado a Móvil (Criterio 1)
        GameObject canvasObj = new GameObject("Canvas_Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasObj, "Crear Canvas_Menu");
        canvasObj.layer = uiLayer;

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Título del Juego
        CrearTextoTMP(canvasObj.transform, "Titulo_Juego", uiLayer,
            new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(900f, 120f), "AURA EVOLUTION", TextAlignmentOptions.Center);

        // Subtítulo
        CrearTextoTMP(canvasObj.transform, "Subtitulo_Juego", uiLayer,
            new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.65f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(900f, 60f), "Desarrollo de Videojuegos 3D Móvil", TextAlignmentOptions.Center);

        // Botón Jugar
        GameObject btnJugar = CrearBotonGenerico(canvasObj.transform, "Btn_Jugar", uiLayer,
            new Vector2(0f, -20f), new Vector2(380f, 90f), "JUGAR", new Color(0.18f, 0.65f, 0.35f, 0.95f));
        Button bJugar = btnJugar.GetComponent<Button>();
        UnityEventTools.AddPersistentListener(bJugar.onClick, mc.IniciarJuego);

        // Botón Salir
        GameObject btnSalir = CrearBotonGenerico(canvasObj.transform, "Btn_Salir", uiLayer,
            new Vector2(0f, -140f), new Vector2(380f, 90f), "SALIR", new Color(0.85f, 0.25f, 0.25f, 0.95f));
        Button bSalir = btnSalir.GetComponent<Button>();
        UnityEventTools.AddPersistentListener(bSalir.onClick, mc.SalirDelJuego);

        Undo.CollapseUndoOperations(groupIndex);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog("Menú Principal Configurado",
            "¡Menú Principal configurado con éxito!\n\n" +
            "✔ Canvas responsive optimizado para móvil (1920x1080).\n" +
            "✔ Botón 'JUGAR' vinculado a cargar SampleScene.\n" +
            "✔ Botón 'SALIR' vinculado a salir del juego.", "Excelente");
    }

    private static GameObject CrearBotonGenerico(Transform parent, string nombre, int layer,
        Vector2 posCentro, Vector2 tamano, string texto, Color colorFondo)
    {
        GameObject obj = new GameObject(nombre, typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(obj, "Crear " + nombre);
        obj.layer = layer;
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = posCentro;
        rect.sizeDelta = tamano;

        Image img = obj.GetComponent<Image>();
        img.color = colorFondo;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.layer = layer;
        txtObj.transform.SetParent(obj.transform, false);

        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = 32;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return obj;
    }
}
