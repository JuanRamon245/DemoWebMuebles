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
    [Tooltip("Si está activo, el margen superior e inferior siempre serán iguales.")]
    public bool margenSimetrico = true;
    [Range(0.05f, 0.6f)] public float margenSuperior = 0.15f;
    [Range(0.05f, 0.6f)] public float margenInferior = 0.15f;

    [Header("Distribución de Baldas")]
    [Tooltip("Total de baldas incluyendo Techo y Suelo (Mínimo 4 = Techo + Suelo + 2 Intermedias)")]
    [Range(4, 10)] public int totalBaldas = 5;

    [Header("Configuración Visual")]
    public TipoMaterial materialActual = TipoMaterial.Madera1;
    [Range(0f, 270f)]
    [Tooltip("Orientación del mueble en incrementos de 90 grados.")]
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
    [Tooltip("Arrastra aquí un GameObject vacío que contendrá las baldas intermedias.")]
    public Transform contenedorBaldas;

    private float ultimoMargenSuperior;
    private float ultimoMargenInferior;
    private BoxCollider boxCollider;

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
        fondo.localPosition = new Vector3(0f, altoEstanteria / 2f, -profundidadEstanteria / 2f + grosorEstructura / 2f);

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
        maxIntermediasPosibles = Mathf.Max(2, maxIntermediasPosibles);

        int numIntermediasEfectivas = Mathf.Clamp(numIntermediasDeseadas, 2, maxIntermediasPosibles);

        totalBaldas = numIntermediasEfectivas + 2;

        float aireEntreBaldas = (espacioDisponibleY - (numIntermediasEfectivas * grosorBaldasHorizontales)) / (numIntermediasEfectivas - 1);

        AjustarPoolDeBaldas(numIntermediasEfectivas);

        float profBaldaIntermedia = profundidadEstanteria - grosorEstructura;
        float zBaldaIntermedia = grosorEstructura / 2f;

        float yInicio = grosorEstructura + margenInferior;

        for (int i = 0; i < numIntermediasEfectivas; i++)
        {
            Transform balda = contenedorBaldas.GetChild(i);
            balda.localScale = new Vector3(anchoInterior, grosorBaldasHorizontales, profBaldaIntermedia);

            float posY = yInicio + (grosorBaldasHorizontales / 2f) + i * (aireEntreBaldas + grosorBaldasHorizontales);
            balda.localPosition = new Vector3(0f, posY, zBaldaIntermedia);
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
            GameObject nuevaBalda = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nuevaBalda.name = "Balda_Intermedia_" + contenedorBaldas.childCount;
            nuevaBalda.transform.SetParent(contenedorBaldas);

            Collider c = nuevaBalda.GetComponent<Collider>();
            if (c != null) DestroyImmediate(c);
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

        MaterialPropertyBlock block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block);

        Vector4 tilingOffset = new Vector4(scaleX, scaleY, 0, 0);
        block.SetVector("_MainTex_ST", tilingOffset);
        block.SetVector("_BaseMap_ST", tilingOffset);

        r.SetPropertyBlock(block);
    }

    // --- LOGICA DE ROTACIÓN ---
    public void Rotar90()
    {
        Rotar90(1f);
    }

    public void Rotar90(float direccion)
    {
        float signo = direccion >= 0 ? 1f : -1f;
        rotacionY = (rotacionY + 90f * signo + 360f) % 360f;
        transform.localRotation = Quaternion.Euler(0f, rotacionY, 0f);
    }
}