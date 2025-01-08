using UnityEngine;

public class Detector : MonoBehaviour
{
    private EnemyBasic character;
    private void Start()
    {
        character = transform.parent.GetComponent<EnemyBasic>();
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            character.Detected(other.transform);
            character.alerted = true;
            Debug.Log("Player Detected");   
        }

    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            character.alerted = false;

        }
    }
}
