using UnityEngine;

public class BellTrigger : MonoBehaviour
{
    public AudioSource playerAudio;
    [Range(0f, 0.2f)] public float pitchJitter = 0.03f;

    private Bell bell; // added reference

    private void Awake()
    {
        bell = GetComponent<Bell>(); // get Bell on same object
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bullet"))
        {
            // added cooldown check
            if (bell != null && bell.IsOnCooldown) return;

            Debug.Log("Bell Triggered by bullet!");
            playerAudio.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            playerAudio.Play();
        }
    }
}
