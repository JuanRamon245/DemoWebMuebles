using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ParametricWindow : MonoBehaviour
{
    public enum TipoMaterial { Madera1, Madera2, Madera3, PlasticoGris }

    [Header("Dimensiones de la Ventana")]
    [Range(0.5f, 3.0f)] public float altoVentana = 1.2f;
    [Range(0.5f, 3.0f)] public float anchoVentana = 1.2f;

    [Tooltip("Grosor real de la ventana (profundidad Z). Debe coincidir con el grosor de la pared.")]
    [Range(0.05f, 0.3f)] public float grosorMarco = 0.1f;

    [Tooltip("Anchura visual del perfil del marco exterior (cara que mira al usuario en X/Y).")]
    [Range(0.03f, 0.15f)] public float perfilExterior = 0.06f;

    [Header("Configuración Visual")]
    public TipoMaterial materialActual = TipoMaterial.Madera1;

    [Header("Materiales (Asignar en Inspector)")]
    public Material matMadera1;
    public Material matMadera2;
    public Material matMadera3;
    public Material matPlasticoGris;
    public Material matOro;
    public Material matNegro;
    [Tooltip("Asigna aquí un material con transparencia para el vidrio")]
    public Material matCristal;

    [Header("Referencias a Marcos Exteriores")]
    public Transform marcoExtIzq;
    public Transform marcoExtDer;
    public Transform marcoExtSup;
    public Transform marcoExtInf;

    [Header("Referencias a Marcos Internos (Hoja)")]
    public Transform marcoIntIzq;
    public Transform marcoIntDer;
    public Transform marcoIntSup;
    public Transform marcoIntInf;
    public Transform pivoteHoja;
    public Transform Cristal;

    [Header("Herrajes")]
    [Tooltip("Debe ser un Cube: representa la manilla de la ventana")]
    public GameObject visualManillar;

    private const float ANCHO_PERFIL_INTERIOR = 0.04f;
    private const float GROSOR_HOJA = 0.04f;

    private const float MANILLAR_LARGO = 0.10f;
    private const float MANILLAR_ALTO = 0.02f;
    private const float MANILLAR_FONDO = 0.02f;

    private BoxCollider boxCollider;

    void OnValidate()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= EjecutarActualizacionSegura;
        UnityEditor.EditorApplication.delayCall += EjecutarActualizacionSegura;
#else
        ActualizarVentana();
#endif
    }

    void EjecutarActualizacionSegura()
    {
        if (this == null) return;
        ActualizarVentana();
    }

    void ActualizarVentana()
    {
        // Validamos que las referencias exteriores esenciales existan
        if (marcoExtIzq == null || marcoExtDer == null || marcoExtSup == null || marcoExtInf == null) return;
        if (pivoteHoja == null || Cristal == null) return;

        // 1. ESCALAR Y POSICIONAR LOS MARCOS EXTERIORES (Centrados en el origen 0,0,0)
        marcoExtIzq.localScale = new Vector3(perfilExterior, altoVentana, grosorMarco);
        marcoExtIzq.localPosition = new Vector3(-anchoVentana / 2f + perfilExterior / 2f, 0f, 0f);

        marcoExtDer.localScale = new Vector3(perfilExterior, altoVentana, grosorMarco);
        marcoExtDer.localPosition = new Vector3(anchoVentana / 2f - perfilExterior / 2f, 0f, 0f);

        marcoExtSup.localScale = new Vector3(anchoVentana, perfilExterior, grosorMarco);
        marcoExtSup.localPosition = new Vector3(0f, altoVentana / 2f - perfilExterior / 2f, 0f);

        marcoExtInf.localScale = new Vector3(anchoVentana, perfilExterior, grosorMarco);
        marcoExtInf.localPosition = new Vector3(0f, -altoVentana / 2f + perfilExterior / 2f, 0f);

        // 2. CONFIGURAR EL HUECO INTERIOR (La hoja que se puede abrir)
        float anchoHojaReal = anchoVentana - (perfilExterior * 2f);
        float altoHojaReal = altoVentana - (perfilExterior * 2f);

        pivoteHoja.localPosition = new Vector3(-anchoVentana / 2f + perfilExterior, -altoVentana / 2f + perfilExterior, 0f);

        // 3. ESCALAR Y POSICIONAR LOS MARCOS INTERNOS (Estructura que sostiene el vidrio)
        if (marcoIntIzq != null && marcoIntDer != null && marcoIntSup != null && marcoIntInf != null)
        {
            marcoIntIzq.localScale = new Vector3(ANCHO_PERFIL_INTERIOR, altoHojaReal, GROSOR_HOJA);
            marcoIntIzq.localPosition = new Vector3(ANCHO_PERFIL_INTERIOR / 2f, altoHojaReal / 2f, 0f);

            marcoIntDer.localScale = new Vector3(ANCHO_PERFIL_INTERIOR, altoHojaReal, GROSOR_HOJA);
            marcoIntDer.localPosition = new Vector3(anchoHojaReal - ANCHO_PERFIL_INTERIOR / 2f, altoHojaReal / 2f, 0f);

            marcoIntSup.localScale = new Vector3(anchoHojaReal, ANCHO_PERFIL_INTERIOR, GROSOR_HOJA);
            marcoIntSup.localPosition = new Vector3(anchoHojaReal / 2f, altoHojaReal - ANCHO_PERFIL_INTERIOR / 2f, 0f);

            marcoIntInf.localScale = new Vector3(anchoHojaReal, ANCHO_PERFIL_INTERIOR, GROSOR_HOJA);
            marcoIntInf.localPosition = new Vector3(anchoHojaReal / 2f, ANCHO_PERFIL_INTERIOR / 2f, 0f);
        }

        // 4. CONFIGURAR EL CRISTAL (Ocupa el espacio central libre de la hoja)
        float anchoCristal = anchoHojaReal - (ANCHO_PERFIL_INTERIOR * 2f);
        float altoCristal = altoHojaReal - (ANCHO_PERFIL_INTERIOR * 2f);
        float grosorCristal = 0.01f;

        Cristal.localScale = new Vector3(anchoCristal, altoCristal, grosorCristal);
        Cristal.localPosition = new Vector3(anchoHojaReal / 2f, altoHojaReal / 2f, 0f);

        // 5. POSICIONAR EL MANILLAR (Centrado verticalmente en la hoja)
        float alturaAccesorios = altoHojaReal / 2f;
        float distanciaAlBorde = ANCHO_PERFIL_INTERIOR + 0.02f;
        float posXAccesorios = anchoHojaReal - distanciaAlBorde;

        ConfigurarManillar(posXAccesorios, alturaAccesorios);

        // 6. ACTUALIZAR EL BOX COLLIDER DINÁMICAMENTE
        ActualizarCollider();

        // 7. APLICAR MATERIALES
        AplicarLogicaMaterialesYComponentes();
    }

    void ActualizarCollider()
    {
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider>();
        }

        if (boxCollider != null)
        {
            boxCollider.size = new Vector3(anchoVentana, altoVentana, grosorMarco);
            boxCollider.center = Vector3.zero;
        }
    }

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

        ConfigurarSuperficieObjeto(marcoExtIzq, matAsignar, perfilExterior, altoVentana);
        ConfigurarSuperficieObjeto(marcoExtDer, matAsignar, perfilExterior, altoVentana);
        ConfigurarSuperficieObjeto(marcoExtSup, matAsignar, anchoVentana, perfilExterior);
        ConfigurarSuperficieObjeto(marcoExtInf, matAsignar, anchoVentana, perfilExterior);

        float anchoHojaReal = anchoVentana - (perfilExterior * 2f);
        float altoHojaReal = altoVentana - (perfilExterior * 2f);

        ConfigurarSuperficieObjeto(marcoIntIzq, matAsignar, ANCHO_PERFIL_INTERIOR, altoHojaReal);
        ConfigurarSuperficieObjeto(marcoIntDer, matAsignar, ANCHO_PERFIL_INTERIOR, altoHojaReal);
        ConfigurarSuperficieObjeto(marcoIntSup, matAsignar, anchoHojaReal, ANCHO_PERFIL_INTERIOR);
        ConfigurarSuperficieObjeto(marcoIntInf, matAsignar, anchoHojaReal, ANCHO_PERFIL_INTERIOR);

        // Aplicar material transparente al cristal
        if (Cristal != null && matCristal != null)
        {
            var glassRenderer = Cristal.GetComponent<MeshRenderer>();
            if (glassRenderer != null)
            {
                glassRenderer.sharedMaterial = matCristal;
                matCristal.EnableKeyword("_EMISSION");
            }
        }

        Material matHerraje = matPlasticoGris;
        if (!esMaderaClara)
        {
            matHerraje = esPlastico ? matNegro : matOro;
        }

        if (visualManillar != null) AsignarMaterialAMatriz(visualManillar, matHerraje);

#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    void ConfigurarSuperficieObjeto(Transform obj, Material mat, float scaleX, float scaleY)
    {
        if (obj == null || mat == null) return;

        var renderer = obj.GetComponent<MeshRenderer>();
        if (renderer == null) return;

        renderer.sharedMaterial = mat;

        mat.EnableKeyword("_EMISSION");

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);

        Vector4 tilingOffset = new Vector4(scaleX, scaleY, 0, 0);

        block.SetVector("_MainTex_ST", tilingOffset);
        block.SetVector("_BaseMap_ST", tilingOffset);

        renderer.SetPropertyBlock(block);
    }

    void AsignarMaterialAMatriz(GameObject parent, Material mat)
    {
        if (parent == null || mat == null) return;

        mat.EnableKeyword("_EMISSION");

        foreach (var r in parent.GetComponentsInChildren<MeshRenderer>(true))
        {
            r.sharedMaterial = mat;
        }
    }
}