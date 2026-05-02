Shader "Greenroad/HexSelectionGlow"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.35, 0.85, 1, 0.68)
        _RimColor ("Rim Color", Color) = (0.75, 0.95, 1, 1)
        _ParticleColor ("Particle Color", Color) = (0.86, 0.98, 1, 1)
        _CoreAlpha ("Core Alpha", Range(0, 1)) = 0.34
        _RimAlpha ("Rim Alpha", Range(0, 1)) = 0.95
        _PulseSpeed ("Pulse Speed", Float) = 2.2
        _ParticleAlpha ("Particle Alpha", Range(0, 1)) = 0.55
        _ParticleScale ("Particle Scale", Float) = 9
        _ParticleSpeed ("Particle Speed", Float) = 0.65
        _ParticleDensity ("Particle Density", Range(0, 1)) = 0.38
        _RimWidth ("Rim Width", Range(0.01, 0.4)) = 0.18
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _BaseColor;
            fixed4 _RimColor;
            fixed4 _ParticleColor;
            float _CoreAlpha;
            float _RimAlpha;
            float _PulseSpeed;
            float _ParticleAlpha;
            float _ParticleScale;
            float _ParticleSpeed;
            float _ParticleDensity;
            float _RimWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
                return frac(value.x * value.y);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 centeredUv = (input.uv - 0.5) * 2.0;
                float2 absoluteUv = abs(centeredUv);
                float hexDistance = max((absoluteUv.x * 0.8660254) + (absoluteUv.y * 0.5), absoluteUv.y);
                float pulse = 0.5 + (0.5 * sin(_Time.y * _PulseSpeed));

                float core = 1.0 - smoothstep(0.08, 0.86, hexDistance);
                float rim = smoothstep(1.0 - _RimWidth, 0.98, hexDistance);
                float rimPulse = rim * lerp(0.72, 1.0, pulse);

                float2 particleUv = input.uv * max(0.01, _ParticleScale);
                particleUv.y += _Time.y * _ParticleSpeed;
                float2 particleCell = floor(particleUv);
                float particleSeed = Hash21(particleCell);
                float2 particleLocal = frac(particleUv) - 0.5;
                particleLocal += (particleSeed - 0.5) * 0.34;

                float particleMask = step(1.0 - _ParticleDensity, particleSeed);
                float particleShape = smoothstep(0.135, 0.0, length(particleLocal));
                float particleTwinkle = 0.45 + (0.55 * sin((_Time.y * 7.5) + (particleSeed * 6.28318)));
                float particles = particleShape * particleMask * particleTwinkle * (1.0 - smoothstep(0.82, 1.02, hexDistance));

                fixed3 color = (_BaseColor.rgb * (0.85 + (0.15 * pulse)))
                    + (_RimColor.rgb * rimPulse)
                    + (_ParticleColor.rgb * particles);
                float alpha = saturate((_BaseColor.a * _CoreAlpha) + (rimPulse * _RimAlpha) + (particles * _ParticleAlpha));

                return fixed4(color, alpha);
            }
            ENDCG
        }

        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _RimColor;
            fixed4 _ParticleColor;
            float _RimAlpha;
            float _PulseSpeed;
            float _ParticleAlpha;
            float _ParticleScale;
            float _ParticleSpeed;
            float _ParticleDensity;
            float _RimWidth;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
                return frac(value.x * value.y);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 centeredUv = (input.uv - 0.5) * 2.0;
                float2 absoluteUv = abs(centeredUv);
                float hexDistance = max((absoluteUv.x * 0.8660254) + (absoluteUv.y * 0.5), absoluteUv.y);
                float pulse = 0.5 + (0.5 * sin(_Time.y * _PulseSpeed));
                float rim = smoothstep(1.0 - _RimWidth, 0.98, hexDistance) * lerp(0.75, 1.0, pulse);

                float2 particleUv = input.uv * max(0.01, _ParticleScale);
                particleUv.y += _Time.y * _ParticleSpeed;
                float2 particleCell = floor(particleUv);
                float particleSeed = Hash21(particleCell);
                float2 particleLocal = frac(particleUv) - 0.5;
                particleLocal += (particleSeed - 0.5) * 0.32;
                float particleMask = step(1.0 - _ParticleDensity, particleSeed);
                float particleShape = smoothstep(0.18, 0.0, length(particleLocal));
                float particleTwinkle = 0.45 + (0.55 * sin((_Time.y * 8.5) + (particleSeed * 6.28318)));
                float particles = particleShape * particleMask * particleTwinkle * (1.0 - smoothstep(0.78, 1.02, hexDistance));

                fixed3 color = (_RimColor.rgb * rim) + (_ParticleColor.rgb * particles);
                float alpha = saturate((rim * _RimAlpha * 0.85) + (particles * _ParticleAlpha));
                return fixed4(color, alpha);
            }
            ENDCG
        }
    }
}
