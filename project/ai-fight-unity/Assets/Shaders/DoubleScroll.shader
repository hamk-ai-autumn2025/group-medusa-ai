Shader "Custom/2D Double Scroll Texture"
{
    Properties 
    {
        _MainTexA("Texture A", 2D) = "white" {}
        _MainTexB("Texture B", 2D) = "white" {}
        _ScrollDirectionA("Scroll Direction A", Vector) = (0, 1, 0, 0)
        _ScrollDirectionB("Scroll Direction B", Vector) = (0, -1, 0, 0)
        _ScrollSpeedA("Scroll Speed A", Range(0, 10)) = 1
        _ScrollSpeedB("Scroll Speed B", Range(0, 10)) = 1
        _TintA("Tint A", Color) = (1, 1, 1, 1)
        _TintB("Tint B", Color) = (1, 1, 1, 1)
        _TintModeA("Tint Mode A", Range(0, 2)) = 0
        _TintModeB("Tint Mode B", Range(0, 2)) = 0
        _TintGradientA("Tint Gradient A", 2D) = "white" {}
        _TintGradientB("Tint Gradient B", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Transparent" "PreviewType" = "Plane" }
        LOD 100

        CGPROGRAM
        #pragma surface surf Lambert alpha

        struct Input 
        {
            float2 uv_MainTexA;
            float2 uv_MainTexB;
        };

        sampler2D _MainTexA;
        sampler2D _MainTexB;
        float4 _ScrollDirectionA;
        float4 _ScrollDirectionB;
        float _ScrollSpeedA;
        float _ScrollSpeedB;
        float4 _TintA;
        float4 _TintB;
        float _TintModeA;
        float _TintModeB;
        sampler2D _TintGradientA;
        sampler2D _TintGradientB;

        fixed4 SampleTintGradient(sampler2D gradientSampler, float2 uv) 
        {
            return tex2D(gradientSampler, uv);
        }

        void surf(Input IN, inout SurfaceOutput o) 
        {
            // Combine the UV coordinates of the two textures
            float2 uvA = IN.uv_MainTexA;
            float2 uvB = IN.uv_MainTexB;

            // Scroll the UV coordinates based on time and scroll speed
            uvA += _Time.y * _ScrollSpeedA * _ScrollDirectionA.xy;
            uvB += _Time.y * _ScrollSpeedB * _ScrollDirectionB.xy;

            // Sample the two textures
            fixed4 colorA = tex2D(_MainTexA, uvA);
            fixed4 colorB = tex2D(_MainTexB, uvB);

            // Apply tint to texture A
            if (_TintModeA == 0) 
            {
                colorA *= _TintA;
            }
            else if (_TintModeA == 1) 
            {
                fixed4 gradientColor = SampleTintGradient(_TintGradientA, uvA);
                // Apply tint to texture A (continued)
                colorA *= gradientColor;
            }
            else if (_TintModeA == 2) 
            {
                fixed4 gradientColor = SampleTintGradient(_TintGradientA, uvA);
                colorA = lerp(colorA, gradientColor, gradientColor.a);
            }

            // Apply tint to texture B
            if (_TintModeB == 0) 
            {
                colorB *= _TintB;
            }
            else if (_TintModeB == 1) 
            {
                fixed4 gradientColor = SampleTintGradient(_TintGradientB, uvB);
                colorB *= gradientColor;
            }
            else if (_TintModeB == 2) 
            {
                fixed4 gradientColor = SampleTintGradient(_TintGradientB, uvB);
                colorB = lerp(colorB, gradientColor, gradientColor.a);
            }

            // Combine the colors of the two textures
            fixed4 finalColor = colorA + colorB;

            o.Albedo = finalColor.rgb;
            o.Alpha = finalColor.a;
        }

        ENDCG
    }
}