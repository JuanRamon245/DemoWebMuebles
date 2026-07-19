using UnityEngine;

public class GestorMuebles : MonoBehaviour
{
    [Header("Arrastra aquí cada raíz de mueble de tu escena")]
    public GameObject raizEstanteria;
    public GameObject raizPuerta;
    public GameObject raizVentana;
    public GameObject raizMesa;

    [Header("Cámara del configurador")]
    public OrbitCamera camaraOrbital;

    [Header("Elementos del modo 'habitación completa' (se ocultan aquí)")]
    public GameObject roomController;
    public GameObject canvasHabitacion;

    void Awake()
    {
        // >>> CAMBIO: apaga explícitamente habitación y canvas de botones
        if (roomController != null) roomController.SetActive(false);
        if (canvasHabitacion != null) canvasHabitacion.SetActive(false);

        DesactivarTodos();
    }

    public void MostrarCategoria(string categoria)
    {
        DesactivarTodos();

        GameObject raizActiva = null;

        switch (categoria)
        {
            case "estanteria":
                raizActiva = raizEstanteria;
                break;
            case "puerta":
                raizActiva = raizPuerta;
                break;
            case "ventana":
                raizActiva = raizVentana;
                break;
            case "mesa":
                raizActiva = raizMesa;
                break;
            default:
                Debug.LogWarning($"Categoría '{categoria}' sin GameObject asignado en GestorMuebles.");
                break;
        }

        if (raizActiva != null)
        {
            raizActiva.SetActive(true);

            if (camaraOrbital != null)
            {
                camaraOrbital.EnfocarObjetivo(raizActiva.transform);
            }
        }
    }

    void DesactivarTodos()
    {
        if (raizEstanteria != null) raizEstanteria.SetActive(false);
        if (raizPuerta != null) raizPuerta.SetActive(false);
        if (raizVentana != null) raizVentana.SetActive(false);
        if (raizMesa != null) raizMesa.SetActive(false);
    }
}