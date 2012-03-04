Texture2D previousframe : register( s0 );
SamplerState sam : register( s0 );

struct VertexShaderInput
{
    float4 Position : POSITION0;
	float2 uv : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float2 uv : TEXCOORD0;
	//float4 PosH  : SV_POSITION;
};

VertexShaderOutput generic_vs(VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = input.Position;
	output.uv = input.uv;
    return output;
}

float4 gpugol_ps(VertexShaderOutput input) : COLOR0
{
	const float2 n = float2(1.0 / 1280.0, 1.0 / 720.0);
	input.uv += n * 0.5;
	float4 mycol = previousframe.Sample(sam, input.uv);
	int aliveneighbours = 0;
	float4 neighbours[8] = 
	{
		previousframe.Sample(sam, input.uv + n),
		previousframe.Sample(sam, input.uv - n),
		previousframe.Sample(sam, float2(input.uv.x, input.uv.y + n.y)),
		previousframe.Sample(sam, float2(input.uv.x, input.uv.y - n.y)),
		previousframe.Sample(sam, float2(input.uv.x + n.x, input.uv.y)),
		previousframe.Sample(sam, float2(input.uv.x - n.x, input.uv.y)),
		previousframe.Sample(sam, float2(input.uv.x + n.x, input.uv.y - n.y)),
		previousframe.Sample(sam, float2(input.uv.x - n.x, input.uv.y + n.y))
	};
	
	for(int i = 0; i < 8; ++i)	
		if ( neighbours[i].a > 0.5 )
			++aliveneighbours;	
	// alive
	if( mycol.a > 0.5 )
	{
		if ( aliveneighbours == 2 || aliveneighbours == 3)
			mycol = 1.0.rrrr;
		else
			mycol = 0.0.rrrr;
	}
	// dead
	else
	{
		if(aliveneighbours == 3)
			mycol = 1.0;
		else
			mycol = 0.0;
	}

	if(mycol.a > 0.5)
		mycol.rg = input.uv;
	

	return mycol;
}

float4 splatter_ps(VertexShaderOutput input) : COLOR0
{
	const float2 n = float2(1.0 / 1280.0, 1.0 / 720.0) * 0.5;
    return previousframe.Sample(sam, input.uv + n);
}

technique gpugol
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 generic_vs();
        PixelShader = compile ps_2_0 gpugol_ps();
    }
}


technique randomsplatter
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 generic_vs();
        PixelShader = compile ps_2_0 splatter_ps();
    }
}
