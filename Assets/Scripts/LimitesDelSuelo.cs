using UnityEngine;

/// <summary>
/// Crea 4 BoxColliders invisibles en los bordes reales del Suelo (Plane).
/// No se parentan al Plane para no heredar su Scale 10.
/// </summary>
public class LimitesDelSuelo : MonoBehaviour
{
    [SerializeField] private float altura = 5f;
    [SerializeField] private float grosor = 1f;

    private const string NombreNorte = "Pared_Norte";
    private const string NombreSur = "Pared_Sur";
    private const string NombreEste = "Pared_Este";
    private const string NombreOeste = "Pared_Oeste";

    private void Awake()
    {
        CrearOActualizar();
    }

    public bool CrearOActualizar()
    {
        GameObject suelo = GameObject.Find("Suelo");
        if (suelo == null)
        {
            return false;
        }

        Renderer rendererSuelo = suelo.GetComponent<Renderer>();
        if (rendererSuelo == null)
        {
            return false;
        }

        Bounds b = rendererSuelo.bounds;
        float y = altura * 0.5f;
        float halfGrosor = grosor * 0.5f;

        Vector3 posNorte = new Vector3(b.center.x, y, b.max.z + halfGrosor);
        Vector3 posSur = new Vector3(b.center.x, y, b.min.z - halfGrosor);
        Vector3 posEste = new Vector3(b.max.x + halfGrosor, y, b.center.z);
        Vector3 posOeste = new Vector3(b.min.x - halfGrosor, y, b.center.z);

        Vector3 escalaNorteSur = new Vector3(b.size.x + grosor, altura, grosor);
        Vector3 escalaEsteOeste = new Vector3(grosor, altura, b.size.z + grosor);

        bool cambio = false;
        cambio |= AsegurarPared(NombreNorte, posNorte, escalaNorteSur);
        cambio |= AsegurarPared(NombreSur, posSur, escalaNorteSur);
        cambio |= AsegurarPared(NombreEste, posEste, escalaEsteOeste);
        cambio |= AsegurarPared(NombreOeste, posOeste, escalaEsteOeste);
        return cambio;
    }

    private bool AsegurarPared(string nombre, Vector3 posicion, Vector3 escala)
    {
        Transform hijo = transform.Find(nombre);
        GameObject pared;
        bool creado = false;

        if (hijo == null)
        {
            pared = new GameObject(nombre);
            pared.transform.SetParent(transform, false);
            if (pared.GetComponent<BoxCollider>() == null)
            {
                pared.AddComponent<BoxCollider>();
            }
            creado = true;
        }
        else
        {
            pared = hijo.gameObject;
            if (pared.GetComponent<BoxCollider>() == null)
            {
                pared.AddComponent<BoxCollider>();
                creado = true;
            }
        }

        MeshRenderer mesh = pared.GetComponent<MeshRenderer>();
        if (mesh != null && mesh.enabled)
        {
            mesh.enabled = false;
            creado = true;
        }

        Transform t = pared.transform;
        if (t.position != posicion || t.localScale != escala)
        {
            t.position = posicion;
            t.rotation = Quaternion.identity;
            t.localScale = escala;
            creado = true;
        }

        BoxCollider box = pared.GetComponent<BoxCollider>();
        if (box != null && box.isTrigger)
        {
            box.isTrigger = false;
            creado = true;
        }

        return creado;
    }
}
