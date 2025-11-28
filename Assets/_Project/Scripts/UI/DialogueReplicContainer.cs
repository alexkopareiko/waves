using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    public class DialogueReplicContainer : MonoBehaviour
    {
        [SerializeField] private TMP_Text _replicText;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private Image _characterImage;

        public void SetupReplic(string replic, string characterName, Sprite characterSprite)
        {
            _replicText.text = replic;
            _nameText.text = characterName;
            _characterImage.sprite = characterSprite;
        }
    }
}
