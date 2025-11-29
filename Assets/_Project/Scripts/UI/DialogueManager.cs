using System;
using System.Collections.Generic;
using Game;
using UnityEngine;

namespace UI
{
    public class DialogueManager : MonoBehaviour
    {
        [Serializable]
        public class DialogueSequence
        {
            public  List<DialogueReplic> Replics;
        }

        [SerializeField] private List<DialogueSequence> _dialogueSequences = new();

        public void StartDialogueSequence(int index, Action onSequenceComplete = null)
        {
            if (index < 0 || index >= _dialogueSequences.Count)
            {
                Debug.LogWarning($"DialogueManager: Invalid dialogue sequence index {index}.");
                return;
            }

            DialogueSequence sequence = _dialogueSequences[index];
            DialogueCanvas dialogueCanvas = UIManager.Instance.DialogueCanvas;
            dialogueCanvas.Initialize(sequence.Replics, onSequenceComplete);

        }
    }
}
