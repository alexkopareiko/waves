using UnityEngine;

namespace Game 
{
    public class Lighthouse : MonoBehaviour
    {
        [SerializeField] private GameObject lighthouseLight;
        [SerializeField] private float rotationSpeed = 10f;
        private void Update()
        {
            RotateLighthouse();
        }
        private void RotateLighthouse()
        {
            lighthouseLight.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
    
}
