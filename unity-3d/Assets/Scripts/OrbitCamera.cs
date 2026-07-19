using UnityEngine;
using System.Globalization;

// >>> Coloca este script en el GameObject de tu cámara ("CamaraPrincipal").
public class OrbitCamera : MonoBehaviour
{
    [Header("Objetivo a mirar (lo asigna GestorMuebles al activar cada mueble)")]
    public Transform objetivo;

    [Header("Distancia de la cámara al objetivo (metros)")]
    public float distanciaBase = 3f;
    public float distanciaMin = 0.8f;
    public float distanciaMax = 8f;

    // Valores que llegan desde los sliders de Angular (camara.x, camara.y, camara.z)
    private float anguloVertical = 45f;
    private float anguloHorizontal = 0f;
    private float zoomOffset = 0f;

    void LateUpdate()
    {
        if (objetivo == null) return;
        Bounds bounds = ObtenerBoundsDelObjetivo();
        Vector3 puntoBase = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        Vector3 puntoMira = bounds.center;

        float distancia = Mathf.Clamp(distanciaBase - (zoomOffset * 0.2f), distanciaMin, distanciaMax);
        Quaternion rotacion = Quaternion.Euler(anguloVertical, anguloHorizontal, 0f);

        Vector3 posicionDeseada = puntoBase + rotacion * new Vector3(0f, 0f, -distancia);

        transform.position = posicionDeseada;
        transform.LookAt(puntoMira);
    }

    private Bounds ObtenerBoundsDelObjetivo()
    {
        Renderer[] renderers = objetivo.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(objetivo.position, Vector3.one);

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            b.Encapsulate(renderers[i].bounds);
        }
        return b;
    }

    // Llamado por SendMessage('CamaraPrincipal', 'ActualizarCamara', 'x,y,z')
    public void ActualizarCamara(string valores)
    {
        var partes = valores.Split(',');
        if (partes.Length < 3) return;

        anguloVertical = float.Parse(partes[0], CultureInfo.InvariantCulture);
        anguloHorizontal = float.Parse(partes[1], CultureInfo.InvariantCulture);
        zoomOffset = float.Parse(partes[2], CultureInfo.InvariantCulture);
    }

    // Llamado por SendMessage('CamaraPrincipal', 'ActivarModoEdicion', 1)
    public void ActivarModoEdicion(int valor) { }

    // Lo llama GestorMuebles cuando activa un mueble
    public void EnfocarObjetivo(Transform nuevoObjetivo)
    {
        objetivo = nuevoObjetivo;
    }
}