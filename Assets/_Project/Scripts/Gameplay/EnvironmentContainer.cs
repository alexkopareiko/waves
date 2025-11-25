using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class EnvironmentContainer : MonoBehaviour
    {
        [SerializeField] private List<GameObject> environmentObjects;
        private Coroutine environmentCoroutine;

        public void ActivateEnvironment() 
        {
            StartEnvironmentRoutine(ChangeStateAsync(true));
        }

        public void DeactivateEnvironment()
        {
            StartEnvironmentRoutine(ChangeStateAsync(false));
        }

        private void StartEnvironmentRoutine(IEnumerator routine)
        {
            if (environmentCoroutine != null)
            {
                StopCoroutine(environmentCoroutine);
            }
            environmentCoroutine = StartCoroutine(routine);
        }

        private IEnumerator ChangeStateAsync(bool desiredState)
        {
            if (environmentObjects == null)
            {
                // yield break;
                // get firstrow child objects
                environmentObjects = new List<GameObject>();
                for (int i = 0; i < transform.childCount; i++)
                {
                    environmentObjects.Add(transform.GetChild(i).gameObject);
                }
            }

            foreach (var obj in environmentObjects)
            {
                if (obj == null)
                {
                    continue;
                }

                obj.SetActive(desiredState);
                yield return null;
            }

            environmentCoroutine = null;
        }
    }
}
