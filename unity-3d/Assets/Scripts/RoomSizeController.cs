using UnityEngine;

public class RoomSizeController : MonoBehaviour
{
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

    // Variables para optimizar y no buscar el MeshRenderer cada frame
    private MeshRenderer mrNorth, mrSouth, mrEast, mrWest;

    void Start()
    {
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

        // Ajustar el Tiling de la cuadrícula para que no se deforme
        AdjustGridTiling(floor, width, length);
        AdjustGridTiling(wallNorth, width, HEIGHT);
        AdjustGridTiling(wallSouth, width, HEIGHT);
        AdjustGridTiling(wallEast, length, HEIGHT);
        AdjustGridTiling(wallWest, length, HEIGHT);
    }

    void AdjustGridTiling(Transform obj, float scaleX, float scaleY)
    {
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            renderer.sharedMaterial.mainTextureScale = new Vector2(scaleX, scaleY);
        }
    }

    void Update()
    {
        if (Camera.main == null) return;

        Vector3 localCamPos = transform.InverseTransformPoint(Camera.main.transform.position);

        if (mrNorth != null)
            mrNorth.enabled = (localCamPos.z < (length / 2) - 0.1f);

        if (mrSouth != null)
            mrSouth.enabled = (localCamPos.z > -(length / 2) + 0.1f);

        if (mrEast != null)
            mrEast.enabled = (localCamPos.x < (width / 2) - 0.1f);

        if (mrWest != null)
            mrWest.enabled = (localCamPos.x > -(width / 2) + 0.1f);
    }
}