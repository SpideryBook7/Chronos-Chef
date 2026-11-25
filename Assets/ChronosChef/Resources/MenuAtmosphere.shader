Shader "ChronosChef/MenuAtmosphere"
{
    Properties {
        [PerRendererData] _MainTex ("Illustration", 2D) = "white" {}
        _Worlds ("World selection", Float) = 0
        _Phase ("Animation phase", Float) = 0
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Pass {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Worlds, _Phase;
            fixed4 frag(v2f_img input) : SV_Target
            {
                float2 uv=input.uv;
                float t=_Phase;
                if (_Worlds>.5) {
                    float slot=min(3,floor(uv.x*4));
                    float2 center=float2((slot+.5)*.25,.59);
                    float2 local=uv-center;
                    float mask=(1-smoothstep(.114,.125,abs(local.x)))*(1-smoothstep(.255,.31,abs(local.y)));
                    float wave=t*.75+slot*1.8;
                    uv.y-=mask*(.010*sin(wave)+local.x*.055*cos(wave));
                    uv.x+=mask*.002*cos(wave*.9);
                    // The foreground cloud bank drifts independently of the islands.
                    float clouds=1-smoothstep(.19,.34,uv.y);
                    uv.x+=.012*sin(t*.16+uv.y*4)*clouds*sin(3.14159*uv.x);
                    uv.y+=.003*sin(t*.22+uv.x*5)*clouds*sin(3.14159*uv.y);
                    return tex2D(_MainTex,uv);
                }
                float diagonal=uv.x-.61*uv.y+.033*sin(t*.23);
                float beam=smoothstep(.1,.83,.5+.5*cos(diagonal*12.4+.20*sin(t*.31)));
                float leaf=.5+.5*sin(diagonal*23+2.2*sin(uv.y*7+t*.41)+.7*cos(uv.x*11-t*.27));
                float light=beam*(.66+.34*leaf);
                fixed4 art=tex2D(_MainTex,uv);
                art.rgb*=lerp(float3(.84,.86,.89),float3(1.06,1.045,1.01),light);
                return art;
            }
            ENDCG
        }
    }
}
