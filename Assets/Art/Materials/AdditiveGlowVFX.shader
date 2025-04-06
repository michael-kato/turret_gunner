Shader "Custom/AdditiveGlowVFX"
{
    Properties
    {
        _GlowColor ("Glow Color", Color) = (1, 0.5, 0, 1)
        _GlowIntensity ("Glow Intensity", Range(0.1, 5.0)) = 1.0
        _CoreSize ("Core Size", Range(0.0, 0.5)) = 0.1
        _FalloffPower ("Falloff Power", Range(0.1, 5.0)) = 2.0
        _PulseSpeed ("Pulse Speed", Range(0.0, 5.0)) = 0.0
        _PulseAmount ("Pulse Amount", Range(0.0, 1.0)) = 0.0
    }
    
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        LOD 100
        
        // No culling or depth writing
        Cull Off
        ZWrite Off
        
        // Additive blending
        Blend SrcAlpha One
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            float4 _GlowColor;
            float _GlowIntensity;
            float _CoreSize;
            float _FalloffPower;
            float _PulseSpeed;
            float _PulseAmount;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate distance from center (0.5, 0.5)
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.uv, center);
                
                // Pulsing effect over time
                float pulseEffect = 1.0;
                if (_PulseSpeed > 0.0 && _PulseAmount > 0.0) {
                    pulseEffect = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                }
                
                // Calculate glow based on distance
                float glow = 0.0;
                
                // Core area with full brightness
                if (dist < _CoreSize * pulseEffect) {
                    glow = 1.0;
                }
                // Falloff area
                else {
                    float falloffDist = (dist - (_CoreSize * pulseEffect));
                    float falloffFactor = 1.0 - saturate(falloffDist * (1.0 / (1.0 - _CoreSize)));
                    glow = pow(falloffFactor, _FalloffPower);
                }
                
                // Apply intensity
                glow *= _GlowIntensity;
                
                // Final color with alpha based on glow
                fixed4 col = _GlowColor * glow;
                col.a = saturate(glow);
                
                return col;
            }
            ENDCG
        }
    }
    
    // Fallback for older graphics
    Fallback "Unlit/Transparent"
}