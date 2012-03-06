Texture2D previousframe : register( s0 );
SamplerState sam : register( s0 );

uniform float2 resolution;
uniform float coloroffset;

struct VertexShaderInput
{
    float4 Position : POSITION0;
	float2 uv : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
	float2 uv : TEXCOORD0;
};

VertexShaderOutput generic_vs(VertexShaderInput input)
{
    VertexShaderOutput output;
    output.Position = input.Position;
	output.uv = input.uv;
    return output;
}

int MooreNeighbours(float2 uv, float2 hp)
{
	int neighbours = 0;
	neighbours += previousframe.Sample(sam, uv + hp).a > 0.5 ? 1 : 0;
	neighbours += previousframe.Sample(sam, uv - hp).a > 0.5 ? 1 : 0;
	neighbours += previousframe.Sample(sam, float2(uv.x, uv.y + hp.y)).a > 0.5 ? 1 : 0;
	neighbours += previousframe.Sample(sam, float2(uv.x, uv.y - hp.y)).a > 0.5 ? 1 : 0;
	neighbours += previousframe.Sample(sam, float2(uv.x + hp.x, uv.y)).a > 0.5 ? 1 : 0;
	neighbours += previousframe.Sample(sam, float2(uv.x - hp.x, uv.y)).a > 0.5 ? 1 : 0;
	neighbours += previousframe.Sample(sam, float2(uv.x + hp.x, uv.y - hp.y)).a > 0.5 ? 1 : 0;
	neighbours += previousframe.Sample(sam, float2(uv.x - hp.x, uv.y + hp.y)).a > 0.5 ? 1 : 0;
	return neighbours;
}

float4 gpugol_ps(VertexShaderOutput input) : COLOR0
{
	const float2 n = float2(1.0 / resolution.x, 1.0 / resolution.y);
	input.uv += n * 0.5; // Adjust for the half pixel.
	float4 mycol = previousframe.Sample(sam, input.uv);
	int aliveneighbours = MooreNeighbours(input.uv, n);

	if( mycol.a > 0.5 )
	{
		if ( aliveneighbours == 2 || aliveneighbours == 3)
			mycol = float4(1.0, input.uv, 1.0);
		else
			mycol = float4(0, 0, 0, 0);
	}
	else
	{
		if(aliveneighbours == 3 )
			mycol = float4(1.0, input.uv, 1.0);
		else
			mycol = float4(0, 0, 0, 0);
	}
	return mycol;
}

float4 gpudan_ps(VertexShaderOutput input) : COLOR0
{
	const float2 n = float2(1.0 / resolution.x, 1.0 / resolution.y);
	input.uv += n * 0.5; // Adjust for the half pixel.
	float4 mycol = previousframe.Sample(sam, input.uv);
	int aliveneighbours = MooreNeighbours(input.uv, n);

	if( mycol.a > 0.5 )
	{
		if ( aliveneighbours == 3 || aliveneighbours == 4 || aliveneighbours >= 6)
			mycol = float4(1.0, input.uv, 1.0);
		else
			mycol = float4(0, 0, 0, 0);
	}
	else
	{
		if(aliveneighbours == 3 || aliveneighbours >= 6)
			mycol = float4(1.0, input.uv, 1.0);
		else
			mycol = float4(0, 0, 0, 0);
	}
	return mycol;
}

float4 gpuhl_ps(VertexShaderOutput input) : COLOR0
{
	const float2 n = float2(1.0 / resolution.x, 1.0 / resolution.y);
	input.uv += n * 0.5; // Adjust for the half pixel.
	float4 mycol = previousframe.Sample(sam, input.uv);
	int aliveneighbours = MooreNeighbours(input.uv, n);

	if( mycol.a > 0.5 )
	{
		if ( aliveneighbours == 2 || aliveneighbours == 3)
			mycol = float4(1.0, input.uv, 1.0);
		else
			mycol = float4(0, 0, 0, 0);
	}
	else
	{
		if(aliveneighbours == 3 || aliveneighbours == 6)
			mycol = float4(1.0, input.uv, 1.0);
		else
			mycol = float4(0, 0, 0, 0);
	}
	return mycol;
}

float4 gpucs_ps(VertexShaderOutput input) : COLOR0
{
	const float2 n = float2(1.0 / resolution.x, 1.0 / resolution.y);
	input.uv += n * 0.5; // Adjust for the half pixel.
	float4 mycol = previousframe.Sample(sam, input.uv);
	int aliveneighbours = MooreNeighbours(input.uv, n);

	if( mycol.a < 0.5 )
	{
		if ( aliveneighbours == 2)
			mycol = float4(1.0, input.uv, 1.0);
		else
			mycol = float4(0, 0, 0, 0);
	}
	return mycol;
}

float4 gpur90_ps(VertexShaderOutput input) : COLOR0
{
	const float2 n = float2(1.0 / resolution.x, 1.0 / resolution.y);
	input.uv += n * 0.5; // Adjust for the half pixel.
	float4 mycol = previousframe.Sample(sam, input.uv);
	bool nl = previousframe.Sample(sam, float2(input.uv.x - n.x, input.uv.y - n.y)).a > 0.5;
	bool nr = previousframe.Sample(sam, float2(input.uv.x + n.x, input.uv.y - n.y)).a > 0.5;

	if( mycol.a < 0.5 )
	{
		if ( (nl || nr) && !(nl && nr))
			mycol = float4(1.0, input.uv, 1.0);
		else
			mycol = float4(0, 0, 0, 0);
	}
	return mycol;
}

float4 splatter_ps(VertexShaderOutput input) : COLOR0
{
	const float2 n = float2(1.0 / resolution.x, 1.0 / resolution.y) * 0.5;
    return previousframe.Sample(sam, input.uv + n);
}

technique gpugol
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 generic_vs();
        PixelShader = compile ps_3_0 gpugol_ps();
    }
}

technique gpudan
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 generic_vs();
        PixelShader = compile ps_3_0 gpudan_ps();
    }
}

technique gpuhl
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 generic_vs();
        PixelShader = compile ps_3_0 gpuhl_ps();
    }
}

technique gpucs
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 generic_vs();
        PixelShader = compile ps_3_0 gpucs_ps();
    }
}

technique gpur90
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 generic_vs();
        PixelShader = compile ps_3_0 gpur90_ps();
    }
}

technique randomsplatter
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 generic_vs();
        PixelShader = compile ps_3_0 splatter_ps();
    }
}