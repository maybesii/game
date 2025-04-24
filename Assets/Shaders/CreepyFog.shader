Shader "Hidden/CreepyFog"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _UITex ("UI Texture", 2D) = "black" {}
        _Intensity ("Fog Intensity", Range(0, 1)) = 0.75
        _FogColor ("Fog Color", Color) = (0.12, 0.18, 0.22, 1)
        _Density ("Fog Density", Range(0.03, 0.12)) = 0.07
        _SwirlScale ("Swirl Scale", Range(0.3, 1.2)) = 0.7
        _SwirlSpeed ("Swirl Speed", Vector) = (0.25, 0.25, 0, 0)
        _WeatherIntensity ("Weather Intensity", Range(0, 1)) = 0
    }
    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _UITex; 
            sampler2D _CameraDepthTexture;
            float _Intensity;
            float4 _FogColor;
            float _Density;
            float _SwirlScale;
            float2 _SwirlSpeed;
            float _WeatherIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            float SimpleNoise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.screenPos = ComputeScreenPos(o.pos);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float depth = LinearEyeDepth(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)).r);
                float depthFactor = saturate(depth * 0.05);
                float2 swirlUV = uv * _SwirlScale + _SwirlSpeed * _Time.y;
                float noise = SimpleNoise(swirlUV) * depthFactor;

                float distortion = noise * 0.015 * depthFactor;
                uv += float2(distortion, distortion);

                float weatherNoise = SimpleNoise(uv + _Time.y * 0.5) * _WeatherIntensity * 0.01 * depthFactor; // Уменьшено с 0.03 до 0.01
                uv += weatherNoise;

                float4 col = tex2D(_MainTex, uv);

                float luminance = dot(col.rgb, float3(0.299, 0.587, 0.114));
                float brightnessMod = saturate(1.0 - luminance * 0.5); 

                float fogFactor = 1.0 - exp(-_Density * depth * depth);
                fogFactor *= brightnessMod; 

                float4 fog = _FogColor;
                fog.a = _Intensity * (1.0 + noise * 0.3 + _WeatherIntensity * 0.05); // Уменьшено с 0.1 до 0.05

                float4 finalColor = lerp(col, fog, fog.a * fogFactor);

                float4 uiColor = tex2D(_UITex, i.uv);
                finalColor = lerp(finalColor, uiColor, uiColor.a);

                return finalColor;
            }
            ENDCG
        }
    }
}