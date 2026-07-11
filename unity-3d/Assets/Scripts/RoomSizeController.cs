using UnityEngine;

public class RoomSizeController : MonoBehaviour
{
    public enum TipoHabitacion { SinTexturas, Dormitorio, Cocina, Salon }

    [Header("Configuración de Estilo")]
    public TipoHabitacion habitacionActual = TipoHabitacion.SinTexturas;
    public bool mostrarCuadrcula = true;

    [Header("Componentes de la Habitación")]
    public Transform floor;
    public Transform wallNorth;
    public Transform wallSouth;
    public Transform wallEast;
    public Transform wallWest;

    [Header("Dimensiones (en metros)")]
    [Range(2f, 30f)] public float width = 5f;  // Eje X
    [Range(2f, 30f)] public float length = 5f; // Eje Z
    private const float HEIGHT = 3f;
    private const float THICKNESS = 0.1f;

    [Header("Banco de Texturas")]
    public Texture2D texturaMadera;
    public Texture2D texturaLadrillo;
    public Texture2D texturaAzulejoBlanco;
    public Texture2D texturaAzulejoAzul;

    [Header("Prefabs Pared Norte 1 (3 seguidos, 2 huecos)")]
    public GameObject norte1Dormitorio;
    public GameObject norte1Cocina;
    public GameObject norte1Salon;

    [Header("Prefabs Pared Norte 2 (1 cada 4, despues de la casilla 1)")]
    public GameObject norte2Dormitorio;
    public GameObject norte2Cocina;
    public GameObject norte2Salon;

    [Header("Prefabs Pared Sur (1 cada 4, inicio en casilla 2)")]
    public GameObject surDormitorio;
    public GameObject surCocina;
    public GameObject surSalon;

    private MeshRenderer mrFloor, mrNorth, mrSouth, mrEast, mrWest;
    private Texture2D cuadrculaProcedural;

    private Material matSuelo;
    private Material matParedesNS;
    private Material matParedesEW;
    private const string CONTENEDOR_NAME = "_DecoracionProcedural";

    void Start()
    {
        ObtenerRenderers();
    }

    void ObtenerRenderers()
    {
        if (floor != null) mrFloor = floor.GetComponent<MeshRenderer>();
        if (wallNorth != null) mrNorth = wallNorth.GetComponent<MeshRenderer>();
        if (wallSouth != null) mrSouth = wallSouth.GetComponent<MeshRenderer>();
        if (wallEast != null) mrEast = wallEast.GetComponent<MeshRenderer>();
        if (wallWest != null) mrWest = wallWest.GetComponent<MeshRenderer>();
    }

    void OnValidate()
    {
        ActualizarGeometriaMuros();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall -= ReconstruirDecoracionSegura;
            UnityEditor.EditorApplication.delayCall += ReconstruirDecoracionSegura;
#endif
    }

    void ReconstruirDecoracionSegura()
    {
        if (this == null) return;
        GenerarDecoracionParedes();

#if UNITY_EDITOR
            UnityEditor.SceneView.RepaintAll();
#endif
    }

    public void ActualizarGeometriaMuros()
    {
        if (floor == null || wallNorth == null || wallSouth == null || wallEast == null || wallWest == null) return;
        InicializarMateriales();

        floor.localScale = new Vector3(width, THICKNESS, length);
        floor.localPosition = new Vector3(0, -THICKNESS / 2, 0);
        floor.localRotation = Quaternion.Euler(0, 180, 0);

        wallNorth.localScale = new Vector3(width, HEIGHT, THICKNESS);
        wallNorth.localPosition = new Vector3(0, HEIGHT / 2, length / 2);
        wallNorth.localRotation = Quaternion.Euler(0, 180, 0);

        wallSouth.localScale = new Vector3(width, HEIGHT, THICKNESS);
        wallSouth.localPosition = new Vector3(0, HEIGHT / 2, -length / 2);
        wallSouth.localRotation = Quaternion.Euler(0, 180, 0);

        wallEast.localScale = new Vector3(length, HEIGHT, THICKNESS);
        wallEast.localPosition = new Vector3(width / 2, HEIGHT / 2, 0);
        wallEast.localRotation = Quaternion.Euler(0, 90, 0);

        wallWest.localScale = new Vector3(length, HEIGHT, THICKNESS);
        wallWest.localPosition = new Vector3(-width / 2, HEIGHT / 2, 0);
        wallWest.localRotation = Quaternion.Euler(0, 90, 0);

        AplicarEstiloHabitacion();
    }

    void InicializarMateriales()
    {
        if (mrFloor == null) ObtenerRenderers();

        if (matSuelo == null && mrFloor != null && mrFloor.sharedMaterial != null) matSuelo = new Material(mrFloor.sharedMaterial);
        if (matParedesNS == null && mrNorth != null && mrNorth.sharedMaterial != null) matParedesNS = new Material(mrNorth.sharedMaterial);
        if (matParedesEW == null && mrEast != null && mrEast.sharedMaterial != null) matParedesEW = new Material(mrEast.sharedMaterial);

        Shader stdShader = Shader.Find("Standard");
        if (matSuelo == null) matSuelo = new Material(stdShader);
        if (matParedesNS == null) matParedesNS = new Material(stdShader);
        if (matParedesEW == null) matParedesEW = new Material(stdShader);

        if (mrFloor != null && mrFloor.sharedMaterial != matSuelo) mrFloor.sharedMaterial = matSuelo;
        if (mrNorth != null && mrNorth.sharedMaterial != matParedesNS) mrNorth.sharedMaterial = matParedesNS;
        if (mrSouth != null && mrSouth.sharedMaterial != matParedesNS) mrSouth.sharedMaterial = matParedesNS;
        if (mrEast != null && mrEast.sharedMaterial != matParedesEW) mrEast.sharedMaterial = matParedesEW;
        if (mrWest != null && mrWest.sharedMaterial != matParedesEW) mrWest.sharedMaterial = matParedesEW;
    }

    void AplicarEstiloHabitacion()
    {
        Color colorTortilla = new Color(0.92f, 0.85f, 0.46f);

        switch (habitacionActual)
        {
            case TipoHabitacion.SinTexturas:
                ConfigurarSuperficie(matSuelo, null, Color.gray, width, length);
                ConfigurarSuperficie(matParedesNS, null, Color.white, width, HEIGHT);
                ConfigurarSuperficie(matParedesEW, null, Color.white, length, HEIGHT);
                break;

            case TipoHabitacion.Dormitorio:
                ConfigurarSuperficie(matSuelo, texturaMadera, Color.white, width, length);
                ConfigurarSuperficie(matParedesNS, null, colorTortilla, width, HEIGHT);
                ConfigurarSuperficie(matParedesEW, null, colorTortilla, length, HEIGHT);
                break;

            case TipoHabitacion.Cocina:
                ConfigurarSuperficie(matSuelo, texturaAzulejoAzul, Color.white, width, length);
                ConfigurarSuperficie(matParedesNS, texturaAzulejoBlanco, Color.white, width, HEIGHT);
                ConfigurarSuperficie(matParedesEW, texturaAzulejoBlanco, Color.white, length, HEIGHT);
                break;

            case TipoHabitacion.Salon:
                ConfigurarSuperficie(matSuelo, texturaMadera, Color.white, width, length);
                ConfigurarSuperficie(matParedesNS, texturaLadrillo, Color.white, width, HEIGHT);
                ConfigurarSuperficie(matParedesEW, texturaLadrillo, Color.white, length, HEIGHT);
                break;
        }
    }

    void ConfigurarSuperficie(Material mat, Texture2D textura, Color color, float scaleX, float scaleY)
    {
        if (mat == null) return;

        mat.mainTexture = textura;
        mat.mainTextureScale = new Vector2(scaleX, scaleY);

        if (mostrarCuadrcula)
        {
            if (cuadrculaProcedural == null) cuadrculaProcedural = GenerarTexturaBordes();

            if (color == Color.white) mat.color = new Color(0.82f, 0.84f, 0.86f);
            else mat.color = color;

            mat.EnableKeyword("_EMISSION");
            mat.SetTexture("_EmissionMap", cuadrculaProcedural);
            mat.SetTextureScale("_EmissionMap", new Vector2(scaleX, scaleY));
            mat.SetColor("_EmissionColor", new Color(0f, 0.65f, 1f) * 4f);
        }
        else
        {
            mat.color = color;
            mat.SetTexture("_EmissionMap", null);
            mat.SetColor("_EmissionColor", Color.black);
            mat.DisableKeyword("_EMISSION");
        }
    }

    void LimpiarDecoracionPrevia()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform hijo = transform.GetChild(i);
            if (hijo.name == CONTENEDOR_NAME)
            {
                DestroyImmediate(hijo.gameObject);
            }
        }
    }

    void GenerarDecoracionParedes()
    {
        LimpiarDecoracionPrevia();

        // 1. DEFINICIÓN DE CONFIGURACIONES POR OBJETO INDIVIDUAL
        GameObject prefabParaNorte1 = null;
        bool pintarNorte1 = false;
        float profNorte1 = 0.3f;
        Vector3 rotExtraNorte1 = Vector3.zero;
        Color colorNorte1 = Color.white;

        GameObject prefabParaNorte2 = null;
        bool pintarNorte2 = false;
        float profNorte2 = 0.3f;
        float correcionNorte2Y = 3.3f;
        float correcionNorte2Z = 0.3f;
        Vector3 rotExtraNorte2 = Vector3.zero;
        Color colorNorte2 = Color.white;

        GameObject prefabParaSur = null;
        bool pintarSur = false;
        float profSur = 0.3f;
        Vector3 rotExtraSur = Vector3.zero;
        Color colorSur = Color.white;

        // 2. CONFIGURACIÓN EN DETALLE DENTRO DEL SWITCH
        switch (habitacionActual)
        {
            case TipoHabitacion.Dormitorio:
                // Estantes flotantes con libros
                prefabParaNorte1 = norte1Dormitorio;
                pintarNorte1 = true;
                profNorte1 = 0.15f;
                rotExtraNorte1 = Vector3.zero;
                colorNorte1 = new Color(0.45f, 0.28f, 0.15f);

                // Mesilla de dormir
                prefabParaNorte2 = norte2Dormitorio;
                correcionNorte2Y = -0.35f;
                correcionNorte2Z = 0f;
                pintarNorte2 = true;
                profNorte2 = 0.35f;
                rotExtraNorte2 = new Vector3(270, 0, 0);
                colorNorte2 = new Color(0.45f, 0.28f, 0.15f);

                // Lámparas de luz
                prefabParaSur = surDormitorio;
                pintarSur = true;
                profSur = -0.1f;
                rotExtraSur = new Vector3(270, 0, 0);
                colorSur = new Color(0.96f, 0.96f, 0.96f);
                break;

            case TipoHabitacion.Cocina:
                // Armarios colgantes
                prefabParaNorte1 = norte1Cocina;
                pintarNorte1 = true;
                profNorte1 = 0.35f;
                rotExtraNorte1 = Vector3.zero;
                colorNorte1 = new Color(0.96f, 0.97f, 1.00f);

                // Fogones
                prefabParaNorte2 = norte2Cocina;
                correcionNorte2Y = 0.34f;
                correcionNorte2Z = -0.4f;
                pintarNorte2 = true;
                profNorte2 = 0.35f;
                rotExtraNorte2 = Vector3.zero;
                colorNorte2 = new Color(0.96f, 0.97f, 1.00f);

                // Reloj de pared
                prefabParaSur = surCocina;
                pintarSur = false;
                profSur = -0.1f;
                rotExtraSur = new Vector3(90, 0, 0);
                colorSur = new Color(0.3f, 0.3f, 0.32f);
                break;

            case TipoHabitacion.Salon:
                // Estantes flotantes con libros
                prefabParaNorte1 = norte1Salon;
                pintarNorte1 = true;
                profNorte1 = 0.15f;
                rotExtraNorte1 = Vector3.zero;
                colorNorte1 = new Color(0.2f, 0.2f, 0.2f);

                // Mesa del televisor
                prefabParaNorte2 = norte2Salon;
                correcionNorte2Y = -0.35f;
                correcionNorte2Z = 0f;
                pintarNorte2 = true;
                profNorte2 = 0.85f;
                rotExtraNorte2 = new Vector3(270, 0, 0);
                colorNorte2 = new Color(0.2f, 0.2f, 0.2f);

                // Minicuadro
                prefabParaSur = surSalon;
                pintarSur = true;
                profSur = -0.1f;
                rotExtraSur = Vector3.zero;
                colorSur = new Color(0.7f, 0.4f, 0.2f);
                break;
        }

        GameObject contenedor = new GameObject(CONTENEDOR_NAME);
        contenedor.transform.SetParent(this.transform);
        contenedor.transform.localPosition = Vector3.zero;
        contenedor.transform.localRotation = Quaternion.identity;

        float alturaEstanteria = 1.7f;
        float alturaMesilla = 0.35f;
        float alturaLamparas = 2f;

        // --- PARED NORTE 1: 3 consecutivos, 2 huecos ---
        if (prefabParaNorte1 != null)
        {
            float offsetZ_Norte = (length / 2f) - THICKNESS - (profNorte1 / 2f);
            for (int x = 0; x < Mathf.FloorToInt(width); x++)
            {
                if (x % 5 < 3 && x < width - 0.5f)
                {
                    float posX = -width / 2f + x + 0.5f;
                    Vector3 pos = new Vector3(posX, alturaEstanteria, offsetZ_Norte);

                    Quaternion rotacionPared = Quaternion.Euler(0, 180, 0);
                    Quaternion rotacionFinal = rotacionPared * Quaternion.Euler(rotExtraNorte1);

                    InstanciarAsset(prefabParaNorte1, pos, rotacionFinal, contenedor.transform, colorNorte1, pintarNorte1);
                }
            }
        }

        // --- PARED NORTE 2: 1 cada 4 ---
        if (prefabParaNorte2 != null)
        {
            float offsetZ_Norte = (length / 2f) - THICKNESS - (profNorte2 / 2f) + correcionNorte2Z;
            for (int x = 0; x < Mathf.FloorToInt(width); x++)
            {
                if ((x - 1) % 5 == 0 && x < width - 0.5f)
                {
                    float posX = -width / 2f + x + 0.5f;
                    Vector3 pos = new Vector3(posX, alturaMesilla + correcionNorte2Y, offsetZ_Norte);

                    Quaternion rotacionPared = Quaternion.Euler(0, 180, 0);
                    Quaternion rotacionFinal = rotacionPared * Quaternion.Euler(rotExtraNorte2);

                    InstanciarAsset(prefabParaNorte2, pos, rotacionFinal, contenedor.transform, colorNorte2, pintarNorte2);
                }
            }
        }

        // --- PARED SUR: 1 cada 4 ---
        if (prefabParaSur != null)
        {
            float offsetZ_Sur = (-length / 2f) + THICKNESS + (profSur / 2f);
            for (int x = 0; x < Mathf.FloorToInt(width); x++)
            {
                if ((x - 1) % 4 == 0 && x < width - 0.5f)
                {
                    float posX = -width / 2f + x + 0.5f;
                    Vector3 pos = new Vector3(posX, alturaLamparas, offsetZ_Sur);

                    Quaternion rotacionPared = Quaternion.identity;
                    Quaternion rotacionFinal = rotacionPared * Quaternion.Euler(rotExtraSur);

                    InstanciarAsset(prefabParaSur, pos, rotacionFinal, contenedor.transform, colorSur, pintarSur);
                }
            }
        }


        // --- PARED ESTE ---


        // --- PARED OESTE ---


    }

    void InstanciarAsset(GameObject prefab, Vector3 posLocal, Quaternion rotLocal, Transform padre, Color colorMuebleEspecifico, bool aplicarColor)
    {
        if (prefab == null) return;

        GameObject obj = Instantiate(prefab);
        obj.transform.SetParent(padre);
        obj.transform.localPosition = posLocal;
        obj.transform.localRotation = rotLocal;

        if (aplicarColor)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

            foreach (Renderer r in renderers)
            {
                r.GetPropertyBlock(propBlock);
                // Ahora aplica directamente el color exclusivo de este tipo de mueble
                propBlock.SetColor("_Color", colorMuebleEspecifico);
                propBlock.SetColor("_BaseColor", colorMuebleEspecifico);
                r.SetPropertyBlock(propBlock);
            }
        }
    }

    private Texture2D GenerarTexturaBordes()
    {
        int resolucion = 128;
        Texture2D tex = new Texture2D(resolucion, resolucion);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;

        Color transparente = new Color(0f, 0f, 0f, 0f);
        Color bordeBlanco = Color.white;
        int grosorPíxeles = 1;

        for (int y = 0; y < resolucion; y++)
        {
            for (int x = 0; x < resolucion; x++)
            {
                if (x < grosorPíxeles || x >= resolucion - grosorPíxeles ||
                    y < grosorPíxeles || y >= resolucion - grosorPíxeles)
                {
                    tex.SetPixel(x, y, bordeBlanco);
                }
                else
                {
                    tex.SetPixel(x, y, transparente);
                }
            }
        }
        tex.Apply();
        return tex;
    }

    void Update()
    {
        if (Camera.main == null) return;

        Vector3 localCamPos = transform.InverseTransformPoint(Camera.main.transform.position);

        if (mrNorth != null) mrNorth.enabled = (localCamPos.z < (length / 2) - 0.1f);
        if (mrSouth != null) mrSouth.enabled = (localCamPos.z > -(length / 2) + 0.1f);
        if (mrEast != null) mrEast.enabled = (localCamPos.x < (width / 2) - 0.1f);
        if (mrWest != null) mrWest.enabled = (localCamPos.x > -(width / 2) + 0.1f);
    }
}