Shader "Custom/CrossfadeLit"
{
    Properties
    {
        _ColorA ("Color A", Color) = (1,1,1,1)
		_ColorB("Color B", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_MainTexAlt("Albedo B (RGB)", 2D) = "white" {}
        _GlossinessA ("Smoothness A", Range(0,1)) = 0.5
		_GlossinessB("Smoothness B", Range(0,1)) = 0.5
        _MetallicA ("Metallic A", Range(0,1)) = 0.0
		_MetallicB("Metallic B", Range(0,1)) = 0.0
		_Mix("Mix A -> B", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

		sampler2D _MainTex;
        sampler2D _MainTexAlt;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _GlossinessA;
		half _GlossinessB;
        half _MetallicA;
		half _MetallicB;
        fixed4 _ColorA;
		fixed4 _ColorB;
		half _Mix;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
			fixed4 colMix = lerp(_ColorA, _ColorB, _Mix);
			fixed4 cA = tex2D(_MainTex, IN.uv_MainTex) * colMix;
			fixed4 cB = tex2D(_MainTexAlt, IN.uv_MainTex) * colMix;
			fixed4 c = lerp(cA, cB, _Mix);
			o.Albedo = c.rgb;

            // Metallic and smoothness come from slider variables
            o.Metallic = lerp(_MetallicA, _MetallicB, _Mix);
            o.Smoothness = lerp(_GlossinessA, _GlossinessB, _Mix);
			o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
