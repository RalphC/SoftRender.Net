using System.Threading.Tasks;
using SharpDX;

namespace SoftRender.Net
{
    public class Mesh
    {
        public string Name { set; get; }
        public Vertex[] Vertices { private set; get; }
        public Face[] Faces { set; get; }
        public Vector3 Position { set; get; }
        public Vector3 Rotation { set; get; }

        public Texture Texture { set; get; }

        public Mesh(string name, int verticesCount, int facesCount)
        {
            Vertices = new Vertex[verticesCount];
            Faces = new Face[facesCount];
            Name = name;
        }

        public void ComputeFacesNormals()
        {
            Parallel.For(0, Faces.Length, faceIndex =>
            {
                var face = Faces[faceIndex];
                var vertexA = Vertices[face.A];
                var vertexB = Vertices[face.B];
                var vertexC = Vertices[face.C];

                Faces[faceIndex].Normal = (vertexA.Normal + vertexB.Normal + vertexC.Normal) / 3.0f;
                Faces[faceIndex].Normal.Normalize();
            });

        }
    }
}
