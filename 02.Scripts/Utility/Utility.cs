using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Comparers;



public static class Utility
{
    private static readonly Dictionary<float, WaitForSeconds> _timeInterval = new (new FloatComparer());

    /// <summary>
    /// WaitForSeconds 캐싱
    /// </summary>
    /// <returns></returns>
    public static WaitForSeconds WaitForSeconds(float seconds)
    {
        WaitForSeconds wfs;
        if (!_timeInterval.TryGetValue(seconds, out wfs))
            _timeInterval.Add(seconds, wfs = new WaitForSeconds(seconds));
        return wfs;
    }


    /// <summary>
    /// 현재 마우스의 위치를 z가 -10인 상태로 반환합니다.
    /// </summary>
    public static Vector3 FieldMousePos
    {
        get
        {

            Vector3 clampedMousePos = Input.mousePosition;
            clampedMousePos.x = Mathf.Clamp(clampedMousePos.x, 0, Screen.width);
            clampedMousePos.y = Mathf.Clamp(clampedMousePos.y, 0, Screen.height);

            Vector3 result = Camera.main.ScreenToWorldPoint(clampedMousePos);
            result.z = -10;
            return result;
        }
    }

    public static Vector3 UIMousePos
    {
        get
        {

            Vector3 clampedMousePos = Input.mousePosition;
            clampedMousePos.x = Mathf.Clamp(clampedMousePos.x, 0, Screen.width);
            clampedMousePos.y = Mathf.Clamp(clampedMousePos.y, 0, Screen.height);

            Vector3 result = CinemachineManager.Instance.UICamera.ScreenToWorldPoint(clampedMousePos);
            result.z = -10;
            return result;
        }
    }

    public static Vector3 FieldWorldToUIWorldPos(Vector3 fieldWorldPos)
    {
        Vector3 screenPos = CinemachineManager.Instance.FieldCamera.WorldToScreenPoint(fieldWorldPos);


        Vector3 result = CinemachineManager.Instance.UICamera.ScreenToWorldPoint(screenPos);
        result.z = -10;
        return result;
    }


    public static Vector3 GetUICenterWorldPos()
    {
        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Vector3 result = CinemachineManager.Instance.UICamera.ScreenToWorldPoint(screenCenter);
        result.z = -10;
        return result;
    }


    public static int GetChebyshevDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }

    public static int GetManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
