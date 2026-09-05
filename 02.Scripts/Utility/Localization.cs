using System.Collections;
using System.Collections.Generic;
using UnityEngine;



// >> /n << 을 개행으로 하기로 약속함
public class Localization : MonoBehaviour
{
    public static Localization Instance;

    public enum LanguageType
    {
        English,
        Korean,
        Japanese,
    }

    public LanguageType CurrentLanguage { get; private set; }

    private Dictionary<string, Dictionary<string, string>> localizationText;




    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        LoadLocalizationData("Localization");
        //일단 한국어로
        CurrentLanguage = LanguageType.Korean;
    }

    public string GetLocalizedText(string key)
    {
        string lang = CurrentLanguage.ToString();

        if (localizationText.ContainsKey(key) && localizationText[key].ContainsKey(lang))
        {
            return localizationText[key][lang];
        }

        return $"{key} << 이 값이 없음";
    }

    public string GetLocalizedCardName(string cardName)
    {
        char last = cardName[cardName.Length - 1];

        // 끝이 대문자 A/B면 떼고 다시 붙이기
        if (last == 'A' || last == 'B')
        {
            string key = cardName.Substring(0, cardName.Length - 1);
            string localized = GetLocalizedText(key);
            return localized + " " + last;
        }

        // 그 외는 그대로
        return GetLocalizedText(cardName);
    }

    public string GetLocalizedCardInfo(CardEntitySO so)
    {
        string cardName = so.CardName;
        string key;
        //따로 번역 예외 키를 지정해두지 않았으면, 이름으로 검색해줍니다.
        //여러개의 변수가 존재하거나 예외를 둘 상황이 필요할 수 있기 때문입니다.
        if (string.IsNullOrWhiteSpace(so.CardInfoKey))
        {
            char last = cardName[cardName.Length - 1];

            // 끝이 대문자 A/B면 떼고 다시 붙이기
            if (last == 'A' || last == 'B')
            {
                key = cardName.Substring(0, cardName.Length - 1) + "Info";
            }
            else
                key = $"{cardName}Info";
        }
        else
        {
            //번역 예외 키가 있으면 해당 키로 검색해줍니다.
            key = $"{so.CardInfoKey}Info";
        }

        return GetLocalizedText(key);
    }

    private void LoadLocalizationData(string fileName)
    {
        localizationText = new Dictionary<string, Dictionary<string, string>>();
        TextAsset csvFile = Resources.Load<TextAsset>(fileName);

        if (csvFile == null)
        {
            Debug.LogError("CSV 파일이 없어요");
            return;
        }

        string[] lines = csvFile.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            Debug.LogError("CSV 파일을 확인하세요");
            return;
        }

        string[] headers = lines[0].Split(',');
        if (headers.Length < 2 || headers[0].Trim().ToLower() != "key")
        {
            Debug.LogError("CSV 파일의 첫 번째 열이 'key' 여야 합니다");
            return;
        }

        for (int i = 1; i < lines.Length; i++) //0번은 "Key"
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] fields = lines[i].Split(',');
            if (fields.Length < 2) continue;

            string key = fields[0].Trim();
            if (string.IsNullOrWhiteSpace(key)) continue;

            if (!localizationText.ContainsKey(key))
            {
                localizationText[key] = new Dictionary<string, string>();
            }

            for (int j = 1; j < headers.Length; j++) //0번은 "Key"
            {
                if (j < fields.Length)
                {
                    string header = headers[j].Trim();
                    string value = fields[j].Trim();
                    // >> /n << 을 개행으로 하기로 약속함
                    if (value.Contains("/n"))
                        value = value.Replace("/n", "\n");
                    // >> /. << 을 ,로 하기로 약속함
                    if (value.Contains("/."))
                        value = value.Replace("/.", ",");

                    if (!string.IsNullOrWhiteSpace(header) && !string.IsNullOrWhiteSpace(value))
                    {
                        localizationText[key][header] = value;
                    }
                }
            }
        }
    }

}
