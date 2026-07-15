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

    private const float ANCHO_PERFIL_MARCO = 0.06f;
    private const float GROSOR_HOJA = 0.04f;
    private const float MANILLAR_LARGO = 0.14f;
    private const float MANILLAR_ALTO = 0.025f;
    private const float MANILLAR_FONDO = 0.025f;

    private const float POMO_DIAMETRO = 0.07f;
    private const float POMO_SALIENTE = 0.035f;

    private const float CERRADURA_LADO = 0.06f;
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

        pivoteHoja.localPosition = new Vector3(-anchoPuerta / 2f + ANCHO_PERFIL_MARCO, 0, 0);

        hojaMesh.localScale = new Vector3(anchoHojaReal, altoHojaReal, GROSOR_HOJA);
        hojaMesh.localPosition = new Vector3(anchoHojaReal / 2f, altoHojaReal / 2f, 0);

        // 3. POSICIONAR Y DAR FORMA A LOS HERRAJES (pomo/manillar/cerradura)
        float alturaAccesorios = 1.0f;
        float distanciaAlBorde = 0.08f;
        float posXAccesorios = anchoHojaReal - distanciaAlBorde;

        ConfigurarManillar(posXAccesorios, alturaAccesorios);
        ConfigurarPomo(posXAccesorios, alturaAccesorios);
        ConfigurarCerradura(posXAccesorios, alturaAccesorios);

        // 4. APLICAR MATERIALES
        AplicarLogicaMaterialesYComponentes();
    }

    // Logica para configurar el manillar de la puerta
    void ConfigurarManillar(float posX, float altura)
    {
        if (visualManillar == null) return;

        Transform t = visualManillar.transform;
        t.localScale = new Vector3(MANILLAR_LARGO, MANILLAR_ALTO, MANILLAR_FONDO);
        t.localRotation = Quaternion.identity;

        float saliente = GROSOR_HOJA / 2f + MANILLAR_FONDO / 2f;
        float offsetX = -MANILLAR_LARGO / 2f;

        t.localPosition = new Vector3(posX + offsetX, altura, saliente);
    }

    // Logica para configurar el pomo de la puerta
    void ConfigurarPomo(float posX, float altura)
    {
        if (visualPomo == null) return;

        Transform t = visualPomo.transform;
        t.localScale = new Vector3(POMO_DIAMETRO, POMO_SALIENTE, POMO_DIAMETRO);
        t.localRotation = Quaternion.Euler(90f, 0f, 0f);

        float saliente = GROSOR_HOJA / 2f + POMO_SALIENTE / 2f;
        t.localPosition = new Vector3(posX, altura, saliente);
    }

    // Logica para configurar la cerradura de la puerta
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

    // Logica para configurar que opciones de la puerta están activos
    void SetRenderersActivos(GameObject obj, bool activo)
    {
        if (obj == null) return;

        // El parámetro 'true' asegura encontrar renderers incluso si se desactivaron previamente
        foreach (var r in obj.GetComponentsInChildren<MeshRenderer>(true))
        {
            r.enabled = activo;
        }
    }

    // Logica para configurar el material y tamaño de la puerta
    void ConfigurarSuperficiePuerta(Transform obj, Material mat, float scaleX, float scaleY)
    {
        if (obj == null || mat == null) return;

        var renderer = obj.GetComponent<MeshRenderer>();
        if (renderer == null) return;

        renderer.sharedMaterial = mat;

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);

        float finalScaleX = scaleX * 1.0f;
        float finalScaleY = scaleY * 1.0f;

        Vector4 tilingOffset = new Vector4(finalScaleX, finalScaleY, 0, 0);

        block.SetVector("_MainTex_ST", tilingOffset);
        block.SetVector("_BaseMap_ST", tilingOffset);

        renderer.SetPropertyBlock(block);
    }

    // Logica para configurar el material de las opciones de la puerta
    void AsignarMaterialAMatriz(GameObject parent, Material mat)
    {
        if (parent == null || mat == null) return;

        foreach (var r in parent.GetComponentsInChildren<MeshRenderer>(true))
        {
            r.sharedMaterial = mat;
        }
    }
}