// Unity 엔진 API Stub — DLL 컴파일용 (배포되지 않음)
// SDK가 참조하는 UnityEngine API만 최소한으로 정의

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace UnityEngine
{
    public class Object
    {
        public string name;
        public static void Destroy(Object obj) { }
        public static void DontDestroyOnLoad(Object target) { }
    }

    public class Component : Object
    {
        public GameObject gameObject => null;
    }

    public class Behaviour : Component
    {
        public bool enabled;
    }

    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator routine) => null;
        public Coroutine StartCoroutine(string methodName) => null;
        public void StopCoroutine(Coroutine routine) { }
        public void StopCoroutine(string methodName) { }
    }

    public class GameObject : Object
    {
    }

    public class Coroutine : YieldInstruction
    {
    }

    public class YieldInstruction
    {
    }

    public class WaitForSeconds : YieldInstruction
    {
        public WaitForSeconds(float seconds) { }
    }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
        public static void LogError(object message) { }
    }

    public static class Application
    {
        public static event Action<string> deepLinkActivated;
        public static string absoluteURL => "";
    }

    public static class PlayerPrefs
    {
        public static int GetInt(string key, int defaultValue = 0) => defaultValue;
        public static void SetInt(string key, int value) { }
        public static string GetString(string key, string defaultValue = "") => defaultValue;
        public static void SetString(string key, string value) { }
        public static void DeleteKey(string key) { }
        public static void Save() { }
    }

    public static class Screen
    {
        public static int width => 0;
        public static int height => 0;
    }

    public static class SystemInfo
    {
        public static string operatingSystem => "";
        public static OperatingSystemFamily operatingSystemFamily => OperatingSystemFamily.Other;
        public static string deviceModel => "";
    }

    public enum OperatingSystemFamily
    {
        Other = 0,
        MacOSX = 1,
        Windows = 2,
        Linux = 3
    }

    public static class Mathf
    {
        public static int Min(int a, int b) => a < b ? a : b;
        public static float Pow(float f, float p) => (float)Math.Pow(f, p);
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeField : Attribute { }

    [AttributeUsage(AttributeTargets.Field)]
    public class HeaderAttribute : Attribute
    {
        public HeaderAttribute(string header) { }
    }
}

namespace UnityEngine.Networking
{
    public class UnityWebRequest : IDisposable
    {
        public enum Result { InProgress, Success, ConnectionError, ProtocolError, DataProcessingError }

        public Result result;
        public long responseCode;
        public string error;
        public UploadHandler uploadHandler { get; set; }
        public DownloadHandler downloadHandler { get; set; }

        public UnityWebRequest() { }
        public UnityWebRequest(string url, string method) { }

        public static UnityWebRequest Get(string url) => new UnityWebRequest();

        public void SetRequestHeader(string name, string value) { }
        public UnityWebRequestAsyncOperation SendWebRequest() => null;
        public void Dispose() { }
    }

    public class UnityWebRequestAsyncOperation : YieldInstruction
    {
    }

    public class UploadHandler : IDisposable
    {
        public void Dispose() { }
    }

    public class UploadHandlerRaw : UploadHandler
    {
        public UploadHandlerRaw(byte[] data) { }
    }

    public class DownloadHandler : IDisposable
    {
        public string text => "";
        public void Dispose() { }
    }

    public class DownloadHandlerBuffer : DownloadHandler
    {
    }
}
