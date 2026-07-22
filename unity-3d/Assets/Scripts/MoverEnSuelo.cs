using UnityEngine;
using UnityEngine.InputSystem;

public class MoverEnSuelo : MonoBehaviour
{
    [Header("Filtros de Capas")]
    [Tooltip("Selecciona aquí la capa 'Suelo'")]
    public LayerMask capaSuelo;
    [Tooltip("Selecciona aquí la capa 'Paredes'")]
    public LayerMask capaParedes;
    [Tooltip("Selecciona aquí la capa 'Obstaculo'")]
    public LayerMask capaObstaculos;

    [Header("Posición Inicial")]
    [Tooltip("Arrastra aquí el objeto Suelo de la habitación (el que tiene el BoxCollider grande).")]
    public Transform sueloReferencia;

    [Tooltip("Si está marcado, al arrancar el mueble se coloca en el CENTRO del suelo.")]
    public bool centrarEnSueloAlArrancar = true;

    [Header("Referencias")]
    [Tooltip("Arrastra aquí el BoxCollider de este mueble")]
    public BoxCollider miColisionador;

    [Header("Rotación (opcional)")]
    [Tooltip("Objeto hijo (flecha/cono) que gira el mueble hacia la izquierda al pulsarlo. " +
             "Déjalo vacío si este mueble no necesita rotarse.")]
    public GameObject flechaIzquierda;
    [Tooltip("Objeto hijo (flecha/cono) que gira el mueble hacia la derecha al pulsarlo.")]
    public GameObject flechaDerecha;

    [Header("Visuales de Selección")]
    [ColorUsage(true, true)]
    public Color colorSeleccion = new Color(0f, 0.4f, 1f, 1f);

    [Header("Modo de Interacción")]
    [Tooltip("Desmarca esto para el Configurador estático para evitar que el clic active flechas o arrastre.")]
    public bool permitirInteraccion = false;

    private Camera camaraPrincipal;
    private bool arrastrando = false;
    private bool seleccionado = false;
    private Vector3 offsetArrastre;
    private float alturaSueloFija;

    private Renderer[] renderersHijos;
    private MaterialPropertyBlock propBlock;
    private ParametricShelf estanteria;

    private void Awake()
    {
        AutoBuscarFlechasSiNull();
    }

    private void OnEnable()
    {
        AutoBuscarFlechasSiNull();
        seleccionado = false;
        arrastrando = false;
        MostrarFlechas(false);
    }

    void Start()
    {
        camaraPrincipal = Camera.main;

        renderersHijos = GetComponentsInChildren<Renderer>(true);
        propBlock = new MaterialPropertyBlock();
        estanteria = GetComponent<ParametricShelf>();

        foreach (var r in renderersHijos)
        {
            if (r == null) continue;
            foreach (var mat in r.sharedMaterials)
            {
                if (mat != null) mat.EnableKeyword("_EMISSION");
            }
        }

        MostrarFlechas(false);

        if (!permitirInteraccion)
            return;

        PegarASueloInicial();
    }

    private void AutoBuscarFlechasSiNull()
    {
        Transform[] todosLosHijos = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in todosLosHijos)
        {
            if (flechaIzquierda == null && child.name.Equals("FlechaRotarIzq", System.StringComparison.OrdinalIgnoreCase))
            {
                flechaIzquierda = child.gameObject;
            }

            if (flechaDerecha == null && child.name.Equals("FlechaRotarDer", System.StringComparison.OrdinalIgnoreCase))
            {
                flechaDerecha = child.gameObject;
            }
        }
    }

    void PegarASueloInicial()
    {
        if (sueloReferencia == null) return;
        Collider colSuelo = sueloReferencia.GetComponent<Collider>();
        if (colSuelo == null) return;

        alturaSueloFija = colSuelo.bounds.max.y;

        Vector3 pos = transform.position;

        if (centrarEnSueloAlArrancar)
        {
            pos.x = colSuelo.bounds.center.x;
            pos.z = colSuelo.bounds.center.z;
        }

        pos.y = alturaSueloFija;
        transform.position = pos;
    }

    void Update()
    {
        if (!permitirInteraccion)
        {
            if (seleccionado || arrastrando || (flechaIzquierda != null && flechaIzquierda.activeSelf) || (flechaDerecha != null && flechaDerecha.activeSelf))
            {
                seleccionado = false;
                arrastrando = false;
                SetBrilloSeleccion(false);
                MostrarFlechas(false);
            }
            return;
        }

        if (camaraPrincipal == null || Mouse.current == null) return;

        Vector2 posicionRaton = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Ray rayoClic = camaraPrincipal.ScreenPointToRay(posicionRaton);
            RaycastHit[] impactos = Physics.RaycastAll(rayoClic, 100f);
            System.Array.Sort(impactos, (a, b) => a.distance.CompareTo(b.distance));

            bool haTocadoAlgoPropio = false;

            foreach (RaycastHit hit in impactos)
            {
                if (seleccionado && flechaIzquierda != null && hit.transform == flechaIzquierda.transform)
                {
                    if (estanteria != null) estanteria.Rotar90(-1f);
                    haTocadoAlgoPropio = true;
                    break;
                }
                if (seleccionado && flechaDerecha != null && hit.transform == flechaDerecha.transform)
                {
                    if (estanteria != null) estanteria.Rotar90(1f);
                    haTocadoAlgoPropio = true;
                    break;
                }

                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    seleccionado = true;
                    arrastrando = true;
                    SetBrilloSeleccion(true);
                    MostrarFlechas(false);

                    if (Physics.Raycast(rayoClic, out RaycastHit hitSuelo, 100f, capaSuelo))
                    {
                        offsetArrastre = transform.position - hitSuelo.point;
                    }
                    else
                    {
                        offsetArrastre = transform.position - hit.point;
                        offsetArrastre.y = 0f;
                    }

                    haTocadoAlgoPropio = true;
                    break;
                }
            }

            if (!haTocadoAlgoPropio && seleccionado)
            {
                seleccionado = false;
                SetBrilloSeleccion(false);
                MostrarFlechas(false);
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (arrastrando)
            {
                arrastrando = false;
                if (seleccionado) MostrarFlechas(true);
            }
        }

        if (arrastrando)
        {
            Ray rayoArrastre = camaraPrincipal.ScreenPointToRay(posicionRaton);
            if (Physics.Raycast(rayoArrastre, out RaycastHit hitSuelo, 100f, capaSuelo))
            {
                Vector3 posicionPropuesta = hitSuelo.point + offsetArrastre;
                posicionPropuesta.y = alturaSueloFija;
                posicionPropuesta = ClampDentroDelSuelo(posicionPropuesta);

                transform.position = posicionPropuesta;
                ResolverColisionesFisicas();
            }
        }
    }

    Vector3 ClampDentroDelSuelo(Vector3 posicion)
    {
        if (sueloReferencia == null || miColisionador == null) return posicion;

        Collider colSuelo = sueloReferencia.GetComponent<Collider>();
        if (colSuelo == null) return posicion;

        Bounds boundsSuelo = colSuelo.bounds;

        Vector3 medioTamano = Vector3.Scale(miColisionador.size / 2f, transform.lossyScale);

        float radianes = transform.eulerAngles.y * Mathf.Deg2Rad;
        float mitadX = Mathf.Abs(Mathf.Cos(radianes)) * medioTamano.x + Mathf.Abs(Mathf.Sin(radianes)) * medioTamano.z;
        float mitadZ = Mathf.Abs(Mathf.Sin(radianes)) * medioTamano.x + Mathf.Abs(Mathf.Cos(radianes)) * medioTamano.z;

        posicion.x = Mathf.Clamp(posicion.x, boundsSuelo.min.x + mitadX, boundsSuelo.max.x - mitadX);
        posicion.z = Mathf.Clamp(posicion.z, boundsSuelo.min.z + mitadZ, boundsSuelo.max.z - mitadZ);

        return posicion;
    }

    void ResolverColisionesFisicas()
    {
        if (miColisionador == null) return;

        Collider[] colisiones = Physics.OverlapBox(
            transform.position + miColisionador.center,
            miColisionador.size / 2f,
            transform.rotation,
            capaParedes | capaObstaculos
        );

        foreach (var col in colisiones)
        {
            if (col == null || col.transform == transform || col.transform.IsChildOf(transform)) continue;

            if (Physics.ComputePenetration(
                miColisionador, transform.position, transform.rotation,
                col, col.transform.position, col.transform.rotation,
                out Vector3 direccion, out float distancia
            ))
            {
                Vector3 empuje = direccion * (distancia + 0.005f);
                empuje.y = 0f;
                transform.position += empuje;
            }
        }
    }

    void MostrarFlechas(bool mostrar)
    {
        // Si no se permite la interacción (modo configurador estático), FORZAMOS el apagado de flechas
        if (!permitirInteraccion)
        {
            mostrar = false;
        }

        if (flechaIzquierda != null) flechaIzquierda.SetActive(mostrar);
        if (flechaDerecha != null) flechaDerecha.SetActive(mostrar);
    }

    void SetBrilloSeleccion(bool activar)
    {
        renderersHijos = GetComponentsInChildren<Renderer>(true);
        if (renderersHijos == null || propBlock == null) return;

        foreach (var r in renderersHijos)
        {
            if (r == null) continue;

            // Evitamos aplicar brillo de selección a las flechas
            if ((flechaIzquierda != null && r.transform.IsChildOf(flechaIzquierda.transform)) ||
                (flechaDerecha != null && r.transform.IsChildOf(flechaDerecha.transform)))
            {
                continue;
            }

            foreach (var mat in r.sharedMaterials)
            {
                if (mat != null) mat.EnableKeyword("_EMISSION");
            }

            r.GetPropertyBlock(propBlock);
            Color colorFinal = activar ? colorSeleccion : Color.black;
            propBlock.SetColor("_EmissionColor", colorFinal);
            r.SetPropertyBlock(propBlock);
        }
    }

    void OnDisable()
    {
        seleccionado = false;
        arrastrando = false;
        SetBrilloSeleccion(false);
        MostrarFlechas(false);
    }
}