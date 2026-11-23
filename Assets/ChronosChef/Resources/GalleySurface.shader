Shader "ChronosChef/GalleySurface"
{
    Properties { _MainTex ("Base colour", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Back ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _ChefFloor;
            float _Burner;
            struct Input { float4 pos : POSITION; float3 normal : NORMAL; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct Varyings { float4 pos : SV_POSITION; half3 shade : COLOR; float2 uv : TEXCOORD0; float3 local : TEXCOORD1; };
            Varyings Vert(Input v)
            {
                Varyings o;
                o.pos = TransformObjectToHClip(v.pos.xyz);
                float3 n = normalize(TransformObjectToWorldNormal(v.normal));
                half light = saturate(dot(n, normalize(float3(-.55,.88,-.47))));
                half edge = saturate(dot(n,normalize(float3(.75,.35,.50))));
                o.shade = v.color.rgb * (half3(.52,.56,.65) + light * half3(.53,.48,.40) + edge * half3(.05,.10,.10));
                #ifndef UNITY_COLORSPACE_GAMMA
                    o.shade = SRGBToLinear(o.shade);
                #endif
                o.uv = v.uv; o.local = v.pos.xyz;
                return o;
            }
            half Contact(float2 p, float2 center, float2 extent)
            {
                float2 outside = max(abs(p-center)-extent,0);
                return 1-smoothstep(0,.65,length(outside));
            }
            half4 Frag(Varyings i) : SV_Target
            {
                half3 colour = i.shade * SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).rgb;
                if (_Burner > 0 && i.local.y < .07)
                {
                    float2 p = i.local.xz;
                    half contact = max(Contact(p,float2(-1.30,.02),float2(.96,.79)),
                                       Contact(p,float2(1.45,.03),float2(.94,.81)));
                    contact = max(contact,Contact(p,float2(.35,2.07),float2(5.65,.86)));
                    float2 shadow = (p-_ChefFloor.xy-float2(.05,.05))*float2(2.2,2.8);
                    colour *= 1-.35*contact;
                    colour *= .60+.40*smoothstep(.15,1,dot(shadow,shadow));
                    colour *= .92+.08*saturate(.5+.5*sin(p.x*9+p.y*4)*sin(p.y*3.1-p.x*1.3));
                }
                float reactor = 1-smoothstep(.12,1.6,length(i.local.xz-float2(1.45,.03)));
                colour += half3(.015,.16,.12)*reactor*_Burner;
                return half4(colour,1);
            }
            ENDHLSL
        }
    }
}
