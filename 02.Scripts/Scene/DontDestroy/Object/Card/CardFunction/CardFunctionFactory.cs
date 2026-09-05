using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static class CardFunctionFactory
{
    /// <summary>
    /// 미리 로드된 카드 함수
    /// </summary>
    private static Dictionary<string, Func<ICardFunction>> loadedCardFunctions;
    private static bool _initialized;

    public static ICardFunction Create(string cardName)
    {
        //    ////테스트
        //    //여기부터
        //    "Makeshift" => new Makeshift(),
        //    "Unease" => new Unease(),
        //    "DarkBurst" => new DarkBurst(),
        //    "Slash" => new Slash(),
        //    "Sacrifice" => new Sacrifice(),
        //    "SoulArrow" => new SoulArrow(),
        //    "SoulBolt" => new SoulBolt(),
        //    "Depletion" => new Depletion(),
        //    "SoulConversion" => new SoulConversion(),
        //    "HealthPotion" => new HealthPotion(),
        //    "ManaPotion" => new ManaPotion(),
        //    "PackUp" => new PackUp(),
        //    "VolatileBlade" => new VolatileBlade(),
        //    "PressTheAdventage" => new PressTheAdventage(),
        //    "ChippedSword" => new ChippedSword(),



        Func<ICardFunction> function;
        if (!loadedCardFunctions.TryGetValue(cardName, out function))
        {
            return CardMissingError(cardName);
        }
        else
            return function.Invoke();
    }

    private static ICardFunction CardMissingError(string cardName)
    {
        Debug.LogError($"[CardFunctionFactory] 카드 기능 미등록: {cardName}");
        return null;
    }


    //-----
    //손 초기화 대신 자동화를 진행해봅니다.

    /// <summary>
    /// 카드 기능들을 리플렉션을 사용하여 등록해줍니다.
    /// </summary>
    public static void InitFunctions()
    {
        if (_initialized)
        {
            return;
        }

        loadedCardFunctions = new Dictionary<string, Func<ICardFunction>>(StringComparer.Ordinal);

        var targetInterface = typeof(ICardFunction);
        var baseType = typeof(CardFunctionBase);

        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(GetTypesSafe)
            .Where(t => t != null)
            .Where(t => t.IsClass)
            .Where(t => !t.IsAbstract)
            .Where(t => targetInterface.IsAssignableFrom(t))
            // 추천: CardFunctionBase 계열만 등록 (원치 않으면 이 줄 삭제)
            .Where(t => baseType.IsAssignableFrom(t))
            // CardFunctionBase 자체는 등록에서 제외
            .Where(t => t != baseType);

        foreach (var type in types)
        {
            // 기본 생성자 없는 타입은 제외
            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                Debug.LogError($"[CardFunctionFactory] 기본 생성자 없음: {type.FullName}");
                continue;
            }

            // 어트리뷰트 대신 클래스명을 키로 사용
            var id = type.Name;
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError($"[CardFunctionFactory] 빈 ID(클래스명): {type.FullName}");
                continue;
            }

            if (loadedCardFunctions.ContainsKey(id))
            {
                Debug.LogError($"[CardFunctionFactory] 중복 ID(클래스명): {id} ({type.FullName})");
                continue;
            }

            loadedCardFunctions.Add(id, () => (ICardFunction)Activator.CreateInstance(type));
        }

        _initialized = true;
    }

    private static IEnumerable<Type> GetTypesSafe(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t != null);
        }
    }
}
