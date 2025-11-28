using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacter", menuName = "Characters/Character Data")]
public class CharacterData : ScriptableObject
{
    [SerializeField] private string characterName;
    [SerializeField] private Sprite avatar;
    [SerializeField] private AudioClip voiceClip;

    public string CharacterName => characterName;
    public Sprite Avatar => avatar;
    public AudioClip VoiceClip => voiceClip;
}
