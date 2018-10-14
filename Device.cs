using SharpDX;
using System;
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
        private Vector3 lightPos;
        private Color4 MeshColor = new Color4(1f, 1.0f, 1.0f, 1f);
        private Color4 NormalColor = new Color4(0f, 1f, 0f, 1f);

        public Device(WriteableBitmap bmp)
        {
            this.bmp = bmp;
            width = bmp.PixelWidth;
            half_width = width / 2.0f;
            height = bmp.PixelHeight;
            half_height = height / 2.0f;
            backBuffer = new byte[width * height * 4];
            length = backBuffer.Length;
            depthBuffer = new float[width * height];

            lockBuffer = new object[width * height];
            for (int i = 0; i < lockBuffer.Length; i++)
            {
                lockBuffer[i] = new object();
            }

            lightPos = new Vector3(0, 10, 10);
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
                backBuffer[index4] = (byte)(color.Blue * 255f);
                backBuffer[index4 + 1] = (byte)(color.Green * 255f);
                backBuffer[index4 + 2] = (byte)(color.Red * 255f);
                backBuffer[index4 + 3] = (byte)(color.Alpha * 255f);

                depthBuffer[index] = z;
            }
        }

        public Vertex Project(Vertex vert, Matrix transMatrix, Matrix world)
        {
            var point = Vector3.TransformCoordinate(vert.Coordinates, transMatrix);
            var point3DWorld = Vector3.TransformCoordinate(vert.Coordinates, world);
            var normal3DWorld = Vector3.TransformCoordinate(vert.Normal, world);
            //var normal3DWorld = Vector3.TransformCoordinate(vert.Normal, transMatrix);

            var x = point.X * width + half_width;
            var y = -point.Y * height + half_height;

            return new Vertex
            {
                Coordinates = new Vector3(x, y, point.Z),
                Normal = normal3DWorld,
                WorldCoordinates = point3DWorld,
                TextureCoordinates = vert.TextureCoordinates
            };
        }

        public void DrawPoint(int x, int y, float z, Color4 color)
        {
            if (x >= 0 && y >= 0 && x < width && y < height)
            {
                PutPixel(x, y, z, color);
            }
        }

        public void DrawBresenhamLine(Vector3 p0, Vector3 p1)
        {
            int x0 = (int)p0.X;
            int y0 = (int)p0.Y;
            int x1 = (int)p1.X;
            int y1 = (int)p1.Y;

            var dx = Math.Abs(x1 - x0);
            var dy = Math.Abs(y1 - y0);
            //var dz = Math.Abs(p1.Z - p0.Z);
            var sx = (x0 < x1) ? 1 : -1;
            var sy = (y0 < y1) ? 1 : -1;
            var err = dx - dy;

            while (true)
            {
                //DrawPoint(new Vector2(x0, y0));
                DrawPoint(x0, y0, 0, NormalColor);

                if (x0 == x1 && (y0 == y1)) break;
                var e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        public void ProcessScanLine(ScanLineData data, Vertex va, Vertex vb, Vertex vc, Vertex vd, Color4 color, Texture texture)
        {
            var pa = va.Coordinates;
            var pb = vb.Coordinates;
            var pc = vc.Coordinates;
            var pd = vd.Coordinates;

            var gradient1 = pa.Y != pb.Y ? (data.CurrentY - pa.Y) / (pb.Y - pa.Y) : 1;
            var gradient2 = pc.Y != pd.Y ? (data.CurrentY - pc.Y) / (pd.Y - pc.Y) : 1;

            int sx = (int)MathLib.Interpolate(pa.X, pb.X, gradient1);
            int ex = (int)MathLib.Interpolate(pc.X, pd.X, gradient2);

            float z1 = MathLib.Interpolate(pa.Z, pb.Z, gradient1);
            float z2 = MathLib.Interpolate(pc.Z, pd.Z, gradient2);

            var snl = MathLib.Interpolate(data.NDotla, data.NDotlb, gradient1);
            var enl = MathLib.Interpolate(data.NDotlc, data.NDotld, gradient2);

            var su = MathLib.Interpolate(data.ua, data.ub, gradient1);
            var eu = MathLib.Interpolate(data.uc, data.ud, gradient2);
            var sv = MathLib.Interpolate(data.va, data.vb, gradient1);
            var ev = MathLib.Interpolate(data.vc, data.vd, gradient2);

            for (int index = sx; index < ex; index++)
            {
                var grad = (index - sx) / (float)(ex - sx);
                var z = MathLib.Interpolate(z1, z2, grad);
                var ndotl = MathLib.Interpolate(snl, enl, grad);

                var u = MathLib.Interpolate(su, eu, grad);
                var v = MathLib.Interpolate(sv, ev, grad);

                var textureColor = texture?.Map(u, v) ?? new Color4(1, 1, 1, 1);

                DrawPoint(index, data.CurrentY, z, new Color4(
                    color.Red * ndotl * textureColor.Red,
                    color.Green * ndotl * textureColor.Green,
                    color.Blue * ndotl * textureColor.Blue,
                    color.Alpha * textureColor.Alpha));
            }
        }

        public void DrawTriangle(Vertex v0, Vertex v1, Vertex v2, Color4 color, Texture texture)
        {
            if (v0.Coordinates.Y > v1.Coordinates.Y)
            {
                var temp = v1;
                v1 = v0;
                v0 = temp;
            }

            if (v1.Coordinates.Y > v2.Coordinates.Y)
            {
                var temp = v1;
                v1 = v2;
                v2 = temp;
            }

            if (v0.Coordinates.Y > v1.Coordinates.Y)
            {
                var temp = v1;
                v1 = v0;
                v0 = temp;
            }

            var p0 = v0.Coordinates;
            var p1 = v1.Coordinates;
            var p2 = v2.Coordinates;

            //var vnFace = (v0.Normal + v1.Normal + v2.Normal) / 3f;
            //var centerPoint = (v0.WorldCoordinates + v1.WorldCoordinates + v2.WorldCoordinates) / 3f;

            //var ndotl = MathLib.ComputeNDotL(centerPoint, vnFace, lightPos);
            //var data = new ScanLineData{NDotla = ndotl};

            //var cent = (v0.Coordinates + v1.Coordinates + v2.Coordinates) / 3f;
            //DrawBresenhamLine(cent, cent + vnFace * 10);
            float nl0 = MathLib.ComputeNDotL(v0.WorldCoordinates, v0.Normal, lightPos);
            float nl1 = MathLib.ComputeNDotL(v1.WorldCoordinates, v1.Normal, lightPos);
            float nl2 = MathLib.ComputeNDotL(v2.WorldCoordinates, v2.Normal, lightPos);

            var data = new ScanLineData();

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
                    data.CurrentY = y;
                    if (y < p1.Y)
                    {
                        data.NDotla = nl0;
                        data.NDotlb = nl2;
                        data.NDotlc = nl0;
                        data.NDotld = nl1;

                        data.ua = v0.TextureCoordinates.X;
                        data.ub = v2.TextureCoordinates.X;
                        data.uc = v0.TextureCoordinates.X;
                        data.ud = v1.TextureCoordinates.X;

                        data.va = v0.TextureCoordinates.Y;
                        data.vb = v2.TextureCoordinates.Y;
                        data.vc = v0.TextureCoordinates.Y;
                        data.vd = v1.TextureCoordinates.Y;

                        ProcessScanLine(data, v0, v2, v0, v1, color, texture);
                    }
                    else
                    {
                        data.NDotla = nl0;
                        data.NDotlb = nl2;
                        data.NDotlc = nl1;
                        data.NDotld = nl2;

                        data.ua = v0.TextureCoordinates.X;
                        data.ub = v2.TextureCoordinates.X;
                        data.uc = v1.TextureCoordinates.X;
                        data.ud = v2.TextureCoordinates.X;

                        data.va = v0.TextureCoordinates.Y;
                        data.vb = v2.TextureCoordinates.Y;
                        data.vc = v1.TextureCoordinates.Y;
                        data.vd = v2.TextureCoordinates.Y;

                        ProcessScanLine(data, v0, v2, v1, v2, color, texture);
                    }
                }
            }
            else
            {
                for (var y = (int)p0.Y; y <= (int)p2.Y; y++)
                {
                    data.CurrentY = y;
                    if (y < p1.Y)
                    {
                        data.NDotla = nl0;
                        data.NDotlb = nl1;
                        data.NDotlc = nl0;
                        data.NDotld = nl2;

                        data.ua = v0.TextureCoordinates.X;
                        data.ub = v1.TextureCoordinates.X;
                        data.uc = v0.TextureCoordinates.X;
                        data.ud = v2.TextureCoordinates.X;

                        data.va = v0.TextureCoordinates.Y;
                        data.vb = v1.TextureCoordinates.Y;
                        data.vc = v0.TextureCoordinates.Y;
                        data.vd = v2.TextureCoordinates.Y;

                        ProcessScanLine(data, v0, v1, v0, v2, color, texture);
                    }
                    else
                    {
                        data.NDotla = nl1;
                        data.NDotlb = nl2;
                        data.NDotlc = nl0;
                        data.NDotld = nl2;

                        data.ua = v1.TextureCoordinates.X;
                        data.ub = v2.TextureCoordinates.X;
                        data.uc = v0.TextureCoordinates.X;
                        data.ud = v2.TextureCoordinates.X;

                        data.va = v1.TextureCoordinates.Y;
                        data.vb = v2.TextureCoordinates.Y;
                        data.vc = v0.TextureCoordinates.Y;
                        data.vd = v2.TextureCoordinates.Y;

                        ProcessScanLine(data, v1, v2, v0, v2, color, texture);
                    }
                }
            }
        }

        public void Render(Camera camera, params Mesh[] meshes)
        {
            var viewMatrix = Matrix.LookAtLH(camera.Position, camera.Target, Vector3.UnitY);
            var projectionMatrix =
                Matrix.PerspectiveFovLH(0.78f, (float)bmp.PixelWidth / bmp.PixelHeight, 0.01f, 1.0f);

            //var faceIndex = 0;
            foreach (var mesh in meshes)
            {
                var worldMatrix = Matrix.RotationYawPitchRoll(mesh.Rotation.Y, mesh.Rotation.X, mesh.Rotation.Z) *
                                  Matrix.Translation(mesh.Position);

                var worldView = worldMatrix * viewMatrix;
                var transformMatrix = worldView * projectionMatrix;

                Parallel.For(0, mesh.Faces.Length, faceIndex =>
                {
                    //foreach (var face in mesh.Faces)
                    {
                        var face = mesh.Faces[faceIndex];

                        var transformedNormal = Vector3.TransformNormal(face.Normal, worldView);

                        if (transformedNormal.Z >= 0)
                        {
                            return;
                        }

                        var vertexA = mesh.Vertices[face.A];
                        var vertexB = mesh.Vertices[face.B];
                        var vertexC = mesh.Vertices[face.C];

                        var pixelA = Project(vertexA, transformMatrix, worldMatrix);
                        var pixelB = Project(vertexB, transformMatrix, worldMatrix);
                        var pixelC = Project(vertexC, transformMatrix, worldMatrix);

                        DrawTriangle(pixelA, pixelB, pixelC, MeshColor, mesh.Texture);
                        faceIndex++;
                    }
                });


            }
        }
    }
}
