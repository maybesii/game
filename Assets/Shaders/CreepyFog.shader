Shader "Hidden/CreepyFog"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
                float2 swirlUV = uv * _SwirlScale + _SwirlSpeed * _Time.y;
                float noise = SimpleNoise(swirlUV);
                uv += noise * 0.02;

                float weatherNoise = SimpleNoise(uv + _Time.y * 0.5) * _WeatherIntensity * 0.05;
                uv += weatherNoise;

                float4 col = tex2D(_MainTex, uv);
                float depth = LinearEyeDepth(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.screenPos)).r);
                float fogFactor = exp(-_Density * depth);

                float4 fog = _FogColor;
                fog.a = _Intensity * (1.0 + noise * 0.4 + _WeatherIntensity * 0.1); 
                return lerp(col, fog, fog.a * (1.0 - fogFactor));
            }
            ENDCG
        }
    }
}