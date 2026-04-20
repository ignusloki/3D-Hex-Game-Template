Shader "Hex/Stylized Water"
{
    Properties
    {
        _Color ("Base Color", Color) = (0.16, 0.63, 0.76, 1)
        _WaveColor ("Wave Color", Color) = (0.30, 0.83, 0.91, 1)
        _FoamColor ("Foam Color", Color) = (0.72, 0.96, 1.0, 1)
        _RimColor ("Rim Color", Color) = (0.52, 0.92, 1.0, 1)
        _Glossiness ("Smoothness", Range(0, 1)) = 0.06
        _Metallic ("Metallic", Range(0, 1)) = 0
        _EmissionStrength ("Emission Strength", Range(0, 1)) = 0.12
        _WaveScaleA ("Wave Scale A", Range(0.2, 8)) = 2.2
        _WaveScaleB ("Wave Scale B", Range(0.2, 12)) = 4.8
        _WaveSpeedA ("Wave Speed A", Vector) = (0.08, 0.04, 0, 0)
        _WaveSpeedB ("Wave Speed B", Vector) = (-0.05, 0.09, 0, 0)
        _BandStrength ("Band Strength", Range(0, 1)) = 0.45
        _BandContrast ("Band Contrast", Range(0.2, 4)) = 1.8
        _FoamStrength ("Foam Strength", Range(0, 1)) = 0.16
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.28
        _RimPower ("Rim Power", Range(0.5, 8)) = 3.2
        _SideDarken ("Side Darken", Range(0, 1)) = 0.32
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0

        fixed4 _Color;
        fixed4 _WaveColor;
        fixed4 _FoamColor;
        fixed4 _RimColor;
        half _Glossiness;
        half _Metallic;
        half _EmissionStrength;
        half _WaveScaleA;
        half _WaveScaleB;
        float4 _WaveSpeedA;
        float4 _WaveSpeedB;
        half _BandStrength;
        half _BandContrast;
        half _FoamStrength;
        half _RimStrength;
        half _RimPower;
        half _SideDarken;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            float3 viewDir;
        };

        inline float SampleWave(float2 worldXZ, float2 speed, float scale, float timeOffset)
        {
            float2 uv = (worldXZ * scale) + (_Time.y * speed);
            float wave = sin(uv.x + (uv.y * 1.37) + timeOffset);
            wave += cos((uv.y * 1.81) - (uv.x * 0.77) + (timeOffset * 1.23));
            return wave * 0.5;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 normalWS = normalize(IN.worldNormal);
            float topMask = saturate(pow(abs(normalWS.y), 6.0));
            float sideMask = 1.0 - topMask;

            float2 worldXZ = IN.worldPos.xz * 0.12;
            float waveA = SampleWave(worldXZ, _WaveSpeedA.xy, _WaveScaleA, 0.0);
            float waveB = SampleWave(worldXZ, _WaveSpeedB.xy, _WaveScaleB, 1.7);
            float combined = saturate(((waveA + waveB) * 0.5) + 0.5);
            float band = pow(combined, _BandContrast);
            float foam = smoothstep(0.78, 0.97, combined) * _FoamStrength * topMask;

            float3 baseColor = _Color.rgb;
            float3 waveColor = lerp(baseColor, _WaveColor.rgb, band * _BandStrength);
            float3 sideColor = baseColor * (1.0 - _SideDarken);
            float3 surfaceColor = lerp(sideColor, waveColor, topMask);
            surfaceColor = lerp(surfaceColor, _FoamColor.rgb, foam);

            float rim = pow(1.0 - saturate(dot(normalize(IN.viewDir), normalWS)), _RimPower);
            rim *= _RimStrength * topMask;

            o.Albedo = surfaceColor;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Occlusion = 1.0;
            o.Emission = ((_WaveColor.rgb * band) + (_RimColor.rgb * rim)) * _EmissionStrength;
            o.Alpha = 1.0;
        }
        ENDCG
    }

    FallBack "Standard"
}
