#if !defined(WATERINCLUDE)
#define WATERINCLUDE

#include "HexMetrics.cginc"

/**
 * Produces a foam effect, as seen when waves hit the coast.
 * @param shore should be [0,1] indicating distance to shore
**/
float Foam (float shore, float2 worldXZ, sampler2D noiseTex) {
	// Shore is the V coordinate, used to see distance to land [0,1]
	// Taking only 90%, this can ofcourse be scaled up or down to make the effect more or less pronounced
	shore = sqrt(shore) * 0.9; // Make wave bigger closer to shore (V = 1) than far away (V = 0)

	// Multiply noise and distortion by _Time to create animation (variance with time)

	// Sample noise from perlin noise texture
	float2 noiseUV = worldXZ + _Time.y * 0.25;
	float4 noise = tex2D(noiseTex, noiseUV * (2 * TILING_SCALE)); // Scale down to avoid excessive tiling

	// This is the foam forming as the wave hits the shore
	float distortion1 = noise.x * (1 - shore); // Add noise that gets weaker closer to shore
	float foam1 = sin((shore + distortion1) * 10 - _Time.y); // Get sine wave to simulate waves [-1,1]
	foam1 *= foam1; // square to return to [0,1]

	// This is the foam coming back after the wave hits the shore
	float distortion2 = noise.y * (1 - shore);
	// +2 time offset to the other foam AND different animation direction (+ _Time instead of - _Time)
	float foam2 = sin((shore + distortion2) * 10 + _Time.y + 2);
	foam2 *= foam2 * 0.7; // Make this one smaller

	return max(foam1, foam2) * shore;
}

/**
 * Produces a wave effect.
**/
float Waves (float2 worldXZ, sampler2D noiseTex) {
	// Apply time elapsed to create water flow animation
	// Sample noise texture and scale down to avoid tiling
	float2 uv1 = worldXZ;
	uv1.y += _Time.y;
	float4 noise1 = tex2D(noiseTex, uv1 * (3 * TILING_SCALE));

	float2 uv2 = worldXZ;
	uv2.x += _Time.y;
	float4 noise2 = tex2D(noiseTex, uv2 * (3 * TILING_SCALE));

	// Scaling down to make large bands and multiply by time to create animation
	// Noise added to make the sine waves less obvious
	float blendWave = sin(
		(worldXZ.x + worldXZ.y) * 0.1 +
		(noise1.y + noise2.z) + _Time.y
	);
	blendWave *= blendWave;

	// Add the two different noise samples, range now is [0,2]
	// Use the sine wave to interpolate between 2 channels of noise
	float waves = lerp(noise1.z, noise1.w, blendWave) + lerp(noise2.x, noise2.y, blendWave);

	// Smoothstep -> [0,0.75] = 0; [0.75,2] = [0,1];
	// This means values bellow 0.75 will not have "waves", and from 0.75 to 2 will be the [0,1] range
	return smoothstep(0.75, 2, waves);
}

/**
 * Produces a river flow effect.
**/
float River (float2 riverUV, sampler2D noiseTex) {
	float2 uv = riverUV; // Get UV coordinates (defined from the UV buffer in HexMesh)
	uv.x *= 0.0625; // Scale down U coordinate, this means we only sample a strip of the texture (uvs are the texture coordinates)
	uv.x += _Time.y * 0.005; // Slide U coordinate around the texture to create a smother animation
	uv.y -= _Time.y * 0.25; // Slide V coordinate with Time to create a smother animation

	// Sample noise texture. Because of U down scale sampling occurs in a narrow band
	// And because we slide both UV coordinates the noise texture is sampled in different places every frame
	// We slide in both axis
	float4 noise = tex2D(noiseTex, uv); 

	// If we didnt scale down U then we would effectively sample the whole noise texture on the U axis.

	// Sample noise again on different scales
	float2 uv2 = riverUV;
	uv2.x = uv2.x * 0.0625 - _Time.y * 0.0052;
	uv2.y -= _Time.y * 0.23;
	float4 noise2 = tex2D(noiseTex, uv2);

	return noise.x * noise2.w;
}

#endif