﻿using SharpDX;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SoftRender.Net
{
    public class AssetManager
    {
        public async Task<Mesh[]> LoadJSONFileAsync(string fileName)
        {
            var meshes = new List<Mesh>();
            var materials = new Dictionary<string, Material>();
            var file = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFileAsync(fileName);
            var data = await Windows.Storage.FileIO.ReadTextAsync(file);
            dynamic jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject(data);

            for (var materialIndex = 0; materialIndex < jsonObject.materials.Count; materialIndex++)
            {
                var material = new Material();
                material.Name = jsonObject.materials[materialIndex].name.Value;
                material.ID = jsonObject.materials[materialIndex].id.Value;
                if (jsonObject.materials[materialIndex].diffuseTexture != null)
                    material.DiffuseTextureName = jsonObject.materials[materialIndex].diffuseTexture.name.Value;

                materials.Add(material.ID, material);
            }

            for (var meshIndex = 0; meshIndex < jsonObject.meshes.Count; meshIndex++)
            {
                var verticesArray = jsonObject.meshes[meshIndex].vertices;
                var indicesArray = jsonObject.meshes[meshIndex].indices;
                var uvCount = jsonObject.meshes[meshIndex].uvCount.Value;
                var verticesStep = 1;

                switch ((int)uvCount)
                {
                    case 0:
                        verticesStep = 6;
                        break;
                    case 1:
                        verticesStep = 8;
                        break;
                    case 2:
                        verticesStep = 10;
                        break;
                }

                var verticesCount = verticesArray.Count / verticesStep;

                var facesCount = indicesArray.Count / 3;
                var mesh = new Mesh(jsonObject.meshes[meshIndex].name.Value, verticesCount, facesCount);

                for (var index = 0; index < verticesCount; index++)
                {
                    var x = (float)verticesArray[index * verticesStep].Value;
                    var y = (float)verticesArray[index * verticesStep + 1].Value;
                    var z = (float)verticesArray[index * verticesStep + 2].Value;

                    var nx = (float)verticesArray[index * verticesStep + 3].Value;
                    var ny = (float)verticesArray[index * verticesStep + 4].Value;
                    var nz = (float)verticesArray[index * verticesStep + 5].Value;
                    mesh.Vertices[index] = new Vertex { Coordinates = new Vector3(x, y, z), Normal = new Vector3(nx, ny, nz) };

                    if (uvCount > 0)
                    {
                        var u = (float)verticesArray[index * verticesStep + 6].Value;
                        var v = (float)verticesArray[index * verticesStep + 7].Value;
                        mesh.Vertices[index].TextureCoordinates = new Vector2(u, v);
                    }
                }

                for (var index = 0; index < facesCount; index++)
                {
                    var a = (int)indicesArray[index * 3].Value;
                    var b = (int)indicesArray[index * 3 + 1].Value;
                    var c = (int)indicesArray[index * 3 + 2].Value;
                    mesh.Faces[index] = new Face { A = a, B = b, C = c };
                }

                var position = jsonObject.meshes[meshIndex].position;
                mesh.Position = new Vector3((float)position[0].Value, (float)position[1].Value, (float)position[2].Value);

                if (uvCount > 0)
                {
                    var meshTextureID = jsonObject.meshes[meshIndex].materialId.Value;
                    var meshTextureName = materials[meshTextureID].DiffuseTextureName;
                    mesh.Texture = new Texture(meshTextureName, 512, 512);
                }

                mesh.ComputeFacesNormals();

                meshes.Add(mesh);
            }
            return meshes.ToArray();
        }
    }
}
