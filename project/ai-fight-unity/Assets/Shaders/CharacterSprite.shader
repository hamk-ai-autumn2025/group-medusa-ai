Shader "Custom/Character Sprite"
{
    Properties
    {
        // Standard sprite stuff
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        _Alpha ("Alpha", Range(0,1)) = 1.0

        // Hit / flash effect
        _HitEffectColor ("Hit Effect Color", Color) = (1,1,1,1)
        _HitEffectBlend ("Hit Effect Blend", Range(0,1)) = 0

        // Shake effect
        _ShakeAmount ("Shake Amount", Range(0,1)) = 0
        _ShakeX ("Shake X Strength", Float) = 0.05
        _ShakeY ("Shake Y Strength", Float) = 0.05
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            fixed4 _Color;
            fixed4 _HitEffectColor;
            float _HitEffectBlend;

            float _ShakeAmount;
            float _ShakeX;
            float _ShakeY;
            float _Alpha;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            v2f vert (appdata IN)
            {
                v2f o;

                // Base vertex position in object space
                float4 pos = IN.vertex;

                // --- SHAKE ---
                // Simple time-based shake using sin/cos,
                // applies to the whole sprite uniformly.
                if (_ShakeAmount > 0.0)
                {
                    // _Time.y = time in seconds
                    float t = _Time.y * 60.0; // shake speed (hardcoded)

                    float2 shakeOffset;
                    shakeOffset.x = sin(t) * _ShakeX * _ShakeAmount;
                    shakeOffset.y = cos(t * 1.37) * _ShakeY * _ShakeAmount;

                    pos.xy += shakeOffset;
                }

                #ifdef PIXELSNAP_ON
                    o.vertex = UnityPixelSnap(UnityObjectToClipPos(pos));
                #else
                    o.vertex = UnityObjectToClipPos(pos);
                #endif

                o.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                o.color = IN.color * _Color;   // SpriteRenderer color * material tint
                return o;
            }

            float4 frag (v2f IN) : SV_Target
            {
                float4 c = tex2D(_MainTex, IN.uv) * IN.color;

                c.a *= _Alpha; // Apply global alpha

                // No hit effect behave like Sprites/Default
                if (_HitEffectBlend <= 0.0001)
                {
                    // PREMULTIPLY RGB BY ALPHA (this is what the default sprite shader does)
                    c.rgb *= c.a;
                    return c;
                }

                // Mask hit effect by alpha so almost-transparent pixels don't glow as much
                float hitMask = saturate((c.a - 0.01) * 100.0);
                float amt = _HitEffectBlend * hitMask;

                // Lerp RGB towards hit color, keep original alpha
                c.rgb = lerp(c.rgb, _HitEffectColor.rgb, amt);

                // PREMULTIPLY at the end so edges don't halo
                c.rgb *= c.a;

                return c;
            }
            ENDCG
        }
    }

    FallBack "Sprites/Default"
}