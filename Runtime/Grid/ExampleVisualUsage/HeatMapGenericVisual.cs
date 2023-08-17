using UnityEngine;

namespace Nevelson.GridPlacementSystem
{
    public class HeatMapGenericVisual : MonoBehaviour
    {
        Grid<HeatMapGridObject> grid;
        Mesh mesh;
        bool updateMesh;

        private void Awake()
        {
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;
        }

        public void SetGrid(Grid<HeatMapGridObject> grid)
        {
            this.grid = grid;
            UpdateHeatMapVisual();
            grid.OnGridValueChanged += Grid_OnGridValueChanged;
        }

        private void Grid_OnGridValueChanged(object sender, Grid<HeatMapGridObject>.OnGridValueChangedEventArgs e)
        {
            updateMesh = true;
        }

        private void LateUpdate()
        {
            if (updateMesh)
            {
                updateMesh = false;
                UpdateHeatMapVisual();
            }
        }

        private void UpdateHeatMapVisual()
        {
            MeshUtils.CreateEmptyMeshArrays(grid.Width * grid.Height, out Vector3[] vertices, out Vector2[] uv, out int[] triangles);

            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    int index = x * grid.Height + y;
                    Vector3 quadSize = new Vector3(1, 1) * grid.CellSize;
                    Vector2Int xy = new Vector2Int(x, y);
                    HeatMapGridObject gridGridObject = grid.GetGridObject(new Vector2Int(x, y));
                    float gridValueNormalized = gridGridObject.GetValueNormalized();
                    Vector2 gridValueUV = new Vector2(gridValueNormalized, 0f);
                    StaticFactory.AddToMeshArrays(vertices, uv, triangles, index, grid.GetWorldPosition(xy) + quadSize * .5f, 0f, quadSize, gridValueUV, gridValueUV);
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
        }

    }
}