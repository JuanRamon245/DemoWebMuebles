using UnityEngine;

public class ParametricDoor : MonoBehaviour
{
    public enum TipoMaterial { Madera1, Madera2, Madera3, PlasticoGris }
    public enum TipoPomo { Manillar, Pomo }

    [Header("Dimensiones y Topes")]
    [Range(1.5f, 2.8f)] public float altoPuerta = 2.1f;
    [Range(0.7f, 1.5f)] public float anchoPuerta = 0.9f;
    [Range(0.05f, 0.2f)] public float grosorMarco = 0.1f;

    [Header("Configuración Visual")]
    public TipoMaterial materialActual = TipoMaterial.Madera1;
    public TipoPomo pomoActual = TipoPomo.Manillar;
    public bool conCerradura = false;

    [Header("Ajuste de Texturas")]
    [Tooltip("Regula el tamaño de la textura. Valores más altos repiten más la textura (ideal si se ve borrosa).")]
    [Range(0.1f, 10f)] public float escalaTextura = 1.0f;

    [Header("Materiales (Asignar en Inspector)")]
    public Material matMadera1;
    public Material matMadera2;
    public Material matMadera3;
    public Material matPlasticoGris;
    public Material matOro;
    public Material matNegro;

    [Header("Referencias a Hijos")]
    public Transform marcoIzq;
    public Transform marcoDer;
    public Transform marcoSup;
    public Transform pivoteHoja;
    public Transform hojaMesh;

    [Tooltip("Debe ser un Cube: representa la barra del manillar")]
    public GameObject visualManillar;
    [Tooltip("Debe ser un Cylinder: representa el pomo")]
    public GameObject visualPomo;
    [Tooltip("Debe ser un Cube: representa la placa de la cerradura")]
    public GameObject visualCerradura;

    private const float ANCHO_PERFIL_MARCO = 0.06f; // Lo ancho que es el listón del marco
    private const float GROSOR_HOJA = 0.04f;        // 4 cm de grosor de puerta estándar

    // --- Dimensiones reales de la herrajería (en metros) ---
    private const float MANILLAR_LARGO = 0.14f;   // longitud de la barra
    private const float MANILLAR_ALTO = 0.025f;   // grosor de la barra
    private const float MANILLAR_FONDO = 0.025f;

    private const float POMO_DIAMETRO = 0.07f;    // diámetro del pomo
    private const float POMO_SALIENTE = 0.035f;   // cuánto sobresale de la hoja

    private const float CERRADURA_LADO = 0.06f;   // placa cuadrada
    private const float CERRADURA_FONDO = 0.015f;

    void OnValidate()
    {
#if UNITY_EDITOR
        // Evitamos ejecutar código visual restrictivo en OnValidate usando delayCall
        UnityEditor.EditorApplication.delayCall -= EjecutarActualizacionSegura;
        UnityEditor.EditorApplication.delayCall += EjecutarActualizacionSegura;
#else
        ActualizarPuerta();
#endif
    }

    void EjecutarActualizacionSegura()
    {
        if (this == null) return;
        ActualizarPuerta();
    }

    void ActualizarPuerta()
    {
        if (marcoIzq == null || marcoDer == null || marcoSup == null || pivoteHoja == null || hojaMesh == null) return;

        // 1. ESCALAR Y POSICIONAR LOS MARCOS
        marcoIzq.localScale = new Vector3(ANCHO_PERFIL_MARCO, altoPuerta, grosorMarco);
        marcoIzq.localPosition = new Vector3(-anchoPuerta / 2f + ANCHO_PERFIL_MARCO / 2f, altoPuerta / 2f, 0);

        marcoDer.localScale = new Vector3(ANCHO_PERFIL_MARCO, altoPuerta, grosorMarco);
        marcoDer.localPosition = new Vector3(anchoPuerta / 2f - ANCHO_PERFIL_MARCO / 2f, altoPuerta / 2f, 0);

        marcoSup.localScale = new Vector3(anchoPuerta, ANCHO_PERFIL_MARCO, grosorMarco);
        marcoSup.localPosition = new Vector3(0, altoPuerta - ANCHO_PERFIL_MARCO / 2f, 0);

        // 2. CONFIGURAR LA HOJA (La puerta en sí)
        float anchoHojaReal = anchoPuerta - (ANCHO_PERFIL_MARCO * 2f);
        float altoHojaReal = altoPuerta - ANCHO_PERFIL_MARCO;

        // Colocar el pivote en la bisagra (borde interior izquierdo)
        pivoteHoja.localPosition = new Vector3(-anchoPuerta / 2f + ANCHO_PERFIL_MARCO, 0, 0);

        // La malla de la hoja se escala y se desplaza el doble de su mitad
        hojaMesh.localScale = new Vector3(anchoHojaReal, altoHojaReal, GROSOR_HOJA);
        hojaMesh.localPosition = new Vector3(anchoHojaReal / 2f, altoHojaReal / 2f, 0);

        // 3. POSICIONAR Y DAR FORMA A LOS HERRAJES (pomo/manillar/cerradura)
        float alturaAccesorios = 1.0f; // Altura estándar de una manilla (1 metro del suelo)
        float distanciaAlBorde = 0.08f; // Distancia desde el borde derecho de la hoja (8 cm)
        float posXAccesorios = anchoHojaReal - distanciaAlBorde;

        ConfigurarManillar(posXAccesorios, alturaAccesorios);
        ConfigurarPomo(posXAccesorios, alturaAccesorios);
        ConfigurarCerradura(posXAccesorios, alturaAccesorios);

        // 4. APLICAR MATERIALES
        AplicarLogicaMaterialesYComponentes();
    }

    // El manillar es una barra (Cube) que sale de la hoja y se prolonga hacia el
    // centro de la puerta (eje -X), simulando la palanca de un manillar real.
    void ConfigurarManillar(float posX, float altura)
    {
        if (visualManillar == null) return;

        Transform t = visualManillar.transform;
        t.localScale = new Vector3(MANILLAR_LARGO, MANILLAR_ALTO, MANILLAR_FONDO);
        t.localRotation = Quaternion.identity;

        // Sobresale de la cara de la hoja (mitad grosor hoja + mitad grosor manillar)
        float saliente = GROSOR_HOJA / 2f + MANILLAR_FONDO / 2f;

        // Se desplaza hacia -X para que la barra "cuelgue" hacia el centro de la puerta,
        // en vez de quedar centrada en el punto de anclaje
        float offsetX = -MANILLAR_LARGO / 2f;

        t.localPosition = new Vector3(posX + offsetX, altura, saliente);
    }

    // El pomo es un Cylinder. El cilindro de Unity crece por defecto en su eje Y local,
    // así que lo rotamos 90° en X para que "crezca" hacia Z (hacia fuera de la puerta).
    void ConfigurarPomo(float posX, float altura)
    {
        if (visualPomo == null) return;

        Transform t = visualPomo.transform;
        // X/Z = diámetro, Y = cuánto sobresale (tras la rotación pasa a ser el eje Z)
        t.localScale = new Vector3(POMO_DIAMETRO, POMO_SALIENTE, POMO_DIAMETRO);
        t.localRotation = Quaternion.Euler(90f, 0f, 0f);

        float saliente = GROSOR_HOJA / 2f + POMO_SALIENTE / 2f;
        t.localPosition = new Vector3(posX, altura, saliente);
    }

    // La cerradura es una placa cuadrada plana, colocada 15 cm por debajo del pomo/manillar
    void ConfigurarCerradura(float posX, float altura)
    {
        if (visualCerradura == null) return;

        Transform t = visualCerradura.transform;
        t.localScale = new Vector3(CERRADURA_LADO, CERRADURA_LADO, CERRADURA_FONDO);
        t.localRotation = Quaternion.identity;

        float saliente = GROSOR_HOJA / 2f + CERRADURA_FONDO / 2f;
        t.localPosition = new Vector3(posX, altura - 0.15f, saliente);
    }

    void AplicarLogicaMaterialesYComponentes()
    {
        // Determinar material de la puerta
        Material matAsignar = matMadera2;
        bool esPlastico = false;
        bool esMaderaClara = false;

        switch (materialActual)
        {
            case TipoMaterial.Madera1: matAsignar = matMadera1; esMaderaClara = true; break;
            case TipoMaterial.Madera3: matAsignar = matMadera3; break;
            case TipoMaterial.PlasticoGris: matAsignar = matPlasticoGris; esPlastico = true; break;
        }

        // Calcular dimensiones reales para el mapeado UV 1:1
        float anchoHojaReal = anchoPuerta - (ANCHO_PERFIL_MARCO * 2f);
        float altoHojaReal = altoPuerta - ANCHO_PERFIL_MARCO;

        ConfigurarSuperficiePuerta(marcoIzq, matAsignar, ANCHO_PERFIL_MARCO, altoPuerta);
        ConfigurarSuperficiePuerta(marcoDer, matAsignar, ANCHO_PERFIL_MARCO, altoPuerta);
        ConfigurarSuperficiePuerta(marcoSup, matAsignar, anchoPuerta, ANCHO_PERFIL_MARCO);
        ConfigurarSuperficiePuerta(hojaMesh, matAsignar, anchoHojaReal, altoHojaReal);

        // Activar/Desactivar visuales mediante el MeshRenderer para evitar errores de OnValidate
        SetRenderersActivos(visualManillar, pomoActual == TipoPomo.Manillar);
        SetRenderersActivos(visualPomo, pomoActual == TipoPomo.Pomo);
        SetRenderersActivos(visualCerradura, conCerradura);

        // Lógica de colores: puerta de madera -> herraje dorado | puerta de plástico -> herraje negro
        Material matPomo = matPlasticoGris;
        if (!esMaderaClara)
        {
            matPomo = esPlastico ? matNegro : matOro;
        }

        if (visualManillar != null) AsignarMaterialAMatriz(visualManillar, matPomo);
        if (visualPomo != null) AsignarMaterialAMatriz(visualPomo, matPomo);
        if (visualCerradura != null) AsignarMaterialAMatriz(visualCerradura, matPomo);

#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    void SetRenderersActivos(GameObject obj, bool activo)
    {
        if (obj == null) return;

        // El parámetro 'true' asegura encontrar renderers incluso si se desactivaron previamente
        foreach (var r in obj.GetComponentsInChildren<MeshRenderer>(true))
        {
            r.enabled = activo;
        }
    }

    void ConfigurarSuperficiePuerta(Transform obj, Material mat, float scaleX, float scaleY)
    {
        if (obj == null || mat == null) return;

        var renderer = obj.GetComponent<MeshRenderer>();
        if (renderer == null) return;

        renderer.sharedMaterial = mat;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);

        float finalScaleX = scaleX * escalaTextura;
        float finalScaleY = scaleY * escalaTextura;

        Vector4 tilingOffset = new Vector4(finalScaleX, finalScaleY, 0, 0);

        block.SetVector("_MainTex_ST", tilingOffset);
        block.SetVector("_BaseMap_ST", tilingOffset);

        renderer.SetPropertyBlock(block);
    }

    void AsignarMaterialAMatriz(GameObject parent, Material mat)
    {
        if (parent == null || mat == null) return;

        foreach (var r in parent.GetComponentsInChildren<MeshRenderer>(true))
        {
            r.sharedMaterial = mat;
        }
    }
}