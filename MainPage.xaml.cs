using SharpDX;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace SoftRender.Net
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Device device;
        private Mesh[] meshes;
        Camera camera = new Camera();
        private AssetManager assetManager = new AssetManager();
        private DateTime previousDateTime;

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            WriteableBitmap bmp = new WriteableBitmap(640, 480);

            device = new Device(bmp);
            frontBuffer.Source = bmp;

            meshes = await assetManager.LoadJSONFileAsync("monkey.babylon");
            //var mesh = new Mesh("Cube", 8, 12);
            //mesh.vertices[0] = new vector3(-1, 1, 1);
            //mesh.vertices[1] = new vector3(1, 1, 1);
            //mesh.vertices[2] = new vector3(-1, -1, 1);
            //mesh.vertices[3] = new vector3(1, -1, 1);
            //mesh.vertices[4] = new vector3(-1, 1, -1);
            //mesh.vertices[5] = new vector3(1, 1, -1);
            //mesh.vertices[6] = new vector3(1, -1, -1);
            //mesh.vertices[7] = new vector3(-1, -1, -1);

            //mesh.faces[0] = new face { a = 0, b = 1, c = 2 };
            //mesh.faces[1] = new face { a = 1, b = 2, c = 3 };
            //mesh.faces[2] = new face { a = 1, b = 3, c = 6 };
            //mesh.faces[3] = new face { a = 1, b = 5, c = 6 };
            //mesh.faces[4] = new face { a = 0, b = 1, c = 4 };
            //mesh.faces[5] = new face { a = 1, b = 4, c = 5 };

            //mesh.faces[6] = new face { a = 2, b = 3, c = 7 };
            //mesh.faces[7] = new face { a = 3, b = 6, c = 7 };
            //mesh.faces[8] = new face { a = 0, b = 2, c = 7 };
            //mesh.faces[9] = new face { a = 0, b = 4, c = 7 };
            //mesh.faces[10] = new face { a = 4, b = 5, c = 6 };
            //mesh.faces[11] = new face { a = 4, b = 6, c = 7 };

            camera.Position = new Vector3(0, 0, 10.0f);
            camera.Target = Vector3.Zero;

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        void CompositionTarget_Rendering(object sender, object e)
        {
            var now = DateTime.Now;
            var currentFps = 1000f / (now - previousDateTime).TotalMilliseconds;
            previousDateTime = now;
            fps.Text = string.Format("{0:0.00} fps", currentFps);

            device.Clear(0, 0, 0, 255);

            foreach (var mesh in meshes)
            {
                mesh.Rotation = new Vector3(mesh.Rotation.X + 0.01f, mesh.Rotation.Y + 0.01f, mesh.Rotation.Z);
            }
            device.Render(camera, meshes);
            device.Present();
        }

        public MainPage()
        {
            this.InitializeComponent();
        }
    }
}
