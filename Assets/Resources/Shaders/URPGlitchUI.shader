Shader "Hidden/ElementalHex/URPGlitchUI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _NoiseTex ("Digital Glitch Noise", 2D) = "black" {}

        // Analog Glitch Parameters (KinoGlitch / URPGlitch)
        // x = displacement amount, y = threshold
        _ScanLineJitter ("Scan Line Jitter (Amount, Threshold)", Vector) = (0, 1, 0, 0)
        // x = amount, y = time offset
        _VerticalJump ("Vertical Jump (Amount, Time)", Vector) = (0, 0, 0, 0)
        _HorizontalShake ("Horizontal Shake", Float) = 0
        // x = amount, y = time
        _ColorDrift ("Color Drift (Amount, Time)", Vector) = (0, 0, 0, 0)

        // Digital Glitch Parameter (0..1)
        _DigitalIntensity ("Digital Glitch Intensity", Range(0, 1)) = 0

        // 0 = Sprite/Image Distortion Mode, 1 = Fullscreen Digital/Scanline Overlay Mode
        _OverlayMode ("Overlay Mode", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "URPGlitchUI"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            float2 _ScanLineJitter;
            float2 _VerticalJump;
            float _HorizontalShake;
            float2 _ColorDrift;
            float _DigitalIntensity;
            float _OverlayMode;

            float nrand(float x, float y)
            {
                return frac(sin(dot(float2(x, y), float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float u = IN.texcoord.x;
                float v = IN.texcoord.y;

                // 1. Analog Scan Line Jitter
                float jitter = nrand(floor(v * 240.0) / 240.0, _Time.x) * 2.0 - 1.0;
                jitter *= step(_ScanLineJitter.y, abs(jitter)) * _ScanLineJitter.x;

                // 2. Analog Vertical Jump
                float jump = lerp(v, frac(v + _VerticalJump.y), _VerticalJump.x);

                // 3. Analog Horizontal Shake
                float shake = (nrand(_Time.x, 2.0) - 0.5) * _HorizontalShake;

                // 4. Analog Color Drift (Chromatic RGB Split)
                float drift = sin(jump * 6.28318 + _ColorDrift.y) * _ColorDrift.x;

                // 5. Digital Glitch Blocky Noise Lookup
                float4 trash = tex2D(_NoiseTex, float2(u, jump));
                float thresh = 1.001 - _DigitalIntensity * 1.001;
                float digitalMask = step(thresh, trash.w);

                // Apply Digital UV block displacement when triggered
                float2 uvDisplaced = frac(float2(u + jitter + shake, jump) + digitalMask * (trash.xy - 0.5) * 0.35);
                float2 uvDrifted   = frac(float2(u + jitter + shake + drift, jump) + digitalMask * (trash.yx - 0.5) * 0.35);

                if (_OverlayMode > 0.5)
                {
                    // Full-screen Digital & Analog Scanline Overlay Mode (visible over 3D battlefield and UI)
                    float scanline = sin(v * 800.0 + _Time.y * 40.0) * 0.5 + 0.5;
                    float tearLine = step(0.94 - _DigitalIntensity * 0.25, abs(nrand(floor(v * 90.0), _Time.x) * 2.0 - 1.0));

                    fixed3 blockCol = lerp(fixed3(0.05, 0.95, 0.85), fixed3(0.98, 0.12, 0.45), trash.z);
                    blockCol = lerp(blockCol, trash.rgb, 0.65);

                    float alpha = 0.0;
                    fixed3 finalRgb = fixed3(0, 0, 0);

                    // Digital corruption blocks
                    if (digitalMask > 0.5)
                    {
                        finalRgb = blockCol;
                        alpha = saturate(0.45 + _DigitalIntensity * 0.40);
                    }
                    // Horizontal analog tear strips
                    else if (tearLine > 0.5)
                    {
                        finalRgb = lerp(fixed3(1.0, 0.15, 0.3), fixed3(0.1, 0.9, 1.0), frac(v * 37.0 + _Time.x));
                        alpha = saturate(0.28 * (_DigitalIntensity + _ScanLineJitter.x));
                    }
                    // Subtle CRT scanline interference
                    else
                    {
                        finalRgb = fixed3(0.05, 0.12, 0.18);
                        alpha = scanline * 0.08 * saturate(_DigitalIntensity * 2.0);
                    }

                    fixed4 overCol = fixed4(finalRgb, alpha);
                    overCol.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                    return overCol;
                }

                // Image / Portrait / Background Distortion Mode (URPGlitch Analog + Digital)
                fixed4 src1 = (tex2D(_MainTex, uvDisplaced) + _TextureSampleAdd);
                fixed4 src2 = (tex2D(_MainTex, uvDrifted) + _TextureSampleAdd);

                // Combine Analog RGB Chromatic Drift
                fixed4 glitched = fixed4(src1.r, src2.g, src1.b, max(src1.a, src2.a));

                // Mix in Digital Glitch channel swap / corruption on triggered blocks
                fixed3 corruptedRgb = lerp(glitched.rgb, 1.0 - glitched.gbr, step(0.5, trash.z));
                corruptedRgb = lerp(corruptedRgb, trash.rgb, 0.35);
                glitched.rgb = lerp(glitched.rgb, corruptedRgb, digitalMask * saturate(_DigitalIntensity * 1.4));

                glitched *= IN.color;
                glitched.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                return glitched;
            }
            ENDCG
        }
    }
}
