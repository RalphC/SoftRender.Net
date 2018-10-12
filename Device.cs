using System;
using SharpDX;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Media.Imaging;

namespace SoftRender.Net
{
    public class Device
    {
        private byte[] backBuffer;
        private WriteableBitmap bmp;
        private int width = 0, height = 0, length = 0;

        public Device(WriteableBitmap bmp)
        {
            this.bmp = bmp;
            width = bmp.PixelWidth;
            height = bmp.PixelHeight;
            backBuffer = new byte[this.bmp.PixelWidth * this.bmp.PixelHeight * 4];
            length = backBuffer.Length;
        }

        public void Clear(byte r, byte g, byte b, byte a)
        {
            var clearBytes = new byte[4]{b, g, r, a};
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
        }

        public void Present()
        {
            using (var stream = bmp.PixelBuffer.AsStream())
            {
                stream.Write(backBuffer, 0, backBuffer.Length);
            }

            bmp.Invalidate();
        }

        public void PutPixel(int x, int y, Color4 color)
        {
            var index = (x + y * bmp.PixelWidth) * 4;

            backBuffer[index] = (byte)(color.Blue * 255);
            backBuffer[index + 1] = (byte)(color.Green * 255);
            backBuffer[index + 2] = (byte)(color.Red * 255);
            backBuffer[index + 3] = (byte)(color.Alpha * 255);
        }

        public Vector2 Project(Vector3 coord, Matrix transMat)
        {
            var point = Vector3.TransformCoordinate(coord, transMat);
            var x = point.X * bmp.PixelWidth + bmp.PixelWidth / 2.0f;
            var y = -point.Y * bmp.PixelHeight + bmp.PixelHeight / 2.0f;
            return (new Vector2(x, y));
        }

        public void DrawPoint(Vector2 point)
        {
            if (point.X >= 0 && point.Y >= 0 && point.X < width && point.Y < height)
            {
                PutPixel((int)point.X, (int)point.Y, new Color4(1.0f, 1.0f, 1.0f, 1.0f));
            }
        }

        public void DrawLine(Vector2 point1, Vector2 point2)
        {
            var dist = (point2 - point1).Length();
            if (dist < 2) return;

            Vector2 midPoint = point1 + (point2 - point1) / 2f;
            DrawPoint(midPoint);
            DrawLine(point1, midPoint);
            DrawLine(midPoint, point2);
        }

        public void DrawBresenhamLine(Vector2 p0, Vector2 p1)
        {
            int x0 = (int)p0.X;
            int y0 = (int)p0.Y;
            int x1 = (int)p1.X;
            int y1 = (int)p1.Y;

            var dx = Math.Abs(x1 - x0);
            var dy = Math.Abs(y1 - y0);
            var sx = (x0 < x1) ? 1 : -1;
            var sy = (y0 < y1) ? 1 : -1;
            var err = dx - dy;

            while (true)
            {
                DrawPoint(new Vector2(x0, y0));

                if (x0 == x1 && (y0 == y1)) break;
                var e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        public void Render(Camera camera, params Mesh[] meshes)
        {
            var viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
            var projectionMatrix =
                Matrix.PerspectiveFovRH(0.78f, (float)bmp.PixelWidth / bmp.PixelHeight, 0.01f, 1.0f);

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

                foreach (var face in mesh.Faces)
                {
                    var vertexA = mesh.Vertices[face.A];
                    var vertexB = mesh.Vertices[face.B];
                    var vertexC = mesh.Vertices[face.C];

                    var pixelA = Project(vertexA, transformMatrix);
                    var pixelB = Project(vertexB, transformMatrix);
                    var pixelC = Project(vertexC, transformMatrix);

                    //DrawLine(pixelA, pixelB);
                    //DrawLine(pixelB, pixelC);
                    //DrawLine(pixelC, pixelA);

                    DrawBresenhamLine(pixelA, pixelB);
                    DrawBresenhamLine(pixelB, pixelC);
                    DrawBresenhamLine(pixelC, pixelA);
                }
            }
        }
    }
}
