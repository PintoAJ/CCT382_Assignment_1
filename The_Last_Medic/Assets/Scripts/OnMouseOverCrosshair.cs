using UnityEngine;
using UnityEngine.UI;
public class OnMouseOverCrosshair : MonoBehaviour
{
    public LayerMask targetLayers; // Assign your desired layers in the Inspector
    public RawImage crosshair;
    public Color colorCrosshair = Color.green;

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, targetLayers))
        {
            // The ray hit an object on one of the targetLayers
            // You can then check hit.collider.gameObject to see which object it was
            //Debug.Log("Mouse is over object: " + hit.collider.name + " on a target layer.");
            if (crosshair.color != colorCrosshair)
            {
                crosshair.color = colorCrosshair;
            }
            
        }
        else
        {
            crosshair.color = Color.white;
        }
    }
}