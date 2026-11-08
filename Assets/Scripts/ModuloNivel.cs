using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa un módulo o sección individual del escenario generado en tiempo de ejecución.
/// Cumple con los criterios de la Actividad 4.3:
/// - Permite configurar desde el Inspector las dimensiones y las porciones que componen cada sección del nivel.
/// - Activa dinámicamente porciones seguras, Zonas de Peligro (DeathParts), obstáculos móviles y coleccionables
///   según la curva de dificultad progresiva.
/// </summary>
public class ModuloNivel : MonoBehaviour
{
    [Header("Identificación y Dimensiones de la Sección")]
    [Tooltip("Índice secuencial del módulo generado")]
    public int indiceModulo = 0;

    [Tooltip("Ancho de la plataforma en el eje X")]
    public float anchoX = 22f;

    [Tooltip("Grosor de la plataforma en el eje Y")]
    public float grosorY = 1.2f;

    [Tooltip("Longitud de la plataforma en el eje Z")]
    public float longitudZ = 22f;

    [Header("Porciones de la Sección (Inspector / Procedural)")]
    [Tooltip("Cantidad de subdivisiones o porciones que componen este módulo")]
    [Range(2, 8)]
    public int cantidadPorciones = 4;

    [Tooltip("Cantidad de porciones peligrosas (DeathParts) activas en este módulo")]
    public int zonasPeligroActivas = 0;

    [Tooltip("Lista de porciones que componen el módulo (se llena automáticamente o desde un Prefab en el Inspector)")]
    public List<GameObject> porcionesDelModulo = new List<GameObject>();

    [Tooltip("Lista de Zonas de Peligro (DeathParts) generadas o activadas en este módulo")]
    public List<ZonaMuerte> deathPartsActivas = new List<ZonaMuerte>();

    /// <summary>
    /// Punto final en Z de este módulo en coordenadas del mundo.
    /// </summary>
    public float FinZ => transform.position.z + longitudZ;

    /// <summary>
    /// Configura y construye las porciones del módulo en tiempo de ejecución.
    /// Si el módulo proviene de un Prefab con porciones preasignadas en el Inspector, las utiliza;
    /// de lo contrario, construye las porciones modularmente mediante código.
    /// </summary>
    public void ConfigurarSeccion(
        int indice,
        Vector3 dimensiones,
        int numPorciones,
        int numZonasPeligro,
        bool esSeguro,
        float velocidadObstaculo,
        float probabilidadObstaculo,
        float probabilidadMonedas,
        int maxMonedas,
        int puntosMoneda,
        Material materialBase,
        Color colorPar,
        Color colorImpar,
        Color colorPeligro)
    {
        indiceModulo = indice;
        anchoX = dimensiones.x;
        grosorY = dimensiones.y;
        longitudZ = dimensiones.z;
        cantidadPorciones = Mathf.Max(2, numPorciones);
        zonasPeligroActivas = esSeguro ? 0 : Mathf.Clamp(numZonasPeligro, 0, cantidadPorciones - 1);

        // Si es un Prefab con porciones ya asignadas en el Inspector, configurarlas al estilo HelixJump
        if (porcionesDelModulo != null && porcionesDelModulo.Count > 0)
        {
            ConfigurarPorcionesExistentes(esSeguro, colorPeligro);
            return;
        }

        porcionesDelModulo = new List<GameObject>();
        deathPartsActivas = new List<ZonaMuerte>();

        // 1. Colisionador maestro uniforme de la superficie para garantizar desplazamiento físico fluido sin enganches
        BoxCollider colMaestro = gameObject.GetComponent<BoxCollider>();
        if (colMaestro == null)
        {
            colMaestro = gameObject.AddComponent<BoxCollider>();
        }
        colMaestro.center = new Vector3(0f, -grosorY * 0.5f, longitudZ * 0.5f);
        colMaestro.size = new Vector3(anchoX, grosorY, longitudZ);

        // 2. Generar las porciones modulares que componen la sección del nivel
        float largoPorcion = longitudZ / cantidadPorciones;

        for (int i = 0; i < cantidadPorciones; i++)
        {
            GameObject porcion = GameObject.CreatePrimitive(PrimitiveType.Cube);
            porcion.name = $"Porcion_{i + 1}";
            porcion.transform.SetParent(transform, false);

            // Desactivar el BoxCollider individual de la porción base porque el módulo ya tiene colisionador maestro continuo
            Collider colPorcion = porcion.GetComponent<Collider>();
            if (colPorcion != null)
            {
                Destroy(colPorcion);
            }

            float posZLocal = (i * largoPorcion) + (largoPorcion * 0.5f);
            porcion.transform.localPosition = new Vector3(0f, -grosorY * 0.5f, posZLocal);
            porcion.transform.localScale = new Vector3(anchoX, grosorY, largoPorcion * 0.98f);

            Color colorPorcion = ((indice + i) % 2 == 0) ? colorPar : colorImpar;
            AplicarMaterialConColor(porcion, materialBase, colorPorcion);

            porcionesDelModulo.Add(porcion);
        }

        // 3. Bordes laterales visuales para delimitar claramente la plataforma en el espacio 3D
        CrearBordeLateral("Borde_Izquierdo", -anchoX * 0.5f + 0.25f, materialBase, colorImpar * 0.8f);
        CrearBordeLateral("Borde_Derecho", anchoX * 0.5f - 0.25f, materialBase, colorImpar * 0.8f);

        // En el primer módulo (índice 0), colocar una barrera trasera para evitar caer hacia atrás de la cámara al inicio
        if (indice == 0)
        {
            GameObject barreraInicio = new GameObject("Limite_Inicio_Sur");
            barreraInicio.transform.SetParent(transform, false);
            barreraInicio.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            BoxCollider colBarrera = barreraInicio.AddComponent<BoxCollider>();
            colBarrera.size = new Vector3(anchoX, 5f, 1f);
        }

        // 4. Activar Porciones de Peligro (DeathParts) según la dificultad actual
        if (!esSeguro && zonasPeligroActivas > 0)
        {
            GenerarZonasDePeligro(largoPorcion, materialBase, colorPeligro);
        }

        // 5. Generar obstáculo móvil de patrulla en el módulo según la curva de dificultad
        if (!esSeguro && Random.value <= probabilidadObstaculo)
        {
            GenerarObstaculoPatrulla(velocidadObstaculo, materialBase, colorPeligro);
        }

        // 6. Generar coleccionables (monedas) sobre rutas seguras o saltos
        if (Random.value <= probabilidadMonedas)
        {
            GenerarColeccionablesModulo(maxMonedas, puntosMoneda, materialBase);
        }
    }

    /// <summary>
    /// Si el desarrollador configuró porciones manualmente en un Prefab desde el Inspector,
    /// selecciona aleatoriamente cuáles se convierten en ZonaMuerte (DeathPart).
    /// </summary>
    private void ConfigurarPorcionesExistentes(bool esSeguro, Color colorPeligro)
    {
        if (esSeguro || zonasPeligroActivas <= 0) return;

        List<int> indicesDisponibles = new List<int>();
        for (int i = 0; i < porcionesDelModulo.Count; i++)
        {
            if (porcionesDelModulo[i] != null)
            {
                indicesDisponibles.Add(i);
            }
        }

        int cantidad = Mathf.Min(zonasPeligroActivas, indicesDisponibles.Count - 1);
        for (int n = 0; n < cantidad; n++)
        {
            int idxLista = Random.Range(0, indicesDisponibles.Count);
            int idxPorcion = indicesDisponibles[idxLista];
            indicesDisponibles.RemoveAt(idxLista);

            GameObject pieza = porcionesDelModulo[idxPorcion];
            ZonaMuerte zm = pieza.GetComponent<ZonaMuerte>();
            if (zm == null)
            {
                zm = pieza.AddComponent<ZonaMuerte>();
            }
            zm.AplicarColorPeligro(colorPeligro);
            deathPartsActivas.Add(zm);
        }
    }

    /// <summary>
    /// Crea las porciones rojas de peligro (DeathParts) sobre sub-secciones del módulo,
    /// dejando siempre carriles laterales libres o altura suficiente para saltarlas.
    /// </summary>
    private void GenerarZonasDePeligro(float largoPorcion, Material materialBase, Color colorPeligro)
    {
        // Elegir qué porciones (evitando la primera porción si el salto anterior fue largo) tendrán DeathPart
        List<int> porcionesCandidatas = new List<int>();
        for (int i = 1; i < cantidadPorciones; i++)
        {
            porcionesCandidatas.Add(i);
        }
        // Si necesitamos más zonas que (cantidadPorciones - 1), incluir también la porción 0
        if (zonasPeligroActivas > porcionesCandidatas.Count)
        {
            porcionesCandidatas.Add(0);
        }

        int creadas = 0;
        while (creadas < zonasPeligroActivas && porcionesCandidatas.Count > 0)
        {
            int randIdx = Random.Range(0, porcionesCandidatas.Count);
            int indicePorcion = porcionesCandidatas[randIdx];
            porcionesCandidatas.RemoveAt(randIdx);

            // Dividir el ancho X en 3 carriles (Izquierda, Centro, Derecha) y ocupar 1 o 2 carriles,
            // garantizando siempre al menos 1 carril seguro además de permitir saltar en Y
            int carril = Random.Range(-1, 2); // -1 = Izquierda, 0 = Centro, 1 = Derecha
            float anchoCarril = (anchoX - 2f) / 3f;
            float anchoZona = anchoCarril * Random.Range(1.0f, 1.65f);
            float posX = carril * anchoCarril;
            float posZ = (indicePorcion * largoPorcion) + (largoPorcion * 0.5f);

            GameObject deathObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deathObj.name = $"DeathPart_Porcion_{indicePorcion + 1}";
            deathObj.transform.SetParent(transform, false);
            deathObj.transform.localPosition = new Vector3(posX, 0.2f, posZ);
            deathObj.transform.localScale = new Vector3(anchoZona, 0.4f, largoPorcion * 0.7f);

            AplicarMaterialConColor(deathObj, materialBase, colorPeligro);

            ZonaMuerte zm = deathObj.AddComponent<ZonaMuerte>();
            zm.colorPeligro = colorPeligro;
            zm.AplicarColorPeligro(colorPeligro);
            deathPartsActivas.Add(zm);

            creadas++;
        }
    }

    private void GenerarObstaculoPatrulla(float velocidad, Material materialBase, Color colorPeligro)
    {
        GameObject obsObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obsObj.name = "Obstaculo_Patrulla_Modulo";
        obsObj.transform.SetParent(transform, false);

        float posZ = longitudZ * Random.Range(0.35f, 0.75f);
        obsObj.transform.localPosition = new Vector3(0f, 1.1f, posZ);
        obsObj.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);

        Color colorObs = new Color(
            Mathf.Clamp01(colorPeligro.r * 1.05f),
            Mathf.Clamp01(colorPeligro.g + 0.15f),
            Mathf.Clamp01(colorPeligro.b)
        );
        AplicarMaterialConColor(obsObj, materialBase, colorObs);

        Obstaculo scriptObs = obsObj.AddComponent<Obstaculo>();
        scriptObs.modo = Obstaculo.ModoMovimiento.PatrullaLateral;
        scriptObs.velocidad = velocidad;
        scriptObs.rangoPatrullaX = Mathf.Max(2f, (anchoX * 0.5f) - 2.5f);
        scriptObs.destruirPorTiempo = false;
    }

    private void GenerarColeccionablesModulo(int maxMonedas, int puntos, Material materialBase)
    {
        int cantidad = Random.Range(1, maxMonedas + 1);
        float carrilX = Random.Range(-1, 2) * ((anchoX - 4f) / 3f);
        float inicioZ = longitudZ * 0.2f;
        float pasoZ = (longitudZ * 0.6f) / Mathf.Max(1, cantidad);

        Color colorOro = new Color(1f, 0.84f, 0.05f, 1f);

        for (int i = 0; i < cantidad; i++)
        {
            GameObject moneda = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            moneda.name = $"Moneda_M{indiceModulo}_{i + 1}";
            moneda.transform.SetParent(transform, false);

            float zLocal = inicioZ + (i * pasoZ);
            moneda.transform.localPosition = new Vector3(carrilX, 1.4f, zLocal);
            moneda.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);

            Collider col = moneda.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            AplicarMaterialConColor(moneda, materialBase, colorOro);

            Coleccionable colScript = moneda.AddComponent<Coleccionable>();
            colScript.puntos = puntos;
        }
    }

    private void CrearBordeLateral(string nombre, float posX, Material materialBase, Color colorBorde)
    {
        GameObject borde = GameObject.CreatePrimitive(PrimitiveType.Cube);
        borde.name = nombre;
        borde.transform.SetParent(transform, false);

        Collider col = borde.GetComponent<Collider>();
        if (col != null)
        {
            Destroy(col);
        }

        borde.transform.localPosition = new Vector3(posX, 0.15f, longitudZ * 0.5f);
        borde.transform.localScale = new Vector3(0.45f, 0.3f, longitudZ);
        AplicarMaterialConColor(borde, materialBase, colorBorde);
    }

    private static void AplicarMaterialConColor(GameObject obj, Material materialPlantilla, Color color)
    {
        Renderer ren = obj.GetComponent<Renderer>();
        if (ren == null) return;

        if (materialPlantilla != null)
        {
            ren.sharedMaterial = materialPlantilla;
        }

        Material matInstancia = ren.material;
        matInstancia.color = color;
        if (matInstancia.HasProperty("_BaseColor"))
        {
            matInstancia.SetColor("_BaseColor", color);
        }
    }
}
