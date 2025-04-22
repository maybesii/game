using UnityEngine;

public class WeatherZone : MonoBehaviour
{
    public enum WeatherType { None, Rain, Snow }
    [SerializeField] private WeatherType weatherType = WeatherType.None;
    [SerializeField] [Range(0f, 1f)] private float intensity = 1f; 

    public WeatherType GetWeatherType() => weatherType;
    public float GetIntensity() => intensity;
}