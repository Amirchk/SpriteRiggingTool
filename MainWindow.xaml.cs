using System;
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

        Vector2? creatingBoneStart = null;
        Line previewLine = null;

        Bone selectedBone = null;
        bool isRotating = false;
		bool paintMode = false;
		float brushRadius = 25f;
		float paintStrength = 0.05f;

        public MainWindow()
        {
            InitializeComponent();
            LoadSprite();
			GenerateMesh(sprite.PixelWidth, sprite.PixelHeight);

        }

        // ---------------- SPRITE ----------------

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

        // ---------------- INPUT ----------------

        void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(EditorCanvas);
            var mouse = new Vector2((float)pos.X, (float)pos.Y);

            // Try select bone first
            selectedBone = FindBoneNearLine(mouse);

            if (selectedBone != null)
            {
                isRotating = true;
                return;
            }

            // Create bone
            Bone parent = FindNearestJoint(mouse);
            creatingBoneStart = parent != null ? parent.WorldEnd : mouse;

            previewLine = new Line
            {
                Stroke = Brushes.Lime,
                StrokeThickness = 2,
                X1 = creatingBoneStart.Value.X,
                Y1 = creatingBoneStart.Value.Y,
                X2 = creatingBoneStart.Value.X,
                Y2 = creatingBoneStart.Value.Y
            };

            EditorCanvas.Children.Add(previewLine);
        }

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(EditorCanvas);
            var mouse = new Vector2((float)pos.X, (float)pos.Y);

            if (isRotating && selectedBone != null)
            {
                RotateBoneToMouse(selectedBone, mouse);
                RedrawBones();
				DrawVertices();
                return;
            }

            if (creatingBoneStart != null && previewLine != null)
            {
                previewLine.X2 = pos.X;
                previewLine.Y2 = pos.Y;
            }
			if (paintMode && selectedBone != null && e.LeftButton == MouseButtonState.Pressed)
			{
				PaintWeights(mouse);
				RedrawBones();
			}
        }

        void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            isRotating = false;

            if (creatingBoneStart == null || previewLine == null)
                return;

            var pos = e.GetPosition(EditorCanvas);
            Vector2 end = new((float)pos.X, (float)pos.Y);

            float length = Vector2.Distance(creatingBoneStart.Value, end);
            float rotation = MathF.Atan2(
                end.Y - creatingBoneStart.Value.Y,
                end.X - creatingBoneStart.Value.X
            );

            Bone parent = FindNearestJoint(creatingBoneStart.Value);

            Bone bone = new()
            {
                WorldStart = creatingBoneStart.Value,
                Length = length,
                LocalRotation = rotation,
                Parent = parent
            };

            if (parent != null)
                parent.Children.Add(bone);

            UpdateBoneWorldTransform(bone);
            skeleton.Bones.Add(bone);

            RedrawBones();

            creatingBoneStart = null;
            previewLine = null;
        }
		void PaintWeights(Vector2 mouse)
		{
			foreach (var vertex in skeleton.Vertices)
			{
				float dist = Vector2.Distance(vertex.Position, mouse);
				if (dist > brushRadius)
					continue;

				float influence = 1f - (dist / brushRadius);
				float weightDelta = influence * paintStrength;

				if (!vertex.Weights.ContainsKey(selectedBone))
					vertex.Weights[selectedBone] = 0;

				vertex.Weights[selectedBone] =
					Math.Clamp(vertex.Weights[selectedBone] + weightDelta, 0f, 1f);

				NormalizeWeights(vertex);
			}
		}
		void NormalizeWeights(Vertex v)
		{
			float total = 0;
			foreach (var w in v.Weights.Values)
				total += w;

			if (total <= 1f) return;

			var keys = new List<Bone>(v.Weights.Keys);
			foreach (var b in keys)
				v.Weights[b] /= total;
		}
		void DrawVertices()
		{
			foreach (var v in skeleton.Vertices)
			{
				float weight = 0;
				if (selectedBone != null && v.Weights.ContainsKey(selectedBone))
					weight = v.Weights[selectedBone];

				byte intensity = (byte)(weight * 255);

				var dot = new Ellipse
				{
					Width = 6,
					Height = 6,
					Fill = new SolidColorBrush(Color.FromRgb(intensity, 0, 0))
				};

				Canvas.SetLeft(dot, v.Position.X - 3);
				Canvas.SetTop(dot, v.Position.Y - 3);
				EditorCanvas.Children.Add(dot);
			}
		}



        // ---------------- ROTATION ----------------

        void RotateBoneToMouse(Bone bone, Vector2 mouse)
        {
            Vector2 start = bone.Parent != null
                ? bone.Parent.WorldEnd
                : bone.WorldStart;

            float worldAngle = MathF.Atan2(mouse.Y - start.Y, mouse.X - start.X);

            float parentRotation = bone.Parent != null
                ? bone.Parent.LocalRotation
                : 0f;

            bone.LocalRotation = worldAngle - parentRotation;

            UpdateBoneWorldTransform(bone);
        }

        // ---------------- BONE MATH ----------------

        void UpdateBoneWorldTransform(Bone bone)
        {
            float rotation = bone.LocalRotation;

            if (bone.Parent != null)
            {
                bone.WorldStart = bone.Parent.WorldEnd;
                rotation += bone.Parent.LocalRotation;
            }

            bone.WorldEnd = bone.WorldStart +
                new Vector2(MathF.Cos(rotation), MathF.Sin(rotation)) * bone.Length;

            foreach (var child in bone.Children)
                UpdateBoneWorldTransform(child);
        }

        // ---------------- SELECTION ----------------

        Bone FindBoneNearLine(Vector2 pos, float threshold = 6f)
        {
            foreach (var bone in skeleton.Bones)
            {
                float dist = DistancePointToLine(
                    pos,
                    bone.WorldStart,
                    bone.WorldEnd
                );

                if (dist < threshold)
                    return bone;
            }
            return null;
        }

        float DistancePointToLine(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab);
            t = Math.Clamp(t, 0, 1);
            Vector2 closest = a + ab * t;
            return Vector2.Distance(p, closest);
        }

        Bone FindNearestJoint(Vector2 pos, float threshold = 15f)
        {
            foreach (var bone in skeleton.Bones)
            {
                if (Vector2.Distance(bone.WorldStart, pos) < threshold)
                    return bone;
                if (Vector2.Distance(bone.WorldEnd, pos) < threshold)
                    return bone;
            }
            return null;
        }
		void GenerateMesh(int width, int height, int spacing = 20)
		{
			skeleton.Vertices.Clear();

			for (int y = 0; y <= height; y += spacing)
			{
				for (int x = 0; x <= width; x += spacing)
				{
					skeleton.Vertices.Add(new Vertex
					{
						Position = new Vector2(400 + x, 200 + y)
					});
        }
    }
}


        // ---------------- DRAWING ----------------

        void RedrawBones()
        {
            while (EditorCanvas.Children.Count > 1)
                EditorCanvas.Children.RemoveAt(1);

            foreach (var bone in skeleton.Bones)
                DrawBoneRecursive(bone);
        }

        void DrawBoneRecursive(Bone bone)
        {
            var line = new Line
            {
                X1 = bone.WorldStart.X,
                Y1 = bone.WorldStart.Y,
                X2 = bone.WorldEnd.X,
                Y2 = bone.WorldEnd.Y,
                Stroke = bone == selectedBone ? Brushes.Yellow : Brushes.Lime,
                StrokeThickness = 2
            };

            EditorCanvas.Children.Add(line);
            DrawJoint(bone.WorldStart);
            DrawJoint(bone.WorldEnd);

            foreach (var child in bone.Children)
                DrawBoneRecursive(child);
        }

        void DrawJoint(Vector2 pos)
        {
            var joint = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill = Brushes.Red
            };

            Canvas.SetLeft(joint, pos.X - 3);
            Canvas.SetTop(joint, pos.Y - 3);
            EditorCanvas.Children.Add(joint);
        }
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.W)
				paintMode = true;
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (e.Key == Key.W)
				paintMode = false;
		}

    }
}
