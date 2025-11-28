using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueReplic", menuName = "Dialogue/Replic")]
public class DialogueReplic : ScriptableObject
{
    [SerializeField] private CharacterData character;
    [TextArea(2, 4)] [SerializeField] private string replic;
    [SerializeField, Min(0f)] private float appearDelay;
    [SerializeField] private AudioClip soundBeforeAppear;

    public CharacterData Character => character;
    public string Replic => replic;
    public float AppearDelay => appearDelay;
    public AudioClip SoundBeforeAppear => soundBeforeAppear;
}
