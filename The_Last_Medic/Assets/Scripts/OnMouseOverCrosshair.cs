using UnityEngine;
using UnityEngine.UI;
public class OnMouseOverCrosshair : MonoBehaviour
{
    public LayerMask allyLayers; // Assign your desired layers in the Inspector
    public LayerMask interLayers;
    public RawImage crosshair;
    public Color colorAllyCrosshair = Color.green;
    public Color colorInterCrosshair = Color.red;

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, allyLayers))
        {
            // The ray hit an object on one of the targetLayers
            // You can then check hit.collider.gameObject to see which object it was
            //Debug.Log("Mouse is over object: " + hit.collider.name + " on a target layer.");
            if (crosshair.color != colorAllyCrosshair)
            {
                crosshair.color = colorAllyCrosshair;
            }
            
        }
        else if (Physics.Raycast(ray, out hit, Mathf.Infinity, interLayers))
        {
            // The ray hit an object on one of the targetLayers
            // You can then check hit.collider.gameObject to see which object it was
            //Debug.Log("Mouse is over object: " + hit.collider.name + " on a target layer.");
            if (crosshair.color != colorInterCrosshair)
            {
                crosshair.color = colorInterCrosshair;
            }

        }
        else
        {
            crosshair.color = Color.white;
        }
    }
}