using UnityEngine;
using System.Collections.Generic;

public class GestorPared : MonoBehaviour
{
    [Tooltip("Lista de muebles montados en esta pared (arrastra aquí Puerta_Parametrica, " +
             "Ventana_Parametrica, etc.). Es solo informativa/organizativa: puedes dejarla vacía " +
             "y seguir usando ActivarDesactivarMueble(mueble) directamente desde un botón.")]
    public List<GameObject> muebles = new List<GameObject>();

    // Llama a esto desde el OnClick() de un botón UI, pasándole el GameObject concreto que quieres mostrar/ocultar.
    public void ActivarDesactivarMueble(GameObject mueble)
    {
        if (mueble == null) return;
        mueble.SetActive(!mueble.activeSelf);
    }

    // Alternativa por índice
    public void ActivarDesactivarMueblePorIndice(int indice)
    {
        if (indice < 0 || indice >= muebles.Count) return;
        ActivarDesactivarMueble(muebles[indice]);
    }

    public void RegistrarMueble(GameObject mueble)
    {
        if (mueble != null && !muebles.Contains(mueble))
        {
            muebles.Add(mueble);
        }
    }
}