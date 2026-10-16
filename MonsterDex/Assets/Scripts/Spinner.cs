using UnityEngine;

public class Spinner : MonoBehaviour
{
    public float degreesPerSecond = 90f;
    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f);
    }
}
