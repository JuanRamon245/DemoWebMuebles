using UnityEngine;
using UnityEngine.InputSystem;

public class MoverEnPared : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Selecciona aquí la capa 'Paredes' que creaste en Unity")]
    public LayerMask capaParedes;

    [Header("Posición inicial (opcional pero recomendado)")]
    [Tooltip("Arrastra aquí la pared a la que quieres que la puerta se pegue al arrancar.")]
    public Transform paredInicialReferencia;

    [Tooltip("Grosor de las paredes de tu habitación. Debe coincidir con THICKNESS en RoomSizeController (por defecto 0.1).")]
    public float grosorPared = 0.1f;

    private Camera camaraPrincipal;
    private bool arrastrando = false;

    void Start()
    {
        camaraPrincipal = Camera.main;

        if (camaraPrincipal == null)
        {
            Debug.LogError("MoverEnPared: no hay ninguna cámara con el tag 'MainCamera' en la escena.");
        }

        PegarAParedInicial();
    }

    void Update()
    {
        if (camaraPrincipal == null || Mouse.current == null) return;

        Vector2 posicionRaton = Mouse.current.position.ReadValue();

        // --- DETECCIÓN DE CLIC MANUAL (Atraviesa paredes invisibles al seleccionar) ---
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray rayoClic = camaraPrincipal.ScreenPointToRay(posicionRaton);

            RaycastHit[] impactosSeleccion = Physics.RaycastAll(rayoClic, 100f);
            foreach (RaycastHit hit in impactosSeleccion)
            {
                if (hit.transform == transform)
                {
                    arrastrando = true;
                    break;
                }
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            arrastrando = false;
        }

        if (!arrastrando) return;

        // --- LÓGICA DE MOVIMIENTO FILTRADO (Perfora obstáculos al arrastrar) ---
        Ray rayo = camaraPrincipal.ScreenPointToRay(posicionRaton);
        RaycastHit[] hits = Physics.RaycastAll(rayo, 100f, capaParedes);

        // Ordenamos los impactos por distancia (de más cercano a más lejano)
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        foreach (RaycastHit hit in hits)
        {
            if (EsCaraInterna(hit))
            {
                Vector3 nuevaPosicion = hit.point;
                nuevaPosicion.y = 0f;
                nuevaPosicion += hit.normal * 0.01f;
                transform.position = nuevaPosicion;
                transform.rotation = Quaternion.LookRotation(hit.normal, Vector3.up);
                break;
            }
        }
    }

    bool EsCaraInterna(RaycastHit hit)
    {
        Transform wall = hit.transform;

        // Buscamos el objeto principal de la habitación para usar su sistema de coordenadas local
        RoomSizeController room = wall.GetComponentInParent<RoomSizeController>();
        Transform refTransform = (room != null) ? room.transform : wall.parent;
        if (refTransform == null) refTransform = wall; // Respaldo extremo

        // Convertimos la normal del impacto al espacio local de la habitación
        Vector3 localNormal = refTransform.InverseTransformDirection(hit.normal);
        string name = wall.name.ToLower();

        // 1. Filtrado ultra-seguro por nombre del objeto
        if (name.Contains("north") || name.Contains("norte"))
        {
            return Vector3.Dot(localNormal, Vector3.back) > 0.9f;
        }
        if (name.Contains("south") || name.Contains("sur"))
        {
            return Vector3.Dot(localNormal, Vector3.forward) > 0.9f;
        }
        if (name.Contains("east") || name.Contains("este"))
        {
            return Vector3.Dot(localNormal, Vector3.left) > 0.9f;
        }
        if (name.Contains("west") || name.Contains("oeste"))
        {
            return Vector3.Dot(localNormal, Vector3.right) > 0.9f;
        }

        // 2. Fallback matemático por coordenadas (por si acaso el objeto no tiene un nombre reconocible)
        Vector3 localWallPos = refTransform.InverseTransformPoint(wall.position);
        if (Mathf.Abs(localWallPos.z) > Mathf.Abs(localWallPos.x))
        {
            return (localWallPos.z > 0) ?
                Vector3.Dot(localNormal, Vector3.back) > 0.9f :
                Vector3.Dot(localNormal, Vector3.forward) > 0.9f;
        }
        else
        {
            return (localWallPos.x > 0) ?
                Vector3.Dot(localNormal, Vector3.left) > 0.9f :
                Vector3.Dot(localNormal, Vector3.right) > 0.9f;
        }
    }

    void PegarAParedInicial()
    {
        if (paredInicialReferencia != null)
        {
            Vector3 haciaElInterior = paredInicialReferencia.forward;
            float distancia = (grosorPared / 2f) + 0.01f;

            Vector3 nuevaPosicion = paredInicialReferencia.position + haciaElInterior * distancia;
            nuevaPosicion.y = 0f;

            transform.position = nuevaPosicion;
            transform.rotation = Quaternion.LookRotation(haciaElInterior, Vector3.up);
            return;
        }

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 50f, capaParedes))
        {
            Vector3 posInicial = hit.point;
            posInicial.y = 0f;
            posInicial += hit.normal * 0.01f;

            transform.position = posInicial;
            transform.rotation = Quaternion.LookRotation(hit.normal, Vector3.up);
        }
    }

    public void ActivarDesactivarPuerta()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}