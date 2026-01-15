using System.Collections.Generic;
using System.Numerics;

namespace SpriteRigEditor.Core
{
   public class Vertex
    {
        public Vector2 BindPosition;
        public Vector2 DeformedPosition;
        public Dictionary<Bone, float> Weights = new();
    }
	
}
