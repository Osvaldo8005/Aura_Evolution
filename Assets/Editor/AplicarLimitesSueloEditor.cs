using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AplicarLimitesSueloEditor
{
    static AplicarLimitesSueloEditor()
    {
        EditorApplication.delayCall += AplicarEnEscenaAbierta;
        EditorSceneManager.sceneOpened += (_, __) => EditorApplication.delayCall += AplicarEnEscenaAbierta;
    }

    [MenuItem("Aura Evolution/Aplicar limites del Suelo")]
    public static void AplicarDesdeMenu()
    {
        AplicarEnEscenaAbierta();
    }

    private static void AplicarEnEscenaAbierta()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        if (scene.name != "SampleScene")
        {
            return;
        }

        GameObject suelo = GameObject.Find("Suelo");
        if (suelo == null)
        {
            return;
        }

        bool cambio = false;

        GameObject limites = GameObject.Find("LimitesDelSuelo");
        if (limites == null)
        {
            limites = new GameObject("LimitesDelSuelo");
            cambio = true;
        }

        LimitesDelSuelo componente = limites.GetComponent<LimitesDelSuelo>();
        if (componente == null)
        {
            componente = limites.AddComponent<LimitesDelSuelo>();
            cambio = true;
        }

        cambio |= componente.CrearOActualizar();

        GameObject jugador = GameObject.Find("Jugador");
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
        }

        if (cambio)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Limites del Suelo aplicados y escena SampleScene guardada.");
        }
    }
}
