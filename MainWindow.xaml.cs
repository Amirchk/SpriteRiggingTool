using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SpriteRigEditor.Core;
using SpriteRigEditor.Rendering;

namespace SpriteRigEditor
{
    public partial class MainWindow : Window
    {
        Skeleton skeleton = new();
        Vector2? boneStart = null;
        Line previewLine = null;

        public MainWindow()
        {
            InitializeComponent();
            LoadSprite();
        }

        void LoadSprite()
        {
            var sprite = SpriteLoader.Load("Assets/character.png");

            var image = new Image
            {
                Source = sprite,
                Width = sprite.PixelWidth,
                Height = sprite.PixelHeight
            };

            Canvas.SetLeft(image, 400);
            Canvas.SetTop(image, 200);

            EditorCanvas.Children.Add(image);
        }

        void OnMouseDown(object sender, MouseButtonEventArgs e)
{
    var pos = e.GetPosition(EditorCanvas);
    var mousePos = new Vector2((float)pos.X, (float)pos.Y);

    Bone parent = FindNearestJoint(mousePos);

    if (parent != null)
    {
        boneStart = parent.End; // snap to parent joint
    }
    else
    {
        boneStart = mousePos;
    }

    previewLine = new Line
    {
        Stroke = Brushes.Lime,
        StrokeThickness = 2,
        X1 = boneStart.Value.X,
        Y1 = boneStart.Value.Y,
        X2 = boneStart.Value.X,
        Y2 = boneStart.Value.Y
    };

    EditorCanvas.Children.Add(previewLine);
}

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (boneStart == null || previewLine == null) return;

            var pos = e.GetPosition(EditorCanvas);
            previewLine.X2 = pos.X;
            previewLine.Y2 = pos.Y;
        }

        void OnMouseUp(object sender, MouseButtonEventArgs e)
{
    if (boneStart == null || previewLine == null) return;

    var pos = e.GetPosition(EditorCanvas);
    var end = new Vector2((float)pos.X, (float)pos.Y);

    Bone parent = FindNearestJoint(boneStart.Value);

    var bone = new Bone
    {
        Start = boneStart.Value,
        End = end,
        Parent = parent
    };

    if (parent != null)
        parent.Children.Add(bone);

    skeleton.Bones.Add(bone);

    boneStart = null;
    previewLine = null;
}

	Bone FindNearestJoint(Vector2 pos, float threshold = 15f)
	{
    	foreach (var bone in skeleton.Bones)
    	{
        if (Vector2.Distance(bone.Start, pos) < threshold)
            return bone;
        if (Vector2.Distance(bone.End, pos) < threshold)
            return bone;
    }
    return null;
}

    }
}
