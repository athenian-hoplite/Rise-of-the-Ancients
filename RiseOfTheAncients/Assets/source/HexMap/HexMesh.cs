using UnityEngine;
using System.Collections.Generic;
using System;
using ROTA.Memory;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour {

	[NonSerialized] List<Vector3> Vertices; // Vertex buffer
	[NonSerialized] List<int> Triangles;	// Index buffer
	[NonSerialized] List<Vector2> UVs, UV2s; // Texture coordinates
	[NonSerialized] List<Vector3> CellIndices;
	[NonSerialized] List<Color> CellWeights;	

	Mesh Mesh;              
    MeshCollider MeshCollider;   
	
	public bool UseCollider, UseCellData, UseUVCoordinates, UseUV2Coordinates;

	/// <summary>
	/// Initializes mesh, index and vertex buffers.
	/// </summary>
	void Awake () {
		GetComponent<MeshFilter>().mesh = Mesh = new Mesh();
        if (UseCollider) {
			MeshCollider = gameObject.AddComponent<MeshCollider>();
		}
		Mesh.name = "Mesh";
	}

	/// <summary>
	/// Clears mesh data.
	/// </summary>
	public void Clear () {
		Mesh.Clear();
		Vertices = ListPool<Vector3>.GLGet();

		if (UseCellData) {
			CellWeights = ListPool<Color>.GLGet();
			CellIndices = ListPool<Vector3>.GLGet();
		}

		if (UseUVCoordinates) {
			UVs = ListPool<Vector2>.GLGet();
		}
		if (UseUV2Coordinates) {
			UV2s = ListPool<Vector2>.GLGet();
		}

		Triangles = ListPool<int>.GLGet();
	}

	/// <summary>
	/// Apply data added to mesh.
	/// </summary>
	public void Apply () {
		Mesh.SetVertices(Vertices);
		ListPool<Vector3>.GLRestore(Vertices);

		if (UseCellData) {
			Mesh.SetColors(CellWeights);
			ListPool<Color>.GLRestore(CellWeights);
			Mesh.SetUVs(2, CellIndices);
			ListPool<Vector3>.GLRestore(CellIndices);
		}

		if (UseUVCoordinates) {
			Mesh.SetUVs(0, UVs);
			ListPool<Vector2>.GLRestore(UVs);
		}

		if (UseUV2Coordinates) {
			Mesh.SetUVs(1, UV2s);
			ListPool<Vector2>.GLRestore(UV2s);
		}

		Mesh.SetTriangles(Triangles, 0);
		ListPool<int>.GLRestore(Triangles);

		Mesh.RecalculateNormals();
		if (UseCollider) {
			MeshCollider.sharedMesh = Mesh;
		}
	}
	
    /// <summary>
    /// Adds the vertices to the vertex buffer and the corresponding indices
    /// to the index buffer so as to form a triangle.
    /// </summary>
    public void AddTriangle (Vector3 v1, Vector3 v2, Vector3 v3) {
		int vertexIndex = Vertices.Count;
		Vertices.Add(HexMetrics.Perturb(v1));
		Vertices.Add(HexMetrics.Perturb(v2));
		Vertices.Add(HexMetrics.Perturb(v3));
		Triangles.Add(vertexIndex);
		Triangles.Add(vertexIndex + 1);
		Triangles.Add(vertexIndex + 2);
	}

	/// <summary>
	/// Same as AddTriangle but with no noise applied to the vertices.
	/// </summary>
	public void AddTriangleUnperturbed (Vector3 v1, Vector3 v2, Vector3 v3) {
		int vertexIndex = Vertices.Count;
		Vertices.Add(v1);
		Vertices.Add(v2);
		Vertices.Add(v3);
		Triangles.Add(vertexIndex);
		Triangles.Add(vertexIndex + 1);
		Triangles.Add(vertexIndex + 2);
	}

	/// <summary>
    /// Adds the vertices to the vertex buffer and the corresponding indices
    /// to the index buffer so as to form a quad.
    /// </summary>
	public void AddQuad (Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) {
		int vertexIndex = Vertices.Count;
		Vertices.Add(HexMetrics.Perturb(v1));
		Vertices.Add(HexMetrics.Perturb(v2));
		Vertices.Add(HexMetrics.Perturb(v3));
		Vertices.Add(HexMetrics.Perturb(v4));
		Triangles.Add(vertexIndex);
		Triangles.Add(vertexIndex + 2);
		Triangles.Add(vertexIndex + 1);
		Triangles.Add(vertexIndex + 1);
		Triangles.Add(vertexIndex + 2);
		Triangles.Add(vertexIndex + 3);
	}

	/// <summary>
	/// Same as AddQuad but with no noise applied to the vertices.
	/// </summary>
	public void AddQuadUnperturbed (Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) {
		int vertexIndex = Vertices.Count;
		Vertices.Add(v1);
		Vertices.Add(v2);
		Vertices.Add(v3);
		Vertices.Add(v4);
		Triangles.Add(vertexIndex);
		Triangles.Add(vertexIndex + 2);
		Triangles.Add(vertexIndex + 1);
		Triangles.Add(vertexIndex + 1);
		Triangles.Add(vertexIndex + 2);
		Triangles.Add(vertexIndex + 3);
	}

	/// <summary>
	/// Adds these 3 points to the UVs.
	/// </summary>
	public void AddTriangleUV (Vector2 uv1, Vector2 uv2, Vector2 uv3) {
		UVs.Add(uv1);
		UVs.Add(uv2);
		UVs.Add(uv3);
	}
	
	/// <summary>
	/// Adds these 3 points to the UV2s.
	/// </summary>
	public void AddTriangleUV2 (Vector2 uv1, Vector2 uv2, Vector3 uv3) {
		UV2s.Add(uv1);
		UV2s.Add(uv2);
		UV2s.Add(uv3);
	}
	
	/// <summary>
	/// Adds these 4 points to the UVs.
	/// </summary>
	public void AddQuadUV (Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4) {
		UVs.Add(uv1);
		UVs.Add(uv2);
		UVs.Add(uv3);
		UVs.Add(uv4);
	}

	/// <summary>
	/// Adds UVs for the rectangle defined by the parameters.
	/// </summary>
	public void AddQuadUV (float uMin, float uMax, float vMin, float vMax) {
		UVs.Add(new Vector2(uMin, vMin));
		UVs.Add(new Vector2(uMax, vMin));
		UVs.Add(new Vector2(uMin, vMax));
		UVs.Add(new Vector2(uMax, vMax));
	}
	
	/// <summary>
	/// Adds these 4 points to the UVs.
	/// </summary>
	public void AddQuadUV2 (Vector2 uv1, Vector2 uv2, Vector3 uv3, Vector3 uv4) {
		UV2s.Add(uv1);
		UV2s.Add(uv2);
		UV2s.Add(uv3);
		UV2s.Add(uv4);
	}

	/// <summary>
	/// Adds UV2s for the rectangle defined by the parameters.
	/// </summary>
	public void AddQuadUV2 (float uMin, float uMax, float vMin, float vMax) {
		UV2s.Add(new Vector2(uMin, vMin));
		UV2s.Add(new Vector2(uMax, vMin));
		UV2s.Add(new Vector2(uMin, vMax));
		UV2s.Add(new Vector2(uMax, vMax));
	}

	public void AddTriangleCellData (Vector3 indices, Color weights1, Color weights2, Color weights3) {
		CellIndices.Add(indices);
		CellIndices.Add(indices);
		CellIndices.Add(indices);
		CellWeights.Add(weights1);
		CellWeights.Add(weights2);
		CellWeights.Add(weights3);
	}
		
	public void AddTriangleCellData (Vector3 indices, Color weights) {
		AddTriangleCellData(indices, weights, weights, weights);
	}

	public void AddQuadCellData (Vector3 indices, Color weights1, Color weights2, Color weights3, Color weights4) {
		CellIndices.Add(indices);
		CellIndices.Add(indices);
		CellIndices.Add(indices);
		CellIndices.Add(indices);
		CellWeights.Add(weights1);
		CellWeights.Add(weights2);
		CellWeights.Add(weights3);
		CellWeights.Add(weights4);
	}

	public void AddQuadCellData (Vector3 indices, Color weights1, Color weights2) {
		AddQuadCellData(indices, weights1, weights1, weights2, weights2);
	}

	public void AddQuadCellData (Vector3 indices, Color weights) {
		AddQuadCellData(indices, weights, weights, weights, weights);
	}

}