using UnityEngine;

public class BellTrigger : MonoBehaviour
{
    public AudioSource playerAudio;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            Debug.Log("Bell Triggered by bullet!");
            playerAudio.Play();
        }
    }
}
