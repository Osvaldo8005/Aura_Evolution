using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Inicializador automático de escena en el Editor.
/// Asegura que el GeneradorNiveles (Actividad 4.3) esté presente y configurado en SampleScene
/// para que el desarrollador pueda ajustar todos los parámetros directamente desde el Inspector.
/// </summary>
[InitializeOnLoad]
public static class AplicarLimitesSueloEditor
{
    static AplicarLimitesSueloEditor()
    {
        EditorApplication.delayCall += ConfigurarEscenaAutomaticamente;
        EditorSceneManager.sceneOpened += (_, __) => EditorApplication.delayCall += ConfigurarEscenaAutomaticamente;
    }

    [MenuItem("Aura Evolution/Configurar Actividad 4.3 (Generador de Niveles y Game Loop)")]
    public static void AplicarDesdeMenu()
    {
        ConfigurarEscenaAutomaticamente();
    }

    private static void ConfigurarEscenaAutomaticamente()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.name != "SampleScene")
        {
            return;
        }

        bool cambio = false;

        GameObject jugador = GameObject.Find("Jugador");
        GameObject suelo = GameObject.Find("Suelo");

        // 1. Asegurar que GeneradorNiveles exista en la Jerarquía para poder configurarlo en el Inspector
        GeneradorNiveles genNiveles = Object.FindAnyObjectByType<GeneradorNiveles>();
        if (genNiveles == null)
        {
            GameObject genObj = new GameObject("GeneradorNiveles");
            genNiveles = genObj.AddComponent<GeneradorNiveles>();
            cambio = true;
        }

        if (jugador != null && genNiveles.jugador != jugador.transform)
        {
            genNiveles.jugador = jugador.transform;
            EditorUtility.SetDirty(genNiveles);
            cambio = true;
        }

        if (suelo != null && genNiveles.materialPlataforma == null)
        {
            Renderer renSuelo = suelo.GetComponent<Renderer>();
            if (renSuelo != null && renSuelo.sharedMaterial != null)
            {
                genNiveles.materialPlataforma = renSuelo.sharedMaterial;
                EditorUtility.SetDirty(genNiveles);
                cambio = true;
            }
        }

        // 2. Desactivar LimitesDelSuelo cerrados para no bloquear el avance infinito en +Z
        GameObject limites = GameObject.Find("LimitesDelSuelo");
        if (limites != null && limites.activeSelf)
        {
            limites.SetActive(false);
            cambio = true;
        }

        // 3. Configurar Rigidbody y salto del Jugador para plataformas procedurales
        if (jugador != null)
        {
            Rigidbody rb = jugador.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (rb.constraints != RigidbodyConstraints.FreezeRotation)
                {
                    rb.constraints = RigidbodyConstraints.FreezeRotation;
                    cambio = true;
                }

                if (rb.collisionDetectionMode != CollisionDetectionMode.Continuous)
                {
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    cambio = true;
                }
            }

            PlayerController3D pc = jugador.GetComponent<PlayerController3D>();
            if (pc != null)
            {
                if (pc.fuerzaSalto < 7.5f || pc.velocidad < 7f)
                {
                    pc.velocidad = 8f;
                    pc.velocidadBase = 8f;
                    pc.fuerzaSalto = 8.5f;
                    EditorUtility.SetDirty(pc);
                    cambio = true;
                }
            }
        }

        if (cambio)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Aura Evolution] Actividad 4.3: GeneradorNiveles y Game Loop configurados en SampleScene.");
        }
    }
}
