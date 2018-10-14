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
                mesh.Rotation = new Vector3(mesh.Rotation.X, mesh.Rotation.Y + 0.01f, mesh.Rotation.Z);
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
