﻿using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TilePuzzle.Rendering;
using UnityEngine;
using UnityEngine.Profiling;

namespace TilePuzzle.Procedural
{
    [ExecuteInEditMode]
    public class PreviewWorld : MonoBehaviour
    {
        [Required]
        public Hexagon hexagonPrefab;
        [Required]
        public Material hexagonMaterial;
        public Transform hexagonHolder;
        public GameObject mountainPrefab;
        public Transform mountainHolder;

        private Vector2Int mapSize;
        private Hexagon[] hexagons;

        public void BuildHexagonMeshes(Vector2Int mapSize, ref Center[] centers)
        {
            int width = mapSize.x;
            int height = mapSize.y;

            if (hexagons == null || this.mapSize != mapSize)
            {
                DestroyAllHexagons();
                hexagons = new Hexagon[width * height];
            }
            this.mapSize = mapSize;

            HexagonMeshGenerator meshGenerator = new HexagonMeshGenerator();
            Mesh planeHexagonMesh = meshGenerator.BuildMesh(Hexagon.Size);
            Mesh cliffHexagonMesh = meshGenerator.BuildMesh(Hexagon.Size, 1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Center center = centers[x + y * width];
                    HexagonMeshGenerator.VertexDirection riverDirection = 0;
                    for (int i = 0; i < center.NeighborCorners.Length; i++)
                    {
                        Corner neighborCorner = center.NeighborCorners[i];
                        if (neighborCorner.river > 0)
                        {
                            riverDirection |= (HexagonMeshGenerator.VertexDirection)(1 << i);
                        }
                    }

                    Mesh hexagonMesh;
                    //if (center.isSea)
                    //{
                    //    hexagonMesh = null;
                    //}
                    if (riverDirection > 0 && center.isWater == false)
                    {
                        hexagonMesh = meshGenerator.BuildMesh(Hexagon.Size, 1, 0.3f, riverDirection);
                    }
                    else if (center.isWater == false && center.NeighborCenters.Values.Any(neighborCenter => neighborCenter.isWater))
                    {
                        hexagonMesh = cliffHexagonMesh;
                    }
                    else
                    {
                        hexagonMesh = planeHexagonMesh;
                    }

                    if (hexagons[x + y * width] == null)
                    {
                        Hexagon newHexagon = CreateNewHexagon(HexagonPos.FromArrayXY(x, y));
                        hexagons[x + y * width] = newHexagon;
                    }
                    hexagons[x + y * width].meshFilter.sharedMesh = hexagonMesh;
                }
            }
        }

        public void MountainTest(Vector2Int mapSize, ref bool[] mountains)
        {
            int width = mapSize.x;
            int height = mapSize.y;

            DestroyAllMountains();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (mountains[x + y * width])
                    {
                        CreateNewMountain(HexagonPos.FromArrayXY(x, y));
                    }
                }
            }
        }

        public void SetHexagonColorMap(int mapWidth, int mapHeight, ref Color[] colorMap)
        {
            Profiler.BeginSample(nameof(SetHexagonColorMap));
            int textureWidth = (int)Mathf.Pow(2, Mathf.CeilToInt(Mathf.Log(mapWidth, 2)));
            int textureHeight = (int)Mathf.Pow(2, Mathf.CeilToInt(Mathf.Log(mapHeight, 2)));
            Texture2D texture = new Texture2D(textureWidth, textureHeight)
            {
                filterMode = FilterMode.Point
            };

            Color[] textureColors = new Color[textureWidth * textureHeight];
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    textureColors[x + y * textureWidth] = colorMap[x + y * mapWidth];
                }
            }
            texture.SetPixels(textureColors);
            texture.Apply();

            hexagonMaterial.SetTexture("_ColorMap", texture);
            hexagonMaterial.SetVector("_ColorMapSize", new Vector2(textureWidth, textureHeight));
            Profiler.EndSample();
        }

        public void SetHexagonsElevation(ref float[] elevations, float multiplier)
        {
            for (int i = 0; i < hexagons.Length; i++)
            {
                var pos = hexagons[i].transform.position;
                pos.y = elevations[i] * multiplier;
                hexagons[i].transform.position = pos;
            }
        }

        [Button]
        private void DestroyAllHexagons()
        {
            foreach (var hexagon in GameObject.FindGameObjectsWithTag("Hexagon"))
            {
                DestroyImmediate(hexagon.gameObject);
            }
            hexagons = null;
        }

        private void DestroyAllMountains()
        {
            foreach (var mountain in GameObject.FindGameObjectsWithTag("MountainTest"))
            {
                DestroyImmediate(mountain.gameObject);
            }
        }

        private Hexagon CreateNewHexagon(HexagonPos hexPos)
        {
            Hexagon newHexagon = Instantiate(hexagonPrefab, hexagonHolder);
            newHexagon.hexPos = hexPos;
            newHexagon.name = $"Hexagon {newHexagon.hexPos}";
            newHexagon.transform.position = newHexagon.hexPos.ToWorldPos();
            newHexagon.GetComponent<MeshRenderer>().sharedMaterial = hexagonMaterial;

            return newHexagon;
        }

        private GameObject CreateNewMountain(HexagonPos hexPos)
        {
            GameObject newMountain = Instantiate(mountainPrefab, hexagonHolder);
            newMountain.name = $"Mountain";
            newMountain.transform.parent = mountainHolder;
            Vector3 mountainPos = hexPos.ToWorldPos();
            mountainPos.y = -0.2f;
            newMountain.transform.position = mountainPos;
            return newMountain;
        }
    }
}
