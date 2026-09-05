using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterDialogueSO", menuName = "ScriptableObject/Dialogue/CharacterDialogueSO")]
public class CharacterDialogueSO : ScriptableObject
{
    public string CharacterID;
    public List<DialogueSequenceSO> Dialogues;

    [Header("이 캐릭터를 좌우 슬롯에 띄울 프리팹")]
    public DialogueCharacterActor ActorPrefab;


    /// <summary>
    /// 상황에 맞는 적합한 시퀸스를 가져옵니다.
    /// </summary>
    public DialogueSequenceSO GetSequence(string situationKey, int affinity)
    {
        for (int i = 0; i < Dialogues.Count; i++)
        {
            DialogueSequenceSO sequence = Dialogues[i];

            if (sequence.IsMatch(situationKey, affinity))
            {
                return sequence;
            }
        }

        return null;
    }


}
