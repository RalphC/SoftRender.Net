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

        public Mesh(string name, int verticesCount, int facesCount)
        {
            Vertices = new Vertex[verticesCount];
            Faces = new Face[facesCount];
            Name = name;
        }
    }
}
