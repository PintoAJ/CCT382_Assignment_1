using UnityEngine;

public class BombTrigger : MonoBehaviour
{
    public AudioSource explosionSound;
    public GameObject explosionEffect; // optional particle prefab

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            Debug.Log("Bomb triggered by bullet!");

            // Play the explosion sound
            if (explosionSound != null)
                explosionSound.Play();

            // Optional explosion VFX
            if (explosionEffect != null)
                Instantiate(explosionEffect, transform.position, Quaternion.identity);

            // Destroy the bomb after the sound finishes
            Destroy(gameObject, explosionSound.clip.length);
        }
    }
}
