using UnityEngine;

public class LaserAudio : MonoBehaviour
{
    private void Start()
    {
        AudioManager.Instance.Play("Laser_SFX");
    }
}