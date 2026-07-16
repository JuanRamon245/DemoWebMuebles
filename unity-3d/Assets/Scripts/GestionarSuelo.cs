using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class GestionarSuelo : MonoBehaviour
{
    [Tooltip("Lista de muebles apoyados en el suelo. Es informativa/organizativa o para control de UI.")]
    public List<GameObject> mueblesSuelo = new List<GameObject>();

    private BoxCollider colliderSuelo;

    void Awake()
    {
        colliderSuelo = GetComponent<BoxCollider>();
    }

    // Activa o desactiva el mueble (útil para botones de la interfaz)
    public void ActivarDesactivarMueble(GameObject mueble)
    {
        if (mueble == null) return;
        mueble.SetActive(!mueble.activeSelf);
    }

    // Activa o desactiva un mueble por su posición en la lista
    public void ActivarDesactivarMueblePorIndice(int indice)
    {
        if (indice < 0 || indice >= mueblesSuelo.Count) return;
        ActivarDesactivarMueble(mueblesSuelo[indice]);
    }

    // Registra un mueble existente en la escena
    public void RegistrarMueble(GameObject mueble)
    {
        if (mueble != null && !mueblesSuelo.Contains(mueble))
        {
            mueblesSuelo.Add(mueble);
        }
    }

    public void SpawnearMuebleEnCentro(GameObject prefabMueble)
    {
        if (prefabMueble == null)
        {
            Debug.LogWarning("¡Falta el prefab del mueble para poder spawnearlo!");
            return;
        }

        if (colliderSuelo == null) colliderSuelo = GetComponent<BoxCollider>();

        // Calculamos el centro horizontal (X, Z) del objeto Suelo
        Vector3 centroSuelo = colliderSuelo.bounds.center;

        // Calculamos el límite superior exacto del colisionador en el eje Y
        float superficieSuperiorY = colliderSuelo.bounds.max.y;

        // Montamos la posición de aparición (pivote inferior del mueble asentado en el suelo)
        Vector3 posicionSpawn = new Vector3(centroSuelo.x, superficieSuperiorY, centroSuelo.z);

        // Instanciamos el mueble en la escena
        GameObject nuevoMueble = Instantiate(prefabMueble, posicionSpawn, Quaternion.identity);
        nuevoMueble.name = prefabMueble.name;

        // Lo registramos automáticamente en nuestra lista de control
        RegistrarMueble(nuevoMueble);
    }
}