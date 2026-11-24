Shader "ChronosChef/KitchenFlame"
{
    Properties { _Phase ("Phase", Float) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 position : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct Output { float4 position : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            float _Phase;
            Output vert(Input v)
            {
                Output o;
                o.position=UnityObjectToClipPos(v.position);
                o.uv=v.uv;
                o.color=v.color;
                return o;
            }
            fixed4 frag(Output i) : SV_Target
            {
                float y=i.uv.y, x=i.uv.x*2-1;
                float t=_Phase;
                float sway=sin(y*9-t*4.2)*.16*y+sin(y*17+t*3)*.07*y;
                float tongues=sin(x*13+t*4)*.09+sin(x*21-t*3)*.05;
                float width=max(.008,(.64-y+ tongues)*.82);
                float core=1-smoothstep(width*.48,width+.04,abs(x-sway));
                core*=smoothstep(0,.09,y)*(1-smoothstep(.58,.85,y));
                float glow=exp(-8*x*x-12*(y-.17)*(y-.17))*.2;
                float sparks=0;
                for(int k=0;k<5;k++)
                {
                    float age=frac(t*(.32+.035*k)+k*.381);
                    float drift=.58*sin(k*9.3+age*3)+.08*sin(age*13+k);
                    float2 offset=float2(x-drift,y-(.15+age*.83));
                    sparks+=exp(-dot(offset*float2(44,66),offset*float2(44,66)))*sin(age*3.14159);
                }
                float fade=smoothstep(0,.05,y)*(1-smoothstep(.9,1,y))*(1-smoothstep(.82,1,abs(x)));
                return fixed4(lerp(i.color.rgb,1,core*.43),saturate((core*.72+glow+sparks)*fade)*i.color.a);
            }
            ENDCG
        }
    }
}
