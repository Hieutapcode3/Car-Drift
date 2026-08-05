Shader "Tree Pack/Standard/Leaf/Leaf Optimized (Vertex Color) Cull OFF"
{
    Properties
    {
        _Cutoff          ("Alpha Cutoff",           Range(0,1))  = 0.5
        _Color           ("Color Tint",             Color)       = (1,1,1,1)
        _MainTex         ("Albedo (RGB) Alpha (A)", 2D)          = "white" {}
        _BumpMap         ("Normal Map",             2D)          = "bump"  {}
        _Smoothness      ("Smoothness",             Range(0,1))  = 0.1
        _Metallic        ("Metallic",               Range(0,1))  = 0.0

        [Header(Translucency)]
        _Translucency    ("Strength",               Range(0,10)) = 1.0
        _TransScattering ("Scattering Falloff",     Range(1,50)) = 2.0
        _TransDirect     ("Direct",                 Range(0,1))  = 1.0
        _TransAmbient    ("Ambient",                Range(0,1))  = 0.2
        _TransShadow     ("Shadow",                 Range(0,1))  = 0.9
        _TransNormalDistortion ("Normal Distortion",Range(0,1))  = 0.1

        [Header(Wind)]
        _LocalWindScale     ("Wind Scale",          Range(0.03,1)) = 0.354
        _LocalWindIntensity ("Wind Intensity",      Range(0,2))    = 0.5
        _LocalWindSpeed     ("Wind Speed",          Range(0,7))    = 1.0

        [HideInInspector] _texcoord ("", 2D)    = "white" {}
        [HideInInspector] __dirty   ("", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "AlphaTest"
            "RenderType"      = "TransparentCutout"
            "IgnoreProjector" = "True"
        }

        LOD 200
        Cull Off

        CGPROGRAM
        // Use Lambert + custom add for simplicity — avoids custom lighting struct issues
        #pragma surface surf Lambert alphatest:_Cutoff vertex:vert addshadow fullforwardshadows
        #pragma multi_compile_fog
        #pragma target 3.0

        #include "UnityCG.cginc"

        // Wind Zone global (set by WindZone component automatically)
        // x=main, y=turbulence, z=pulse magnitude, w=pulse frequency
        float4 _Wind;

        sampler2D _MainTex;
        sampler2D _BumpMap;

        fixed4  _Color;
        half    _Smoothness;
        half    _Metallic;
        half    _Translucency;
        half    _TransScattering;
        half    _TransDirect;
        half    _TransAmbient;
        half    _TransShadow;
        half    _TransNormalDistortion;
        half    _LocalWindScale;
        half    _LocalWindIntensity;
        half    _LocalWindSpeed;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
            fixed4 color : COLOR;
        };

        // Triangle wave — smoother than raw sin for leaves
        float TriWave(float x)
        {
            return abs(frac(x + 0.5) * 2.0 - 1.0) * 2.0 - 1.0;
        }

        void vert(inout appdata_full v)
        {
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

            // Use WindZone if present, otherwise use material properties
            float mainWind = max(_Wind.x, _LocalWindIntensity);
            float turbulence = max(_Wind.y, 0.5);
            float speed = _LocalWindSpeed;

            float time = _Time.y * speed;
            float phase = dot(worldPos.xz, float2(0.37, 0.53)) * _LocalWindScale;

            float swayX = TriWave(time * 0.31 + phase) * mainWind;
            float swayZ = TriWave(time * 0.23 + phase * 1.3) * mainWind;
            float flutterX = TriWave(time * 1.7 + phase * 0.8) * turbulence * mainWind * 0.3;
            float flutterZ = TriWave(time * 1.3 + phase * 1.1) * turbulence * mainWind * 0.3;

            // Weight by vertex colour alpha (leaf tips move more)
            float w = v.color.a;

            v.vertex.x += (swayX + flutterX) * w * 0.06;
            v.vertex.z += (swayZ + flutterZ) * w * 0.04;
            v.vertex.y += abs(swayX) * w * 0.02;
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 c   = tex * _Color * IN.color;

            o.Albedo  = c.rgb;
            o.Alpha   = c.a;
            o.Normal  = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
            // Translucency approximation via emission (backlit glow)
            o.Emission = c.rgb * _Translucency * 0.08;
        }
        ENDCG
    }

    FallBack "Nature/Tree Creator Leaves Fast"
    //CustomEditor "AmplifyShaderEditor.MaterialInspector"
}