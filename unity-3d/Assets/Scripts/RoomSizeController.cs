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
    public Texture2D texturaCuadrcula;

    // Variables para optimizar y no buscar el MeshRenderer cada frame
    private MeshRenderer mrFloor, mrNorth, mrSouth, mrEast, mrWest;

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

    public void UpdateRoomSize()
    {
        if (floor == null || wallNorth == null || wallSouth == null || wallEast == null || wallWest == null) return;
        if (mrFloor == null) ObtenerRenderers();

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
                ConfigurarSuperficie(mrFloor, null, Color.gray, width, length);
                ConfigurarSuperficie(mrNorth, null, Color.white, width, HEIGHT);
                ConfigurarSuperficie(mrSouth, null, Color.white, width, HEIGHT);
                ConfigurarSuperficie(mrEast, null, Color.white, length, HEIGHT);
                ConfigurarSuperficie(mrWest, null, Color.white, length, HEIGHT);
                break;

            case TipoHabitacion.Dormitorio:
                ConfigurarSuperficie(mrFloor, texturaMadera, Color.white, width, length);
                ConfigurarSuperficie(mrNorth, null, colorTortilla, width, HEIGHT);
                ConfigurarSuperficie(mrSouth, null, colorTortilla, width, HEIGHT);
                ConfigurarSuperficie(mrEast, null, colorTortilla, length, HEIGHT);
                ConfigurarSuperficie(mrWest, null, colorTortilla, length, HEIGHT);
                break;

            case TipoHabitacion.Cocina:
                ConfigurarSuperficie(mrFloor, null, Color.white, width, length);
                ConfigurarSuperficie(mrNorth, null, Color.white, width, HEIGHT);
                ConfigurarSuperficie(mrSouth, null, Color.white, width, HEIGHT);
                ConfigurarSuperficie(mrEast, null, Color.white, length, HEIGHT);
                ConfigurarSuperficie(mrWest, null, Color.white, length, HEIGHT);
                break;

            case TipoHabitacion.Salon:
                ConfigurarSuperficie(mrFloor, null, Color.white, width, length);
                ConfigurarSuperficie(mrNorth, null, Color.white, width, HEIGHT);
                ConfigurarSuperficie(mrSouth, null, Color.white, width, HEIGHT);
                ConfigurarSuperficie(mrEast, null, Color.white, length, HEIGHT);
                ConfigurarSuperficie(mrWest, null, Color.white, length, HEIGHT);
                break;
        }
    }
    void ConfigurarSuperficie(MeshRenderer renderer, Texture2D textura, Color color, float scaleX, float scaleY)
    {
        if (renderer == null || renderer.sharedMaterial == null) return;

        Material mat = renderer.sharedMaterial;

        // Asignamos la textura base y el color de tinte
        mat.mainTexture = textura;
        mat.color = color;
        mat.mainTextureScale = new Vector2(scaleX, scaleY);

        if (mostrarCuadrcula && texturaCuadrcula != null)
        {
            mat.SetTexture("_DetailMap", texturaCuadrcula);
            mat.SetVector("_DetailMap_ST", new Vector4(scaleX, scaleY, 0, 0));
            mat.SetFloat("_DetailAlbedoMapScale", 1f);
        }
        else
        {
            mat.SetTexture("_DetailMap", null);
            mat.SetFloat("_DetailAlbedoMapScale", 0f);
        }
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