using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[CreateAssetMenu(fileName = "Sequence01", menuName = "ScriptableObject/Dialogue/DialogueSequenceSO")]
public class DialogueSequenceSO : ScriptableObject
{
    /// <summary>
    /// 해당 시퀸스를 출력할 상황
    /// </summary>
    public string SituationKey;
    /// <summary>
    /// 순서대로 대화내용
    /// </summary>
    public List<DialogueLine> DialogueLines;

    [Header("호감도 요구치: 같은 상황이더라도 호감도에 따라 대사를 다르게 배정하기 위해 설정합니다.")]
    public int MinAffinity = 0;
    public int MaxAffinity = 3;

    /// <summary>
    /// 시퀸스 내용 메모용
    /// </summary>
    [TextArea(2, 5)]
    public string Memo;

    /// <summary>
    /// 이 시퀸스가 찾고 있는 대상 시퀸스인지 검사합니다.
    /// </summary>
    public bool IsMatch(string situationKey, int affinity)
    {
        //대소문자 무시
        if (!string.Equals(SituationKey, situationKey, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (affinity < MinAffinity)
        {
            return false;
        }

        if (affinity > MaxAffinity)
        {
            return false;
        }

        return true;
    }
}

[Serializable]
public class DialogueLine
{
    [Header("현재 말하고 있는 캐릭터")]
    public DialogueSpeakerSlot SpeakerSlot = DialogueSpeakerSlot.Left;
    [Header("번역될 문장 키")]
    public string TextKey;

    public CharacterDialogueSO LeftCharacter;
    public DialogueEmotion LeftEmotion = DialogueEmotion.Default;

    //없을 경우 표시하지 않음
    public CharacterDialogueSO RightCharacter;
    public DialogueEmotion RightEmotion = DialogueEmotion.Default;


    /// <summary>
    /// 출력 시점에 캐릭터의 호감도를 확인 후 해당 요구치보다 낮으면 이 문장을 건너뜁니다.
    /// </summary>
    [Header("출력시 필요 플레이어 호감도")]
    public int RequireAffinity = 0;


    public bool CanPlay(int affinity)
    {
        if (affinity < RequireAffinity)
        {
            return false;
        }

        return true;
    }
}


[Serializable]
public class CharacterEmotionSpritePair
{
    public DialogueEmotion Emotion = DialogueEmotion.Default;
    public Sprite Sprite;
}

public enum DialogueEmotion
{
    Default,
    Surprised,
}

public enum DialogueSpeakerSlot
{
    Left,
    Right,
    None
}