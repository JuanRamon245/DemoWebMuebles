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

    [Header("Ajustes de Altura")]
    [Tooltip("Si está activo, el objeto se pega automáticamente al suelo (Y = 0) y no se puede subir/bajar.")]
    public bool pegadoAlSuelo = true;

    [Tooltip("Altura de partida la PRIMERA VEZ que desmarcas 'pegadoAlSuelo'. Después podrás " +
             "subir y bajar el objeto libremente arrastrando el ratón sobre la pared.")]
    public float alturaLibrePorDefecto = 1.0f;

    [Tooltip("Límites entre los que se puede mover en vertical cuando 'pegadoAlSuelo' está desmarcado.")]
    public float alturaMinima = 0.1f;
    public float alturaMaxima = 2.8f;

    private bool pegadoAlSueloAnterior;

    [Header("Evasión de Obstáculos")]
    [Tooltip("Capa asignada a los obstáculos (ventanas, columnas, etc.) que la puerta/ventana no debe atravesar.")]
    public LayerMask capaObstaculos;

    [Tooltip("Arrastra aquí el BoxCollider de este objeto para calcular sus dimensiones.")]
    public BoxCollider miColisionador;

    [Header("Visuales de Selección (Brillo)")]
    [ColorUsage(true, true)]
    [Tooltip("Color de emisión que se aplicará al arrastrar el objeto.")]
    public Color colorSeleccion = new Color(0f, 0.4f, 1f, 1f);

    private Camera camaraPrincipal;
    private bool arrastrando = false;

    private Renderer[] renderersHijos;
    private MaterialPropertyBlock propBlock;

    void Start()
    {
        camaraPrincipal = Camera.main;

        if (camaraPrincipal == null)
        {
            Debug.LogError("MoverEnPared: no hay ninguna cámara con el tag 'MainCamera' en la escena.");
        }

        renderersHijos = GetComponentsInChildren<Renderer>(true);
        propBlock = new MaterialPropertyBlock();

        foreach (var r in renderersHijos)
        {
            if (r == null) continue;
            foreach (var mat in r.sharedMaterials)
            {
                if (mat != null) mat.EnableKeyword("_EMISSION");
            }
        }

        pegadoAlSueloAnterior = pegadoAlSuelo;

        PegarAParedInicial();

        if (paredInicialReferencia != null)
        {
            GestorPared gestor = paredInicialReferencia.GetComponent<GestorPared>();
            if (gestor != null) gestor.RegistrarMueble(gameObject);
        }
    }

    void Update()
    {
        if (camaraPrincipal == null || Mouse.current == null) return;

        // --- DETECCIÓN DE CAMBIO EN "pegadoAlSuelo" ---
        if (pegadoAlSuelo != pegadoAlSueloAnterior)
        {
            if (!pegadoAlSuelo)
            {
                Vector3 pos = transform.position;
                pos.y = alturaLibrePorDefecto - ObtenerOffsetCentro();
                transform.position = pos;
            }
            else
            {
                Vector3 pos = transform.position;
                pos.y = 0f;
                transform.position = pos;
            }
            pegadoAlSueloAnterior = pegadoAlSuelo;
        }

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
                    SetBrilloSeleccion(true);
                    break;
                }
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            arrastrando = false;
            SetBrilloSeleccion(false);
        }

        if (!arrastrando) return;

        // --- LÓGICA DE MOVIMIENTO FILTRADO (Perfora obstáculos al arrastrar) ---
        Ray rayo = camaraPrincipal.ScreenPointToRay(posicionRaton);
        RaycastHit[] hits = Physics.RaycastAll(rayo, 100f, capaParedes);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        foreach (RaycastHit hit in hits)
        {
            if (EsCaraInterna(hit))
            {
                // 1. Calculamos la posición propuesta
                Vector3 posicionPropuesta = hit.point;
                posicionPropuesta.y = ObtenerAlturaDestino(hit.point.y);
                posicionPropuesta += hit.normal * 0.01f;

                // 2. Aplicamos la lógica de evasión de obstáculos (el salto)
                if (miColisionador != null)
                {
                    Vector3 medioTamano = miColisionador.size / 2f;
                    medioTamano = Vector3.Scale(medioTamano, transform.lossyScale);
                    medioTamano.z *= 0.1f;

                    Quaternion rotacionPropuesta = Quaternion.LookRotation(hit.normal, Vector3.up);

                    Vector3 offsetCentroCollider = rotacionPropuesta * Vector3.Scale(miColisionador.center, transform.lossyScale);
                    Vector3 centroCajaTest = posicionPropuesta + offsetCentroCollider;

                    // Buscamos si hay algún obstáculo en la posición donde nos queremos mover
                    Collider[] obstaculosDetectados = Physics.OverlapBox(
                        centroCajaTest,
                        medioTamano,
                        rotacionPropuesta,
                        capaObstaculos
                    );

                    if (obstaculosDetectados.Length > 0)
                    {
                        // Buscamos el primer obstáculo que no sea este mismo objeto
                        Collider obstaculo = null;
                        foreach (var obs in obstaculosDetectados)
                        {
                            if (obs.transform != transform && !obs.transform.IsChildOf(transform))
                            {
                                obstaculo = obs;
                                break;
                            }
                        }

                        if (obstaculo != null)
                        {
                            // Calculamos el ancho de ambos objetos en el eje global para un salto perfecto
                            float anchoEste = miColisionador.size.x * transform.lossyScale.x;
                            float anchoObstaculo = obstaculo.bounds.size.x;
                            float distanciaSegura = (anchoEste / 2f) + (anchoObstaculo / 2f);

                            // Detectamos de qué lado del obstáculo se encuentra el puntero del ratón
                            Vector3 direccionHaciaRaton = posicionPropuesta - obstaculo.transform.position;
                            float lado = Vector3.Dot(direccionHaciaRaton, transform.right);

                            if (lado > 0)
                            {
                                // El ratón está a la derecha -> Salta al extremo derecho del obstáculo
                                posicionPropuesta = obstaculo.transform.position + transform.right * (distanciaSegura + 0.02f);
                            }
                            else
                            {
                                // El ratón está a la izquierda -> Salta al extremo izquierdo del obstáculo
                                posicionPropuesta = obstaculo.transform.position - transform.right * (distanciaSegura + 0.02f);
                            }

                            // Aseguramos que conserve la altura configurada tras el salto
                            posicionPropuesta.y = ObtenerAlturaDestino(hit.point.y);
                        }
                    }
                }

                // 3. Aplicamos la posición y rotación finales
                transform.position = posicionPropuesta;
                transform.rotation = Quaternion.LookRotation(hit.normal, Vector3.up);
                break;
            }
        }
    }

    float ObtenerOffsetCentro()
    {
        if (miColisionador == null) return 0f;
        return miColisionador.center.y * transform.lossyScale.y;
    }

    // Devuelve la altura Y a aplicar al PIVOTE (no al centro visual):
    float ObtenerAlturaDestino(float alturaImpacto)
    {
        if (pegadoAlSuelo) return 0f;

        float alturaCentroDeseada = Mathf.Clamp(alturaImpacto, alturaMinima, alturaMaxima);
        return alturaCentroDeseada - ObtenerOffsetCentro();
    }

    // Lógica para aplicar o remover el color de brillo (emisión) mediante MaterialPropertyBlocks
    void SetBrilloSeleccion(bool activar)
    {
        if (renderersHijos == null || propBlock == null) return;

        foreach (var r in renderersHijos)
        {
            if (r == null) continue;

            r.GetPropertyBlock(propBlock);
            Color colorFinal = activar ? colorSeleccion : Color.black;
            propBlock.SetColor("_EmissionColor", colorFinal);

            r.SetPropertyBlock(propBlock);
        }
    }

    bool EsCaraInterna(RaycastHit hit)
    {
        Transform wall = hit.transform;

        // Buscamos el objeto principal de la habitación para usar su sistema de coordenadas local
        RoomSizeController room = wall.GetComponentInParent<RoomSizeController>();
        Transform refTransform = (room != null) ? room.transform : wall.parent;
        if (refTransform == null) refTransform = wall;

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
            nuevaPosicion.y = pegadoAlSuelo ? 0f : (alturaLibrePorDefecto - ObtenerOffsetCentro());

            transform.position = nuevaPosicion;
            transform.rotation = Quaternion.LookRotation(haciaElInterior, Vector3.up);
            return;
        }

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 50f, capaParedes))
        {
            Vector3 posInicial = hit.point;
            posInicial.y = pegadoAlSuelo ? 0f : (alturaLibrePorDefecto - ObtenerOffsetCentro());
            posInicial += hit.normal * 0.01f;

            transform.position = posInicial;
            transform.rotation = Quaternion.LookRotation(hit.normal, Vector3.up);
        }
    }
}