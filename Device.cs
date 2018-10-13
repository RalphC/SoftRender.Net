using System.Runtime.InteropServices.ComTypes;
using SharpDX;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace SoftRender.Net
{
    public class Device
    {
        private byte[] backBuffer;
        private readonly float[] depthBuffer;
        private object[] lockBuffer;

        private WriteableBitmap bmp;
        private int width = 0, height = 0, length = 0;
        private float half_width = 0f, half_height = 0f;

        public Device(WriteableBitmap bmp)
        {
            this.bmp = bmp;
            width = bmp.PixelWidth;
            half_width = width / 2.0f;
            height = bmp.PixelHeight;
            half_height = height / 2.0f;
            backBuffer = new byte[this.bmp.PixelWidth * this.bmp.PixelHeight * 4];
            length = backBuffer.Length;
            depthBuffer = new float[width * height];

            lockBuffer = new object[width * height];
            for (int i = 0; i < lockBuffer.Length; i++)
            {
                lockBuffer[i] = new object();
            }
        }

        public void Clear(byte r, byte g, byte b, byte a)
        {
            //var clearBytes = new byte[4]{b, g, r, a};
            for (int index = 0; index < length - 4; index += 4)
            {
                //unsafe
                //{
                //    fixed (byte* buff = &backBuffer[index])
                //    {
                //        *buff = b;
                //        *(buff + 1) = g;
                //        *(buff + 2) = r;
                //        *(buff + 3) = a;
                //    }
                //}

                backBuffer[index] = b;
                backBuffer[index + 1] = g;
                backBuffer[index + 2] = r;
                backBuffer[index + 3] = a;
            }

            for (int index = 0; index < depthBuffer.Length; index++)
            {
                depthBuffer[index] = float.MaxValue;
            }
        }

        public void Present()
        {
            using (var stream = bmp.PixelBuffer.AsStream())
            {
                stream.Write(backBuffer, 0, backBuffer.Length);
            }

            bmp.Invalidate();
        }

        public void PutPixel(int x, int y, float z, Color4 color)
        {
            var index = x + y * width;

            lock (lockBuffer[index])
            {
                if (depthBuffer[index] < z)
                {
                    return;
                }

                var index4 = index * 4;
                backBuffer[index4] = (byte)(color.Blue * 255);
                backBuffer[index4 + 1] = (byte)(color.Green * 255);
                backBuffer[index4 + 2] = (byte)(color.Red * 255);
                backBuffer[index4 + 3] = (byte)(color.Alpha * 255);

                depthBuffer[index] = z;
            }
        }

        //public Vector2 Project(Vector3 coord, Matrix transMat)
        //{
        //    var point = Vector3.TransformCoordinate(coord, transMat);
        //    var x = point.X * width + half_width;
        //    var y = -point.Y * height + half_height;
        //    return (new Vector2(x, y));
        //}

        public Vector3 Project(Vector3 coord, Matrix transMatrix)
        {
            var point = Vector3.TransformCoordinate(coord, transMatrix);
            var x = point.X * width + half_width;
            var y = -point.Y * height + half_height;
            return new Vector3(x, y, point.Z);
        }

        //public void DrawPoint(Vector2 point)
        //{
        //    if (point.X >= 0 && point.Y >= 0 && point.X < width && point.Y < height)
        //    {
        //        PutPixel((int)point.X, (int)point.Y, Color4.White);
        //    }
        //}

        //public void DrawPoint(Vector2 point, Color4 color)
        //{
        //    if (point.X >= 0 && point.Y >= 0 && point.X < width && point.Y < height)
        //    {
        //        PutPixel((int)point.X, (int)point.Y, color);
        //    }
        //}

        public void DrawPoint(int x, int y, float z)
        {
            if (x >= 0 && y >= 0 && x < width && y < height)
            {
                PutPixel(x, y, z, Color4.White);
            }
        }

        public void DrawPoint(int x, int y, float z, Color4 color)
        {
            if (x >= 0 && y >= 0 && x < width && y < height)
            {
                PutPixel(x, y, z, color);
            }
        }

        //public void DrawLine(Vector2 point1, Vector2 point2)
        //{
        //    var dist = (point2 - point1).Length();
        //    if (dist < 2) return;

        //    Vector2 midPoint = point1 + (point2 - point1) / 2f;
        //    DrawPoint(midPoint);
        //    DrawLine(point1, midPoint);
        //    DrawLine(midPoint, point2);
        //}

        //public void DrawBresenhamLine(Vector2 p0, Vector2 p1)
        //{
        //    int x0 = (int)p0.X;
        //    int y0 = (int)p0.Y;
        //    int x1 = (int)p1.X;
        //    int y1 = (int)p1.Y;

        //    var dx = Math.Abs(x1 - x0);
        //    var dy = Math.Abs(y1 - y0);
        //    var sx = (x0 < x1) ? 1 : -1;
        //    var sy = (y0 < y1) ? 1 : -1;
        //    var err = dx - dy;

        //    while (true)
        //    {
        //        //DrawPoint(new Vector2(x0, y0));
        //        DrawPoint(x0, y0);

        //        if (x0 == x1 && (y0 == y1)) break;
        //        var e2 = 2 * err;
        //        if (e2 > -dy) { err -= dy; x0 += sx; }
        //        if (e2 < dx) { err += dx; y0 += sy; }
        //    }
        //}

        public void ProcessScanLine(int y, Vector3 pa, Vector3 pb, Vector3 pc, Vector3 pd, Color4 color)
        {
            var gradient1 = pa.Y != pb.Y ? (y - pa.Y) / (pb.Y - pa.Y) : 1;
            var gradient2 = pc.Y != pd.Y ? (y - pc.Y) / (pd.Y - pc.Y) : 1;

            int sx = (int)MathLib.Interpolate(pa.X, pb.X, gradient1);
            int ex = (int)MathLib.Interpolate(pc.X, pd.X, gradient2);

            float z1 = MathLib.Interpolate(pa.Z, pb.Z, gradient1);
            float z2 = MathLib.Interpolate(pc.Z, pd.Z, gradient2);

            for (int index = sx; index < ex; index++)
            {
                var grad = (index - sx) / (float)(ex - sx);
                var z = MathLib.Interpolate(z1, z2, grad);
                DrawPoint(index, y, z, color);
            }
        }

        public void DrawTriangle(Vector3 p0, Vector3 p1, Vector3 p2, Color4 color)
        {
            if (p0.Y > p1.Y)
            {
                var temp = p1;
                p1 = p0;
                p0 = temp;
            }

            if (p1.Y > p2.Y)
            {
                var temp = p2;
                p2 = p1;
                p1 = temp;
            }

            if (p0.Y > p1.Y)
            {
                var temp = p1;
                p1 = p0;
                p0 = temp;
            }

            float dP0P1, dP0P2;

            if (p1.Y - p0.Y > 0)
            {
                dP0P1 = (p1.X - p0.X) / (p1.Y - p0.Y);

            }
            else
            {
                dP0P1 = 0;
            }

            if (p2.Y - p0.Y > 0)
            {
                dP0P2 = (p2.X - p0.X) / (p2.Y - p0.Y);

            }
            else
            {
                dP0P2 = 0;
            }

            if (dP0P1 > dP0P2)
            {
                for (var y = (int)p0.Y; y <= (int)p2.Y; y++)
                {
                    if (y < p1.Y)
                    {
                        ProcessScanLine(y, p0, p2, p0, p1, color);
                    }
                    else
                    {
                        ProcessScanLine(y, p0, p2, p1, p2, color);
                    }
                }
            }
            else
            {
                for (var y = (int)p0.Y; y <= (int)p2.Y; y++)
                {
                    if (y < p1.Y)
                    {
                        ProcessScanLine(y, p0, p1, p0, p2, color);
                    }
                    else
                    {
                        ProcessScanLine(y, p1, p2, p0, p2, color);
                    }
                }
            }
        }

        public void Render(Camera camera, params Mesh[] meshes)
        {
            var viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
            var projectionMatrix =
                Matrix.PerspectiveFovRH(0.78f, (float)bmp.PixelWidth / bmp.PixelHeight, 0.01f, 1.0f);

            //var faceIndex = 0;
            foreach (var mesh in meshes)
            {
                var worldMatrix = Matrix.RotationYawPitchRoll(mesh.Rotation.Y, mesh.Rotation.X, mesh.Rotation.Z) *
                                  Matrix.Translation(mesh.Position);

                var transformMatrix = worldMatrix * viewMatrix * projectionMatrix;

                //foreach (var vertex in mesh.Vertices)
                //{
                //    var point = Project(vertex, transformMatrix);
                //    DrawPoint(point);
                //}

                //for (int index = 0; index < mesh.Vertices.Length - 1; index++)
                //{
                //    var point1 = Project(mesh.Vertices[index], transformMatrix);
                //    var point2 = Project(mesh.Vertices[index + 1], transformMatrix);
                //    DrawLine(point1, point2);
                //}

                Parallel.For(0, mesh.Faces.Length, faceIndex =>
                {
                    //foreach (var face in mesh.Faces)
                    {
                        var face = mesh.Faces[faceIndex];
                        var vertexA = mesh.Vertices[face.A];
                        var vertexB = mesh.Vertices[face.B];
                        var vertexC = mesh.Vertices[face.C];

                        var pixelA = Project(vertexA, transformMatrix);
                        var pixelB = Project(vertexB, transformMatrix);
                        var pixelC = Project(vertexC, transformMatrix);

                        //DrawLine(pixelA, pixelB);
                        //DrawLine(pixelB, pixelC);
                        //DrawLine(pixelC, pixelA);

                        //DrawBresenhamLine(pixelA, pixelB);
                        //DrawBresenhamLine(pixelB, pixelC);
                        //DrawBresenhamLine(pixelC, pixelA);

                        var color = 0.25f + (faceIndex % mesh.Faces.Length) * 0.75f / mesh.Faces.Length;
                        DrawTriangle(pixelA, pixelB, pixelC, new Color4(color, color, color, 1f));
                        faceIndex++;
                    }
                });


            }
        }
    }
}
