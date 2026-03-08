using UnityEngine;

public class WheelSpin : MonoBehaviour
{
    public float Speed = 200f; 
    public float ac = 100f;
    private float x = 0f;
    private bool spinning = false;

    public AudioSource spinningSound; 

    void Update()
    {
        if (spinning && x < Speed)
        {
            x += ac * Time.deltaTime;
        }
        else if (!spinning && x > 0)
        {
            x -= ac * Time.deltaTime;
        }

        x = Mathf.Clamp(x, 0, Speed);
        transform.Rotate(Vector3.forward, x * Time.deltaTime);

        
        if (!spinning && x <= 0.1f && spinningSound.isPlaying)
        {
            spinningSound.Stop();
        }
    }

    public void StartSpinning()
    {
        spinning = true;
        if (spinningSound != null && !spinningSound.isPlaying)
        {
            spinningSound.Play();
        }
    }

    public void StopSpinning()
    {
        spinning = false;
        
    }
}