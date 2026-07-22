using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class ParametricShelf : MonoBehaviour
{
    public enum TipoMaterial { Madera1, Madera2, Madera3 }

    [Header("Dimensiones Estantería")]
    [Range(1.0f, 3.0f)] public float altoEstanteria = 2.0f;
    [Range(0.6f, 2.4f)] public float anchoEstanteria = 1.0f;
    [Range(0.3f, 1.0f)] public float profundidadEstanteria = 0.4f;

    [Header("Grosores")]
    [Range(0.015f, 0.06f)] public float grosorBaldasHorizontales = 0.02f;
    [Range(0.02f, 0.08f)] public float grosorEstructura = 0.03f;

    [Header("Márgenes (Espaciado)")]
    public bool margenSimetrico = true;
    [Range(0.05f, 0.6f)] public float margenSuperior = 0.15f;
    [Range(0.05f, 0.6f)] public float margenInferior = 0.15f;

    [Header("Distribución de Baldas")]
    [Range(4, 10)] public int totalBaldas = 5;

    [Header("Configuración Visual")]
    public TipoMaterial materialActual = TipoMaterial.Madera1;
    private float rotacionY = 0f;

    [Header("Materiales (Asignar en Inspector)")]
    public Material matMadera1;
    public Material matMadera2;
    public Material matMadera3;

    [Header("Referencias a Estructuras")]
    public Transform lateralIzq;
    public Transform lateralDer;
    public Transform techo;
    public Transform suelo;
    public Transform fondo;
    public Transform contenedorBaldas;

    [Header("Prefabs Optimización")]
    [Tooltip("Asigna aquí un prefab base para la balda (un Cubo sencillo con MeshFilter y MeshRenderer)")]
    public GameObject prefabBalda;

    private float ultimoMargenSuperior;
    private float ultimoMargenInferior;
    private BoxCollider boxCollider;

    // OPTIMIZACIÓN WEBGL: Reutilizar el bloque de propiedades para no saturar el GC
    private MaterialPropertyBlock propBlock;

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();
    }

    void OnValidate()
    {
        if (margenSimetrico)
        {
            if (margenSuperior != ultimoMargenSuperior)
            {
                margenInferior = margenSuperior;
            }
            else if (margenInferior != ultimoMargenInferior)
            {
                margenSuperior = margenInferior;
            }
            else if (margenSuperior != margenInferior)
            {
                float minMargen = Mathf.Min(margenSuperior, margenInferior);
                margenSuperior = minMargen;
                margenInferior = minMargen;
            }
        }

        ultimoMargenSuperior = margenSuperior;
        ultimoMargenInferior = margenInferior;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= EjecutarActualizacionSegura;
        UnityEditor.EditorApplication.delayCall += EjecutarActualizacionSegura;
#else
        ActualizarEstanteria();
#endif
    }

    void EjecutarActualizacionSegura()
    {
        if (this == null) return;
        ActualizarEstanteria();
    }

    public void SetAlto(float valor) { altoEstanteria = valor; ActualizarEstanteria(); }
    public void SetAncho(float valor) { anchoEstanteria = valor; ActualizarEstanteria(); }
    public void SetProfundidad(float valor) { profundidadEstanteria = valor; ActualizarEstanteria(); }
    public void SetTamanoBaldas(float valor) { grosorBaldasHorizontales = valor; ActualizarEstanteria(); }
    public void SetNumBaldas(float valor) { totalBaldas = Mathf.RoundToInt(valor); ActualizarEstanteria(); }
    public void SetMargenSuperior(float valor) { margenSuperior = valor; ActualizarEstanteria(); }
    public void SetMargenInferior(float valor) { margenInferior = valor; ActualizarEstanteria(); }
    public void SetMargenSimetrico(float valor) { margenSimetrico = valor > 0.5f; ActualizarEstanteria(); }

    public void SetMaterial(string valor)
    {
        switch (valor)
        {
            case "Abedul": materialActual = TipoMaterial.Madera1; break;
            case "Cerezo": materialActual = TipoMaterial.Madera2; break;
            case "Nogal": materialActual = TipoMaterial.Madera3; break;
            default:
                Debug.LogWarning($"Material '{valor}' sin mapeo definido");
                break;
        }
        AplicarMateriales();
    }

    [System.Serializable]
    private class ConfigEstanteriaJson
    {
        public float alto, ancho, profundidad, tamanoBaldas, margenSuperior, margenInferior;
        public int numBaldas;
        public string material;
        public bool margenSimetrico;
    }

    public void AplicarConfiguracion(string json)
    {
        var cfg = JsonUtility.FromJson<ConfigEstanteriaJson>(json);
        altoEstanteria = cfg.alto;
        anchoEstanteria = cfg.ancho;
        profundidadEstanteria = cfg.profundidad;
        grosorBaldasHorizontales = cfg.tamanoBaldas;
        totalBaldas = cfg.numBaldas;
        margenSuperior = cfg.margenSuperior;
        margenInferior = cfg.margenInferior;
        margenSimetrico = cfg.margenSimetrico;
        SetMaterial(cfg.material);
        ActualizarEstanteria();
    }

    public void ActualizarEstanteria()
    {
        if (lateralIzq == null || lateralDer == null || techo == null || suelo == null || fondo == null || contenedorBaldas == null) return;

        lateralIzq.localScale = new Vector3(grosorEstructura, altoEstanteria, profundidadEstanteria);
        lateralIzq.localPosition = new Vector3(-anchoEstanteria / 2f + grosorEstructura / 2f, altoEstanteria / 2f, 0f);

        lateralDer.localScale = new Vector3(grosorEstructura, altoEstanteria, profundidadEstanteria);
        lateralDer.localPosition = new Vector3(anchoEstanteria / 2f - grosorEstructura / 2f, altoEstanteria / 2f, 0f);

        float anchoInterior = anchoEstanteria - (grosorEstructura * 2f);
        techo.localScale = new Vector3(anchoInterior, grosorEstructura, profundidadEstanteria);
        techo.localPosition = new Vector3(0f, altoEstanteria - grosorEstructura / 2f, 0f);

        suelo.localScale = new Vector3(anchoInterior, grosorEstructura, profundidadEstanteria);
        suelo.localPosition = new Vector3(0f, grosorEstructura / 2f, 0f);

        float altoInteriorY = altoEstanteria - (grosorEstructura * 2f);
        fondo.localScale = new Vector3(anchoInterior, altoInteriorY, grosorEstructura);
        fondo.localPosition = new Vector3(0f, altoEstanteria / 2f, profundidadEstanteria / 2f - grosorEstructura / 2f);

        float espacioDisponibleY = altoEstanteria - (grosorEstructura * 2f) - margenSuperior - margenInferior;

        if (espacioDisponibleY < 0.3f)
        {
            float maxMargen = (altoEstanteria - (grosorEstructura * 2f) - 0.3f) / 2f;
            maxMargen = Mathf.Max(0.05f, maxMargen);
            margenSuperior = Mathf.Clamp(margenSuperior, 0.05f, maxMargen);
            margenInferior = Mathf.Clamp(margenInferior, 0.05f, maxMargen);
            espacioDisponibleY = altoEstanteria - (grosorEstructura * 2f) - margenSuperior - margenInferior;
        }

        int numIntermediasDeseadas = totalBaldas - 2;
        int maxIntermediasPosibles = Mathf.FloorToInt((espacioDisponibleY - 0.3f) / (0.3f + grosorBaldasHorizontales));
        maxIntermediasPosibles = Mathf.Max(1, maxIntermediasPosibles);

        int numIntermediasEfectivas = Mathf.Clamp(numIntermediasDeseadas, 1, maxIntermediasPosibles);
        totalBaldas = numIntermediasEfectivas + 2;

        AjustarPoolDeBaldas(numIntermediasEfectivas);

        float profBaldaIntermedia = profundidadEstanteria - grosorEstructura;
        float zBaldaIntermedia = grosorEstructura / 2f;
        float yInicio = grosorEstructura + margenInferior;

        if (numIntermediasEfectivas == 1)
        {
            Transform balda = contenedorBaldas.GetChild(0);
            balda.localScale = new Vector3(anchoInterior, grosorBaldasHorizontales, profBaldaIntermedia);
            float posY = yInicio + (espacioDisponibleY / 2f);
            balda.localPosition = new Vector3(0f, posY, zBaldaIntermedia);
        }
        else if (numIntermediasEfectivas > 1)
        {
            float aireSeparacion = (espacioDisponibleY - (numIntermediasEfectivas * grosorBaldasHorizontales)) / (numIntermediasEfectivas - 1);
            for (int i = 0; i < numIntermediasEfectivas; i++)
            {
                Transform balda = contenedorBaldas.GetChild(i);
                balda.localScale = new Vector3(anchoInterior, grosorBaldasHorizontales, profBaldaIntermedia);

                float posY = yInicio + (grosorBaldasHorizontales / 2f) + i * (aireSeparacion + grosorBaldasHorizontales);
                balda.localPosition = new Vector3(0f, posY, zBaldaIntermedia);
            }
        }

        ActualizarCollider();
        AplicarMateriales();
    }

    void AjustarPoolDeBaldas(int cantidadRequerida)
    {
        int hijosActuales = contenedorBaldas.childCount;
        for (int i = 0; i < hijosActuales; i++)
        {
            contenedorBaldas.GetChild(i).gameObject.SetActive(i < cantidadRequerida);
        }

        while (contenedorBaldas.childCount < cantidadRequerida)
        {
            GameObject nuevaBalda;

            // OPTIMIZACIÓN WEBGL: Clonar Prefab en lugar de usar CreatePrimitive
            if (prefabBalda != null)
            {
                nuevaBalda = Instantiate(prefabBalda, contenedorBaldas);
            }
            else
            {
                // Fallback por si no se asignó Prefab en el Inspector
                nuevaBalda = GameObject.CreatePrimitive(PrimitiveType.Cube);
                nuevaBalda.transform.SetParent(contenedorBaldas);
                Collider c = nuevaBalda.GetComponent<Collider>();
                if (c != null) DestroyImmediate(c);
            }

            nuevaBalda.name = "Balda_Intermedia_" + contenedorBaldas.childCount;
        }
    }

    void ActualizarCollider()
    {
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider>();

        if (boxCollider != null)
        {
            boxCollider.size = new Vector3(anchoEstanteria, altoEstanteria, profundidadEstanteria);
            boxCollider.center = new Vector3(0f, altoEstanteria / 2f, 0f);
        }
    }

    void AplicarMateriales()
    {
        Material matAsignar = matMadera2;
        switch (materialActual)
        {
            case TipoMaterial.Madera1: matAsignar = matMadera1; break;
            case TipoMaterial.Madera3: matAsignar = matMadera3; break;
        }

        ConfigurarTexturaMueble(lateralIzq, matAsignar, profundidadEstanteria, altoEstanteria);
        ConfigurarTexturaMueble(lateralDer, matAsignar, profundidadEstanteria, altoEstanteria);
        ConfigurarTexturaMueble(techo, matAsignar, anchoEstanteria, profundidadEstanteria);
        ConfigurarTexturaMueble(suelo, matAsignar, anchoEstanteria, profundidadEstanteria);
        ConfigurarTexturaMueble(fondo, matAsignar, anchoEstanteria, altoEstanteria);

        float anchoInterior = anchoEstanteria - (grosorEstructura * 2f);
        float profBalda = profundidadEstanteria - grosorEstructura;
        foreach (Transform child in contenedorBaldas)
        {
            if (child.gameObject.activeSelf)
            {
                ConfigurarTexturaMueble(child, matAsignar, anchoInterior, profBalda);
            }
        }

#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    void ConfigurarTexturaMueble(Transform obj, Material mat, float scaleX, float scaleY)
    {
        if (obj == null || mat == null) return;
        var r = obj.GetComponent<MeshRenderer>();
        if (r == null) return;

        r.sharedMaterial = mat;
        mat.EnableKeyword("_EMISSION");

        // OPTIMIZACIÓN CRÍTICA: Reutilizar 'propBlock' en lugar de instanciar uno nuevo
        if (propBlock == null) propBlock = new MaterialPropertyBlock();
        r.GetPropertyBlock(propBlock);

        Vector4 tilingOffset = new Vector4(scaleX, scaleY, 0, 0);
        propBlock.SetVector("_MainTex_ST", tilingOffset);
        propBlock.SetVector("_BaseMap_ST", tilingOffset);

        r.SetPropertyBlock(propBlock);
    }

    public void Rotar90(float direccion)
    {
        float signo = direccion >= 0 ? 1f : -1f;
        rotacionY = (rotacionY + 90f * signo + 360f) % 360f;
        transform.localRotation = Quaternion.Euler(0f, rotacionY, 0f);
    }
}