using System;
using System.Collections;
using System.Collections.Generic;
using Game;
using UnityEngine;

namespace UI
{
    public class DialogueCanvas : UISubCanvas
    {
        [SerializeField] private DialogueReplicContainer _leftReplicPrefab;
        [SerializeField] private DialogueReplicContainer _rightReplicPrefab;
        [SerializeField] private Transform _replicParent;
        [SerializeField, Min(0f)] private float _characterRevealInterval = 0.03f;

        private enum DialogueSide
        {
            Left,
            Right
        }

        private readonly List<DialogueReplic> _replics = new();
        private Action _onSequenceComplete;
        private Coroutine _sequenceRoutine;
        private Coroutine _typingRoutine;
        private DialogueReplicContainer _currentContainer;
        private int _currentIndex;
        private bool _waitingForInput;
        private bool _isTyping;
        private bool _shouldSkipTyping;
        private bool _hasLastSide;
        private DialogueSide _lastSide;
        private CharacterData _lastCharacter;

        public void Initialize(IReadOnlyList<DialogueReplic> replics, Action onSequenceComplete)
        {
            if (replics == null || replics.Count == 0)
            {
                onSequenceComplete?.Invoke();
                return;
            }

            ResetSequence();
            _replics.AddRange(replics);
            _onSequenceComplete = onSequenceComplete;
            _currentIndex = 0;
            _hasLastSide = false;

            Show();
            _sequenceRoutine = StartCoroutine(RunSequence());
        }

        private void Awake()
        {
            if (_replicParent == null)
                _replicParent = transform;
        }

        private void Update()
        {
            if (!IsInputTriggered())
                return;

            if (_isTyping)
            {
                _shouldSkipTyping = true;
                return;
            }

            if (_waitingForInput)
                _waitingForInput = false;
        }

        private bool IsInputTriggered()
        {
            if (Input.GetMouseButtonDown(0))
                return true;

            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                return true;

            return false;
        }

        private IEnumerator RunSequence()
        {
            while (_currentIndex < _replics.Count)
            {
                var replic = _replics[_currentIndex];
                var delay = replic.AppearDelay;

                if (replic.SoundBeforeAppear != null)
                    SoundManager.Instance?.PlaySoundEffect(replic.SoundBeforeAppear);

                if (delay > 0f)
                    yield return new WaitForSeconds(delay);

                if (!SpawnReplic(replic))
                {
                    _currentIndex++;
                    continue;
                }

                _typingRoutine = StartCoroutine(TypeReplicText(replic.Replic));
                if (_typingRoutine != null)
                    yield return _typingRoutine;
                _typingRoutine = null;

                _waitingForInput = true;
                while (_waitingForInput)
                    yield return null;

                DestroyCurrentContainer();
                _currentIndex++;
            }

            _sequenceRoutine = null;
            _onSequenceComplete?.Invoke();
            _onSequenceComplete = null;
            Hide();
        }

        private IEnumerator TypeReplicText(string text)
        {
            if (_currentContainer == null || _currentContainer.ReplicText == null)
                yield break;

            _isTyping = true;
            _shouldSkipTyping = false;

            var textField = _currentContainer.ReplicText;
            textField.text = string.Empty;

            var content = text ?? string.Empty;

            foreach (var character in content)
            {
                if (_shouldSkipTyping)
                    break;

                textField.text += character;
                if (_characterRevealInterval > 0f)
                    yield return new WaitForSeconds(_characterRevealInterval);
                else
                    yield return null;
            }

            if (_shouldSkipTyping)
                textField.text = text;

            _isTyping = false;
            _shouldSkipTyping = false;
        }

        private bool SpawnReplic(DialogueReplic replic)
        {
            if (replic.Character == null)
            {
                Debug.LogWarning("Dialogue replic has no character assigned.");
                return false;
            }

            var side = DetermineSide(replic.Character);
            var prefab = side == DialogueSide.Left ? _leftReplicPrefab : _rightReplicPrefab;

            if (prefab == null)
            {
                Debug.LogWarning("Dialogue prefab is not assigned.");
                return false;
            }

            DestroyCurrentContainer();

            _currentContainer = Instantiate(prefab, _replicParent);
            _currentContainer.SetupReplic(replic.Character.CharacterName, replic.Character.Avatar);
            _currentContainer.ReplicText.text = string.Empty;

            _lastCharacter = replic.Character;
            _lastSide = side;
            _hasLastSide = true;

            if (replic.Character?.VoiceClip != null)
                SoundManager.Instance?.PlaySoundEffect(replic.Character.VoiceClip);

            return true;
        }

        private DialogueSide DetermineSide(CharacterData character)
        {
            if (!_hasLastSide)
                return DialogueSide.Left;

            if (_lastCharacter == character)
                return _lastSide;

            return _lastSide == DialogueSide.Left ? DialogueSide.Right : DialogueSide.Left;
        }

        private void DestroyCurrentContainer()
        {
            if (_currentContainer != null)
            {
                Destroy(_currentContainer.gameObject);
                _currentContainer = null;
            }
        }

        private void ResetSequence()
        {
            if (_sequenceRoutine != null)
                StopCoroutine(_sequenceRoutine);

            if (_typingRoutine != null)
                StopCoroutine(_typingRoutine);

            _sequenceRoutine = null;
            _typingRoutine = null;
            _replics.Clear();
            _waitingForInput = false;
            _isTyping = false;
            _shouldSkipTyping = false;
            DestroyCurrentContainer();
        }
    }
}
