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

    private MeshRenderer mrFloor, mrNorth, mrSouth, mrEast, mrWest;
    private Texture2D cuadrculaProcedural;

    private Material matSuelo;
    private Material matParedesNS; // Norte y Sur
    private Material matParedesEW; // Este y Oeste

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
        UpdateRoomSize();
    }

    // Inicializa o recupera los materiales independientes para cada grupo
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

    public void UpdateRoomSize()
    {
        if (floor == null || wallNorth == null || wallSouth == null || wallEast == null || wallWest == null) return;
        InicializarMateriales();

        // 1. Escalar y posicionar el SUELO
        floor.localScale = new Vector3(width, THICKNESS, length);
        floor.localPosition = new Vector3(0, -THICKNESS / 2, 0);

        // 2. Escalar y posicionar Pared NORTE (Fondo Z+)
        wallNorth.localScale = new Vector3(width, HEIGHT, THICKNESS);
        wallNorth.localPosition = new Vector3(0, HEIGHT / 2, length / 2);

        // 3. Escalar y posicionar Pared SUR (Frente Z-)
        wallSouth.localScale = new Vector3(width, HEIGHT, THICKNESS);
        wallSouth.localPosition = new Vector3(0, HEIGHT / 2, -length / 2);

        // 4. Escalar y posicionar Pared ESTE (Derecha X+)
        wallEast.localScale = new Vector3(THICKNESS, HEIGHT, length);
        wallEast.localPosition = new Vector3(width / 2, HEIGHT / 2, 0);

        // 5. Escalar y posicionar Pared OESTE (Izquierda X-)
        wallWest.localScale = new Vector3(THICKNESS, HEIGHT, length);
        wallWest.localPosition = new Vector3(-width / 2, HEIGHT / 2, 0);

        AplicarEstiloHabitacion();
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

    // Ahora configuramos directamente el Material independiente de cada grupo
    void ConfigurarSuperficie(Material mat, Texture2D textura, Color color, float scaleX, float scaleY)
    {
        if (mat == null) return;

        mat.mainTexture = textura;
        mat.mainTextureScale = new Vector2(scaleX, scaleY);

        if (mostrarCuadrcula)
        {
            if (cuadrculaProcedural == null)
            {
                cuadrculaProcedural = GenerarTexturaBordes();
            }

            if (color == Color.white)
            {
                mat.color = new Color(0.82f, 0.84f, 0.86f);
            }
            else
            {
                mat.color = color;
            }

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