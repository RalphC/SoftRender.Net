using SharpDX;

namespace SoftRender.Net
{
    public class Camera
    {
        public Vector3 Position { set; get; }
        public Vector3 Target { set; get; }

        
    }

    public struct Face
    {
        public int A;
        public int B;
        public int C;
    }

    public class Mesh
    {
        public string Name { set; get; }
        public Vector3[] Vertices { private set; get; }
        public Face[] Faces { set; get; }
        public Vector3 Position { set; get; }
        public Vector3 Rotation { set; get; }

        public Mesh(string name, int verticesCount, int facesCount)
        {
            Vertices = new Vector3[verticesCount];
            Faces = new Face[facesCount];
            Name = name;
        }
    }
}
