using TMPro;
using UnityEngine;
using Febucci.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TypewriterByCharacter typewriter;
    [SerializeField] private GameObject endIndicator;

    [Header("Character Slots")]
    [SerializeField] private DialogueCharacterSlot leftCharacterSlot;
    [SerializeField] private DialogueCharacterSlot rightCharacterSlot;

    [Header("Input")]
    [SerializeField] private KeyCode advanceKey = KeyCode.Return;
    [SerializeField] private KeyCode advanceKeyAlt = KeyCode.KeypadEnter;
    [SerializeField] private bool allowMouseLeftClick = true;
    [SerializeField] private float inputLockSecondsAtStart = 0.08f;

    private DialogueSequenceSO currentSequence;
    private int currentAffinity;
    private int currentIndex = -1;
    private bool isDialogueRunning;
    private bool isCurrentLineCompleted;
    private float inputUnlockTime;

    public bool IsDialogueRunning
    {
        get { return isDialogueRunning; }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        dialogueRoot.SetActive(false);
        endIndicator.SetActive(false);
        speakerNameText.text = string.Empty;
        dialogueText.text = string.Empty;

        leftCharacterSlot.Hide();
        rightCharacterSlot.Hide();
    }

    private void Update()
    {
        if (!isDialogueRunning)
        {
            return;
        }

        if (Time.unscaledTime < inputUnlockTime)
        {
            return;
        }

        if (!IsAdvanceInputPressed())
        {
            return;
        }

        if (!isCurrentLineCompleted)
        {
            typewriter.SkipTypewriter();
            NotifyCurrentLineFullyShown();
            return;
        }

        MoveNextLine();
    }


    /// <summary>
    /// 대상의 CharactorDialogueSO를 넣은뒤, 맞는 key를 넣어주면 됩니다.
    /// </summary>
    public void ShowDialogue(CharacterDialogueSO dialogueOwner, string situationKey, int affinity)
    {
        DialogueSequenceSO sequence = dialogueOwner.GetSequence(situationKey, affinity);

        if (sequence == null)
        {
            Debug.LogWarning(
                "대화 시퀀스를 찾지 못했습니다. CharacterID: " + dialogueOwner.CharacterID +
                ", SituationKey: " + situationKey +
                ", Affinity: " + affinity);
            return;
        }

        BeginDialogue(sequence, affinity);
    }

    public void BeginDialogue(DialogueSequenceSO sequence, int affinity)
    {
        currentSequence = sequence;
        currentAffinity = Mathf.Clamp(affinity, 0, 3);
        currentIndex = -1;
        isDialogueRunning = true;
        isCurrentLineCompleted = false;
        inputUnlockTime = Time.unscaledTime + inputLockSecondsAtStart;

        dialogueRoot.SetActive(true);
        endIndicator.SetActive(false);

        MoveNextLine();
    }

    public void NotifyCurrentLineFullyShown()
    {
        if (!isDialogueRunning)
        {
            return;
        }

        if (isCurrentLineCompleted)
        {
            return;
        }

        isCurrentLineCompleted = true;
        endIndicator.SetActive(true);
    }

    public void EndDialogue()
    {
        currentSequence = null;
        currentIndex = -1;
        isDialogueRunning = false;
        isCurrentLineCompleted = false;

        dialogueRoot.SetActive(false);
        endIndicator.SetActive(false);
        speakerNameText.text = string.Empty;
        dialogueText.text = string.Empty;

        leftCharacterSlot.Hide();
        rightCharacterSlot.Hide();
    }

    private void MoveNextLine()
    {
        if (currentSequence == null)
        {
            EndDialogue();
            return;
        }

        if (currentSequence.DialogueLines == null)
        {
            EndDialogue();
            return;
        }

        int nextIndex = currentIndex + 1;

        while (nextIndex < currentSequence.DialogueLines.Count)
        {
            DialogueLine nextLine = currentSequence.DialogueLines[nextIndex];

            if (nextLine != null)
            {
                if (nextLine.CanPlay(currentAffinity))
                {
                    currentIndex = nextIndex;
                    ShowCurrentLine(nextLine);
                    return;
                }
            }

            nextIndex++;
        }

        EndDialogue();
    }

    private void ShowCurrentLine(DialogueLine line)
    {
        isCurrentLineCompleted = false;
        endIndicator.SetActive(false);

        ApplyCharacterSlots(line);
        string speakerName = GetSpeakerName(line);
        if(string.IsNullOrEmpty(speakerName))
            speakerNameText.text = string.Empty;
        else
            speakerNameText.text = Localization.Instance.GetLocalizedText(speakerName);

        string localizedText = Localization.Instance.GetLocalizedText($"{line.TextKey}_{currentIndex}");
        dialogueText.text = string.Empty;
        typewriter.ShowText(localizedText);
    }

    private void ApplyCharacterSlots(DialogueLine line)
    {
        if (line.LeftCharacter == null)
        {
            leftCharacterSlot.Hide();
        }
        else
        {
            leftCharacterSlot.Show(line.LeftCharacter, line.LeftEmotion);
        }

        if (line.RightCharacter == null)
        {
            rightCharacterSlot.Hide();
        }
        else
        {
            rightCharacterSlot.Show(line.RightCharacter, line.RightEmotion);
        }
    }

    private string GetSpeakerName(DialogueLine line)
    {
        if (line.SpeakerSlot == DialogueSpeakerSlot.Left)
        {
            if (line.LeftCharacter == null)
            {
                return string.Empty;
            }

            return line.LeftCharacter.CharacterID;
        }

        if (line.SpeakerSlot == DialogueSpeakerSlot.Right)
        {
            if (line.RightCharacter == null)
            {
                return string.Empty;
            }

            return line.RightCharacter.CharacterID;
        }

        return string.Empty;
    }

    private bool IsAdvanceInputPressed()
    {
        if (Input.GetKeyDown(advanceKey))
        {
            return true;
        }

        if (Input.GetKeyDown(advanceKeyAlt))
        {
            return true;
        }

        if (allowMouseLeftClick)
        {
            if (Input.GetMouseButtonDown(0))
            {
                return true;
            }
        }

        return false;
    }
}