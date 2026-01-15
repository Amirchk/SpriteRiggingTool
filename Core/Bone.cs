using System.Collections.Generic;
using System.Numerics;

namespace SpriteRigEditor.Core
{
    public class Bone
    {
        public Vector2 WorldStart;
        public float Length;
        public float LocalRotation;

        public Bone? Parent;
        public List<Bone> Children = new();

        public Vector2 WorldEnd;
		
		public Matrix3x2 GetWorldMatrix()
		{
			return Matrix3x2.CreateRotation(LocalRotation) *
				   Matrix3x2.CreateTranslation(WorldStart);
		}
    }
	
}
